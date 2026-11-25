using AiEcoSys.Core;
using AiEcoSys.Security;
using Xunit;

/// <summary>
/// Security tests for the AiEcoSys application.
/// </summary>
public class TestSecurity
{
    /// <summary>
    /// Security test: invalid path (directory traversal) rejected by RefreshCatalogueFromSnapshot.
    /// </summary>
    [Fact]
    public void Security_InvalidPath_DirectoryTraversalRejected()
    {
        // Arrange: path attempting directory traversal
        string traversalPath = "D:/AiEcoSys/../../Windows/System32/config";
        var registry = new AiEcoSys.Core.CentralRegistry();

        // Act: attempt to load snapshot from traversal path
        var result = registry.RefreshCatalogueFromSnapshot(traversalPath);

        // Assert: path traversal should be rejected
        Assert.Equal(SyncResult.Failed, result);
    }

    /// <summary>
    /// Security test: malformed/non-HTTPS URL rejected by RefreshCatalogueAsync.
    /// </summary>
    [Fact]
    public async Task Security_MalformedUrl_NonHttpsRejected()
    {
        // Arrange: non-HTTPS URL
        var registry = new AiEcoSys.Core.CentralRegistry();

        // Act: attempt refresh from non-HTTPS URL
        var result = await registry.RefreshCatalogueAsync("http://example.com/api.json", 30);

        // Assert: non-HTTPS should be rejected
        Assert.Equal(SyncResult.Failed, result);
    }

    /// <summary>
    /// Security test: localhost URL accepted by RefreshCatalogueAsync.
    /// </summary>
    [Fact]
    public async Task Security_LocalhostUrl_Accepted()
    {
        // Arrange: localhost URL
        var registry = new AiEcoSys.Core.CentralRegistry();

        // Act: attempt refresh from localhost URL
        var result = await registry.RefreshCatalogueAsync("http://localhost:5000/api.json", 30);

        // Assert: localhost should be accepted (not rejected for non-HTTPS)
        // Note: This test verifies the explicit localhost handling
        Assert.Equal(SyncResult.Synchronising, result);
    }

    /// <summary>
    /// Security test: credential pattern detection works correctly.
    /// </summary>
    [Fact]
    public void Security_CredentialPattern_DetectionWorks()
    {
        // Arrange
        var testStrings = new[]
        {
            "api_key=secret123",
            "password=mysecretpassword",
            "auth_token=tokendef456",
            "this is normal text with no credentials",
            "Api_Key=anotherexample"
        };

        // Act & Assert
        foreach (var testString in testStrings)
        {
            bool detected = AiEcoSys.Security.SecurityService.ContainsCredentialPattern(testString);
            // Known credential patterns should be detected (case-insensitive check)
            bool hasCredentialPattern = testString.ToLowerInvariant().Contains("api_key")
                || testString.ToLowerInvariant().Contains("password")
                || testString.ToLowerInvariant().Contains("auth_token");
            if (hasCredentialPattern)
            {
                Assert.True(detected, $"Expected credential pattern to be detected in: {testString}");
            }
            // Normal text should not be flagged
            else
            {
                Assert.False(detected, $"Unexpected credential pattern detection in: {testString}");
            }
        }
    }

    /// <summary>
    /// Security test: credentials redacted from log messages.
    /// </summary>
    [Fact]
    public void Security_LogRedaction_CredentialsRedacted()
    {
        // Arrange
        string logMessage = "Failed to load catalogue from http://example.com/api_key=secret123";
        var redacted = AiEcoSys.Security.SecurityService.RedactCredentials(logMessage);

        // Assert: credentials should be redacted
        Assert.DoesNotContain("api_key=secret123", redacted);
        Assert.DoesNotContain("secret123", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    /// <summary>
    /// Security test: path traversal protection with configurable base directory.
    /// </summary>
    [Fact]
    public void Security_PathTraversal_ConfigurableBaseDirectory()
    {
        // Arrange: path outside the allowed base directory
        var registry = new AiEcoSys.Core.CentralRegistry();
        string outsidePath = "C:/windows/system32/config";

        // Act: attempt to load snapshot from outside base directory
        var result = registry.RefreshCatalogueFromSnapshot(outsidePath);

        // Assert: should be rejected
        Assert.Equal(SyncResult.Failed, result);
    }

    /// <summary>
    /// Security test: safe path within base directory accepted.
    /// </summary>
    [Fact]
    public async Task Security_PathSafe_WithinBaseDirectoryAccepted()
    {
        // Arrange: path within the allowed base directory
        var registry = new AiEcoSys.Core.CentralRegistry();
        string safePath = "D:/AiEcoSys/tests/test-snapshot.json";

        // Act: attempt to load snapshot from within base directory
        // First create a valid snapshot file
        var json = @"{
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            }
        }";
        File.WriteAllText(safePath, json);

        try
        {
            // Act: attempt to load snapshot
            var result = registry.RefreshCatalogueFromSnapshot(safePath);

            // Assert: should succeed
            Assert.Equal(SyncResult.Success, result);
            Assert.Equal(1, registry.Providers.Count);
        }
        finally
        {
            // Cleanup: remove test file
            if (File.Exists(safePath))
                File.Delete(safePath);
        }
    }
}