using System.Collections.ObjectModel;
using System.Reactive;
using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using BusOps.Design;
using DynamicData;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BusOps.ViewModels;

public class EntitiesTreeViewModel : ViewModelBase
{
    private readonly ILogger<EntitiesTreeViewModel> _logger;
    private readonly IServiceBusManagementService _managementService;
    private string _entitySearchText = string.Empty;
    private bool _isLoadingEntities;
    public ObservableCollection<EntityTreeItemViewModel> Entities { get; } = new();
    private EntityTreeItemViewModel? _selectedEntity;

    public EntitiesTreeViewModel(ILogger<EntitiesTreeViewModel> logger, IServiceBusManagementService managementService)
    {
        _logger = logger;
        _managementService = managementService;

        Entities.CollectionChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(HasEntities));
        };

        // Initialize the LoadEntitiesCommand
        LoadEntitiesCommand = ReactiveCommand.CreateFromTask(LoadEntities);
    }

    public EntitiesTreeViewModel() : this(null!, null!)
    {
        if(!Avalonia.Controls.Design.IsDesignMode)
            throw new NotSupportedException("This constructor is only for Design mode.");

        Entities.AddRange(DesignData.SampleEntities);
        SelectedEntity = Entities[0].Children[0];
        IsLoadingEntities = false;
    }
    
    public ReactiveCommand<Unit, Unit> LoadEntitiesCommand { get; }
    
    public bool IsLoadingEntities
    {
        get => _isLoadingEntities;
        set => this.RaiseAndSetIfChanged(ref _isLoadingEntities, value);
    }
    
    public string EntitySearchText
    {
        get => _entitySearchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _entitySearchText, value);
            ApplyEntityFilter(value);
        }
    }
    
    public bool HasEntities => Entities.Count > 0;

    public EntityTreeItemViewModel? SelectedEntity
    {
        get => _selectedEntity;
        set => this.RaiseAndSetIfChanged(ref _selectedEntity, value);
    }
    
    private void ApplyEntityFilter(string filterText)
    {
        _logger.LogInformation("Applying entity filter: {FilterText}", filterText);

        // Apply the filter to all entities recursively
        foreach (var entity in Entities)
        {
            entity.ApplyFilter(filterText);
        }
    }

    private async Task LoadEntities()
    {
        IsLoadingEntities = true;
        try
        {
            // StatusText = "Loading entities...";
            // ConnectionStatus = $"Connected to {connectionName}";

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
            //var topics = (await _managementService.GetTopicsAsync()).ToList();
            List<ServiceBusTopic> topics = [];
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
                        MessageCount = subscription.MessageCount,
                        Parent = topicNode
                    });
                }

                topicsNode.Children.Add(topicNode);
            }

            Entities.Add(topicsNode);

            //StatusText = "Ready";
            _logger.LogInformation("Successfully loaded {QueueCount} queues and {TopicCount} topics",
                queues.Count, topics.Count);
        }
        catch (Exception ex)
        {
            // StatusText = "Error loading entities";
            // ConnectionStatus = "Error";
            _logger.LogError(ex, "Failed to load Service Bus entities");

            // Show error dialog with exception details
            // if (ShowErrorDialog != null)
            // {
            //     await ShowErrorDialog("Failed to Load Entities", ex);
            // }
        }
        finally
        {
            IsLoadingEntities = false;
        }
    }
}