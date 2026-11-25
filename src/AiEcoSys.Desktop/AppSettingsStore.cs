using System;
using System.IO;
using System.Text.Json;

namespace AiEcoSys.Desktop;

/// <summary>
/// Persisted application settings. All paths must be absolute and must not be on
/// the C: drive; the default locations are resolved by <see cref="AppPaths"/>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Gets or sets the preferred data directory.</summary>
    public string DataLocation { get; set; } = AppPaths.DataDirectory;

    /// <summary>Gets or sets the directory used for catalogue snapshots.</summary>
    public string SnapshotLocation { get; set; } = AppPaths.DefaultSnapshotDirectory;

    /// <summary>Gets or sets a value indicating whether anonymous telemetry is enabled.</summary>
    public bool EnableTelemetry { get; set; }

    /// <summary>Gets or sets a value indicating whether online API calls are disabled.</summary>
    public bool OfflineMode { get; set; }

    /// <summary>Gets or sets a value indicating whether the catalogue is refreshed on launch.</summary>
    public bool AutoSyncOnLaunch { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the AcademyOS integration is enabled.</summary>
    public bool AcademyOSEnabled { get; set; }

    /// <summary>Gets or sets the optional AcademyOS endpoint.</summary>
    public string? AcademyOSEndpoint { get; set; }

    /// <summary>Gets or sets a value indicating whether the UBOS integration is enabled.</summary>
    public bool UBOSEnabled { get; set; }

    /// <summary>Gets or sets the optional UBOS endpoint.</summary>
    public string? UBOSEndpoint { get; set; }

    /// <summary>Gets or sets a value indicating whether the MavenMeeM integration is enabled.</summary>
    public bool MavenMeeMEnabled { get; set; }

    /// <summary>Gets or sets the optional MavenMeeM endpoint.</summary>
    public string? MavenMeeMEndpoint { get; set; }
}

/// <summary>
/// Loads and saves <see cref="AppSettings"/> beneath the resolved data directory.
/// </summary>
public static class AppSettingsStore
{
    /// <summary>The directory containing the settings file.</summary>
    public static string SettingsDirectory => AppPaths.SettingsDirectory;

    /// <summary>The full path of the settings file.</summary>
    public static string SettingsPath => AppPaths.SettingsPath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Loads settings from disk, returning defaults when the file is absent or invalid.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            string json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
            Normalize(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Validates settings and saves them to disk.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <param name="message">A human-readable result message.</param>
    /// <returns><c>true</c> when saved; otherwise <c>false</c>.</returns>
    public static bool TrySave(AppSettings settings, out string message)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        if (!TryValidateDataLocation(settings.DataLocation, out message))
            return false;

        if (!TryValidateSnapshotLocation(settings.SnapshotLocation, settings.DataLocation, out message))
            return false;

        if (!TryValidateEndpoint(settings.AcademyOSEndpoint, out message))
            return false;

        if (!TryValidateEndpoint(settings.UBOSEndpoint, out message))
            return false;

        if (!TryValidateEndpoint(settings.MavenMeeMEndpoint, out message))
            return false;

        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
            message = $"Settings saved to {SettingsPath}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Could not save settings: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Validates a data directory: it must be absolute, on a non-C: drive, and
    /// existing or creatable.
    /// </summary>
    public static bool TryValidateDataLocation(string path, out string message)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            message = "Data location cannot be empty.";
            return false;
        }

        if (!Path.IsPathRooted(path))
        {
            message = "Data location must be an absolute path.";
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            message = $"Data location is not a valid path: {ex.Message}";
            return false;
        }

        if (IsOnForbiddenDrive(full))
        {
            message = "Data location must not be on the C: drive.";
            return false;
        }

        try
        {
            Directory.CreateDirectory(full);
        }
        catch (Exception ex)
        {
            message = $"Data location cannot be created: {ex.Message}";
            return false;
        }

        message = "Data location is valid.";
        return true;
    }

    /// <summary>
    /// Validates a snapshot directory against the current data directory.
    /// </summary>
    public static bool TryValidateSnapshotLocation(string path, out string message) =>
        TryValidateSnapshotLocation(path, AppPaths.DataDirectory, out message);

    /// <summary>
    /// Validates a snapshot directory: absolute, not on C:, and inside the
    /// configured data directory so that the registry's path-traversal guard
    /// accepts it.
    /// </summary>
    public static bool TryValidateSnapshotLocation(string path, string dataDirectory, out string message)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            message = "Snapshot location cannot be empty.";
            return false;
        }

        if (!Path.IsPathRooted(path))
        {
            message = "Snapshot location must be an absolute path.";
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            message = $"Snapshot location is not a valid path: {ex.Message}";
            return false;
        }

        if (IsOnForbiddenDrive(full))
        {
            message = "Snapshot location must not be on the C: drive.";
            return false;
        }

        if (!AppPaths.TryNormalizeNonC(dataDirectory, out string dataRoot, out string dataError))
        {
            message = $"Data location is not usable: {dataError}.";
            return false;
        }

        if (!IsPathInside(full, dataRoot))
        {
            message = $"Snapshot location must be inside {dataRoot}.";
            return false;
        }

        try
        {
            Directory.CreateDirectory(full);
        }
        catch (Exception ex)
        {
            message = $"Snapshot location cannot be created: {ex.Message}";
            return false;
        }

        message = "Snapshot location is valid.";
        return true;
    }

    /// <summary>
    /// Repairs settings loaded from an older release or from a copy whose data
    /// directory moved. Invalid paths are replaced with portable-safe defaults.
    /// </summary>
    private static void Normalize(AppSettings settings)
    {
        if (!TryValidateDataLocation(settings.DataLocation, out _))
            settings.DataLocation = AppPaths.DataDirectory;

        if (!TryValidateSnapshotLocation(settings.SnapshotLocation, settings.DataLocation, out _))
            settings.SnapshotLocation = Path.Combine(settings.DataLocation, "snapshots");
    }

    private static bool IsPathInside(string path, string root)
    {
        string normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates an optional integration endpoint. Empty values are accepted;
    /// otherwise the endpoint must be an absolute HTTPS (or localhost) URI.
    /// </summary>
    public static bool TryValidateEndpoint(string? endpoint, out string message)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            message = "No endpoint configured.";
            return true;
        }

        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeFile))
        {
            message = "Endpoint must be an absolute URI.";
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && !IsLocalHost(uri))
        {
            message = "Remote endpoints must use HTTPS.";
            return false;
        }

        message = "Endpoint is valid.";
        return true;
    }

    private static bool IsLocalHost(Uri uri)
    {
        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOnForbiddenDrive(string fullPath)
    {
        string root = Path.GetPathRoot(fullPath) ?? string.Empty;
        return root.StartsWith("C:", StringComparison.OrdinalIgnoreCase);
    }
}
