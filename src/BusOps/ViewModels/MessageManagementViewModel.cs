using System.Collections.ObjectModel;
using System.Reactive;
using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using BusOps.Design;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BusOps.ViewModels;

public class MessageManagementViewModel : ViewModelBase
{
    private readonly IServiceBusMessageService _messageService;
    private readonly ILogger<MessageManagementViewModel>? _logger;
    private EntityTreeItemViewModel? _selectedEntity;
    private bool _isLoadingMessages;
    private ServiceBusMessage? _selectedMessage;

    public EntityTreeItemViewModel? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            _logger?.LogInformation("[MessageManagement] SelectedEntity changed to: {SelectedEntity}", value?.Name);
            this.RaiseAndSetIfChanged(ref _selectedEntity, value);
        }
    }
    
    public bool SelectedEntityIsManageable =>
        SelectedEntity is { Type: "Queue" or "Topic" };

    public List<int> MaxMessagesToShowOptions { get; } = [25, 50, 100, 200, 500, 1000];
    private int _maxMessagesToShow = 100;
    public int MaxMessagesToShow
    {
        get => _maxMessagesToShow;
        set => this.RaiseAndSetIfChanged(ref _maxMessagesToShow, value);
    }

    public ObservableCollection<ServiceBusMessage> Messages { get; } = new();
    public bool HasMessages => Messages.Count > 0;
    public ServiceBusMessage? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            _logger?.LogInformation("Message selected: ID={MessageId}, Seq={SequenceNumber}",
                value?.MessageId ?? "null", value?.SequenceNumber.ToString() ?? "null");
            this.RaiseAndSetIfChanged(ref _selectedMessage, value);
        }
    }

    public bool IsLoadingMessages
    {
        get => _isLoadingMessages;
        set => this.RaiseAndSetIfChanged(ref _isLoadingMessages, value);
    }

    private readonly ReactiveCommand<Unit, Unit> _loadMessagesCommand;

    public MessageManagementViewModel(IServiceBusMessageService messageService, ILogger<MessageManagementViewModel>? logger)
    {
        _messageService = messageService;
        _logger = logger;
        
        this.WhenAnyValue(x => x.SelectedEntity)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SelectedEntityIsManageable));
                _loadMessagesCommand?.Execute().Subscribe();
            });
        
        Messages.CollectionChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(HasMessages));
        };
        
        var canLoadMessages = this.WhenAnyValue(x => x.SelectedEntityIsManageable);
        _loadMessagesCommand = ReactiveCommand.CreateFromTask(LoadMessagesAsync, canLoadMessages);
        
        this.WhenAnyValue(x => x.MaxMessagesToShow)
            .Subscribe(_ =>
            {
                _logger?.LogDebug("MaxMessagesToShow changed to {MaxMessagesToShow}", MaxMessagesToShow);
                _loadMessagesCommand?.Execute().Subscribe();
            });
    }

    public MessageManagementViewModel() : this(null, null)
    {
        if(!Avalonia.Controls.Design.IsDesignMode)
            throw new NotSupportedException("This constructor is only for design time.");
        
        SelectedEntity = DesignData.SampleEntities.First().Children.First();
        Messages.AddRange(SampleMessages.GenerateSampleMessages());
        SelectedMessage = Messages.First();
    }

    private async Task LoadMessagesAsync()
    {
        _logger?.LogInformation("LoadMessagesForSelectedEntityAsync called. SelectedEntity: {EntityName}, Type: {EntityType}", 
            SelectedEntity?.Name ?? "null", SelectedEntity?.Type ?? "null");

        if (SelectedEntity == null)
            return;
        
        // Only load messages for Queue or Subscription entities, not folders or topics
        if (SelectedEntity.Type != "Queue" && SelectedEntity.Type != "Subscription")
        {
            _logger?.LogInformation("Clearing messages - Selected entity type is {Type}, not a Queue or Subscription", SelectedEntity.Type);
            Messages.Clear();
            return;
        }
    
        try
        {
            IsLoadingMessages = true;
            Messages.Clear();
            
            _logger?.LogInformation("Messages collection cleared. About to fetch messages...");
    
            IEnumerable<ServiceBusMessage> messages;
    
            if (SelectedEntity.Type == "Queue")
            {
                _logger?.LogInformation("Peeking {MaxMessages} messages from queue {QueueName}", 
                    MaxMessagesToShow, SelectedEntity.Name);
                messages = await _messageService.ReceiveMessagesAsync(
                    SelectedEntity.Name, 
                    MaxMessagesToShow, 
                    peekOnly: true);
            }
            else // Subscription
            {
                // Find the parent topic name
                var topicName = SelectedEntity.Parent?.Name;
                if (string.IsNullOrEmpty(topicName))
                {
                    _logger?.LogWarning("Could not find parent topic for subscription {SubscriptionName}", 
                        SelectedEntity.Name);
                    return;
                }
    
                _logger?.LogInformation("Peeking {MaxMessages} messages from subscription {TopicName}/{SubscriptionName}", 
                    MaxMessagesToShow, topicName, SelectedEntity.Name);
                messages = await _messageService.ReceiveSubscriptionMessagesAsync(
                    topicName, 
                    SelectedEntity.Name, 
                    MaxMessagesToShow, 
                    peekOnly: true);
            }
    
            var messagesList = messages.ToList();
            _logger?.LogInformation("Received {Count} messages from service", messagesList.Count);
            
            foreach (var message in messagesList)
            {
                _logger?.LogDebug("Adding message: ID={MessageId}, Seq={SequenceNumber}", 
                    message.MessageId, message.SequenceNumber);
                Messages.Add(message);
            }
            
            _logger?.LogInformation("Messages collection now has {Count} items. HasMessages={HasMessages}", 
                Messages.Count, HasMessages);
    
            _logger?.LogInformation("Successfully loaded {Count} messages", Messages.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load messages for {EntityType} {EntityName}", 
                SelectedEntity.Type, SelectedEntity.Name);
            
            // if (ShowErrorDialog != null)
            // {
            //     await ShowErrorDialog("Failed to Load Messages", ex);
            // }
        }
        finally
        {
            IsLoadingMessages = false;
            _logger?.LogInformation("IsLoadingMessages set to false. Final state - Messages.Count: {Count}, HasMessages: {HasMessages}", 
                Messages.Count, HasMessages);
        }
    }
}