using AiEcoSys.Core;
using Xunit;

/// <summary>
/// Tests for catalogue JSON parsing and validation via LoadCatalogueFromJson.
/// </summary>
public class TestCatalogueParsing
{
    private readonly CentralRegistry _registry = new();

    /// <summary>
    /// Valid catalogue with providers and models loads successfully.
    /// </summary>
    [Fact]
    public void LoadCatalogueFromJson_ValidCatalogue_ReturnsTrue()
    {
        // Arrange: A minimal valid JSON with providers and models
        var json = @"
        {
            ""providers"": {
                ""OpenAI"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""Modality"": ""text"",
                    ""ContextWindow"": 8192,
                    ""InputTokenPrice"": 0.01,
                    ""OutputTokenPrice"": 0.03,
                    ""ToolCalling"": true
                }
            }
        }";

        // Act
        var result = _registry.LoadCatalogueFromJson(json, "test-source");

        // Assert
        Assert.True(result);
        Assert.Equal(CatalogueStatus.Online, _registry.Status.Status);
        Assert.Equal(1, _registry.Providers.Count);
        Assert.Equal(1, _registry.Models.Count);
        Assert.Equal("OpenAI", _registry.GetProvider("openai")?.Name);
        Assert.Equal("GPT-4", _registry.GetModel("gpt-4")?.Name);
    }

    /// <summary>
    /// Empty JSON returns false.
    /// </summary>
    [Fact]
    public void LoadCatalogueFromJson_EmptyJson_ReturnsFalse()
    {
        // Arrange
        var json = "";

        // Act
        var result = _registry.LoadCatalogueFromJson(json, "test-source");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// JSON missing providers and models returns false.
    /// </summary>
    [Fact]
    public void LoadCatalogueFromJson_MissingStructure_ReturnsFalse()
    {
        // Arrange
        var json = "{ \"other\": \"data\" }";

        // Act
        var result = _registry.LoadCatalogueFromJson(json, "test-source");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// JSON with providers but no models still valid (providers OR models required).
    /// </summary>
    [Fact]
    public void LoadCatalogueFromJson_ProvidersOnly_StillValid()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""Anthropic"": {
                    ""Id"": ""anthropic"",
                    ""Name"": ""Anthropic""
                }
            }
        }";

        // Act
        var result = _registry.LoadCatalogueFromJson(json, "test-source");

        // Assert
        Assert.True(result);
        Assert.Equal(1, _registry.Providers.Count);
        Assert.Equal(0, _registry.Models.Count);
    }

    /// <summary>
    /// Provider deserialization works correctly.
    /// </summary>
    [Fact]
    public void GetProvider_Found_ReturnsProvider()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI"",
                    ""Description"": ""GPT provider""
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var provider = _registry.GetProvider("openai");

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("OpenAI", provider?.Name);
        Assert.Equal("openai", provider?.Id);
    }

    /// <summary>
    /// GetProvider case-insensitive.
    /// </summary>
    [Fact]
    public void GetProvider_CaseInsensitive_ReturnsProvider()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var provider = _registry.GetProvider("OPENAI");

        // Assert
        Assert.NotNull(provider);
    }

    /// <summary>
    /// GetProvider not found returns null.
    /// </summary>
    [Fact]
    public void GetProvider_NotFound_ReturnsNull()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var provider = _registry.GetProvider("nonexistent");

        // Assert
        Assert.Null(provider);
    }

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
}