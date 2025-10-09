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
    private readonly IServiceBusManagementService _managementService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private string _greeting = "Welcome to BusOps!";
    private string _statusText = "Ready";
    private string _connectionStatus = "No active connections";

    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }

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

    public ObservableCollection<ConnectionItemViewModel> Connections { get; } = new();
    public ObservableCollection<EntityTreeItemViewModel> Entities { get; } = new();

    public ReactiveCommand<Unit, Unit> AddConnectionCommand { get; }
    
    // This will be set by the view
    public Func<Task>? ShowAddConnectionDialog { get; set; }

    public MainWindowViewModel(
        IServiceBusConnectionService connectionService,
        IServiceBusManagementService managementService,
        ILogger<MainWindowViewModel> logger)
    {
        _connectionService = connectionService;
        _managementService = managementService;
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
                var connectionViewModel = new ConnectionItemViewModel(connection, this);
                Connections.Add(connectionViewModel);
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

    public async Task LoadEntitiesAsync(string connectionString, string connectionName)
    {
        try
        {
            StatusText = "Connecting...";
            ConnectionStatus = $"Connecting to {connectionName}...";
            
            _logger.LogInformation("Connecting to Service Bus: {ConnectionName}", connectionName);
            
            var connected = await _managementService.ConnectAsync(connectionString);
            
            if (!connected)
            {
                StatusText = "Connection failed";
                ConnectionStatus = "Connection failed";
                _logger.LogError("Failed to connect to Service Bus");
                return;
            }

            StatusText = "Loading entities...";
            ConnectionStatus = $"Connected to {connectionName}";
            
            Entities.Clear();

            // Load queues
            _logger.LogInformation("Loading queues...");
            var queues = (await _managementService.GetQueuesAsync()).ToList();
            var queuesNode = new EntityTreeItemViewModel
            {
                Name = "Queues",
                Type = "Folder",
                IsExpanded = true
            };

            foreach (var queue in queues)
            {
                queuesNode.Children.Add(new EntityTreeItemViewModel
                {
                    Name = queue.Name,
                    Type = "Queue",
                    MessageCount = queue.MessageCount
                });
            }

            Entities.Add(queuesNode);

            // Load topics
            _logger.LogInformation("Loading topics...");
            var topics = (await _managementService.GetTopicsAsync()).ToList();
            var topicsNode = new EntityTreeItemViewModel
            {
                Name = "Topics",
                Type = "Folder",
                IsExpanded = true
            };

            foreach (var topic in topics)
            {
                var topicNode = new EntityTreeItemViewModel
                {
                    Name = topic.Name,
                    Type = "Topic",
                    IsExpanded = false
                };

                // Add subscriptions under each topic
                foreach (var subscription in topic.Subscriptions)
                {
                    topicNode.Children.Add(new EntityTreeItemViewModel
                    {
                        Name = subscription.Name,
                        Type = "Subscription",
                        MessageCount = subscription.MessageCount
                    });
                }

                topicsNode.Children.Add(topicNode);
            }

            Entities.Add(topicsNode);

            StatusText = "Ready";
            _logger.LogInformation("Successfully loaded {QueueCount} queues and {TopicCount} topics", 
                queues.Count, topics.Count);
        }
        catch (Exception ex)
        {
            StatusText = "Error loading entities";
            ConnectionStatus = "Error";
            _logger.LogError(ex, "Failed to load Service Bus entities");
        }
    }
}