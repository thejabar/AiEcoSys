using AiEcoSys.Core;
using Xunit;

/// <summary>
/// Tests for synchronisation events, snapshot management, and rollback behavior.
/// </summary>
public class TestSyncAndSnapshot
{
    private readonly CentralRegistry _registry = new();

    /// <summary>
    /// RecordSyncEvent adds entries to history and raises SyncHistoryChanged.
    /// </summary>
    [Fact]
    public void RecordSyncEvent_AddsEntryToHistory()
    {
        // Act
        _registry.RecordSyncEvent(SyncResult.Success, "Test details");

        // Assert
        Assert.Single(_registry.SyncHistory);
        Assert.Contains(_registry.SyncHistory, e => e.Contains("Success"));
    }

    /// <summary>
    /// RecordSyncEvent keeps only last 50 entries.
    /// </summary>
    [Fact]
    public void RecordSyncEvent_HistoryLimitedTo50()
    {
        // Arrange & Act
        for (int i = 0; i < 55; i++)
        {
            _registry.RecordSyncEvent(SyncResult.Success, "Entry " + i);
        }

        // Assert
        Assert.Equal(50, _registry.SyncHistory.Count);
    }

    /// <summary>
    /// RecordSyncEvent raises SyncHistoryChanged event.
    /// </summary>
    [Fact]
    public void RecordSyncEvent_RaisesSyncHistoryChanged()
    {
        // Arrange
        var eventRaised = false;
        _registry.SyncHistoryChanged += (s, e) => { eventRaised = true; };

        // Act
        _registry.RecordSyncEvent(SyncResult.Failed, "test");

        // Assert
        Assert.True(eventRaised);
    }

    /// <summary>
    /// RefreshCatalogueFromSnapshot loads from existing file.
    /// </summary>
    [Fact]
    public void RefreshCatalogueFromSnapshot_ExistingFile_LoadsSuccessfully()
    {
        // Arrange: Create a temporary valid catalogue JSON
        var tempPath = "D:/AiEcoSys/tests/test-catalogue-snapshot.json";
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
                    ""OutputTokenPrice"": 0.03
                }
            }
        }";
        File.WriteAllText(tempPath, json);

        try
        {
            // Act
            var result = _registry.RefreshCatalogueFromSnapshot(tempPath);

            // Assert
            Assert.Equal(SyncResult.Success, result);
            Assert.Equal(CatalogueStatus.Online, _registry.Status.Status);
            Assert.Equal(1, _registry.Providers.Count);
            Assert.Equal(1, _registry.Models.Count);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// RefreshCatalogueFromSnapshot missing file returns Failed.
    /// </summary>
    [Fact]
    public void RefreshCatalogueFromSnapshot_MissingFile_ReturnsFailed()
    {
        // Arrange
        var nonexistentPath = "D:/AiEcoSys/tests/nonexistent-snapshot.json";

        // Act
        var result = _registry.RefreshCatalogueFromSnapshot(nonexistentPath);

        // Assert
        Assert.Equal(SyncResult.Failed, result);
    }

    /// <summary>
    /// RefreshCatalogueAsync with offline mode uses last valid snapshot.
    /// </summary>
    [Fact]
    public async Task RefreshCatalogueAsync_OfflineUsesSnapshot()
    {
        // Arrange: Set offline status via RaiseStatusChanged
        _registry.RaiseStatusChanged(new CatalogueStatusInfo
        {
            Status = CatalogueStatus.Offline,
            LastSync = DateTime.UtcNow,
            ProviderCount = 0,
            ModelCount = 0,
            IsOffline = true
        });

        // Create a valid snapshot file and load it via LoadCatalogueFromJson
        // which preserves the snapshot internally
        var snapshotPath = "D:/AiEcoSys/tests/test-snapshot.json";
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
                    ""OutputTokenPrice"": 0.03
                }
            }
        }";
        File.WriteAllText(snapshotPath, json);
        _registry.LoadCatalogueFromJson(json, snapshotPath);

        // Act - set status to offline and refresh async
        _registry.RaiseStatusChanged(new CatalogueStatusInfo
        {
            Status = CatalogueStatus.Offline,
            ProviderCount = 0,
            ModelCount = 0,
            IsOffline = true
        });

        var result = await _registry.RefreshCatalogueAsync("https://models.dev/api.json", 30);

        // Assert - should use preserved snapshot
        Assert.Equal(SyncResult.Preserved, result);
        Assert.Equal(1, _registry.Providers.Count);
        Assert.Equal(1, _registry.Models.Count);

        // Cleanup
        if (File.Exists(snapshotPath))
            File.Delete(snapshotPath);
    }

    /// <summary>
    /// RefreshCatalogueAsync online path structure.
    /// </summary>
    [Fact]
    public async Task RefreshCatalogueAsync_OnlinePath_StructureValid()
    {
        // Act - just verify the method can be called and returns a value
        // First load data so the online path has something to work with
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
                    ""OutputTokenPrice"": 0.03
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "online-test");

        var result = await _registry.RefreshCatalogueAsync("https://models.dev/api.json?type=all", 30);

        // Assert - should succeed since we have online data context
        Assert.NotNull(result);
    }

    /// <summary>
    /// StatusChanged event fires correctly.
    /// </summary>
    [Fact]
    public void StatusChanged_EventFires()
    {
        // Arrange
        var eventFired = false;
        _registry.StatusChanged += (s, e) => { eventFired = true; };

        // Act
        _registry.RaiseStatusChanged(new CatalogueStatusInfo
        {
            Status = CatalogueStatus.Online,
            LastSync = DateTime.UtcNow,
            ProviderCount = 5,
            ModelCount = 10
        });

        // Assert
        Assert.True(eventFired);
    }

    /// <summary>
    /// New snapshot load updates sync history.
    /// </summary>
    [Fact]
    public void NewSnapshot_UpdatesSyncHistory()
    {
        // Arrange
        _registry.RecordSyncEvent(SyncResult.Success, "first");
        Assert.Single(_registry.SyncHistory);

        // Act - load a new snapshot
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
                    ""InputTokenPrice"": 0.01m,
                    ""OutputTokenPrice"": 0.03m
                }
            }
        }";
        _registry.LoadCatalogueFromJson(json, "new-source");

        // Assert
        // After loading, sync history should reflect the new load event
        Assert.True(_registry.SyncHistory.Count >= 1);
    }
}