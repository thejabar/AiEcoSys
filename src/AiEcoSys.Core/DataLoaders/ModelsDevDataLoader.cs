using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AiEcoSys.Core.DataLoaders;

/// <summary>
/// Loads provider and model metadata from the local Models.dev data source.
/// This provides offline access to Models.dev catalogue data without requiring
/// live API calls at runtime.
/// </summary>
public static class ModelsDevDataLoader
{
    /// <summary>
    /// The base path for Models.dev data files.
    /// </summary>
    private const string BasePath = @"D:\AI-ECOSYSTEM\models-dev\data";

    /// <summary>
    /// Loads model metadata from Models.dev and integrates it with the CentralRegistry.
    /// The method loads the models.json file and calls LoadCatalogueFromJson to
    /// populate the registry's providers and models collections.
    /// </summary>
    /// <param name="registry">The CentralRegistry to populate with model data.</param>
    /// <param name="providerName">Optional provider name filter. If provided, only models
    /// from this provider will be loaded. If null, all models are loaded.</param>
    /// <returns>true if models were loaded successfully; false otherwise.</returns>
    public static bool LoadModelsDevCatalogue(CentralRegistry registry, string? providerName = null)
    {
        string modelsPath = Path.Combine(BasePath, "models.json");

        if (!File.Exists(modelsPath))
        {
            // Try alternative paths
            var altPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "models.json"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "AI-ECOSYSTEM", "models-dev", "data", "models.json")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "AI-ECOSYSTEM", "models-dev", "data", "models.json"))
            };

            foreach (var path in altPaths)
            {
                if (File.Exists(path))
                {
                    modelsPath = path;
                    break;
                }
            }

            if (!File.Exists(modelsPath))
                return false;
        }

        try
        {
            string json = File.ReadAllText(modelsPath, Encoding.UTF8);

            // Parse the JSON to validate structure
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Models.dev models.json is a dictionary keyed by model ID
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            // Build providers section - at minimum, add openai as a placeholder
            string providersJson = "{\"openai\":{\"Id\":\"openai\",\"Name\":\"OpenAI\"}}";

            // Build models section from the dictionary
            string modelsJson = BuildModelsJson(root, providerName);

            // Combine into the format expected by LoadCatalogueFromJson
            string combinedJson = $@"{{""providers"":{providersJson},""models"":{modelsJson}}}";

            // Try to load the combined JSON
            bool result = registry.LoadCatalogueFromJson(combinedJson, "Models.dev");

            if (result)
            {
                // Update the status via the registry's method (public API)
                registry.MarkModelsDevLoaded();

                // Record the sync event
                registry.RecordSyncEvent(SyncResult.Success, $"Loaded {registry.Models.Count} models from Models.dev catalogue");

                return true;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds the models JSON section from the root JsonElement.
    /// </summary>
    /// <param name="root">The root JsonElement of the models.json.</param>
    /// <param name="providerName">Optional provider name filter.</param>
    /// <returns>A JSON string for the models section.</returns>
    private static string BuildModelsJson(JsonElement root, string? providerName)
    {
        var sb = new StringBuilder();
        bool first = true;

        foreach (var prop in root.EnumerateObject())
        {
            string modelId = prop.Name;
            var modelElement = prop.Value;

            // If a specific provider was requested, filter by it
            string? provider = InferProviderFromKey(modelId);
            if (providerName != null && !string.Equals(provider, providerName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!first)
                sb.Append(",");

            first = false;

            // Start the model entry
            sb.Append("\"");
            sb.Append(EscapeJson(modelId));
            sb.Append("\":{\"Id\":\"");
            sb.Append(EscapeJson(modelId));
            sb.Append("\",\"Name\":\"");
            sb.Append(EscapeJson(modelId));
            sb.Append("\",\"Modality\":\"text\"");

            // Try to add optional properties
            if (modelElement.TryGetProperty("description", out var descElem) && descElem.ValueKind == JsonValueKind.String)
            {
                string descVal = descElem.GetString()!;
                descVal = descVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
                sb.Append(",\"Description\":\"");
                sb.Append(descVal);
                sb.Append("\"");
            }
            else
            {
                sb.Append(",\"Description\":\"\"");
            }

            if (modelElement.TryGetProperty("modality", out var modElem) && modElem.ValueKind == JsonValueKind.String)
            {
                string modVal = modElem.GetString()!;
                modVal = modVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
                sb.Append(",\"Modality\":\"");
                sb.Append(modVal);
                sb.Append("\"");
            }

            if (modelElement.TryGetProperty("context", out var ctxElem) && ctxElem.ValueKind == JsonValueKind.Number)
                sb.Append(",\"ContextWindow\":").Append(ctxElem.GetInt32());
            else
                sb.Append(",\"ContextWindow\":0");

            if (modelElement.TryGetProperty("input_token_price", out var inputPriceElem) && inputPriceElem.ValueKind == JsonValueKind.Number)
                sb.Append(",\"InputTokenPrice\":").Append(inputPriceElem.GetDecimal().ToString("0.0000"));
            else
                sb.Append(",\"InputTokenPrice\":0");

            if (modelElement.TryGetProperty("output_token_price", out var outputPriceElem) && outputPriceElem.ValueKind == JsonValueKind.Number)
                sb.Append(",\"OutputTokenPrice\":").Append(outputPriceElem.GetDecimal().ToString("0.0000"));
            else
                sb.Append(",\"OutputTokenPrice\":0");

            if (modelElement.TryGetProperty("tool_calling", out var toolElem) && toolElem.ValueKind == JsonValueKind.True)
                sb.Append(",\"ToolCalling\":true");
            else if (modelElement.TryGetProperty("tool_calling", out var toolElemFalse) && toolElemFalse.ValueKind == JsonValueKind.False)
                sb.Append(",\"ToolCalling\":false");

            // Add provider info
            if (!string.IsNullOrEmpty(provider))
                sb.Append($",\"ProviderName\":\"{provider}\"");
            else
                sb.Append(",\"ProviderName\":\"");
            sb.Append(provider ?? modelId);
            sb.Append("\"}");
        }

        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    /// Escapes a string for safe inclusion in JSON.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The escaped string.</returns>
    private static string EscapeJson(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    /// <summary>
    /// Infers the provider name from the model ID key.
    /// Models.dev model IDs often follow the pattern "provider/modelname".
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>The inferred provider name, or null if cannot be determined.</returns>
    private static string? InferProviderFromKey(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
            return null;

        // Check for common provider prefixes
        var parts = modelId.Split('/');
        if (parts.Length >= 2)
            return parts[0];

        // Check for known provider patterns
        if (modelId.StartsWith("mistral/", StringComparison.OrdinalIgnoreCase))
            return "mistral";
        if (modelId.StartsWith("openai/", StringComparison.OrdinalIgnoreCase))
            return "openai";
        if (modelId.StartsWith("anthropic/", StringComparison.OrdinalIgnoreCase))
            return "anthropic";
        if (modelId.StartsWith("google/", StringComparison.OrdinalIgnoreCase))
            return "google";
        if (modelId.StartsWith("cohere/", StringComparison.OrdinalIgnoreCase))
            return "cohere";

        return null;
    }
}