using System;
using System.Windows;
using AiEcoSys.Core;
using AiEcoSys.Core.DataLoaders;

namespace AiEcoSys.Desktop;

/// <summary>
/// Application entry point. Creates the catalogue registry, loads the local
/// Models.dev metadata (when available), and shows the main window with a
/// fully wired view model.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Creates the registry, loads local metadata, and displays the shell window.
    /// </summary>
    /// <param name="e">The startup arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var registry = new CentralRegistry();

        // Load the bundled Models.dev api.json catalogue. Resolution order is
        // documented in AppPaths: environment override, application directory,
        // portable data directory, then the legacy development location.
        ModelsDevApiLoader.TryLoad(AppPaths.CataloguePath, registry, out string loadMessage);
        if (registry.Models.Count == 0)
        {
            registry.RecordSyncEvent(
                SyncResult.Failed,
                string.IsNullOrWhiteSpace(loadMessage) ? "No local Models.dev metadata loaded." : loadMessage);
        }

        var viewModel = new MainViewModel(registry);
        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();

        // The persisted preference is honoured after the local catalogue is
        // available, so startup remains useful when the network is unavailable.
        if (viewModel.AutoSyncOnLaunch && !viewModel.OfflineMode)
        {
            viewModel.RefreshOnline.Execute(null);
        }
    }
}
