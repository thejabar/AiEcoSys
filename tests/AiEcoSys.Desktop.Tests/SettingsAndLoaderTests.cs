using System;
using System.IO;
using AiEcoSys.Core;
using AiEcoSys.Core.DataLoaders;

namespace AiEcoSys.Desktop.Tests;

/// <summary>
/// Validation tests for the settings store and the Models.dev api.json loader.
/// </summary>
public class SettingsAndLoaderTests
{
    private static string TempRoot =>
        Path.Combine(@"D:\AiEcoSys\tests\.temp", Guid.NewGuid().ToString("N"));

    [Fact]
    public void DataLocation_Must_Be_Absolute_And_Not_On_C_Drive()
    {
        Assert.False(AppSettingsStore.TryValidateDataLocation(string.Empty, out _));
        Assert.False(AppSettingsStore.TryValidateDataLocation(@"relative\data", out _));
        Assert.False(AppSettingsStore.TryValidateDataLocation(@"C:\AiEcoSys", out _));
        Assert.True(AppSettingsStore.TryValidateDataLocation(TempRoot, out _));
    }

    [Fact]
    public void SnapshotLocation_Must_Be_Inside_The_Data_Directory()
    {
        string valid = Path.Combine(AppPaths.DataDirectory, "snapshots");
        string outside = Path.Combine(
            Path.GetDirectoryName(AppPaths.DataDirectory) ?? AppPaths.DataDirectory,
            "outside-snapshots");

        Assert.False(AppSettingsStore.TryValidateSnapshotLocation(outside, out _));
        Assert.False(AppSettingsStore.TryValidateSnapshotLocation(@"C:\AiEcoSys\snapshots", out _));
        Assert.True(AppSettingsStore.TryValidateSnapshotLocation(valid, out _));
    }

    [Fact]
    public void Portable_Path_Resolution_Rejects_C_And_Uses_Local_Data()
    {
        string baseDirectory = @"D:\PortableTest";
        string resolved = AppPaths.ResolveDataDirectory(baseDirectory, null, null, out string? warning);

        Assert.Equal(Path.Combine(baseDirectory, "Data"), resolved);
        Assert.Null(warning);

        string rejected = AppPaths.ResolveDataDirectory(
            baseDirectory,
            @"C:\Users\Test\AiEcoSys",
            null,
            out string? rejectedWarning);

        Assert.Equal(Path.Combine(baseDirectory, "Data"), rejected);
        Assert.Contains("C:", rejectedWarning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalogue_Resolution_Prefers_Bundled_File()
    {
        string baseDirectory = TempRoot;
        string dataDirectory = Path.Combine(baseDirectory, "Data");
        string catalogueDirectory = Path.Combine(baseDirectory, "catalogue");
        Directory.CreateDirectory(catalogueDirectory);
        string bundled = Path.Combine(catalogueDirectory, "api.json");
        File.WriteAllText(bundled, "{}");

        string resolved = AppPaths.ResolveCataloguePath(baseDirectory, dataDirectory, null);

        Assert.Equal(Path.GetFullPath(bundled), Path.GetFullPath(resolved));
    }

    [Fact]
    public void Endpoint_Validation_Requires_Https_For_Remote_Hosts()
    {
        Assert.True(AppSettingsStore.TryValidateEndpoint(null, out _));
        Assert.True(AppSettingsStore.TryValidateEndpoint(string.Empty, out _));
        Assert.False(AppSettingsStore.TryValidateEndpoint("not a uri", out _));
        Assert.False(AppSettingsStore.TryValidateEndpoint("http://example.com", out _));
        Assert.True(AppSettingsStore.TryValidateEndpoint("https://example.com", out _));
        Assert.True(AppSettingsStore.TryValidateEndpoint("http://localhost:11434", out _));
    }

    [Fact]
    public void ApiLoader_Reads_Real_Values_And_Does_Not_Invent_Costs()
    {
        string directory = TempRoot;
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "api.json");

        File.WriteAllText(path, """
        {
          "alpha": {
            "id": "alpha",
            "name": "Alpha Provider",
            "api": "https://api.alpha.example/v1",
            "doc": "https://docs.alpha.example",
            "env": ["ALPHA_API_KEY"],
            "models": {
              "alpha-1": {
                "id": "alpha-1",
                "name": "Alpha One",
                "description": "Fixture model with complete metadata.",
                "tool_call": true,
                "modalities": { "input": ["text", "image"], "output": ["text"] },
                "limit": { "context": 200000, "output": 8192 },
                "cost": { "input": 0.5, "output": 1.5 }
              },
              "alpha-2": {
                "id": "alpha-2",
                "name": "Alpha Two"
              }
            }
          }
        }
        """);

        var registry = new CentralRegistry();
        bool loaded = ModelsDevApiLoader.TryLoad(path, registry, out string message);

        Assert.True(loaded, message);
        Assert.Single(registry.Providers);
        Assert.Equal(2, registry.Models.Count);

        var complete = registry.GetModel("alpha/alpha-1");
        Assert.NotNull(complete);
        Assert.Equal("Alpha Provider", complete!.ProviderName);
        Assert.Equal(200000, complete.ContextWindow);
        Assert.True(complete.ToolCalling);
        Assert.Equal(0.5m, complete.InputTokenPrice);
        Assert.Equal(1.5m, complete.OutputTokenPrice);
        Assert.Equal("USD", complete.PricingCurrency);
        Assert.NotNull(complete.InputModalities);
        Assert.Equal(new[] { "text", "image" }, complete.InputModalities!);

        var minimal = registry.GetModel("alpha/alpha-2");
        Assert.NotNull(minimal);
        Assert.Null(minimal!.InputTokenPrice);
        Assert.Null(minimal.OutputTokenPrice);
        Assert.Null(minimal.PricingCurrency);
        Assert.Null(minimal.ContextWindow);
        Assert.Null(minimal.ToolCalling);
    }

    [Fact]
    public void ApiLoader_Reports_Missing_File()
    {
        var registry = new CentralRegistry();
        bool loaded = ModelsDevApiLoader.TryLoad(
            @"D:\AiEcoSys\tests\.temp\does-not-exist.json", registry, out string message);

        Assert.False(loaded);
        Assert.Contains("not found", message, StringComparison.OrdinalIgnoreCase);
    }
}
