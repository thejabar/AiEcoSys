using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AiEcoSys.Core;
using AiEcoSys.Security;

namespace AiEcoSys.Desktop;

/// <summary>
/// Represents a navigation menu item.
/// </summary>
public class NavMenuItem
{
    /// <summary>Gets or sets the display content (icon + label).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the command to execute when selected.</summary>
    public ICommand Command { get; set; } = null!;

    /// <summary>Gets or sets the associated view key for routing.</summary>
    public string? ViewKey { get; set; }
}

/// <summary>A single model row prepared for display.</summary>
public sealed class ModelRow
{
    /// <summary>Gets the registry identifier of the model.</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Gets the display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the provider display name.</summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>Gets the description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the formatted context window.</summary>
    public string ContextDisplay { get; init; } = "—";

    /// <summary>Gets the formatted input price.</summary>
    public string InputPriceDisplay { get; init; } = "—";

    /// <summary>Gets the formatted output price.</summary>
    public string OutputPriceDisplay { get; init; } = "—";

    /// <summary>Gets the tool-calling display value.</summary>
    public string ToolCallingDisplay { get; init; } = "—";

    /// <summary>Gets the input modalities display value.</summary>
    public string ModalitiesDisplay { get; init; } = "—";
}

/// <summary>A single pricing row prepared for display.</summary>
public sealed class PricingRow
{
    /// <summary>Gets the model display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the provider display name.</summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>Gets the formatted input price.</summary>
    public string InputPriceDisplay { get; init; } = "—";

    /// <summary>Gets the formatted output price.</summary>
    public string OutputPriceDisplay { get; init; } = "—";

    /// <summary>Gets the price status label.</summary>
    public string PriceStatus { get; init; } = string.Empty;

    /// <summary>Gets the price status colour.</summary>
    public string PriceStatusColor { get; init; } = "#9E9E9E";
}

/// <summary>A single provider row prepared for display.</summary>
public sealed class ProviderRow
{
    /// <summary>Gets the provider identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the provider display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the API endpoint, or a dash when absent.</summary>
    public string Api { get; init; } = "—";

    /// <summary>Gets the documentation URL, or a dash when absent.</summary>
    public string Doc { get; init; } = "—";

    /// <summary>Gets the number of models known for this provider.</summary>
    public int ModelCount { get; init; }

    /// <summary>Gets the credential status label.</summary>
    public string CredentialStatus { get; init; } = "No credential stored";

    /// <summary>Gets the credential status colour.</summary>
    public string CredentialStatusColor { get; init; } = "#9E9E9E";
}

/// <summary>A single catalogue record prepared for display.</summary>
public sealed class CatalogueRecord
{
    /// <summary>Gets the record type ("Provider" or "Model").</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Gets the record display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the provider name or identifier.</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Gets the data source description.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Gets the catalogue state description.</summary>
    public string State { get; init; } = string.Empty;
}

