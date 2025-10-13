using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using BusOps.Design;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace BusOps.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceBusConnectionService _connectionService;
    private readonly IServiceBusManagementService _managementService;
    private readonly IServiceBusMessageService _messageService;
    private readonly ILogger<MainWindowViewModel> _logger;

    private bool _isConnected;
    private string _statusText = "Ready";
    private string _connectionStatus = "No active connections";
    private EntityTreeItemViewModel? _selectedEntity;
    private int _maxMessagesToShow = 100;
    private bool _isLoadingMessages;
    private ServiceBusMessage? _selectedMessage;
    private bool? _selectAll = false;

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
    
    public bool HasSelectedMessage => SelectedMessage != null;

    public bool? SelectAll
    {
        get => _selectAll;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectAll, value);
            if (value.HasValue)
            {
                ToggleSelectAllMessages(value.Value);
            }
        }
    }

    public bool HasMessages => Messages.Count > 0;
    
    // public bool ShowNoSelectionMessage => !IsLoadingMessages && SelectedEntity == null;
    //
    // public bool ShowNoMessagesForEntity => !IsLoadingMessages && SelectedEntity != null 
    //     && (SelectedEntity.Type == "Queue" || SelectedEntity.Type == "Subscription") 
    //     && !HasMessages;
    //
    // public bool ShowNonMessageableEntityMessage => !IsLoadingMessages && SelectedEntity != null 
    //     && SelectedEntity.Type != "Queue" && SelectedEntity.Type != "Subscription";

    public ObservableCollection<int> MessageLimitOptions { get; } = new()
    {
        10, 25, 50, 100, 250, 500, 1000
    };

    public ObservableCollection<ConnectionItemViewModel> Connections { get; } = new();
    
    public ObservableCollection<ServiceBusMessage> Messages { get; } = new();

    public ReactiveCommand<Unit, Unit> AddConnectionCommand { get; }
    
    // This will be set by the view
    public Func<Task>? ShowAddConnectionDialog { get; set; }
    public Func<string, Exception, Task>? ShowErrorDialog { get; set; }

    public EntitiesTreeViewModel EntitiesTreeViewModel { get; protected set; }
    public EntityTreeItemViewModel? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            _logger.LogInformation("Setting SelectedEntity to {EntityName} of type {EntityType}",
                value?.Name ?? "null", value?.Type ?? "null");
            this.RaiseAndSetIfChanged(ref _selectedEntity, value);
        }
    }

    public MainWindowViewModel(
        IServiceBusConnectionService connectionService,
        IServiceBusManagementService managementService,
        IServiceBusMessageService messageService,
        ILogger<MainWindowViewModel> logger,
        EntitiesTreeViewModel entitiesTreeViewModel)
    {
        _connectionService = connectionService;
        _managementService = managementService;
        _messageService = messageService;
        _logger = logger;
        EntitiesTreeViewModel = entitiesTreeViewModel;

        AddConnectionCommand = ReactiveCommand.CreateFromTask(OnAddConnectionAsync);
        
        // Load connections on initialization
        _ = LoadConnectionsAsync();
        
        // Notify when Messages collection changes
        Messages.CollectionChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(HasMessages));
            //this.RaisePropertyChanged(nameof(ShowNoMessagesForEntity));
        };
        
        // // Watch for changes to SelectedEntity and MaxMessagesToShow
        // this.WhenAnyValue(x => x.SelectedEntity, x => x.MaxMessagesToShow)
        //     .Subscribe(tuple =>
        //     {
        //         this.RaisePropertyChanged(nameof(ShowNoSelectionMessage));
        //         this.RaisePropertyChanged(nameof(ShowNoMessagesForEntity));
        //         this.RaisePropertyChanged(nameof(ShowNonMessageableEntityMessage));
        //         _ = LoadMessagesForSelectedEntityAsync();
        //     });
        //     
        // // Watch for loading state changes
        // this.WhenAnyValue(x => x.IsLoadingMessages)
        //     .Subscribe(_ =>
        //     {
        //         this.RaisePropertyChanged(nameof(ShowNoSelectionMessage));
        //         this.RaisePropertyChanged(nameof(ShowNoMessagesForEntity));
        //         this.RaisePropertyChanged(nameof(ShowNonMessageableEntityMessage));
        //     });

        this.WhenAnyValue(x => x.SelectedMessage)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(HasSelectedMessage));
            });

        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            // Execute LoadEntitiesCommand when IsConnected changes to true
            this.WhenAnyValue(x => x.IsConnected)
                .Where(isConnected => isConnected)
                .Subscribe(_ => { EntitiesTreeViewModel.LoadEntitiesCommand.Execute().Subscribe(); });
        }
    }

    public MainWindowViewModel() : this(null!, null!, null!, null!, new EntitiesTreeViewModel())
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

    public async Task ConnectAsync(string connectionString, string connectionName)
    {
        try
        {
            StatusText = "Connecting...";
            ConnectionStatus = $"Connecting to {connectionName}...";
            
            _logger.LogInformation("Connecting to Service Bus: {ConnectionName}", connectionName);
            
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
            _logger.LogError(ex, "Exception while connecting to Service Bus");
            
            // Show error dialog with exception details
            if (ShowErrorDialog != null)
            {
                await ShowErrorDialog("Connection Error", ex);
            }
        }

        
    }

    // private async Task LoadMessagesForSelectedEntityAsync()
    // {
    //     _logger.LogInformation("LoadMessagesForSelectedEntityAsync called. SelectedEntity: {EntityName}, Type: {EntityType}", 
    //         SelectedEntity?.Name ?? "null", SelectedEntity?.Type ?? "null");
    //         
    //     if (SelectedEntity == null || string.IsNullOrEmpty(_currentConnectionString))
    //     {
    //         _logger.LogInformation("Clearing messages - SelectedEntity is null or no connection string");
    //         Messages.Clear();
    //         return;
    //     }
    //
    //     // Only load messages for Queue or Subscription entities, not folders or topics
    //     if (SelectedEntity.Type != "Queue" && SelectedEntity.Type != "Subscription")
    //     {
    //         _logger.LogInformation("Clearing messages - Selected entity type is {Type}, not a Queue or Subscription", SelectedEntity.Type);
    //         Messages.Clear();
    //         return;
    //     }
    //
    //     try
    //     {
    //         IsLoadingMessages = true;
    //         StatusText = $"Loading messages from {SelectedEntity.Name}...";
    //         Messages.Clear();
    //         
    //         _logger.LogInformation("Messages collection cleared. About to fetch messages...");
    //
    //         IEnumerable<ServiceBusMessage> messages;
    //
    //         if (SelectedEntity.Type == "Queue")
    //         {
    //             _logger.LogInformation("Peeking {MaxMessages} messages from queue {QueueName}", 
    //                 MaxMessagesToShow, SelectedEntity.Name);
    //             messages = await _messageService.ReceiveMessagesAsync(
    //                 SelectedEntity.Name, 
    //                 MaxMessagesToShow, 
    //                 peekOnly: true);
    //         }
    //         else // Subscription
    //         {
    //             // Find the parent topic name
    //             var topicName = FindParentTopicName(SelectedEntity);
    //             if (string.IsNullOrEmpty(topicName))
    //             {
    //                 _logger.LogWarning("Could not find parent topic for subscription {SubscriptionName}", 
    //                     SelectedEntity.Name);
    //                 StatusText = "Error: Could not find parent topic";
    //                 return;
    //             }
    //
    //             _logger.LogInformation("Peeking {MaxMessages} messages from subscription {TopicName}/{SubscriptionName}", 
    //                 MaxMessagesToShow, topicName, SelectedEntity.Name);
    //             messages = await _messageService.ReceiveSubscriptionMessagesAsync(
    //                 topicName, 
    //                 SelectedEntity.Name, 
    //                 MaxMessagesToShow, 
    //                 peekOnly: true);
    //         }
    //
    //         var messagesList = messages.ToList();
    //         _logger.LogInformation("Received {Count} messages from service", messagesList.Count);
    //         
    //         foreach (var message in messagesList)
    //         {
    //             _logger.LogDebug("Adding message: ID={MessageId}, Seq={SequenceNumber}", 
    //                 message.MessageId, message.SequenceNumber);
    //             Messages.Add(message);
    //         }
    //         
    //         _logger.LogInformation("Messages collection now has {Count} items. HasMessages={HasMessages}", 
    //             Messages.Count, HasMessages);
    //
    //         StatusText = $"Loaded {Messages.Count} messages";
    //         _logger.LogInformation("Successfully loaded {Count} messages", Messages.Count);
    //     }
    //     catch (Exception ex)
    //     {
    //         StatusText = "Error loading messages";
    //         _logger.LogError(ex, "Failed to load messages for {EntityType} {EntityName}", 
    //             SelectedEntity.Type, SelectedEntity.Name);
    //         
    //         if (ShowErrorDialog != null)
    //         {
    //             await ShowErrorDialog("Failed to Load Messages", ex);
    //         }
    //     }
    //     finally
    //     {
    //         IsLoadingMessages = false;
    //         _logger.LogInformation("IsLoadingMessages set to false. Final state - Messages.Count: {Count}, HasMessages: {HasMessages}, ShowNoMessagesForEntity: {ShowNoMessages}", 
    //             Messages.Count, HasMessages, ShowNoMessagesForEntity);
    //     }
    // }
    //
    // private string? FindParentTopicName(EntityTreeItemViewModel subscriptionEntity)
    // {
    //     // Find the Topics folder in the Entities tree
    //     var topicsFolder = Entities.FirstOrDefault(e => e.Type == "Folder" && e.Name == "Topics");
    //     if (topicsFolder == null)
    //         return null;
    //
    //     // Search through all topics to find the one containing this subscription
    //     foreach (var topic in topicsFolder.Children)
    //     {
    //         if (topic.Children.Any(sub => sub.Name == subscriptionEntity.Name))
    //         {
    //             return topic.Name;
    //         }
    //     }
    //
    //     return null;
    // }

    private void ToggleSelectAllMessages(bool selectAll)
    {
        _logger.LogInformation("ToggleSelectAllMessages called with selectAll={SelectAll}", selectAll);

        if (selectAll)
        {
            // Clear existing selections if any
            foreach (var message in Messages)
            {
                message.IsSelected = false;
            }

            // Select all messages
            foreach (var message in Messages)
            {
                message.IsSelected = true;
            }

            _logger.LogInformation("All messages selected. Messages.Count={Count}", Messages.Count);
        }
        else
        {
            // Deselect all messages
            foreach (var message in Messages)
            {
                message.IsSelected = false;
            }

            _logger.LogInformation("All messages deselected.");
        }
    }
}