using ReactiveUI;
using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using System.Collections.ObjectModel;
using System.Reactive;
using Microsoft.Extensions.Logging;

namespace BusOps.ViewModels;

public class AddConnectionDialogViewModel : ReactiveObject
{
    private readonly IServiceBusConnectionService _connectionService;
    private readonly ILogger<AddConnectionDialogViewModel>? _logger;
    
    private ObservableCollection<DiscoveredServiceBusNamespace> _discoveredNamespaces = new();
    private DiscoveredServiceBusNamespace? _selectedNamespace;
    private bool _isDiscovering;
    private string _errorMessage = string.Empty;
    private string _connectionName = string.Empty;
    private string _description = string.Empty;
    private string _customConnectionString = string.Empty;
    private bool _useCustomConnectionString;
    
    public AddConnectionDialogViewModel(IServiceBusConnectionService connectionService, ILogger<AddConnectionDialogViewModel>? logger = null, bool startDiscovery = true)
    {
        _connectionService = connectionService;
        _logger = logger;
        
        // Create commands
        DiscoverNamespacesCommand = ReactiveCommand.CreateFromTask(DiscoverNamespacesAsync);
        
        var canAdd = this.WhenAnyValue(
            x => x.SelectedNamespace,
            x => x.ConnectionName,
            x => x.UseCustomConnectionString,
            x => x.CustomConnectionString,
            (selected, name, useCustom, customConn) => 
                !string.IsNullOrWhiteSpace(name) && 
                (useCustom ? !string.IsNullOrWhiteSpace(customConn) : selected != null));
        
        AddCommand = ReactiveCommand.Create(() => { }, canAdd);
        CancelCommand = ReactiveCommand.Create(() => { });

        if (startDiscovery)
        {
            // Start discovery automatically
            DiscoverNamespacesCommand.Execute().Subscribe();
        }
    }

    public ObservableCollection<DiscoveredServiceBusNamespace> DiscoveredNamespaces
    {
        get => _discoveredNamespaces;
        set => this.RaiseAndSetIfChanged(ref _discoveredNamespaces, value);
    }

    public DiscoveredServiceBusNamespace? SelectedNamespace
    {
        get => _selectedNamespace;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedNamespace, value);
            
            // Auto-populate connection name if empty
            if (value != null && string.IsNullOrWhiteSpace(ConnectionName))
            {
                ConnectionName = value.Name;
            }
        }
    }

    public bool IsDiscovering
    {
        get => _isDiscovering;
        set => this.RaiseAndSetIfChanged(ref _isDiscovering, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public string ConnectionName
    {
        get => _connectionName;
        set => this.RaiseAndSetIfChanged(ref _connectionName, value);
    }

    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    public string CustomConnectionString
    {
        get => _customConnectionString;
        set => this.RaiseAndSetIfChanged(ref _customConnectionString, value);
    }

    public bool UseCustomConnectionString
    {
        get => _useCustomConnectionString;
        set => this.RaiseAndSetIfChanged(ref _useCustomConnectionString, value);
    }

    public ReactiveCommand<Unit, Unit> DiscoverNamespacesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public ServiceBusConnection? CreatedConnection { get; private set; }

    private async Task DiscoverNamespacesAsync()
    {
        try
        {
            IsDiscovering = true;
            ErrorMessage = string.Empty;
            DiscoveredNamespaces.Clear();

            _logger?.LogInformation("Starting namespace discovery...");
            
            var namespaces = await _connectionService.DiscoverNamespacesAsync();
            
            foreach (var ns in namespaces)
            {
                DiscoveredNamespaces.Add(ns);
            }

            if (DiscoveredNamespaces.Count == 0)
            {
                ErrorMessage = "No Service Bus namespaces found. Make sure you're authenticated with Azure CLI or have appropriate credentials configured.";
            }
            else
            {
                _logger?.LogInformation("Discovered {Count} namespaces", DiscoveredNamespaces.Count);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to discover namespaces: {ex.Message}";
            _logger?.LogError(ex, "Failed to discover namespaces");
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    public async Task<bool> SaveConnectionAsync()
    {
        try
        {
            string connectionString;
            
            if (UseCustomConnectionString)
            {
                connectionString = CustomConnectionString;
            }
            else if (SelectedNamespace != null)
            {
                // For Azure AD authentication, we'll store the fully qualified namespace
                connectionString = SelectedNamespace.FullyQualifiedNamespace;
            }
            else
            {
                ErrorMessage = "Please select a namespace or provide a connection string";
                return false;
            }

            var connection = new ServiceBusConnection
            {
                Name = ConnectionName,
                Description = Description,
                ConnectionString = connectionString,
                IsActive = true
            };

            CreatedConnection = await _connectionService.SaveConnectionAsync(connection);
            _logger?.LogInformation("Connection saved: {ConnectionName}", ConnectionName);
            
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save connection: {ex.Message}";
            _logger?.LogError(ex, "Failed to save connection");
            return false;
        }
    }
}
