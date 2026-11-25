using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AiEcoSys.Core;
using AiEcoSys.Desktop.Views;

namespace AiEcoSys.Desktop.Tests;

/// <summary>
/// Verifies that every view parses and loads against the real application
/// resources, and that navigation wiring resolves to the correct view type.
/// </summary>
public class ViewLoadTests
{
    [Fact]
    public void All_Views_Load_And_Navigation_Resolves()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();

                var registry = new CentralRegistry();
                registry.LoadLocalCatalogue(
                    new List<ProviderModel>
                    {
                        new()
                        {
                            Id = "test-provider",
                            Name = "Test Provider",
                            Api = "https://api.example.com/v1",
                            Doc = "https://docs.example.com",
                            Models = new List<string> { "test-model" }
                        }
                    },
                    new List<ModelMetadata>
                    {
                        new()
                        {
                            Id = "test-provider/test-model",
                            Name = "Test Model",
                            ProviderId = "test-provider",
                            ProviderName = "Test Provider",
                            Description = "Synthetic fixture model used by tests.",
                            InputModalities = new List<string> { "text", "image" },
                            OutputModalities = new List<string> { "text" },
                            ContextWindow = 131072,
                            ToolCalling = true,
                            InputTokenPrice = 1.5m,
                            OutputTokenPrice = 3m,
                            PricingCurrency = "USD"
                        }
                    },
                    @"D:\AiEcoSys\tests\.temp\fixture.json");

                var viewModel = new MainViewModel(registry);

                var views = new UserControl[]
                {
                    new DashboardView(),
                    new ModelExplorerView(),
                    new ProviderManagerView(),
                    new PricingView(),
                    new ModelSelectionView(),
                    new CatalogueView(),
                    new IntegrationsView(),
                    new SecurityView(),
                    new SettingsView(),
                    new AboutView()
                };

                foreach (var view in views)
                {
                    Assert.NotNull(view.Content);
                    view.DataContext = viewModel;
                    view.Measure(new Size(1000, 700));
                    view.Arrange(new Rect(0, 0, 1000, 700));
                }

                Assert.Equal(1, viewModel.RegistryProviderCount);
                Assert.Equal(1, viewModel.RegistryModelCount);
                Assert.Single(viewModel.FilteredModels);
                Assert.Single(viewModel.SelectedModels);
                Assert.Single(viewModel.PricingRecords);
                Assert.Single(viewModel.Providers);

                // Model Explorer filters
                viewModel.SearchQuery = "missing-model";
                Assert.Empty(viewModel.FilteredModels);
                viewModel.SearchQuery = "Test Model";
                Assert.Single(viewModel.FilteredModels);
                viewModel.SearchQuery = string.Empty;

                viewModel.ModalitySelectedIndex = 2; // image input
                Assert.Single(viewModel.FilteredModels);
                viewModel.ModalitySelectedIndex = 3; // pdf input
                Assert.Empty(viewModel.FilteredModels);
                viewModel.ModalitySelectedIndex = 0;

                // Model Selection filters
                viewModel.TaskSelectedIndex = 1; // tool calling
                Assert.Single(viewModel.SelectedModels);
                viewModel.TaskSelectedIndex = 3; // pdf input
                Assert.Empty(viewModel.SelectedModels);
                viewModel.TaskSelectedIndex = 0;

                viewModel.ProviderSelectedIndex = 1; // the single test provider
                Assert.Single(viewModel.SelectedModels);
                viewModel.ProviderSelectedIndex = 0;

                viewModel.ContextFilterSelectedIndex = 3; // at least 1M tokens
                Assert.Empty(viewModel.SelectedModels);
                viewModel.ContextFilterSelectedIndex = 2; // at least 128k tokens
                Assert.Single(viewModel.SelectedModels);
                viewModel.ContextFilterSelectedIndex = 0;

                viewModel.MinInputPriceSelectedIndex = 4; // at least $10
                Assert.Empty(viewModel.SelectedModels);
                Assert.Empty(viewModel.PricingRecords);
                viewModel.MinInputPriceSelectedIndex = 0;

                // Pricing filters
                viewModel.PriceModeSelectedIndex = 1; // missing prices
                Assert.Empty(viewModel.PricingRecords);
                viewModel.PriceModeSelectedIndex = 3; // positive prices
                Assert.Single(viewModel.PricingRecords);
                viewModel.PriceModeSelectedIndex = 2; // zero-cost
                Assert.Empty(viewModel.PricingRecords);
                viewModel.PriceModeSelectedIndex = 0;
                Assert.Single(viewModel.PricingRecords);

                string[] navigationKeys =
                {
                    "Dashboard", "ModelExplorer", "ProviderManager", "Pricing",
                    "ModelSelection", "Catalogue", "Integrations", "Security",
                    "Settings", "About"
                };

                foreach (var key in navigationKeys)
                {
                    viewModel.NavCommand.Execute(key);
                    Assert.NotNull(viewModel.CurrentView);
                    var navigated = Assert.IsAssignableFrom<UserControl>(viewModel.CurrentView);
                    Assert.NotNull(navigated.Content);
                }

                viewModel.NavCommand.Execute("Pricing");
                Assert.IsType<PricingView>(viewModel.CurrentView);
                viewModel.NavCommand.Execute("About");
                Assert.IsType<AboutView>(viewModel.CurrentView);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }
}
