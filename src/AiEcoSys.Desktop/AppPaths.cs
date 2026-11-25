using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AiEcoSys.Desktop;

/// <summary>
/// Resolved application paths for the current process. Portable copies use a
/// <c>Data</c> directory beside the executable; installed copies read the data
/// directory from <c>aiecoSys.paths.json</c>. Paths on the C: drive are never
/// selected silently.
/// </summary>
public sealed record AppPathInfo(string DataDirectory, string CataloguePath, string? Warning);

/// <summary>
/// Resolves the data, settings, catalogue, and snapshot locations used by
/// AiEcoSys. The resolver is deliberately independent of the development
/// machine layout so that portable and installed copies behave the same way.
/// </summary>
public static class AppPaths
{
    /// <summary>The environment variable that overrides the data directory.</summary>
    public const string DataDirectoryEnvironmentVariable = "AIECOSYS_DATA_DIR";

    /// <summary>The environment variable that overrides the catalogue path.</summary>
    public const string CataloguePathEnvironmentVariable = "AIECOSYS_CATALOGUE_PATH";

    /// <summary>The optional per-copy path configuration file name.</summary>
    public const string PathsFileName = "aiecoSys.paths.json";

    private const string CatalogueFileName = "api.json";

    private static readonly Lazy<AppPathInfo> Current = new(ResolveCurrent);

    /// <summary>Gets the directory containing the application executable.</summary>
    public static string BaseDirectory => Path.GetFullPath(AppContext.BaseDirectory);

    /// <summary>Gets the writable data directory for this copy.</summary>
    public static string DataDirectory => Current.Value.DataDirectory;

    /// <summary>Gets the settings directory.</summary>
    public static string SettingsDirectory => Path.Combine(DataDirectory, "settings");

    /// <summary>Gets the full path of the persisted settings file.</summary>
    public static string SettingsPath => Path.Combine(SettingsDirectory, "app-settings.json");

    /// <summary>Gets the default snapshot directory.</summary>
    public static string DefaultSnapshotDirectory => Path.Combine(DataDirectory, "snapshots");

    /// <summary>Gets the catalogue file selected for this copy.</summary>
    public static string CataloguePath => Current.Value.CataloguePath;

    /// <summary>Gets a non-fatal resolution warning, when one was produced.</summary>
    public static string? Warning => Current.Value.Warning;

    /// <summary>
    /// Resolves the current process paths. The order is: environment override,
    /// per-copy configuration file, then a portable <c>Data</c> directory.
    /// </summary>
    public static AppPathInfo ResolveCurrent()
    {
        string baseDirectory = BaseDirectory;
        string? configured = null;
        string? warning = null;
        string configPath = Path.Combine(baseDirectory, PathsFileName);

        if (File.Exists(configPath))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(configPath));
                if (document.RootElement.TryGetProperty("DataDirectory", out var property) &&
                    property.ValueKind == JsonValueKind.String)
                {
                    configured = property.GetString();
                }
            }
            catch (Exception ex)
            {
                warning = $"Could not read {configPath}: {ex.Message}";
            }
        }

        string dataDirectory = ResolveDataDirectory(
            baseDirectory,
            Environment.GetEnvironmentVariable(DataDirectoryEnvironmentVariable),
            configured,
            out string? dataWarning);

        if (!string.IsNullOrWhiteSpace(dataWarning))
            warning = CombineWarnings(warning, dataWarning);

        try
        {
            Directory.CreateDirectory(dataDirectory);
        }
        catch (Exception ex)
        {
            warning = CombineWarnings(
                warning,
                $"Could not create the data directory {dataDirectory}: {ex.Message}");
        }

        string cataloguePath = ResolveCataloguePath(
            baseDirectory,
            dataDirectory,
            Environment.GetEnvironmentVariable(CataloguePathEnvironmentVariable));

        return new AppPathInfo(dataDirectory, cataloguePath, warning);
    }

    /// <summary>
    /// Resolves a data directory from an environment value, a per-copy
    /// configuration value, or the portable default. A C: path is rejected.
    /// </summary>
    public static string ResolveDataDirectory(
        string baseDirectory,
        string? environmentValue,
        string? configuredValue,
        out string? warning)
    {
        warning = null;

        if (TryNormalizeNonC(environmentValue, out string environmentPath, out string environmentError))
            return environmentPath;

        if (!string.IsNullOrWhiteSpace(environmentValue))
            warning = $"Ignoring {DataDirectoryEnvironmentVariable}: {environmentError}";

        if (TryNormalizeNonC(configuredValue, out string configuredPath, out string configuredError))
            return configuredPath;

        if (!string.IsNullOrWhiteSpace(configuredValue))
            warning = CombineWarnings(warning, $"Ignoring configured data directory: {configuredError}");

        string portablePath = Path.Combine(baseDirectory, "Data");
        if (TryNormalizeNonC(portablePath, out string normalizedPortable, out string portableError))
            return normalizedPortable;

        warning = CombineWarnings(
            warning,
            $"The portable data directory was rejected ({portableError}); using D:\\AiEcoSys\\Data.");

        return @"D:\AiEcoSys\Data";
    }

    /// <summary>
    /// Selects the first existing catalogue from the environment override, the
    /// application directory, the data directory, or the legacy development
    /// location.
    /// </summary>
    public static string ResolveCataloguePath(
        string baseDirectory,
        string dataDirectory,
        string? environmentValue)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            candidates.Add(Path.IsPathRooted(environmentValue)
                ? Path.GetFullPath(environmentValue)
                : Path.GetFullPath(Path.Combine(baseDirectory, environmentValue)));
        }

        candidates.Add(Path.Combine(baseDirectory, "catalogue", CatalogueFileName));
        candidates.Add(Path.Combine(dataDirectory, "catalogue", CatalogueFileName));
        candidates.Add(Path.Combine(dataDirectory, "models-dev", CatalogueFileName));
        candidates.Add(@"D:\AI-ECOSYSTEM\models-dev\data\api.json");

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return candidates[0];
    }

    /// <summary>
    /// Normalises an absolute path and rejects paths rooted on C:.
    /// </summary>
    public static bool TryNormalizeNonC(string? path, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "the path is empty";
            return false;
        }

        if (!Path.IsPathRooted(path))
        {
            error = "the path is not absolute";
            return false;
        }

        try
        {
            normalized = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        string root = Path.GetPathRoot(normalized) ?? string.Empty;
        if (root.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
        {
            error = "the C: drive is not permitted";
            return false;
        }

        return true;
    }

    private static string CombineWarnings(string? first, string second) =>
        string.IsNullOrWhiteSpace(first) ? second : $"{first} {second}";
}
