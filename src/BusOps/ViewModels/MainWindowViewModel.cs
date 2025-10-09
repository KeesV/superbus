using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using BusOps.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusOps.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly IServiceBusConnectionService _connectionService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private string _greeting = "Welcome to BusOps!";

    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }

    public ObservableCollection<ConnectionItemViewModel> Connections { get; } = new();

    public ReactiveCommand<Unit, Unit> AddConnectionCommand { get; }
    
    // This will be set by the view
    public Func<Task>? ShowAddConnectionDialog { get; set; }

    public MainWindowViewModel(
        IServiceBusConnectionService connectionService,
        ILogger<MainWindowViewModel> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
        
        AddConnectionCommand = ReactiveCommand.CreateFromTask(OnAddConnectionAsync);
        
        // Load connections on initialization
        _ = LoadConnectionsAsync();
    }

    private async Task LoadConnectionsAsync()
    {
        try
        {
            _logger.LogInformation("Loading connections...");
            var connections = await _connectionService.GetConnectionsAsync();
            
            Connections.Clear();
            foreach (var connection in connections)
            {
                Connections.Add(new ConnectionItemViewModel(connection));
            }
            
            _logger.LogInformation("Loaded {Count} connections", Connections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load connections");
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
}