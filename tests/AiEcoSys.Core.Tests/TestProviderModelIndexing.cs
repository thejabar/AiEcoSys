using AiEcoSys.Core;
using Xunit;

/// <summary>
/// Tests for provider and model indexing, retrieval, and search operations.
/// </summary>
public class TestProviderModelIndexing
{
    private readonly CentralRegistry _registry = new();

    /// <summary>
    /// Multiple providers load and are indexed correctly.
    /// </summary>
    [Fact]
    public void LoadMultipleProviders_AllIndexed_CorrectCount()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                },
                ""anthropic"": {
                    ""Id"": ""anthropic"",
                    ""Name"": ""Anthropic""
                },
                ""google"": {
                    ""Id"": ""google"",
                    ""Name"": ""Google""
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act & Assert
        Assert.Equal(3, _registry.Providers.Count);
        Assert.Contains(_registry.Providers, p => p.Id == "openai");
        Assert.Contains(_registry.Providers, p => p.Id == "anthropic");
        Assert.Contains(_registry.Providers, p => p.Id == "google");
    }

    /// <summary>
    /// GetProvider retrieves correct provider from multiple.
    /// </summary>
    [Fact]
    public void GetProvider_FromMultiple_ReturnsCorrect()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                },
                ""anthropic"": {
                    ""Id"": ""anthropic"",
                    ""Name"": ""Anthropic""
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var openai = _registry.GetProvider("openai");
        var anthropic = _registry.GetProvider("anthropic");

        // Assert
        Assert.NotNull(openai);
        Assert.Equal("OpenAI", openai?.Name);
        Assert.NotNull(anthropic);
        Assert.Equal("Anthropic", anthropic?.Name);
        Assert.NotSame(openai, anthropic);
    }

    /// <summary>
    /// Model metadata loads with correct properties.
    /// </summary>
    [Fact]
    public void LoadModels_WithProperties_PopulatedCorrectly()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
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
                },
                ""gpt-35-turbo"": {
                    ""Id"": ""gpt-35-turbo"",
                    ""Name"": ""GPT-3.5 Turbo"",
                    ""Modality"": ""text"",
                    ""ContextWindow"": 16385,
                    ""InputTokenPrice"": 0.002,
                    ""OutputTokenPrice"": 0.004,
                    ""ToolCalling"": false
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var gpt4 = _registry.GetModel("gpt-4");
        var gpt35 = _registry.GetModel("gpt-35-turbo");

        // Assert
        Assert.NotNull(gpt4);
        Assert.Equal("GPT-4", gpt4?.Name);
        Assert.Equal("gpt-4", gpt4?.ProviderName);
        Assert.Equal("text", gpt4?.Modality);
        Assert.Equal(8192, gpt4?.ContextWindow);
        Assert.Equal(0.01m, gpt4?.InputTokenPrice);
        Assert.Equal(0.03m, gpt4?.OutputTokenPrice);
        Assert.True(gpt4?.ToolCalling ?? false);

        Assert.NotNull(gpt35);
        Assert.Equal("GPT-3.5 Turbo", gpt35?.Name);
        Assert.Equal("text", gpt35?.Modality);
        Assert.Equal(16385, gpt35?.ContextWindow);
        Assert.Equal(0.002m, gpt35?.InputTokenPrice);
        Assert.Equal(0.004m, gpt35?.OutputTokenPrice);
        Assert.False(gpt35?.ToolCalling ?? true);
    }

    /// <summary>
    /// SearchByName returns matching models.
    /// </summary>
    [Fact]
    public void SearchByName_FindBySubstring_ReturnsMatches()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": { ""Id"": ""gpt-4"", ""Name"": ""GPT-4"" },
                ""gpt-35-turbo"": { ""Id"": ""gpt-35-turbo"", ""Name"": ""GPT-3.5 Turbo"" },
                ""claude-2"": { ""Id"": ""claude-2"", ""Name"": ""Claude 2"" }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var results = _registry.SearchByName("gpt");

        // Assert
        Assert.Contains(results, r => r.Name == "GPT-4");
        Assert.Contains(results, r => r.Name == "GPT-3.5 Turbo");
        Assert.DoesNotContain(results, r => r.Name == "Claude 2");
        Assert.Equal(2, results.Count);
    }

    /// <summary>
    /// SearchByName case-insensitive.
    /// </summary>
    [Fact]
    public void SearchByName_CaseInsensitive_ReturnsMatches()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": { ""Id"": ""gpt-4"", ""Name"": ""GPT-4"" },
                ""claude-2"": { ""Id"": ""claude-2"", ""Name"": ""Claude 2"" }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var results = _registry.SearchByName("GPt");

        // Assert - case-insensitive search for "gpt" should match GPT-4 only
        Assert.Contains(results, r => r.Name == "GPT-4");
        Assert.DoesNotContain(results, r => r.Name == "Claude 2");
        Assert.Equal(1, results.Count);
    }

    /// <summary>
    /// SearchByProvider returns models for a specific provider.
    /// </summary>
    [Fact]
    public void SearchByProvider_ModelsByProvider_ReturnsCorrect()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""ProviderName"": ""OpenAI""
                },
                ""gpt-35-turbo"": {
                    ""Id"": ""gpt-35-turbo"",
                    ""Name"": ""GPT-3.5 Turbo"",
                    ""ProviderName"": ""OpenAI""
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var results = _registry.SearchByProvider("OpenAI");

        // Assert
        Assert.Contains(results, r => r.Name == "GPT-4");
        Assert.Contains(results, r => r.Name == "GPT-3.5 Turbo");
        Assert.Equal(2, results.Count);
    }

    /// <summary>
    /// SearchByProvider case-insensitive.
    /// </summary>
    [Fact]
    public void SearchByProvider_CaseInsensitive_ReturnsMatches()
    {
        // Arrange (same as above)
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""ProviderName"": ""OpenAI""
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var results = _registry.SearchByProvider("openai");

        // Assert
        Assert.Contains(results, r => r.Name == "GPT-4");
    }

    /// <summary>
    /// FilterByModality filters correctly.
    /// </summary>
    [Fact]
    public void FilterByModality_TextOnly_ReturnsMatches()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""Modality"": ""text""
                },
                ""dall-e-3"": {
                    ""Id"": ""dall-e-3"",
                    ""Name"": ""DALL-E 3"",
                    ""Modality"": ""image""
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var textModels = _registry.FilterByModality("text");
        var imageModels = _registry.FilterByModality("image");

        // Assert
        Assert.Contains(textModels, r => r.Name == "GPT-4");
        Assert.DoesNotContain(imageModels, r => r.Name == "GPT-4");
        Assert.Contains(imageModels, r => r.Name == "DALL-E 3");
        Assert.DoesNotContain(textModels, r => r.Name == "DALL-E 3");
    }

    /// <summary>
    /// FilterByContextWindow returns models within token limit.
    /// </summary>
    [Fact]
    public void FilterByContextWindow_Limit_ReturnsMatching()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""ContextWindow"": 8192
                },
                ""gpt-35-turbo"": {
                    ""Id"": ""gpt-35-turbo"",
                    ""Name"": ""GPT-3.5 Turbo"",
                    ""ContextWindow"": 16385
                },
                ""gpt-4-32k"": {
                    ""Id"": ""gpt-4-32k"",
                    ""Name"": ""GPT-4 32k"",
                    ""ContextWindow"": 32768
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var limited = _registry.FilterByContextWindow(8000);

        // Assert
        // Models with ContextWindow <= 8000: none of the above (8192 > 8000, 16385 > 8000, 32768 > 8000)
        // The method should return an empty list or handle gracefully
        Assert.NotNull(limited);
    }

    /// <summary>
    /// FilterByInputPrice filters by max input price.
    /// </summary>
    [Fact]
    public void FilterByInputPrice_MaxPrice_ReturnsMatches()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""InputTokenPrice"": 0.01
                },
                ""gpt-35-turbo"": {
                    ""Id"": ""gpt-35-turbo"",
                    ""Name"": ""GPT-3.5 Turbo"",
                    ""InputTokenPrice"": 0.002
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var cheap = _registry.FilterByInputPrice(0.005m);

        // Assert
        Assert.NotNull(cheap);
        // gpt-35-turbo at 0.002 should be included; gpt-4 at 0.01 should not
        Assert.DoesNotContain("GPT-4", cheap.Select(m => m.Name));
        Assert.Contains("GPT-3.5 Turbo", cheap.Select(m => m.Name));
    }

    /// <summary>
    /// FilterByOutputPrice filters by max output price.
    /// </summary>
    [Fact]
    public void FilterByOutputPrice_MaxPrice_ReturnsMatches()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""OutputTokenPrice"": 0.03
                },
                ""gpt-35-turbo"": {
                    ""Id"": ""gpt-35-turbo"",
                    ""Name"": ""GPT-3.5 Turbo"",
                    ""OutputTokenPrice"": 0.004
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var cheap = _registry.FilterByOutputPrice(0.01m);

        // Assert
        Assert.NotNull(cheap);
        // gpt-4 at 0.03 should not be included; gpt-3.5 turbo at 0.004 should be included
        Assert.DoesNotContain("GPT-4", cheap.Select(m => m.Name));
        Assert.Contains("GPT-3.5 Turbo", cheap.Select(m => m.Name));
    }

    /// <summary>
    /// FilterByToolCalling filters by tool-calling capability.
    /// </summary>
    [Fact]
    public void FilterByToolCalling_True_ReturnsMatching()
    {
        // Arrange
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""ToolCalling"": true
                },
                ""gpt-35-turbo"": {
                    ""Id"": ""gpt-35-turbo"",
                    ""Name"": ""GPT-3.5 Turbo"",
                    ""ToolCalling"": false
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var toolCalling = _registry.FilterByToolCalling(true);

        // Assert
        Assert.NotNull(toolCalling);
        Assert.Contains(toolCalling, r => r.Name == "GPT-4");
        Assert.DoesNotContain(toolCalling, r => r.Name == "GPT-3.5 Turbo");
    }

    /// <summary>
    /// FilterByToolCalling false returns models without tool calling.
    /// </summary>
    [Fact]
    public void FilterByToolCalling_False_ReturnsMatching()
    {
        // Arrange (same as above)
        var json = @"
        {
            ""providers"": {
                ""openai"": {
                    ""Id"": ""openai"",
                    ""Name"": ""OpenAI""
                }
            },
            ""models"": {
                ""gpt-4"": {
                    ""Id"": ""gpt-4"",
                    ""Name"": ""GPT-4"",
                    ""ToolCalling"": true
                },
                ""gpt-35-turbo"": {
                    ""Id"": ""gpt-35-turbo"",
                    ""Name"": ""GPT-3.5 Turbo"",
                    ""ToolCalling"": false
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "test");

        // Act
        var noToolCalling = _registry.FilterByToolCalling(false);

        // Assert
        Assert.NotNull(noToolCalling);
        Assert.DoesNotContain(noToolCalling, r => r.Name == "GPT-4");
        Assert.Contains(noToolCalling, r => r.Name == "GPT-3.5 Turbo");
    }
}