/// <summary>
/// Main view model for the AiEcoSys application shell.
/// Handles navigation between views and provides catalogue-backed data
/// for every view.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly ICentralRegistry _registry;
    private readonly AppSettings _settings;
    private object? _currentView;

    private string _statusText = "Offline";
    private string _lastSyncText = "—";
    private bool _isRefreshing;
    private string? _apiStatusText = "API: offline";
    private string _dataSourceText = "No catalogue loaded";

    private string _searchQuery = string.Empty;
    private int _modalitySelectedIndex;
    private int _contextSelectedIndex;

    private string _catalogueSearchQuery = string.Empty;
    private string _snapshotStatusText = "No snapshot saved yet.";

    private int _taskSelectedIndex;
    private int _providerSelectedIndex;
    private int _modalityFilterSelectedIndex;
    private int _contextFilterSelectedIndex;
    private int _minInputPriceSelectedIndex;
    private int _minOutputPriceSelectedIndex;

    private int _priceModeSelectedIndex;

    private string _testConnectionStatus = string.Empty;
    private string _credentialTarget = string.Empty;
    private string _credentialStatusText = "No credential configured.";

    private string _dataLocationStatusText;
    private string _snapshotLocationStatusText;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="registry">The central registry providing catalogue data.</param>
    public MainViewModel(ICentralRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _settings = AppSettingsStore.Load();

        _dataLocationStatusText = $"Using: {_settings.DataLocation}";
        _snapshotLocationStatusText = $"Using: {_settings.SnapshotLocation}";

        _registry.StatusChanged += OnStatusChanged;
        _registry.SyncHistoryChanged += OnSyncHistoryChanged;

        NavCommand = new DelegateCommand(NavCommand_Execute);
        RefreshFromSnapshot = new DelegateCommand(RefreshFromSnapshot_Execute);
        RefreshOnline = new DelegateCommand(RefreshOnline_Execute);
        SaveSnapshotCommand = new DelegateCommand(SaveSnapshot_Execute);
        TestConnectionCommand = new DelegateCommand(TestConnection_Execute);
        SaveIntegrationsCommand = new DelegateCommand(SaveIntegrations_Execute);
        TestIntegrationCommand = new DelegateCommand(TestIntegration_Execute);
        SaveDataLocationCommand = new DelegateCommand(SaveDataLocation_Execute);
        SaveSnapshotLocationCommand = new DelegateCommand(SaveSnapshotLocation_Execute);
        DeleteCredentialCommand = new DelegateCommand(DeleteCredential_Execute);
        OpenProjectLinkCommand = new DelegateCommand(OpenProjectLink_Execute);
        OpenDocumentationCommand = new DelegateCommand(OpenDocumentation_Execute);

        MenuItems = new List<NavMenuItem>
        {
            new() { Label = "🏠 Dashboard", ViewKey = "Dashboard", Command = NavCommand },
            new() { Label = "🔍 Model Explorer", ViewKey = "ModelExplorer", Command = NavCommand },
            new() { Label = "👥 Provider Manager", ViewKey = "ProviderManager", Command = NavCommand },
            new() { Label = "💰 Pricing", ViewKey = "Pricing", Command = NavCommand },
            new() { Label = "🧠 Model Selection", ViewKey = "ModelSelection", Command = NavCommand },
            new() { Label = "📦 Catalogue", ViewKey = "Catalogue", Command = NavCommand },
            new() { Label = "🔗 Integrations", ViewKey = "Integrations", Command = NavCommand },
            new() { Label = "🔒 Security", ViewKey = "Security", Command = NavCommand },
            new() { Label = "⚙️ Settings", ViewKey = "Settings", Command = NavCommand },
            new() { Label = "ℹ️ About", ViewKey = "About", Command = NavCommand }
        };

        RefreshIntegrationStatuses();
        RefreshSecurityStatus();
        CurrentView = new Views.DashboardView();
        ApplyStatus(_registry.Status);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    // ---------------------------------------------------------------------
    // Shell / navigation
    // ---------------------------------------------------------------------

    /// <summary>Gets or sets the current displayed view.</summary>
    public object? CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the status text displayed in the status bar.</summary>
    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the last synchronization timestamp text.</summary>
    public string LastSyncText
    {
        get => _lastSyncText;
        set
        {
            _lastSyncText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the API status text.</summary>
    public string ApiStatusText
    {
        get => _apiStatusText ?? "API: offline";
        set
        {
            _apiStatusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the catalogue data source description.</summary>
    public string DataSourceText
    {
        get => _dataSourceText;
        private set
        {
            _dataSourceText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the number of providers in the registry.</summary>
    public int RegistryProviderCount => _registry.Providers.Count;

    /// <summary>Gets the number of models in the registry.</summary>
    public int RegistryModelCount => _registry.Models.Count;

    /// <summary>Gets the last synchronisation timestamp.</summary>
    public DateTime? RegistryLastSync => _registry.Status.LastSync;

    /// <summary>Gets the last synchronisation time string.</summary>
    public string RegistryLastSyncTime => _registry.Status.LastSync?.ToString("G") ?? "—";

    /// <summary>Gets the data timestamp string of the current catalogue.</summary>
    public string DataTimestampText => _registry.Status.DataTimestamp?.ToString("G") ?? "—";

    /// <summary>Gets a value indicating whether the application is refreshing.</summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            _isRefreshing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotRefreshing));
        }
    }

    /// <summary>Gets a value indicating whether the application is not refreshing.</summary>
    public bool IsNotRefreshing => !_isRefreshing;

    /// <summary>Gets the recent synchronisation history.</summary>
    public IReadOnlyList<string> RecentSyncHistory => _registry.SyncHistory;

    /// <summary>Gets the command to refresh the catalogue from the local snapshot.</summary>
    public ICommand RefreshFromSnapshot { get; }

    /// <summary>Gets the command to refresh the catalogue online.</summary>
    public ICommand RefreshOnline { get; }

    /// <summary>Gets the command to save the current catalogue as a snapshot.</summary>
    public ICommand SaveSnapshotCommand { get; }

    /// <summary>Gets the navigation command for menu items.</summary>
    public ICommand NavCommand { get; }

    /// <summary>Gets the list of available navigation sections.</summary>
    public List<NavMenuItem> MenuItems { get; }

    // ---------------------------------------------------------------------
    // Model Explorer
    // ---------------------------------------------------------------------

    /// <summary>Gets or sets the search query for model filtering.</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            _searchQuery = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredModels));
            OnPropertyChanged(nameof(ModelSummaryText));
        }
    }

    /// <summary>Gets or sets the selected modality index.</summary>
    public int ModalitySelectedIndex
    {
        get => _modalitySelectedIndex;
        set
        {
            _modalitySelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredModels));
            OnPropertyChanged(nameof(ModelSummaryText));
        }
    }

    /// <summary>Gets the modality filter items.</summary>
    public ObservableCollection<string> ModalityItems { get; } = new()
    {
        "All modalities",
        "Text input",
        "Image input",
        "PDF input",
        "Audio input"
    };

    /// <summary>Gets or sets the selected context window limit index.</summary>
    public int ContextSelectedIndex
    {
        get => _contextSelectedIndex;
        set
        {
            _contextSelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredModels));
            OnPropertyChanged(nameof(ModelSummaryText));
        }
    }

    /// <summary>Gets the context window filter items.</summary>
    public ObservableCollection<string> ContextItems { get; } = new()
    {
        "Any context",
        "≤ 32k tokens",
        "≤ 128k tokens",
        "≤ 1M tokens"
    };

    /// <summary>Gets the filtered model list for the Model Explorer.</summary>
    public ObservableCollection<ModelRow> FilteredModels =>
        new(BuildModelRows(_registry.Models, applyExplorerFilters: true));

    /// <summary>Gets a summary of the current model filter.</summary>
    public string ModelSummaryText =>
        $"{FilteredModels.Count} of {_registry.Models.Count} models shown.";

    // ---------------------------------------------------------------------
    // Catalogue
    // ---------------------------------------------------------------------

    /// <summary>Gets or sets the catalogue search query.</summary>
    public string CatalogueSearchQuery
    {
        get => _catalogueSearchQuery;
        set
        {
            _catalogueSearchQuery = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredCatalogueRecords));
        }
    }

    /// <summary>Gets all catalogue records (providers and models).</summary>
    public ObservableCollection<CatalogueRecord> CatalogueRecords => new(BuildCatalogueRecords());

    /// <summary>Gets the catalogue records filtered by the search query.</summary>
    public ObservableCollection<CatalogueRecord> FilteredCatalogueRecords
    {
        get
        {
            var records = BuildCatalogueRecords();
            string query = _catalogueSearchQuery.Trim();
            if (query.Length == 0)
                return new ObservableCollection<CatalogueRecord>(records);

            return new ObservableCollection<CatalogueRecord>(records.Where(record =>
                record.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                record.Provider.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                record.Type.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }
    }

    /// <summary>Gets or sets the snapshot operation status text.</summary>
    public string SnapshotStatusText
    {
        get => _snapshotStatusText;
        private set
        {
            _snapshotStatusText = value;
            OnPropertyChanged();
        }
    }

    // ---------------------------------------------------------------------
    // Integrations
    // ---------------------------------------------------------------------

    private bool _academyOSEnabled;
    private string? _academyOSEndpoint;
    private bool _ubosEnabled;
    private string? _ubosEndpoint;
    private bool _mavenMeeMEnabled;
    private string? _mavenMeeMEndpoint;

    /// <summary>Gets or sets a value indicating whether the AcademyOS integration is enabled.</summary>
    public bool AcademyOSEnabled
    {
        get => _academyOSEnabled;
        set
        {
            _academyOSEnabled = value;
            OnPropertyChanged();
            RefreshIntegrationStatuses();
        }
    }

    /// <summary>Gets or sets the AcademyOS endpoint.</summary>
    public string? AcademyOSEndpoint
    {
        get => _academyOSEndpoint;
        set
        {
            _academyOSEndpoint = value;
            OnPropertyChanged();
            RefreshIntegrationStatuses();
        }
    }

    /// <summary>Gets the AcademyOS status text.</summary>
    public string AcademyOSStatusText => BuildIntegrationStatus(_academyOSEnabled, _academyOSEndpoint);

    /// <summary>Gets or sets a value indicating whether the UBOS integration is enabled.</summary>
    public bool UBOSEnabled
    {
        get => _ubosEnabled;
        set
        {
            _ubosEnabled = value;
            OnPropertyChanged();
            RefreshIntegrationStatuses();
        }
    }

    /// <summary>Gets or sets the UBOS endpoint.</summary>
    public string? UBOSEndpoint
    {
        get => _ubosEndpoint;
        set
        {
            _ubosEndpoint = value;
            OnPropertyChanged();
            RefreshIntegrationStatuses();
        }
    }

    /// <summary>Gets the UBOS status text.</summary>
    public string UBOSStatusText => BuildIntegrationStatus(_ubosEnabled, _ubosEndpoint);

    /// <summary>Gets or sets a value indicating whether the MavenMeeM integration is enabled.</summary>
    public bool MavenMeeMEnabled
    {
        get => _mavenMeeMEnabled;
        set
        {
            _mavenMeeMEnabled = value;
            OnPropertyChanged();
            RefreshIntegrationStatuses();
        }
    }

    /// <summary>Gets or sets the MavenMeeM endpoint.</summary>
    public string? MavenMeeMEndpoint
    {
        get => _mavenMeeMEndpoint;
        set
        {
            _mavenMeeMEndpoint = value;
            OnPropertyChanged();
            RefreshIntegrationStatuses();
        }
    }

    /// <summary>Gets the MavenMeeM status text.</summary>
    public string MavenMeeMStatusText => BuildIntegrationStatus(_mavenMeeMEnabled, _mavenMeeMEndpoint);

    /// <summary>Gets the command that validates and saves the integration settings.</summary>
    public ICommand SaveIntegrationsCommand { get; }

    /// <summary>Gets the command that tests an integration endpoint.</summary>
    public ICommand TestIntegrationCommand { get; }

    private string _integrationStatusText = "Integrations are opt-in. Nothing is enabled by default.";

    /// <summary>Gets the integration configuration status text.</summary>
    public string IntegrationStatusText
    {
        get => _integrationStatusText;
        private set
        {
            _integrationStatusText = value;
            OnPropertyChanged();
        }
    }

    // ---------------------------------------------------------------------
    // Model Selection
    // ---------------------------------------------------------------------

    /// <summary>Gets the task filter items.</summary>
    public ObservableCollection<string> TaskItems { get; } = new()
    {
        "Any task",
        "Tool calling",
        "Image input",
        "PDF input"
    };

    /// <summary>Gets or sets the selected task index.</summary>
    public int TaskSelectedIndex
    {
        get => _taskSelectedIndex;
        set
        {
            _taskSelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedModels));
            OnPropertyChanged(nameof(SelectionSummaryText));
        }
    }

    /// <summary>Gets the provider filter items.</summary>
    public ObservableCollection<string> ProviderItems
    {
        get
        {
            var items = new ObservableCollection<string> { "All providers" };
            foreach (var name in _registry.Providers
                         .Select(provider => provider.Name)
                         .Where(name => !string.IsNullOrWhiteSpace(name))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                items.Add(name);
            }

            return items;
        }
    }

    /// <summary>Gets or sets the selected provider index.</summary>
    public int ProviderSelectedIndex
    {
        get => _providerSelectedIndex;
        set
        {
            _providerSelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedModels));
            OnPropertyChanged(nameof(SelectionSummaryText));
        }
    }

    /// <summary>Gets the modality filter items for model selection.</summary>
    public ObservableCollection<string> ModalityFilterItems { get; } = new()
    {
        "Any modality",
        "Text input",
        "Image input",
        "PDF input",
        "Audio input"
    };

    /// <summary>Gets or sets the selected modality filter index.</summary>
    public int ModalityFilterSelectedIndex
    {
        get => _modalityFilterSelectedIndex;
        set
        {
            _modalityFilterSelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedModels));
            OnPropertyChanged(nameof(SelectionSummaryText));
        }
    }

    /// <summary>Gets the context filter items for model selection.</summary>
    public ObservableCollection<string> ContextFilterItems { get; } = new()
    {
        "Any context",
        "≥ 32k tokens",
        "≥ 128k tokens",
        "≥ 1M tokens"
    };

    /// <summary>Gets or sets the selected context filter index.</summary>
    public int ContextFilterSelectedIndex
    {
        get => _contextFilterSelectedIndex;
        set
        {
            _contextFilterSelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedModels));
            OnPropertyChanged(nameof(SelectionSummaryText));
        }
    }

    /// <summary>Gets the minimum input price filter items.</summary>
    public ObservableCollection<string> MinInputPriceItems { get; } = new()
    {
        "Any input price",
        "≥ $0.01",
        "≥ $0.10",
        "≥ $1.00",
        "≥ $10.00"
    };

    /// <summary>Gets or sets the minimum input price index.</summary>
    public int MinInputPriceSelectedIndex
    {
        get => _minInputPriceSelectedIndex;
        set
        {
            _minInputPriceSelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedModels));
            OnPropertyChanged(nameof(SelectionSummaryText));
            OnPropertyChanged(nameof(PricingRecords));
            OnPropertyChanged(nameof(PricingSummaryText));
        }
    }

    /// <summary>Gets the minimum output price filter items.</summary>
    public ObservableCollection<string> MinOutputPriceItems { get; } = new()
    {
        "Any output price",
        "≥ $0.01",
        "≥ $0.10",
        "≥ $1.00",
        "≥ $10.00"
    };

    /// <summary>Gets or sets the minimum output price index.</summary>
    public int MinOutputPriceSelectedIndex
    {
        get => _minOutputPriceSelectedIndex;
        set
        {
            _minOutputPriceSelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedModels));
            OnPropertyChanged(nameof(SelectionSummaryText));
            OnPropertyChanged(nameof(PricingRecords));
            OnPropertyChanged(nameof(PricingSummaryText));
        }
    }

    /// <summary>Gets the models matching the selection criteria.</summary>
    public ObservableCollection<ModelRow> SelectedModels =>
        new(BuildModelRows(_registry.Models, applySelectionFilters: true));

    /// <summary>Gets a summary of the current selection.</summary>
    public string SelectionSummaryText =>
        $"{SelectedModels.Count} models match the selected criteria.";

    // ---------------------------------------------------------------------
    // Pricing
    // ---------------------------------------------------------------------

    /// <summary>Gets the price mode filter items.</summary>
    public ObservableCollection<string> PriceModeItems { get; } = new()
    {
        "All models",
        "Missing prices",
        "Zero-cost",
        "Positive prices",
        "Partial prices"
    };

    /// <summary>Gets or sets the price mode index.</summary>
    public int PriceModeSelectedIndex
    {
        get => _priceModeSelectedIndex;
        set
        {
            _priceModeSelectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PricingRecords));
            OnPropertyChanged(nameof(PricingSummaryText));
        }
    }

    /// <summary>Gets the pricing rows. Costs are USD per one million tokens.</summary>
    public ObservableCollection<PricingRow> PricingRecords
    {
        get
        {
            var rows = new List<PricingRow>();
            decimal? minInput = GetMinPrice(MinInputPriceSelectedIndex);
            decimal? minOutput = GetMinPrice(MinOutputPriceSelectedIndex);

            foreach (var model in _registry.Models)
            {
                bool hasInput = model.InputTokenPrice.HasValue;
                bool hasOutput = model.OutputTokenPrice.HasValue;
                decimal input = model.InputTokenPrice ?? 0m;
                decimal output = model.OutputTokenPrice ?? 0m;

                if (minInput.HasValue && (!hasInput || input < minInput.Value))
                    continue;
                if (minOutput.HasValue && (!hasOutput || output < minOutput.Value))
                    continue;

                string status;
                string color;

                if (!hasInput && !hasOutput)
                {
                    status = "missing";
                    color = "#9E9E9E";
                }
                else if (hasInput && hasOutput && input == 0m && output == 0m)
                {
                    status = "zero-cost";
                    color = "#FFC107";
                }
                else if (hasInput && hasOutput)
                {
                    status = "positive";
                    color = "#4CAF50";
                }
                else
                {
                    status = hasInput ? "input only" : "output only";
                    color = "#FF9800";
                }

                bool include = _priceModeSelectedIndex switch
                {
                    1 => !hasInput && !hasOutput,
                    2 => hasInput && hasOutput && input == 0m && output == 0m,
                    3 => hasInput && hasOutput && (input > 0m || output > 0m),
                    4 => hasInput ^ hasOutput,
                    _ => true
                };

                if (!include)
                    continue;

                rows.Add(new PricingRow
                {
                    Name = model.Name,
                    ProviderName = model.ProviderName,
                    InputPriceDisplay = hasInput ? FormatPrice(input) : "—",
                    OutputPriceDisplay = hasOutput ? FormatPrice(output) : "—",
                    PriceStatus = status,
                    PriceStatusColor = color
                });
            }

            return new ObservableCollection<PricingRow>(rows);
        }
    }

    /// <summary>Gets a summary of the pricing filter.</summary>
    public string PricingSummaryText =>
        $"{PricingRecords.Count} of {_registry.Models.Count} models shown. Prices are USD per 1M tokens from Models.dev metadata.";

    // ---------------------------------------------------------------------
    // Provider Manager
    // ---------------------------------------------------------------------

    /// <summary>Gets the provider rows for the Provider Manager.</summary>
    public ObservableCollection<ProviderRow> Providers =>
        new(_registry.Providers
            .OrderBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase)
            .Select(provider =>
            {
                bool hasCredential = CredentialExists(provider.Id);
                return new ProviderRow
                {
                    Id = provider.Id,
                    Name = provider.Name,
                    Api = string.IsNullOrWhiteSpace(provider.Api) ? "—" : provider.Api!,
                    Doc = string.IsNullOrWhiteSpace(provider.Doc) ? "—" : provider.Doc!,
                    ModelCount = provider.Models.Count,
                    CredentialStatus = hasCredential ? "Credential stored" : "No credential stored",
                    CredentialStatusColor = hasCredential ? "#4CAF50" : "#9E9E9E"
                };
            }));

    /// <summary>Gets or sets the connection test status text.</summary>
    public string TestConnectionStatus
    {
        get => _testConnectionStatus;
        private set
        {
            _testConnectionStatus = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the command that tests a provider API endpoint.</summary>
    public ICommand TestConnectionCommand { get; }

    // ---------------------------------------------------------------------
    // Security
    // ---------------------------------------------------------------------

    /// <summary>Gets the credential storage status text.</summary>
    public string SecurityStatusText =>
        "Credentials are stored with the Windows Credential Manager for the current user.";

    /// <summary>Gets the transport security status text.</summary>
    public string HttpsStatusText =>
        "Remote endpoints require HTTPS. Localhost endpoints are allowed for local inference servers.";

    /// <summary>Gets the recent security/catalogue event text.</summary>
    public string SecurityEventsText
    {
        get
        {
            var entries = _registry.SyncHistory
                .Skip(Math.Max(0, _registry.SyncHistory.Count - 5))
                .Select(SecurityService.RedactCredentials)
                .ToList();

            return entries.Count == 0
                ? "No security events recorded."
                : string.Join(Environment.NewLine, entries);
        }
    }

    /// <summary>Gets or sets the credential target identifier.</summary>
    public string CredentialTarget
    {
        get => _credentialTarget;
        set
        {
            _credentialTarget = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the credential status text.</summary>
    public string CredentialStatusText
    {
        get => _credentialStatusText;
        private set
        {
            _credentialStatusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the command that deletes the credential for the current target.</summary>
    public ICommand DeleteCredentialCommand { get; }

    /// <summary>
    /// Stores a credential for the current target in the Windows Credential Manager.
    /// The value is never kept in the view model.
    /// </summary>
    /// <param name="value">The secret value to store.</param>
    public void StoreCredential(string value)
    {
        if (string.IsNullOrWhiteSpace(CredentialTarget))
        {
            CredentialStatusText = "Enter a credential target first (for example: openai).";
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            CredentialStatusText = "Enter a credential value to store.";
            return;
        }

        try
        {
            string target = BuildCredentialTarget(CredentialTarget);
            SecurityService.StoreCredential(target, value);
            CredentialStatusText = $"Credential stored for '{CredentialTarget}' in Windows Credential Manager.";
            OnPropertyChanged(nameof(Providers));
            RefreshSecurityStatus();
        }
        catch (Exception ex)
        {
            CredentialStatusText = $"Could not store credential: {SecurityService.RedactCredentials(ex.Message)}";
        }
    }

    // ---------------------------------------------------------------------
    // Settings
    // ---------------------------------------------------------------------

    /// <summary>Gets or sets the configured data location.</summary>
    public string DataLocation
    {
        get => _settings.DataLocation;
        set
        {
            _settings.DataLocation = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the configured snapshot location.</summary>
    public string SnapshotLocation
    {
        get => _settings.SnapshotLocation;
        set
        {
            _settings.SnapshotLocation = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets a value indicating whether anonymous telemetry is enabled.</summary>
    public bool EnableTelemetry
    {
        get => _settings.EnableTelemetry;
        set
        {
            _settings.EnableTelemetry = value;
            OnPropertyChanged();
            PersistBehaviourSettings();
        }
    }

    /// <summary>Gets or sets a value indicating whether offline mode is enabled.</summary>
    public bool OfflineMode
    {
        get => _settings.OfflineMode;
        set
        {
            _settings.OfflineMode = value;
            OnPropertyChanged();
            PersistBehaviourSettings();
        }
    }

    /// <summary>Gets or sets a value indicating whether the catalogue refreshes on launch.</summary>
    public bool AutoSyncOnLaunch
    {
        get => _settings.AutoSyncOnLaunch;
        set
        {
            _settings.AutoSyncOnLaunch = value;
            OnPropertyChanged();
            PersistBehaviourSettings();
        }
    }

    /// <summary>Gets the data location status text.</summary>
    public string DataLocationStatusText
    {
        get => _dataLocationStatusText;
        private set
        {
            _dataLocationStatusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the snapshot location status text.</summary>
    public string SnapshotLocationStatusText
    {
        get => _snapshotLocationStatusText;
        private set
        {
            _snapshotLocationStatusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the command that validates and saves the data location.</summary>
    public ICommand SaveDataLocationCommand { get; }

    /// <summary>Gets the command that validates and saves the snapshot location.</summary>
    public ICommand SaveSnapshotLocationCommand { get; }

    // ---------------------------------------------------------------------
    // About
    // ---------------------------------------------------------------------

    /// <summary>Gets the application version.</summary>
    public string AppVersion
    {
        get
        {
            var version = typeof(MainViewModel).Assembly.GetName().Version;
            return version == null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    /// <summary>Gets the build configuration recorded in the assembly metadata.</summary>
    public string BuildConfiguration
    {
        get
        {
            var attribute = typeof(MainViewModel).Assembly
                .GetCustomAttribute<System.Reflection.AssemblyConfigurationAttribute>();
            return string.IsNullOrWhiteSpace(attribute?.Configuration)
                ? "Unknown"
                : attribute!.Configuration;
        }
    }

    /// <summary>Gets the operating system and .NET runtime description.</summary>
    public string RuntimeDescription =>
        $"Windows {Environment.OSVersion.Version} / .NET {Environment.Version}";

    /// <summary>Gets the command that opens the project repository.</summary>
    public ICommand OpenProjectLinkCommand { get; }

    /// <summary>Gets the command that opens the project documentation.</summary>
    public ICommand OpenDocumentationCommand { get; }

    // ---------------------------------------------------------------------
    // Actions
    // ---------------------------------------------------------------------

    private void NavCommand_Execute(object? parameter)
    {
        if (parameter is not string key)
            return;

        CurrentView = key switch
        {
            "Dashboard" => new Views.DashboardView(),
            "ModelExplorer" => new Views.ModelExplorerView(),
            "ProviderManager" => new Views.ProviderManagerView(),
            "Pricing" => new Views.PricingView(),
            "ModelSelection" => new Views.ModelSelectionView(),
            "Catalogue" => new Views.CatalogueView(),
            "Integrations" => new Views.IntegrationsView(),
            "Security" => new Views.SecurityView(),
            "Settings" => new Views.SettingsView(),
            "About" => new Views.AboutView(),
            _ => CurrentView
        };
    }

    private void RefreshFromSnapshot_Execute(object? parameter)
    {
        string path = Path.Combine(SnapshotLocation, "latest.json");

        if (!File.Exists(path))
        {
            SnapshotStatusText = $"No snapshot found at {path}.";
            return;
        }

        var result = _registry.RefreshCatalogueFromSnapshot(path);
        SnapshotStatusText = result == SyncResult.Success
            ? $"Snapshot loaded: {_registry.Providers.Count} providers, {_registry.Models.Count} models."
            : $"Snapshot could not be loaded from {path}.";

        RefreshAllData();
    }

    private void SaveSnapshot_Execute(object? parameter)
    {
        try
        {
            Directory.CreateDirectory(SnapshotLocation);

            var snapshot = new
            {
                providers = _registry.Providers.ToDictionary(provider => provider.Id, provider => provider),
                models = _registry.Models.ToDictionary(model => model.Id, model => model)
            };

            string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            string path = Path.Combine(SnapshotLocation, "latest.json");
            File.WriteAllText(path, json);
            SnapshotStatusText = $"Snapshot saved: {path}.";
            _registry.RecordSyncEvent(SyncResult.Success, $"Snapshot saved to {path}");
        }
        catch (Exception ex)
        {
            SnapshotStatusText = $"Snapshot could not be saved: {ex.Message}";
        }
    }

    private async void RefreshOnline_Execute(object? parameter)
    {
        if (OfflineMode)
        {
            SnapshotStatusText = "Offline mode is enabled. Online refresh was skipped.";
            return;
        }

        IsRefreshing = true;
        try
        {
            var result = await _registry.RefreshCatalogueAsync("https://models.dev/api.json", 30);
            SnapshotStatusText = result == SyncResult.Success
                ? "Online catalogue refresh completed."
                : $"Online refresh result: {result}.";
        }
        catch (Exception ex)
        {
            SnapshotStatusText = $"Online refresh failed: {SecurityService.RedactCredentials(ex.Message)}";
        }
        finally
        {
            IsRefreshing = false;
            RefreshAllData();
        }
    }

    private async void TestConnection_Execute(object? parameter)
    {
        if (parameter is not string providerId)
            return;

        var provider = _registry.Providers.FirstOrDefault(item =>
            string.Equals(item.Id, providerId, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            TestConnectionStatus = "Provider not found.";
            return;
        }

        if (string.IsNullOrWhiteSpace(provider.Api))
        {
            TestConnectionStatus = $"{provider.Name}: no API endpoint in Models.dev metadata.";
            return;
        }

        if (!Uri.TryCreate(provider.Api, UriKind.Absolute, out var uri) || !SecurityService.ValidateHttps(uri))
        {
            TestConnectionStatus = $"{provider.Name}: endpoint rejected (HTTPS or localhost required).";
            return;
        }

        TestConnectionStatus = $"{provider.Name}: testing {uri.Host}…";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            using var response = await client.SendAsync(request);
            TestConnectionStatus = $"{provider.Name}: {uri.Host} responded {(int)response.StatusCode} ({response.StatusCode}).";
        }
        catch (Exception ex)
        {
            TestConnectionStatus = $"{provider.Name}: connection failed — {SecurityService.RedactCredentials(ex.Message)}";
        }
    }

    private void SaveIntegrations_Execute(object? parameter)
    {
        var settings = BuildSettings();
        settings.AcademyOSEnabled = AcademyOSEnabled;
        settings.AcademyOSEndpoint = AcademyOSEndpoint;
        settings.UBOSEnabled = UBOSEnabled;
        settings.UBOSEndpoint = UBOSEndpoint;
        settings.MavenMeeMEnabled = MavenMeeMEnabled;
        settings.MavenMeeMEndpoint = MavenMeeMEndpoint;

        IntegrationStatusText = AppSettingsStore.TrySave(settings, out string message)
            ? $"Integration settings saved. {message}"
            : message;

        RefreshIntegrationStatuses();
    }

    private async void TestIntegration_Execute(object? parameter)
    {
        if (parameter is not string integration)
            return;

        (string name, string? endpoint) = integration switch
        {
            "AcademyOS" => ("AcademyOS", AcademyOSEndpoint),
            "UBOS" => ("UBOS", UBOSEndpoint),
            "MavenMeeM" => ("MavenMeeM", MavenMeeMEndpoint),
            _ => (integration, null)
        };

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            IntegrationStatusText = $"{name}: no endpoint configured.";
            return;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme == Uri.UriSchemeHttp && !IsLocalHost(uri)))
        {
            IntegrationStatusText = $"{name}: endpoint rejected. Remote endpoints must use HTTPS.";
            return;
        }

        IntegrationStatusText = $"{name}: testing {uri.Host}…";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            using var response = await client.SendAsync(request);
            IntegrationStatusText = $"{name}: {uri.Host} responded {(int)response.StatusCode} ({response.StatusCode}).";
        }
        catch (Exception ex)
        {
            IntegrationStatusText = $"{name}: connection failed — {SecurityService.RedactCredentials(ex.Message)}";
        }
    }

    private void SaveDataLocation_Execute(object? parameter)
    {
        if (!AppSettingsStore.TryValidateDataLocation(DataLocation, out string message))
        {
            DataLocationStatusText = message;
            return;
        }

        if (!AppSettingsStore.TryValidateSnapshotLocation(SnapshotLocation, DataLocation, out _))
        {
            SnapshotLocation = Path.Combine(DataLocation, "snapshots");
            SnapshotLocationStatusText = $"Using: {SnapshotLocation}";
        }

        var settings = BuildSettings();
        if (!AppSettingsStore.TrySave(settings, out string saveMessage))
        {
            DataLocationStatusText = saveMessage;
            return;
        }

        DataLocationStatusText = $"Saved: {DataLocation}";
    }

    private void SaveSnapshotLocation_Execute(object? parameter)
    {
        var settings = BuildSettings();
        if (!AppSettingsStore.TryValidateSnapshotLocation(SnapshotLocation, DataLocation, out string message))
        {
            SnapshotLocationStatusText = message;
            return;
        }

        if (!AppSettingsStore.TrySave(settings, out string saveMessage))
        {
            SnapshotLocationStatusText = saveMessage;
            return;
        }

        SnapshotLocationStatusText = $"Saved: {SnapshotLocation}";
    }

    private void DeleteCredential_Execute(object? parameter)
    {
        if (string.IsNullOrWhiteSpace(CredentialTarget))
        {
            CredentialStatusText = "Enter a credential target first.";
            return;
        }

        try
        {
            SecurityService.DeleteCredential(BuildCredentialTarget(CredentialTarget));
            CredentialStatusText = $"Credential deleted for '{CredentialTarget}'.";
            OnPropertyChanged(nameof(Providers));
            RefreshSecurityStatus();
        }
        catch (Exception ex)
        {
            CredentialStatusText = $"Could not delete credential: {SecurityService.RedactCredentials(ex.Message)}";
        }
    }

    private void OpenProjectLink_Execute(object? parameter)
    {
        OpenExternalUrl("https://github.com/thejabar/AiEcoSys");
    }

    private void OpenDocumentation_Execute(object? parameter)
    {
        OpenExternalUrl("https://github.com/thejabar/AiEcoSys/tree/main/docs");
    }

    private static void OpenExternalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Opening a browser is best-effort; failures must not crash the app.
        }
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private IEnumerable<ModelRow> BuildModelRows(
        IReadOnlyList<ModelMetadata> models,
        bool applyExplorerFilters = false,
        bool applySelectionFilters = false)
    {
        string query = _searchQuery.Trim();
        string? modality = GetModalityFilter(applyExplorerFilters ? ModalitySelectedIndex : ModalityFilterSelectedIndex);
        int? maxContext = applyExplorerFilters ? GetMaxContext(ContextSelectedIndex) : null;
        int? minContext = applySelectionFilters ? GetMinContext(ContextFilterSelectedIndex) : null;
        decimal? minInput = applySelectionFilters ? GetMinPrice(MinInputPriceSelectedIndex) : null;
        decimal? minOutput = applySelectionFilters ? GetMinPrice(MinOutputPriceSelectedIndex) : null;
        string? providerName = applySelectionFilters && ProviderSelectedIndex > 0
            ? ProviderItems.ElementAtOrDefault(ProviderSelectedIndex)
            : null;

        foreach (var model in models)
        {
            if (query.Length > 0 &&
                !model.Name.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                !model.ProviderName.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                !(model.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                continue;
            }

            if (modality != null && !HasModality(model, modality))
                continue;

            if (maxContext.HasValue && (!model.ContextWindow.HasValue || model.ContextWindow.Value > maxContext.Value))
                continue;

            if (minContext.HasValue && (!model.ContextWindow.HasValue || model.ContextWindow.Value < minContext.Value))
                continue;

            if (minInput.HasValue && (!model.InputTokenPrice.HasValue || model.InputTokenPrice.Value < minInput.Value))
                continue;

            if (minOutput.HasValue && (!model.OutputTokenPrice.HasValue || model.OutputTokenPrice.Value < minOutput.Value))
                continue;

            if (providerName != null && !string.Equals(model.ProviderName, providerName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (applySelectionFilters && !MatchesTask(model))
                continue;

            yield return new ModelRow
            {
                ModelId = model.Id,
                Name = model.Name,
                ProviderName = model.ProviderName,
                Description = model.Description ?? string.Empty,
                ContextDisplay = model.ContextWindow.HasValue ? FormatTokens(model.ContextWindow.Value) : "—",
                InputPriceDisplay = model.InputTokenPrice.HasValue ? FormatPrice(model.InputTokenPrice.Value) : "—",
                OutputPriceDisplay = model.OutputTokenPrice.HasValue ? FormatPrice(model.OutputTokenPrice.Value) : "—",
                ToolCallingDisplay = model.ToolCalling.HasValue ? (model.ToolCalling.Value ? "Yes" : "No") : "—",
                ModalitiesDisplay = model.InputModalities is { Count: > 0 }
                    ? string.Join(", ", model.InputModalities)
                    : "—"
            };
        }
    }

    private bool MatchesTask(ModelMetadata model)
    {
        return _taskSelectedIndex switch
        {
            1 => model.ToolCalling == true,
            2 => HasModality(model, "image"),
            3 => HasModality(model, "pdf"),
            _ => true
        };
    }

    private static bool HasModality(ModelMetadata model, string modality)
    {
        if (model.InputModalities?.Any(value => string.Equals(value, modality, StringComparison.OrdinalIgnoreCase)) == true)
            return true;

        if (model.OutputModalities?.Any(value => string.Equals(value, modality, StringComparison.OrdinalIgnoreCase)) == true)
            return true;

        return model.Modality?.Contains(modality, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? GetModalityFilter(int index)
    {
        return index switch
        {
            1 => "text",
            2 => "image",
            3 => "pdf",
            4 => "audio",
            _ => null
        };
    }

    private static int? GetMaxContext(int index)
    {
        return index switch
        {
            1 => 32768,
            2 => 131072,
            3 => 1_048_576,
            _ => null
        };
    }

    private static int? GetMinContext(int index)
    {
        return index switch
        {
            1 => 32768,
            2 => 131072,
            3 => 1_048_576,
            _ => null
        };
    }

    private static decimal? GetMinPrice(int index)
    {
        return index switch
        {
            1 => 0.01m,
            2 => 0.10m,
            3 => 1.00m,
            4 => 10.00m,
            _ => null
        };
    }

    private static string FormatPrice(decimal value) =>
        "$" + value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string FormatTokens(int tokens)
    {
        if (tokens >= 1_000_000)
            return $"{tokens / 1_000_000d:0.#}M";
        if (tokens >= 1_000)
            return $"{tokens / 1_000d:0.#}k";
        return tokens.ToString(CultureInfo.InvariantCulture);
    }

    private List<CatalogueRecord> BuildCatalogueRecords()
    {
        string source = _registry.Status.IsOffline
            ? "Local Models.dev api.json (offline)"
            : "Models.dev API (online)";
        string state = _registry.Status.Status.ToString();

        var records = new List<CatalogueRecord>();

        foreach (var provider in _registry.Providers.OrderBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase))
        {
            records.Add(new CatalogueRecord
            {
                Type = "Provider",
                Name = provider.Name,
                Provider = provider.Id,
                Source = source,
                State = state
            });
        }

        foreach (var model in _registry.Models.OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase))
        {
            records.Add(new CatalogueRecord
            {
                Type = "Model",
                Name = model.Name,
                Provider = model.ProviderName,
                Source = source,
                State = state
            });
        }

        return records;
    }

    private void OnStatusChanged(object? sender, CatalogueStatusInfo status)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            dispatcher.Invoke(() => ApplyStatus(status));
        else
            ApplyStatus(status);
    }

    private void ApplyStatus(CatalogueStatusInfo status)
    {
        StatusText = status.Status.ToString();
        LastSyncText = status.LastSync?.ToString("G") ?? "—";
        ApiStatusText = status.IsOffline
            ? $"API: offline ({status.ProviderCount} providers)"
            : $"API: online ({status.ProviderCount} providers)";
        DataSourceText = status.IsOffline
            ? "Local Models.dev api.json (offline snapshot)"
            : "Models.dev API (online)";
        IsRefreshing = status.Status == CatalogueStatus.Synchronising;
        RefreshAllData();
    }

    private void OnSyncHistoryChanged(object? sender, EventArgs e)
    {
        void Apply()
        {
            OnPropertyChanged(nameof(RecentSyncHistory));
            OnPropertyChanged(nameof(SecurityEventsText));
        }

        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            dispatcher.Invoke(Apply);
        else
            Apply();
    }

    private void RefreshAllData()
    {
        OnPropertyChanged(nameof(RegistryProviderCount));
        OnPropertyChanged(nameof(RegistryModelCount));
        OnPropertyChanged(nameof(RegistryLastSync));
        OnPropertyChanged(nameof(RegistryLastSyncTime));
        OnPropertyChanged(nameof(DataTimestampText));
        OnPropertyChanged(nameof(FilteredModels));
        OnPropertyChanged(nameof(ModelSummaryText));
        OnPropertyChanged(nameof(CatalogueRecords));
        OnPropertyChanged(nameof(FilteredCatalogueRecords));
        OnPropertyChanged(nameof(SelectedModels));
        OnPropertyChanged(nameof(SelectionSummaryText));
        OnPropertyChanged(nameof(PricingRecords));
        OnPropertyChanged(nameof(PricingSummaryText));
        OnPropertyChanged(nameof(Providers));
        OnPropertyChanged(nameof(ProviderItems));
        OnPropertyChanged(nameof(SecurityEventsText));
    }

    private void RefreshIntegrationStatuses()
    {
        OnPropertyChanged(nameof(AcademyOSStatusText));
        OnPropertyChanged(nameof(UBOSStatusText));
        OnPropertyChanged(nameof(MavenMeeMStatusText));
    }

    private void RefreshSecurityStatus()
    {
        OnPropertyChanged(nameof(SecurityStatusText));
        OnPropertyChanged(nameof(HttpsStatusText));
        OnPropertyChanged(nameof(SecurityEventsText));
    }

    private static string BuildIntegrationStatus(bool enabled, string? endpoint)
    {
        if (!enabled)
            return "Disabled (opt-in required)";

        return string.IsNullOrWhiteSpace(endpoint)
            ? "Enabled — no endpoint configured"
            : $"Enabled — endpoint {endpoint}";
    }

    private void PersistBehaviourSettings()
    {
        var settings = BuildSettings();
        AppSettingsStore.TrySave(settings, out _);
    }

    private AppSettings BuildSettings() => new()
    {
        DataLocation = DataLocation,
        SnapshotLocation = SnapshotLocation,
        EnableTelemetry = EnableTelemetry,
        OfflineMode = OfflineMode,
        AutoSyncOnLaunch = AutoSyncOnLaunch,
        AcademyOSEnabled = AcademyOSEnabled,
        AcademyOSEndpoint = AcademyOSEndpoint,
        UBOSEnabled = UBOSEnabled,
        UBOSEndpoint = UBOSEndpoint,
        MavenMeeMEnabled = MavenMeeMEnabled,
        MavenMeeMEndpoint = MavenMeeMEndpoint
    };

    private static string BuildCredentialTarget(string target) => $"AiEcoSys:{target.Trim()}";

    private static bool CredentialExists(string providerId)
    {
        try
        {
            return SecurityService.RetrieveCredential(BuildCredentialTarget(providerId)) != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLocalHost(Uri uri) =>
        uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);

    /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
