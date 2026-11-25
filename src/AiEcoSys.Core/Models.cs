namespace AiEcoSys.Core;

/// <summary>
/// Represents a provider from the Models.dev catalogue.
/// </summary>
public class ProviderModel
{
    /// <summary>
    /// Unique identifier for the provider.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the provider.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Documentation URL for the provider.
    /// </summary>
    public string? Doc { get; set; }

    /// <summary>
    /// API endpoint URL for the provider.
    /// </summary>
    public string? Api { get; set; }

    /// <summary>
    /// npm package name for the provider SDK.
    /// </summary>
    public string? Npm { get; set; }

    /// <summary>
    /// Environment-specific configuration.
    /// </summary>
    public object[] Env { get; set; } = Array.Empty<object>();

    /// <summary>
    /// Associated model IDs from this provider.
    /// </summary>
    public List<string> Models { get; set; } = new();
}

/// <summary>
/// Represents a model from the Models.dev catalogue.
/// </summary>
public class ModelMetadata
{
    /// <summary>
    /// Unique identifier for the model (typically provider/model format).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable model name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the model.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Provider identifier this model belongs to.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Model author or lab.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Modality of the model (e.g., "text", "vision", "code").
    /// </summary>
    public string? Modality { get; set; }

    /// <summary>
    /// Input modalities supported (e.g., "text", "image").
    /// </summary>
    public List<string>? InputModalities { get; set; }

    /// <summary>
    /// Output modalities supported (e.g., "text", "image").
    /// </summary>
    public List<string>? OutputModalities { get; set; }

    /// <summary>
    /// Context window size in tokens.
    /// </summary>
    public int? ContextWindow { get; set; }

    /// <summary>
    /// Pricing information for the model.
    /// </summary>
    public decimal? InputTokenPrice { get; set; }
    public decimal? OutputTokenPrice { get; set; }
    public string? PricingCurrency { get; set; }

    /// <summary>
    /// whether tool calling is supported, if metadata exists.
    /// </summary>
    public bool? ToolCalling { get; set; }

    /// <summary>
    /// Provider name for display purposes.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;
}

/// <summary>
/// Represents a snapshot of the catalogue at a point in time.
/// </summary>
public class CatalogueSnapshot
{
    /// <summary>
    /// Timestamp when the snapshot was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source API endpoint from which the snapshot was downloaded.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Provider data included in the snapshot.
    /// </summary>
    public List<ProviderModel>? Providers { get; set; }

    /// <summary>
    /// Model data included in the snapshot.
    /// </summary>
    public List<ModelMetadata>? Models { get; set; }

    /// <summary>
    /// Indicates whether the snapshot is from an online (fresh) source.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Indicates whether the snapshot has been validated.
    /// </summary>
    public bool IsValid { get; set; }
}