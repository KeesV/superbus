using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using BusOps.Core.Interfaces;
using BusOps.Design;
using BusOps.Services;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace BusOps.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceBusConnectionService _connectionService;
    private readonly IServiceBusManagementService _managementService;
    private readonly IServiceBusMessageService _messageService;
    private readonly IErrorDialogService? _errorDialogService;
    private readonly ILogger<MainWindowViewModel>? _logger;
    public Window? ParentWindow { get; set; }

    private bool _isConnected;
    private string _statusText = "Ready";
    private string _connectionStatus = "No active connections";
    private EntityTreeItemViewModel? _selectedEntity;

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }
    
    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }
    
    public ObservableCollection<ConnectionItemViewModel> Connections { get; } = new();
    
    public ReactiveCommand<Unit, Unit> AddConnectionCommand { get; }
    
    // This will be set by the view
    public Func<Task>? ShowAddConnectionDialog { get; set; }

    public EntitiesTreeViewModel EntitiesTreeViewModel { get; protected set; }
    public EntityTreeItemViewModel? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            _logger?.LogInformation("Setting SelectedEntity to {EntityName} of type {EntityType}",
                value?.Name ?? "null", value?.Type ?? "null");
            this.RaiseAndSetIfChanged(ref _selectedEntity, value);
        }
    }
    
    public MessageManagementViewModel MessageManagementViewModel { get; protected set; }

    public MainWindowViewModel(
        IServiceBusConnectionService connectionService,
        IServiceBusManagementService managementService,
        IServiceBusMessageService messageService,
        IErrorDialogService? errorDialogService,
        ILogger<MainWindowViewModel>? logger,
        EntitiesTreeViewModel entitiesTreeViewModel,
        MessageManagementViewModel messageManagementViewModel)
    {
        _connectionService = connectionService;
        _managementService = managementService;
        _messageService = messageService;
        _errorDialogService = errorDialogService;
        _logger = logger;
        EntitiesTreeViewModel = entitiesTreeViewModel;
        MessageManagementViewModel = messageManagementViewModel;

        AddConnectionCommand = ReactiveCommand.CreateFromTask(OnAddConnectionAsync);
        
        // Load connections on initialization
        _ = LoadConnectionsAsync();

        this.WhenAnyValue(x => x.EntitiesTreeViewModel.SelectedEntity)
            .Subscribe(_ =>
            {
                SelectedEntity = EntitiesTreeViewModel.SelectedEntity;
                MessageManagementViewModel.SelectedEntity = SelectedEntity;
            });

        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            // Execute LoadEntitiesCommand when IsConnected changes to true
            this.WhenAnyValue(x => x.IsConnected)
                .Where(isConnected => isConnected)
                .Subscribe(_ => { EntitiesTreeViewModel.LoadEntitiesCommand.Execute().Subscribe(); });
        }
    }

    public MainWindowViewModel() : this(
        null!, 
        null!, 
        null!, 
        null!,
        null!, 
        new EntitiesTreeViewModel(),
        new MessageManagementViewModel(null!, null))
    {
        if(!Avalonia.Controls.Design.IsDesignMode)
            throw new NotSupportedException("This constructor is only for Design mode.");
        
        Connections.AddRange(DesignData.SampleConnections);

        IsConnected = true;
    }

    private async Task LoadConnectionsAsync()
    {
        try
        {
            _logger?.LogInformation("Loading connections...");
            var connections = await _connectionService.GetConnectionsAsync();
            
            Connections.Clear();
            foreach (var connection in connections)
            {
                var connectionViewModel = new ConnectionItemViewModel(connection, this);
                Connections.Add(connectionViewModel);
            }
            
            _logger?.LogInformation("Loaded {Count} connections", Connections.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load connections");
        }
    }

    private async Task OnAddConnectionAsync()
    {
        if (ShowAddConnectionDialog != null)
        {
            await ShowAddConnectionDialog();
            // Reload connections after adding a new one
            await LoadConnectionsAsync();
        }
    }

    public async Task ConnectAsync(string connectionString, string connectionName)
    {
        try
        {
            StatusText = "Connecting...";
            ConnectionStatus = $"Connecting to {connectionName}...";
            
            _logger?.LogInformation("Connecting to Service Bus: {ConnectionName}", connectionName);
            
            await _managementService.ConnectAsync(connectionString);
            
            // Initialize the message service with the same connection
            _messageService.Initialize(connectionString);
            
            StatusText = "Ready";
            ConnectionStatus = $"Connected to {connectionName}";
            IsConnected = true;
        } catch (Exception ex)
        {
            StatusText = "Connection failed";
            ConnectionStatus = "Connection failed";
            _logger?.LogError(ex, "Exception while connecting to Service Bus");
            
            // Show error dialog with exception details
            _errorDialogService?.ShowErrorDialog("Connection Error", ex, ParentWindow);
        }
    }
}