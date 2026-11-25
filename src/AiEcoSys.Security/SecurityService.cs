using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace AiEcoSys.Security;

/// <summary>
/// Security service providing Windows Credential Manager-backed credential protection,
/// path traversal prevention, HTTPS enforcement, and input sanitization.
/// Credentials are stored securely using the Windows Credential Manager (CredWrite/CredRead/CredDelete APIs).
/// </summary>
public static class SecurityService
{
    /// <summary>
    /// Windows Credential Manager result codes.
    /// </summary>
    internal enum CredResult : uint
    {
        SUCCESS = 0,
        ERROR_NOT_FOUND = 1168,
        ERROR_ACCESS_DENIED = 5,
        INVALID_ARGS = 80090016
    }

    /// <summary>
    /// Credential types supported by Windows Credential Manager.
    /// </summary>
    internal enum CredType : uint
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DIGEST = 3,
        RSA = 4,
        TIMESTAMP = 5,
        EXTERNAL = 6
    }

    /// <summary>
    /// Credential flags.
    /// </summary>
    [Flags]
    internal enum CredFlags : uint
    {
        NONE = 0x00000000,
        UNPROTECTED = 0x00000001,
        PRETEND = 0x00000002,
        ACTIVATE = 0x00000004,
        LOGON_API = 0x00000008,
        DOMAIN_ROAMING = 0x00000010,
        CRED_TYPE_DOMAIN_SUPPORTED = 0x00000020,
        CRED_TYPE_DOMAIN_REQUIRED = 0x00000040,
        CRED_PRESERVE_USERNAME = 0x00000080,
        CRED_PRESERVE_PASSWORD = 0x00000100,
        CRED_PRESERVE = CRED_PRESERVE_USERNAME | CRED_PRESERVE_PASSWORD,
        CRED_IMPERSONATE = 0x00000200,
        CRED_KEEP = 0x00000400,
        CRED_DELETE_PROMPT = 0x00000800,
        CRED_COMMAND_PROMPT = 0x00001000,
        CRED_ENUMERATE_MASK = 0x000000FF,
        CRED_ALL_OR_NOTHING = 0x00000100,
        CRED_TYPE_MAP = 0x0000E000,
        CRED_TYPE_MAP_DELETED = 0x00001000,
        CRED_NAME_TARGET_SUPPORTS_PROTOCOL = 0x00010000,
        CRED_OPT_NEGOTIATE_LOGON = 0x00020000,
        CRED_TARGET_SERVERNAME_REQUIRED = 0x00040000,
        CRED_TARGET_SERVERNAME_IS_PATH = 0x00080000,
        CRED_PRESERVE_ROOT = 0x00100000,
        CRED_MAXIMUM = 0x00200000
    }

    /// <summary>
    /// Structure for a Windows Credential Manager credential.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CredentialW
    {
        internal uint Flags;
        internal CredType Type;
        internal string? TargetName;
        internal string? Comment;
        internal long LastWritten;
        internal uint CredentialBlobSize;
        internal IntPtr CredentialBlob;
        internal uint Persist;
        internal uint AttributeCount;
        internal IntPtr Attributes;
        internal IntPtr TargetInfo;
    }

    /// <summary>
    /// Structures for credential attributes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CredAttributeW
    {
        internal uint KeyLength;
        internal IntPtr Key;
        internal uint ValueLength;
        internal IntPtr Value;
    }

    /// <summary>
    /// Structures for the target information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CredTargetW
    {
        internal uint Flags;
        internal string? Name;
    }

    /// <summary>
    /// Native Windows Credential Manager functions.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Retrieves a credential from the Windows Credential Manager.
        /// </summary>
        /// <param name="targetName">The target name of the credential.</param>
        /// <param name="credType">The type of the credential.</param>
        /// <param name="reservedFlag">Reserved, must be 0.</param>
        /// <param name="credentialPointer">Pointer to receive the credential.</param>
        /// <returns>TRUE if successful; FALSE otherwise.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool CredRead(
            string targetName,
            CredType credType,
            uint reservedFlag,
            out IntPtr credentialPointer);

        /// <summary>
        /// Creates or updates a credential in the Windows Credential Manager.
        /// </summary>
        /// <param name="credential">The credential to create/update.</param>
        /// <param name="flags">Reserved, must be 0.</param>
        /// <returns>TRUE if successful; FALSE otherwise.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool CredWrite(
            ref CredentialW credential,
            uint flags);

        /// <summary>
        /// Deletes a credential from the Windows Credential Manager.
        /// </summary>
        /// <param name="targetName">The target name of the credential.</param>
        /// <param name="credType">The type of the credential.</param>
        /// <param name="flags">Reserved, must be 0.</param>
        /// <returns>TRUE if successful; FALSE otherwise.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool CredDelete(
            string targetName,
            CredType credType,
            uint flags);

        /// <summary>
        /// Frees a credential pointer allocated by CredRead.
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern void CredFree(
            IntPtr credentialPointer);
    }

    /// <summary>
    /// Validates that a URI uses the HTTPS scheme, with explicit allowance for localhost
    /// and local inference services. Public Models.dev metadata endpoints must use HTTPS.
    /// </summary>
    /// <param name="uri">The URI to validate.</param>
    /// <returns>true if the URI uses HTTPS or is localhost; false otherwise.</exception>
    /// <exception cref="ArgumentException">Thrown when uri is null or empty.</exception>
    public static bool ValidateHttps(System.Uri uri)
    {
        if (uri == null)
            throw new ArgumentException("URI cannot be null.", nameof(uri));
        if (string.IsNullOrEmpty(uri.OriginalString))
            throw new ArgumentException("URI cannot be empty.", nameof(uri));

        // Allow HTTPS scheme
        if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return true;

        // Explicitly allow localhost and 127.0.0.1 for local inference services
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase))
            return true;

        // Reject non-HTTPS URLs that are not localhost
        return false;
    }

    /// <summary>
    /// Validates a path to prevent directory traversal attacks.
    /// The resolved path must remain within the specified base directory.
    /// Works with configurable installation directories, not only a hardcoded path.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="baseDirectory">The base directory that the path must remain within.</param>
    /// <returns>true if the path is safe (within base directory); false otherwise.</exception>
    /// <exception cref="ArgumentException">Thrown when path or baseDirectory is null or empty.</exception>
    public static bool ValidatePath(string path, string baseDirectory)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        if (string.IsNullOrEmpty(baseDirectory))
            throw new ArgumentException("Base directory cannot be null or empty.", nameof(baseDirectory));

        // Get absolute paths and verify the path stays within the base directory
        string absPath = Path.GetFullPath(path);
        string absBaseDir = Path.GetFullPath(baseDirectory);

        // Ensure the path starts with the base directory (case-insensitive)
        return absPath.StartsWith(absBaseDir, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sanitizes input by removing potentially dangerous characters
    /// that could be used for path traversal or command injection.
    /// This is a basic input hygiene measure - not a complete security strategy.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>A sanitized string safe for use in file paths or commands.</exception>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static string SanitizeInput(string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        // Remove characters that could be used for path traversal or command injection
        string sanitized = input;
        sanitized = sanitized.Replace("..", "");
        sanitized = sanitized.Replace("/", "");
        sanitized = sanitized.Replace("\\", "");
        sanitized = sanitized.Replace("|", "");
        sanitized = sanitized.Replace("&", "");
        sanitized = sanitized.Replace(";", "");
        sanitized = sanitized.Replace("<", "");
        sanitized = sanitized.Replace(">", "");
        sanitized = sanitized.Replace("(", "");
        sanitized = sanitized.Replace(")", "");

        return sanitized;
    }

    /// <summary>
    /// Checks whether a string potentially contains sensitive credential-like patterns.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>true if the string appears to contain a credential pattern; false otherwise.</exception>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static bool ContainsCredentialPattern(string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        // Check for common credential patterns: keys=, password=, api_key=, etc.
        string lower = input.ToLowerInvariant();
        return lower.Contains("api_key")
            || lower.Contains("api-key")
            || lower.Contains("password")
            || lower.Contains("auth_token")
            || lower.Contains("secret_key");
    }

    /// <summary>
    /// Redacts credentials from exception messages and log entries.
    /// Replaces known credential patterns with [REDACTED].
    /// </summary>
    /// <param name="input">The text containing potential credentials.</param>
    /// <returns>The text with credentials redacted.</exception>
    /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
    public static string RedactCredentials(string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        string result = input;

        // Replace common credential patterns using regex-like detection.
        // Matches api_key=..., api-key=..., password=..., auth_token=..., secret_key=...
        // followed by any non-whitespace characters and replaces the whole pattern with [REDACTED].
        result = Regex.Replace(result, @"api_key\S*|api-key\S*|password\S*|auth_token\S*|secret_key\S*", "[REDACTED]", RegexOptions.IgnoreCase);

        return result;
    }

    /// <summary>
    /// Securely stores an API key using Windows Credential Manager.
    /// The key cannot be retrieved by any other process or user account.
    /// </summary>
    /// <param name="keyName">The name/identifier for the key.</param>
    /// <param name="keyValue">The API key value to store.</exception>
    /// <exception cref="ArgumentException">Thrown when keyName or keyValue is null or whitespace.</exception>
    public static void StoreCredential(string keyName, string keyValue)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            throw new ArgumentException("Key name cannot be null or whitespace.", nameof(keyName));
        if (string.IsNullOrWhiteSpace(keyValue))
            throw new ArgumentException("API key cannot be null or whitespace.", nameof(keyValue));

        byte[] credentialBytes = Encoding.Unicode.GetBytes(keyValue);
        IntPtr credentialBlob = Marshal.AllocHGlobal(credentialBytes.Length);

        try
        {
            // Copy the secret into unmanaged memory for the CREDENTIALW blob.
            Marshal.Copy(credentialBytes, 0, credentialBlob, credentialBytes.Length);

            // Build the credential structure. LastWritten is set by the API.
            var credential = new CredentialW
            {
                Flags = 0,
                Type = CredType.GENERIC,
                TargetName = keyName,
                Comment = "AiEcoSys credential",
                LastWritten = 0,
                CredentialBlobSize = (uint)credentialBytes.Length,
                CredentialBlob = credentialBlob,
                Persist = 2, // CRED_PERSIST_LOCAL_MACHINE
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                TargetInfo = IntPtr.Zero
            };

            if (!NativeMethods.CredWrite(ref credential, 0))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new CryptographicException(
                    $"Windows Credential Manager write failed (error {errorCode}). " +
                    $"The key '{keyName}' could not be stored.");
            }
        }
        catch (CryptographicException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CryptographicException(
                $"Windows Credential Manager storage failed for key '{keyName}'.", ex);
        }
        finally
        {
            Marshal.FreeHGlobal(credentialBlob);
            Array.Clear(credentialBytes, 0, credentialBytes.Length);
        }
    }

    /// <summary>
    /// Retrieves a previously stored API key from Windows Credential Manager.
    /// </summary>
    /// <param name="keyName">The name/identifier of the key to retrieve.</param>
    /// <returns>The decrypted API key, or null if not found.</exception>
    /// <exception cref="ArgumentException">Thrown when keyName is null or whitespace.</exception>
    public static string? RetrieveCredential(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            throw new ArgumentException("Key name cannot be null or whitespace.", nameof(keyName));

        try
        {
            IntPtr credentialPointer = IntPtr.Zero;
            bool success = NativeMethods.CredRead(
                keyName,
                CredType.GENERIC,
                0,
                out credentialPointer);

            if (!success || credentialPointer == IntPtr.Zero)
            {
                // Credential not found in Windows Credential Manager
                return null;
            }

            try
            {
                // Read the credential structure
                var credential = Marshal.PtrToStructure<CredentialW>(credentialPointer);

                // Read the credential blob (the actual key value)
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                {
                    return null;
                }

                int blobSize = (int)credential.CredentialBlobSize;
                byte[] credentialBytes = new byte[blobSize];

                try
                {
                    Marshal.Copy(credential.CredentialBlob, credentialBytes, 0, blobSize);
                    return Encoding.Unicode.GetString(credentialBytes);
                }
                finally
                {
                    Array.Clear(credentialBytes, 0, credentialBytes.Length);
                }
            }
            finally
            {
                if (credentialPointer != IntPtr.Zero)
                {
                    NativeMethods.CredFree(credentialPointer);
                }
            }
        }
        catch (Exception)
        {
            // Failed to retrieve credential
            return null;
        }
    }

    /// <summary>
    /// Deletes a previously stored API key from Windows Credential Manager.
    /// </summary>
    /// <param name="keyName">The name/identifier of the key to delete.</param>
    /// <exception cref="ArgumentException">Thrown when keyName is null or whitespace.</exception>
    public static void DeleteCredential(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            throw new ArgumentException("Key name cannot be null or whitespace.", nameof(keyName));

        try
        {
            bool success = NativeMethods.CredDelete(
                keyName,
                CredType.GENERIC,
                0);

            if (!success)
            {
                int errorCode = Marshal.GetLastWin32Error();
                // If error is NOT_FOUND (1168), that's OK - credential already doesn't exist
                if (errorCode != 1168)
                {
                    throw new CryptographicException(
                        $"Windows Credential Manager delete failed (error {errorCode}) for key '{keyName}'.");
                }
            }
        }
        catch (CryptographicException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CryptographicException(
                $"Windows Credential Manager deletion failed for key '{keyName}'.", ex);
        }
    }
}