using System.Text.Json;
using AiEcoSys.Security;

namespace AiEcoSys.Core;

/// <summary>
/// Central registry implementation that manages provider and model discovery,
/// catalogue synchronisation, and filtering capabilities.
/// </summary>
public class CentralRegistry : ICentralRegistry
{
    private readonly List<ProviderModel> _providers = new();
    private readonly List<ModelMetadata> _models = new();
    private readonly List<string> _syncHistory = new();
    private CatalogueSnapshot? _lastValidSnapshot;
    private readonly object _lock = new();

    /// <summary>
    /// Occurs when the catalogue status changes.
    /// </summary>
    public event EventHandler<CatalogueStatusInfo>? StatusChanged;

    /// <inheritdoc />
    public event EventHandler? SyncHistoryChanged;

    /// <inheritdoc />
    public CatalogueStatusInfo Status { get; private set; } = new CatalogueStatusInfo
    {
        Status = CatalogueStatus.Offline,
        ProviderCount = 0,
        ModelCount = 0
    };

    /// <inheritdoc />
    public IReadOnlyList<ProviderModel> Providers => _providers.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> Models => _models.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<string> SyncHistory => _syncHistory.AsReadOnly();

    /// <inheritdoc />
    public CatalogueSnapshot? LastValidSnapshot => _lastValidSnapshot;

