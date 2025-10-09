using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace BusOps.ViewModels;

public class DesignMainWindowViewModel : MainWindowViewModel
{
    public DesignMainWindowViewModel() : base(null!, null!, null!, null!)
    {
        Connections.AddRange([
            new ConnectionItemViewModel(new ServiceBusConnection
            {
                CreatedAt = DateTimeOffset.Now,
                Description = "sb-013-hub-nonprod",
                Id = "sb-013-hub-nonprod",
                IsActive = true,
                LastConnected = DateTimeOffset.Now.AddHours(-1),
            }, this)
        ]);
        Entities.AddRange([
            new EntityTreeItemViewModel
            {
                Name = "Queues",
                Type = "Folder",
                MessageCount = 79,
            }
        ]);
        Entities[0].Children.AddRange([
            new EntityTreeItemViewModel
            {
                Name = "my-queue-1",
                Type = "Queue",
                MessageCount = 42,
            },
            new EntityTreeItemViewModel
            {
                Name = "my-queue-2",
                Type = "Queue",
                MessageCount = 0,
            },
            new EntityTreeItemViewModel
            {
                Name = "my-queue-3",
                Type = "Queue",
                MessageCount = 37,
            }
        ]);
    }
}

public class MainWindowViewModel : ReactiveObject
{
    private readonly IServiceBusConnectionService _connectionService;
    private readonly IServiceBusManagementService _managementService;
    private readonly IServiceBusMessageService _messageService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private string _greeting = "Welcome to BusOps!";
    private string _statusText = "Ready";
    private string _connectionStatus = "No active connections";
    private bool _hasEntities;
    private EntityTreeItemViewModel? _selectedEntity;
    private int _maxMessagesToShow = 100;
    private bool _isLoadingMessages;
    private string _currentConnectionString = string.Empty;
    private ServiceBusMessage? _selectedMessage;

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

    public bool HasEntities
    {
        get => _hasEntities;
        set => this.RaiseAndSetIfChanged(ref _hasEntities, value);
    }

    public EntityTreeItemViewModel? SelectedEntity
    {
        get => _selectedEntity;
        set => this.RaiseAndSetIfChanged(ref _selectedEntity, value);
    }

    public int MaxMessagesToShow
    {
        get => _maxMessagesToShow;
        set => this.RaiseAndSetIfChanged(ref _maxMessagesToShow, value);
    }

    public bool IsLoadingMessages
    {
        get => _isLoadingMessages;
        set => this.RaiseAndSetIfChanged(ref _isLoadingMessages, value);
    }

    public ServiceBusMessage? SelectedMessage
    {
        get => _selectedMessage;
        set => this.RaiseAndSetIfChanged(ref _selectedMessage, value);
    }

    public bool HasMessages => Messages.Count > 0;
    
    public bool ShowNoSelectionMessage => !IsLoadingMessages && SelectedEntity == null;
    
    public bool ShowNoMessagesForEntity => !IsLoadingMessages && SelectedEntity != null 
        && (SelectedEntity.Type == "Queue" || SelectedEntity.Type == "Subscription") 
        && !HasMessages;
    
    public bool ShowNonMessageableEntityMessage => !IsLoadingMessages && SelectedEntity != null 
        && SelectedEntity.Type != "Queue" && SelectedEntity.Type != "Subscription";

    public ObservableCollection<int> MessageLimitOptions { get; } = new()
    {
        10, 25, 50, 100, 250, 500, 1000
    };

    public ObservableCollection<ConnectionItemViewModel> Connections { get; } = new();
    public ObservableCollection<EntityTreeItemViewModel> Entities { get; } = new();
    public ObservableCollection<ServiceBusMessage> Messages { get; } = new();

    public ReactiveCommand<Unit, Unit> AddConnectionCommand { get; }
    
    // This will be set by the view
    public Func<Task>? ShowAddConnectionDialog { get; set; }
    public Func<string, Exception, Task>? ShowErrorDialog { get; set; }

