namespace AiEcoSys.Core;

/// <summary>
/// Represents the result of a catalogue synchronisation operation.
/// </summary>
public enum SyncResult
{
    /// <summary>
    /// Synchronisation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Synchronisation failed - data is invalid or unavailable.
    /// </summary>
    Failed,

    /// <summary>
    /// Synchronisation was skipped (offline mode, no new data).
    /// </summary>
    Skipped,

    /// <summary>
    /// Synchronisation completed with rollback to previous valid snapshot.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Synchronisation completed but used cached/preserved data.
    /// </summary>
    Preserved,

    /// <summary>
    /// Synchronisation in progress.
    /// </summary>
    Synchronising
}

/// <summary>
/// Represents the status of the catalogue and synchronisation.
/// </summary>
public enum CatalogueStatus
{
    /// <summary>
    /// Online mode, catalogue is up to date.
    /// </Meter>
    Online,

    /// <summary>
    /// Offline mode, using locally stored snapshot.
    /// </summary>
    Offline,

    /// <summary>
    /// Catalogue is being synchronised.
    /// </summary>
    Synchronising,

    /// <summary>
    /// Catalogue validation failed, using last known valid data.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// No catalogue data available.
    /// </summary>
    NoData
}

/// <summary>
/// Status information for the catalogue.
/// </summary>
public class CatalogueStatusInfo
{
    /// <summary>
    /// Current catalogue status.
    /// </summary>
    public CatalogueStatus Status { get; set; } = CatalogueStatus.Offline;

    /// <summary>
    /// Timestamp of the last synchronisation.
    /// </summary>
    public DateTime? LastSync { get; set; }

    /// <summary>
    /// Number of providers in the catalogue.
    /// </summary>
    public int ProviderCount { get; set; }

    /// <summary>
    /// Number of models in the catalogue.
    /// </summary>
    public int ModelCount { get; set; }

    /// <summary>
    /// Timestamp of the current data source.
    /// </summary>
    public DateTime? DataTimestamp { get; set; }

    /// <summary>
    /// Error message if synchronisation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the application is running in offline mode.
    /// </summary>
    public bool IsOffline { get; set; }
}

/// <summary>
/// Base interface for the central registry abstraction.
/// Exposes consistent services for provider discovery, model discovery,
/// model metadata, pricing metadata, capability filtering, and catalogue version.
/// </summary>
public interface ICentralRegistry
{
    /// <summary>
    /// Gets the current catalogue status.
    /// </summary>
    CatalogueStatusInfo Status { get; }

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    IReadOnlyList<ProviderModel> Providers { get; }

    /// <summary>
    /// Gets all registered models with their metadata.
    /// </summary>
    IReadOnlyList<ModelMetadata> Models { get; }

    /// <summary>
    /// Gets a specific provider by identifier.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    ProviderModel? GetProvider(string providerId);

    /// <summary>
    /// Gets models filtered by provider identifier.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    IReadOnlyList<ModelMetadata> GetModelsByProvider(string providerId);

    /// <summary>
    /// Searches models by name (case-insensitive partial match).
    /// </summary>
    /// <param name="query">The search query.</param>
    IReadOnlyList<ModelMetadata> SearchByName(string query);

    /// <summary>
    /// Searches models by provider name (case-insensitive partial match).
    /// </summary>
    /// <param name="providerName">The provider name to search by.</param>
    IReadOnlyList<ModelMetadata> SearchByProvider(string providerName);

    /// <summary>
    /// Filters models by modality.
    /// </summary>
    /// <param name="modality">The modality to filter by (e.g., "text", "vision").</param>
    IReadOnlyList<ModelMetadata> FilterByModality(string? modality);

    /// <summary>
    /// Filters models by context window (maximum tokens).
    /// </summary>
    /// <param name="maxTokens">Maximum context window in tokens.</param>
    IReadOnlyList<ModelMetadata> FilterByContextWindow(int maxTokens);

    /// <summary>
    /// Filters models by minimum input token pricing.
    /// </summary>
    /// <param name maxInputPrice">Maximum input token price.</param>
    IReadOnlyList<ModelMetadata> FilterByInputPrice(decimal maxInputPrice);

    /// <summary>
    /// Filters models by minimum output token pricing.
    /// </summary>
    /// <param name="maxOutputPrice">Maximum output token price.</param>
    IReadOnlyList<ModelMetadata> FilterByOutputPrice(decimal maxOutputPrice);

    /// <summary>
    /// Filters models by tool calling support.
    /// </summary>
    /// <param name="supportsToolCalling">Whether to include only models that support tool calling.</param>
    IReadOnlyList<ModelMetadata> FilterByToolCalling(bool supportsToolCalling);

    /// <summary>
    /// Gets model metadata by model ID.
    /// </summary>
    /// <param name="modelId">The model identifier (provider/model format).</param>
    ModelMetadata? GetModel(string modelId);

    /// <summary>
    /// Records a synchronisation event.
    /// </summary>
    /// <param name="result">The synchronisation result.</param>
    /// <param name="details">Details about the synchronisation.</param>
    void RecordSyncEvent(SyncResult result, string details);

    /// <summary>
    /// Gets the synchronisation history/log.
    /// </summary>
    IReadOnlyList<string> SyncHistory { get; }

    /// <summary>
    /// Gets the last known valid catalogue snapshot.
    /// </summary>
    CatalogueSnapshot? LastValidSnapshot { get; }

    /// <summary>
    /// Occurs when the catalogue status changes.
    /// </summary>
    event EventHandler<CatalogueStatusInfo>? StatusChanged;

    /// <summary>
    /// Occurs when the synchronisation history changes.
    /// </summary>
    event EventHandler? SyncHistoryChanged;

    /// <summary>
    /// Refreshes the catalogue from a source URL by downloading and validating catalogue data.
    /// </summary>
    /// <param name="sourceUrl">The URL to download the catalogue from.</param>
    /// <param name="timeoutSeconds">Download timeout in seconds.</param>
    /// <returns>A SyncResult indicating the outcome of the refresh.</returns>
    Task<SyncResult> RefreshCatalogueAsync(string sourceUrl, int timeoutSeconds = 30);

    /// <summary>
    /// Refreshes the catalogue from a local snapshot file path.
    /// </summary>
    /// <param name="snapshotPath">The file path to the catalogue snapshot.</param>
    /// <returns>A SyncResult indicating the outcome of the refresh.</returns>
    SyncResult RefreshCatalogueFromSnapshot(string snapshotPath);
}