    /// <summary>
    /// Gets a specific provider by identifier.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>The provider model, or null if not found.</returns>
    public ProviderModel? GetProvider(string providerId)
    {
        return _providers.FirstOrDefault(p => string.Equals(p.Id, providerId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> GetModelsByProvider(string providerId)
    {
        return _models
            .Where(m => string.Equals(m.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> SearchByName(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Models;

        var lowerQuery = query.ToLowerInvariant();
        return _models
            .Where(m => m.Name.ToLowerInvariant().Contains(lowerQuery))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> SearchByProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return Models;

        var lowerName = providerName.ToLowerInvariant();
        return _models
            .Where(m => m.ProviderName.ToLowerInvariant().Contains(lowerName))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> FilterByModality(string? modality)
    {
        if (string.IsNullOrWhiteSpace(modality))
            return Models;

        var lowerModality = modality.ToLowerInvariant();
        return _models
            .Where(m => m.Modality != null && m.Modality.ToLowerInvariant() == lowerModality)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> FilterByContextWindow(int maxTokens)
    {
        return _models
            .Where(m => m.ContextWindow.HasValue && m.ContextWindow.Value <= maxTokens)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> FilterByInputPrice(decimal maxInputPrice)
    {
        return _models
            .Where(m => m.InputTokenPrice.HasValue && m.InputTokenPrice.Value <= maxInputPrice)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> FilterByOutputPrice(decimal maxOutputPrice)
    {
        return _models
            .Where(m => m.OutputTokenPrice.HasValue && m.OutputTokenPrice.Value <= maxOutputPrice)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelMetadata> FilterByToolCalling(bool supportsToolCalling)
    {
        return _models
            .Where(m => m.ToolCalling.HasValue && m.ToolCalling.Value == supportsToolCalling)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public ModelMetadata? GetModel(string modelId)
    {
        return _models.FirstOrDefault(m => string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public void RecordSyncEvent(SyncResult result, string details)
    {
        lock (_lock)
        {
            var timestamp = DateTime.UtcNow.ToString("o");
            var entry = $"{timestamp} - {result}: {details}";
            _syncHistory.Add(entry);

            // Keep history manageable (last 50 entries)
            if (_syncHistory.Count > 50)
            {
                _syncHistory.RemoveAt(0);
            }

            // Raise sync history changed event
            SyncHistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Marks that Models.dev catalogue has been loaded, updating the registry status.
    /// </summary>
    public void MarkModelsDevLoaded()
    {
        Status = new CatalogueStatusInfo
        {
            Status = CatalogueStatus.Online,
            LastSync = DateTime.UtcNow,
            ProviderCount = _providers.Count,
            ModelCount = _models.Count,
            DataTimestamp = DateTime.UtcNow,
            IsOffline = false
        };

        // Raise status changed event
        StatusChanged?.Invoke(this, Status);
    }

    /// <summary>
    /// Replaces the in-memory catalogue with metadata loaded from a local source
    /// (for example a local Models.dev api.json file). The data is treated as an
    /// offline snapshot: nothing is invented and the source path is recorded.
    /// </summary>
    /// <param name="providers">The providers to load.</param>
    /// <param name="models">The models to load.</param>
    /// <param name="source">The local file path the data was read from.</param>
    /// <param name="dataTimestamp">The timestamp of the source data, if known.</param>
    public void LoadLocalCatalogue(
        IReadOnlyList<ProviderModel> providers,
        IReadOnlyList<ModelMetadata> models,
        string source,
        DateTime? dataTimestamp = null)
    {
        lock (_lock)
        {
            _providers.Clear();
            _providers.AddRange(providers);
            _models.Clear();
            _models.AddRange(models);

            Status = new CatalogueStatusInfo
            {
                Status = CatalogueStatus.Offline,
                IsOffline = true,
                LastSync = DateTime.UtcNow,
                DataTimestamp = dataTimestamp ?? DateTime.UtcNow,
                ProviderCount = _providers.Count,
                ModelCount = _models.Count
            };

            _lastValidSnapshot = new CatalogueSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Source = source,
                Providers = new List<ProviderModel>(_providers),
                Models = new List<ModelMetadata>(_models),
                IsOnline = false,
                IsValid = true
            };
        }

        RecordSyncEvent(
            SyncResult.Success,
            $"Loaded local catalogue: {_providers.Count} providers, {_models.Count} models from {source}");

        StatusChanged?.Invoke(this, Status);
    }

    /// <inheritdoc />
    public void RaiseStatusChanged(CatalogueStatusInfo status)
    {
        Status = status;
        StatusChanged?.Invoke(this, status);
    }

    /// <inheritdoc />
    public SyncResult RefreshCatalogueFromSnapshot(string snapshotPath)
    {
        // Path traversal protection: ensure path is within allowed base directory
        string allowedBase = "D:/AiEcoSys";
        if (!SecurityService.ValidatePath(snapshotPath, allowedBase))
        {
            RecordSyncEvent(SyncResult.Failed, $"Snapshot path outside allowed base directory: {snapshotPath}");
            return SyncResult.Failed;
        }

        if (!System.IO.File.Exists(snapshotPath))
        {
            RecordSyncEvent(SyncResult.Failed, $"Snapshot file not found: {snapshotPath}");
            return SyncResult.Failed;
        }

        string json;
        try
        {
            json = System.IO.File.ReadAllText(snapshotPath);
        }
        catch (Exception ex)
        {
            RecordSyncEvent(SyncResult.Failed, $"Failed to read snapshot: {ex.Message}");
            return SyncResult.Failed;
        }

        // Load the catalogue from JSON
        bool valid = LoadCatalogueFromJson(json, snapshotPath);

        if (valid)
        {
            Status = new CatalogueStatusInfo
            {
                Status = CatalogueStatus.Online,
                LastSync = DateTime.UtcNow,
                ProviderCount = _providers.Count,
                ModelCount = _models.Count,
                DataTimestamp = DateTime.UtcNow,
                IsOffline = false
            };

            RecordSyncEvent(SyncResult.Success, $"Catalogue loaded from snapshot: {_providers.Count} providers, {_models.Count} models");
            return SyncResult.Success;
        }
        else
        {
            RecordSyncEvent(SyncResult.Failed, "Failed to validate catalogue from snapshot");
            return SyncResult.Failed;
        }
    }

    /// <summary>
    /// Loads catalogue data from a JSON string, validating the structure.
    /// </summary>
    /// <param name="json">The JSON data to load.</param>
    /// <param name="source">Optional source URL for logging.</param>
    /// <returns>True if the data was loaded and validated successfully.</returns>
    public bool LoadCatalogueFromJson(string json, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            RecordSyncEvent(SyncResult.Failed, "Empty JSON data");
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Get structure elements - declared at method scope so they persist
            JsonElement providersElem;
            JsonElement modelsElem;

            // Validate structure: must have providers and/or models
            bool hasProviders = root.TryGetProperty("providers", out providersElem);
            bool hasModels = root.TryGetProperty("models", out modelsElem);

            if (!hasProviders && !hasModels)
            {
                RecordSyncEvent(SyncResult.Failed, "Missing 'providers' or 'models' in catalogue JSON");
                return false;
            }

            // Load providers if present
            if (hasProviders && providersElem.ValueKind != JsonValueKind.Null)
            {
                var loadedProviders = new List<ProviderModel>();
                foreach (var prop in providersElem.EnumerateObject())
                {
                    var providerJson = prop.Value.GetRawText();
                    var provider = JsonSerializer.Deserialize<ProviderModel>(providerJson);
                    if (provider != null && !string.IsNullOrEmpty(provider.Id))
                    {
                        loadedProviders.Add(provider);
                    }
                }

                // Preserve existing providers and add new ones (merge by ID)
                lock (_lock)
                {
                    foreach (var p in loadedProviders)
                    {
                        var existingIndex = _providers.FindIndex(
                            x => string.Equals(x.Id, p.Id, StringComparison.OrdinalIgnoreCase));
                        if (existingIndex >= 0)
                        {
                            // Update existing
                            _providers[existingIndex] = p;
                        }
                        else
                        {
                            _providers.Add(p);
                        }
                    }
                }
            }

            // Load models if present (using method-scoped variables)
            if (hasModels && modelsElem.ValueKind != JsonValueKind.Null)
            {
                var loadedModels = new List<ModelMetadata>();
                foreach (var prop in modelsElem.EnumerateObject())
                {
                    var modelJson = prop.Value.GetRawText();
                    var model = JsonSerializer.Deserialize<ModelMetadata>(modelJson);
                    if (model != null && !string.IsNullOrEmpty(model.Id))
                    {
                        // Preserve ProviderName from JSON if provided, otherwise use dictionary key
                        if (string.IsNullOrEmpty(model.ProviderName))
                            model.ProviderName = prop.Name; // Key of the model entry is the provider name
                        loadedModels.Add(model);
                    }
                }

                lock (_lock)
                {
                    foreach (var m in loadedModels)
                    {
                        var existingIndex = _models.FindIndex(
                            x => string.Equals(x.Id, m.Id, StringComparison.OrdinalIgnoreCase));
                        if (existingIndex >= 0)
                        {
                            _models[existingIndex] = m;
                        }
                        else
                        {
                            _models.Add(m);
                        }
                    }
                }
            }

            // Update status
            lock (_lock)
            {
                Status = new CatalogueStatusInfo
                {
                    Status = CatalogueStatus.Online,
                    LastSync = DateTime.UtcNow,
                    ProviderCount = _providers.Count,
                    ModelCount = _models.Count,
                    DataTimestamp = DateTime.UtcNow,
                    IsOffline = false
                };

                // Preserve the valid snapshot
                _lastValidSnapshot = new CatalogueSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Source = source,
                    Providers = new List<ProviderModel>(_providers),
                    Models = new List<ModelMetadata>(_models),
                    IsOnline = true,
                    IsValid = true
                };
            }

            RecordSyncEvent(SyncResult.Success, $"Loaded {_models.Count} models from {_providers.Count} providers");

            return true;
        }
        catch (JsonException ex)
        {
            RecordSyncEvent(SyncResult.Failed, $"JSON parsing error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            RecordSyncEvent(SyncResult.Failed, $"Unexpected error: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<SyncResult> RefreshCatalogueAsync(string sourceUrl, int timeoutSeconds = 30)
    {
        // If offline, try to use last valid snapshot
        if (Status.IsOffline && _lastValidSnapshot != null)
        {
            lock (_lock)
            {
                _providers.Clear();
                _providers.AddRange(_lastValidSnapshot.Providers ?? new List<ProviderModel>());
                _models.Clear();
                _models.AddRange(_lastValidSnapshot.Models ?? new List<ModelMetadata>());
            }

            Status = new CatalogueStatusInfo
            {
                Status = CatalogueStatus.Offline,
                LastSync = _lastValidSnapshot.Timestamp,
                ProviderCount = _providers.Count,
                ModelCount = _models.Count,
                DataTimestamp = _lastValidSnapshot.Timestamp,
                IsOffline = false
            };

            RecordSyncEvent(SyncResult.Preserved, "Using preserved last valid snapshot");
            return SyncResult.Preserved;
        }

        // HTTPS enforcement for remote provider URLs, with explicit localhost allowance
        if (!string.IsNullOrWhiteSpace(sourceUrl))
        {
            try
            {
                var uri = new Uri(sourceUrl);

                // Reject non-HTTPS URLs that are not localhost
                if (!SecurityService.ValidateHttps(uri))
                {
                    RecordSyncEvent(SyncResult.Failed, $"Non-HTTPS source URL rejected: {sourceUrl}");
                    return SyncResult.Failed;
                }

                // Allow localhost and 127.0.0.1 for local inference services
                // without requiring HTTPS, but also don't block on download failure
                if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    RecordSyncEvent(SyncResult.Synchronising, $"Starting catalogue refresh from localhost source: {sourceUrl}");
                    return SyncResult.Synchronising;
                }
            }
            catch (UriFormatException)
            {
                // Invalid URL format - allow attempt but log warning
                RecordSyncEvent(SyncResult.Failed, $"Invalid URL format: {sourceUrl}");
                return SyncResult.Failed;
            }
        }

        RecordSyncEvent(SyncResult.Synchronising, $"Starting catalogue refresh from {sourceUrl}");

        string json;
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            json = await httpClient.GetStringAsync(sourceUrl);
        }
        catch (TaskCanceledException)
        {
            RecordSyncEvent(SyncResult.Failed, $"Download timeout after {timeoutSeconds}s");
            return SyncResult.Failed;
        }
        catch (Exception ex)
        {
            RecordSyncEvent(SyncResult.Failed, $"Download error: {ex.Message}");
            return SyncResult.Failed;
        }

        // Validate and load the catalogue
        bool valid = LoadCatalogueFromJson(json, sourceUrl);

        if (valid)
        {
            Status = new CatalogueStatusInfo
            {
                Status = CatalogueStatus.Online,
                LastSync = DateTime.UtcNow,
                ProviderCount = _providers.Count,
                ModelCount = _models.Count,
                DataTimestamp = DateTime.UtcNow,
                IsOffline = false
            };

            RecordSyncEvent(SyncResult.Success, $"Catalogue refreshed: {_providers.Count} providers, {_models.Count} models");
            return SyncResult.Success;
        }
        else
        {
            // Rollback to last valid snapshot
            if (_lastValidSnapshot != null)
            {
                lock (_lock)
                {
                    _providers.Clear();
                    _providers.AddRange(_lastValidSnapshot.Providers ?? new List<ProviderModel>());
                    _models.Clear();
                    _models.AddRange(_lastValidSnapshot.Models ?? new List<ModelMetadata>());
                }

                Status = new CatalogueStatusInfo
                {
                    Status = CatalogueStatus.ValidationFailed,
                    LastSync = DateTime.UtcNow,
                    ProviderCount = _providers.Count,
                    ModelCount = _models.Count,
                    DataTimestamp = _lastValidSnapshot.Timestamp,
                    IsOffline = true,
                    ErrorMessage = "Catalogue validation failed, rolled back to last valid snapshot"
                };
            }
            else
            {
                Status = new CatalogueStatusInfo
                {
                    Status = CatalogueStatus.NoData,
                    LastSync = DateTime.UtcNow,
                    ProviderCount = 0,
                    ModelCount = 0,
                    IsOffline = true,
                    ErrorMessage = "Catalogue validation failed and no previous snapshot available"
                };
            }

            RecordSyncEvent(SyncResult.RolledBack, "Catalogue validation failed, rolled back to last valid snapshot");
            return SyncResult.RolledBack;
        }
    }
}