    public MainWindowViewModel(
        IServiceBusConnectionService connectionService,
        IServiceBusManagementService managementService,
        IServiceBusMessageService messageService,
        ILogger<MainWindowViewModel> logger)
    {
        _connectionService = connectionService;
        _managementService = managementService;
        _messageService = messageService;
        _logger = logger;
        
        AddConnectionCommand = ReactiveCommand.CreateFromTask(OnAddConnectionAsync);
        
        // Load connections on initialization
        _ = LoadConnectionsAsync();
        
        // Notify when Messages collection changes
        Messages.CollectionChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(HasMessages));
            this.RaisePropertyChanged(nameof(ShowNoMessagesForEntity));
        };
        
        // Watch for changes to SelectedEntity and MaxMessagesToShow
        this.WhenAnyValue(x => x.SelectedEntity, x => x.MaxMessagesToShow)
            .Subscribe(tuple =>
            {
                this.RaisePropertyChanged(nameof(ShowNoSelectionMessage));
                this.RaisePropertyChanged(nameof(ShowNoMessagesForEntity));
                this.RaisePropertyChanged(nameof(ShowNonMessageableEntityMessage));
                _ = LoadMessagesForSelectedEntityAsync();
            });
            
        // Watch for loading state changes
        this.WhenAnyValue(x => x.IsLoadingMessages)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ShowNoSelectionMessage));
                this.RaisePropertyChanged(nameof(ShowNoMessagesForEntity));
                this.RaisePropertyChanged(nameof(ShowNonMessageableEntityMessage));
            });
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
            
            // Store the connection string for message operations
            _currentConnectionString = connectionString;
            
            await _managementService.ConnectAsync(connectionString);
            
            // Initialize the message service with the same connection
            _messageService.Initialize(connectionString);
        } catch (Exception ex)
        {
            StatusText = "Connection failed";
            ConnectionStatus = "Connection failed";
            _logger.LogError(ex, "Exception while connecting to Service Bus");
            
            // Show error dialog with exception details
            if (ShowErrorDialog != null)
            {
                await ShowErrorDialog("Connection Error", ex);
            }
            return;
        }
        
        try
        {
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
                MessageCount = queues.Count,
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
                MessageCount = topics.Count,
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

            HasEntities = Entities.Count > 0;

            StatusText = "Ready";
            _logger.LogInformation("Successfully loaded {QueueCount} queues and {TopicCount} topics", 
                queues.Count, topics.Count);
        }
        catch (Exception ex)
        {
            StatusText = "Error loading entities";
            ConnectionStatus = "Error";
            _logger.LogError(ex, "Failed to load Service Bus entities");
            
            // Show error dialog with exception details
            if (ShowErrorDialog != null)
            {
                await ShowErrorDialog("Failed to Load Entities", ex);
            }
        }
    }

    private async Task LoadMessagesForSelectedEntityAsync()
    {
        _logger.LogInformation("LoadMessagesForSelectedEntityAsync called. SelectedEntity: {EntityName}, Type: {EntityType}", 
            SelectedEntity?.Name ?? "null", SelectedEntity?.Type ?? "null");
            
        if (SelectedEntity == null || string.IsNullOrEmpty(_currentConnectionString))
        {
            _logger.LogInformation("Clearing messages - SelectedEntity is null or no connection string");
            Messages.Clear();
            return;
        }

        // Only load messages for Queue or Subscription entities, not folders or topics
        if (SelectedEntity.Type != "Queue" && SelectedEntity.Type != "Subscription")
        {
            _logger.LogInformation("Clearing messages - Selected entity type is {Type}, not a Queue or Subscription", SelectedEntity.Type);
            Messages.Clear();
            return;
        }

        try
        {
            IsLoadingMessages = true;
            StatusText = $"Loading messages from {SelectedEntity.Name}...";
            Messages.Clear();
            
            _logger.LogInformation("Messages collection cleared. About to fetch messages...");

            IEnumerable<ServiceBusMessage> messages;

            if (SelectedEntity.Type == "Queue")
            {
                _logger.LogInformation("Peeking {MaxMessages} messages from queue {QueueName}", 
                    MaxMessagesToShow, SelectedEntity.Name);
                messages = await _messageService.ReceiveMessagesAsync(
                    SelectedEntity.Name, 
                    MaxMessagesToShow, 
                    peekOnly: true);
            }
            else // Subscription
            {
                // Find the parent topic name
                var topicName = FindParentTopicName(SelectedEntity);
                if (string.IsNullOrEmpty(topicName))
                {
                    _logger.LogWarning("Could not find parent topic for subscription {SubscriptionName}", 
                        SelectedEntity.Name);
                    StatusText = "Error: Could not find parent topic";
                    return;
                }

                _logger.LogInformation("Peeking {MaxMessages} messages from subscription {TopicName}/{SubscriptionName}", 
                    MaxMessagesToShow, topicName, SelectedEntity.Name);
                messages = await _messageService.ReceiveSubscriptionMessagesAsync(
                    topicName, 
                    SelectedEntity.Name, 
                    MaxMessagesToShow, 
                    peekOnly: true);
            }

            var messagesList = messages.ToList();
            _logger.LogInformation("Received {Count} messages from service", messagesList.Count);
            
            foreach (var message in messagesList)
            {
                _logger.LogDebug("Adding message: ID={MessageId}, Seq={SequenceNumber}", 
                    message.MessageId, message.SequenceNumber);
                Messages.Add(message);
            }
            
            _logger.LogInformation("Messages collection now has {Count} items. HasMessages={HasMessages}", 
                Messages.Count, HasMessages);

            StatusText = $"Loaded {Messages.Count} messages";
            _logger.LogInformation("Successfully loaded {Count} messages", Messages.Count);
        }
        catch (Exception ex)
        {
            StatusText = "Error loading messages";
            _logger.LogError(ex, "Failed to load messages for {EntityType} {EntityName}", 
                SelectedEntity.Type, SelectedEntity.Name);
            
            if (ShowErrorDialog != null)
            {
                await ShowErrorDialog("Failed to Load Messages", ex);
            }
        }
        finally
        {
            IsLoadingMessages = false;
            _logger.LogInformation("IsLoadingMessages set to false. Final state - Messages.Count: {Count}, HasMessages: {HasMessages}, ShowNoMessagesForEntity: {ShowNoMessages}", 
                Messages.Count, HasMessages, ShowNoMessagesForEntity);
        }
    }

    private string? FindParentTopicName(EntityTreeItemViewModel subscriptionEntity)
    {
        // Find the Topics folder in the Entities tree
        var topicsFolder = Entities.FirstOrDefault(e => e.Type == "Folder" && e.Name == "Topics");
        if (topicsFolder == null)
            return null;

        // Search through all topics to find the one containing this subscription
        foreach (var topic in topicsFolder.Children)
        {
            if (topic.Children.Any(sub => sub.Name == subscriptionEntity.Name))
            {
                return topic.Name;
            }
        }

        return null;
    }
}