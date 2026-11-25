using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AiEcoSys.Core.DataLoaders;

/// <summary>
/// Loads provider and model metadata from a local Models.dev <c>api.json</c> file.
/// Values are taken only from the file; absent fields are left as <c>null</c>
/// rather than being invented. Costs in Models.dev data are USD per one million
/// tokens, so <see cref="ModelMetadata.PricingCurrency"/> is set to <c>USD</c>
/// only when a cost value is actually present.
/// </summary>
public static class ModelsDevApiLoader
{
    /// <summary>
    /// The default location of the local Models.dev api.json file.
    /// </summary>
    public const string DefaultApiPath = @"D:\AI-ECOSYSTEM\models-dev\data\api.json";

    /// <summary>
    /// Loads provider and model metadata from <paramref name="path"/> into the registry.
    /// </summary>
    /// <param name="path">The api.json path to read.</param>
    /// <param name="registry">The registry to populate.</param>
    /// <param name="message">A human-readable result message.</param>
    /// <returns><c>true</c> when metadata was loaded; otherwise <c>false</c>.</returns>
    public static bool TryLoad(string path, CentralRegistry registry, out string message)
    {
        if (registry == null)
            throw new ArgumentNullException(nameof(registry));

        if (string.IsNullOrWhiteSpace(path))
        {
            message = "API data path is empty.";
            return false;
        }

        if (!File.Exists(path))
        {
            message = $"Models.dev API data not found: {path}";
            return false;
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                message = "Models.dev API data root is not an object.";
                return false;
            }

            var providers = new List<ProviderModel>();
            var models = new List<ModelMetadata>();

            foreach (var providerProperty in doc.RootElement.EnumerateObject())
            {
                var providerElement = providerProperty.Value;
                if (providerElement.ValueKind != JsonValueKind.Object)
                    continue;

                string providerId = GetString(providerElement, "id") ?? providerProperty.Name;
                string providerName = GetString(providerElement, "name") ?? providerId;

                var provider = new ProviderModel
                {
                    Id = providerId,
                    Name = providerName,
                    Api = GetString(providerElement, "api"),
                    Doc = GetString(providerElement, "doc"),
                    Npm = GetString(providerElement, "npm")
                };

                if (providerElement.TryGetProperty("env", out var envElement) &&
                    envElement.ValueKind == JsonValueKind.Array)
                {
                    provider.Env = envElement.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => (object?)item.GetString() ?? string.Empty)
                        .ToArray();
                }

                if (providerElement.TryGetProperty("models", out var modelsElement) &&
                    modelsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var modelProperty in modelsElement.EnumerateObject())
                    {
                        var modelElement = modelProperty.Value;
                        if (modelElement.ValueKind != JsonValueKind.Object)
                            continue;

                        string modelId = GetString(modelElement, "id") ?? modelProperty.Name;
                        var inputModalities = GetStringArray(modelElement, "modalities", "input");
                        var outputModalities = GetStringArray(modelElement, "modalities", "output");

                        var model = new ModelMetadata
                        {
                            Id = $"{providerId}/{modelId}",
                            Name = GetString(modelElement, "name") ?? modelId,
                            Description = GetString(modelElement, "description"),
                            ProviderId = providerId,
                            ProviderName = providerName,
                            InputModalities = inputModalities,
                            OutputModalities = outputModalities,
                            Modality = outputModalities?.FirstOrDefault(),
                            ContextWindow = GetNumberAsInt(modelElement, "limit", "context"),
                            ToolCalling = GetBool(modelElement, "tool_call"),
                            InputTokenPrice = GetNumberAsDecimal(modelElement, "cost", "input"),
                            OutputTokenPrice = GetNumberAsDecimal(modelElement, "cost", "output")
                        };

                        bool hasCost = modelElement.TryGetProperty("cost", out var costElement) &&
                                       costElement.ValueKind == JsonValueKind.Object;
                        if (hasCost)
                        {
                            model.PricingCurrency = "USD";
                        }

                        models.Add(model);
                        provider.Models.Add(modelId);
                    }
                }

                providers.Add(provider);
            }

            var timestamp = File.GetLastWriteTime(path);
            registry.LoadLocalCatalogue(providers, models, path, timestamp);

            message = $"Loaded {providers.Count} providers and {models.Count} models from {path}.";
            return models.Count > 0;
        }
        catch (Exception ex)
        {
            message = $"Failed to load Models.dev API data: {ex.Message}";
            return false;
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    private static bool? GetBool(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.True)
                return true;
            if (property.ValueKind == JsonValueKind.False)
                return false;
        }

        return null;
    }

    private static int? GetNumberAsInt(JsonElement element, string parentName, string propertyName)
    {
        if (element.TryGetProperty(parentName, out var parent) &&
            parent.ValueKind == JsonValueKind.Object &&
            parent.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out int value))
        {
            return value;
        }

        return null;
    }

    private static decimal? GetNumberAsDecimal(JsonElement element, string parentName, string propertyName)
    {
        if (element.TryGetProperty(parentName, out var parent) &&
            parent.ValueKind == JsonValueKind.Object &&
            parent.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetDecimal(out decimal value))
        {
            return value;
        }

        return null;
    }

    private static List<string>? GetStringArray(JsonElement element, string parentName, string propertyName)
    {
        if (element.TryGetProperty(parentName, out var parent) &&
            parent.ValueKind == JsonValueKind.Object &&
            parent.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Array)
        {
            var values = property.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString() ?? string.Empty)
                .Where(value => value.Length > 0)
                .ToList();

            return values.Count > 0 ? values : null;
        }

        return null;
    }
}
