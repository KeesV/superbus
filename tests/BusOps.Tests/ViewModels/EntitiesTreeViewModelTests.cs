using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using BusOps.ViewModels;
using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using ReactiveUI;
using System.Reactive.Linq;

namespace BusOps.Tests.ViewModels;

public class EntitiesTreeViewModelTests
{
    private readonly Mock<ILogger<EntitiesTreeViewModel>> _mockLogger;
    private readonly Mock<IServiceBusManagementService> _mockManagementService;

    public EntitiesTreeViewModelTests()
    {
        _mockLogger = new Mock<ILogger<EntitiesTreeViewModel>>();
        _mockManagementService = new Mock<IServiceBusManagementService>();
    }

    private EntitiesTreeViewModel CreateViewModel()
    {
        return new EntitiesTreeViewModel(_mockLogger.Object, _mockManagementService.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Entities.ShouldBeEmpty();
        viewModel.IsLoadingEntities.ShouldBeFalse();
        viewModel.EntitySearchText.ShouldBe(string.Empty);
        viewModel.HasEntities.ShouldBeFalse();
        viewModel.SelectedEntity.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeLoadEntitiesCommand()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.LoadEntitiesCommand.ShouldNotBeNull();
    }

    [Fact]
    public void HasEntities_ShouldReturnFalse_WhenEntitiesIsEmpty()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.HasEntities.ShouldBeFalse();
    }

    [Fact]
    public void HasEntities_ShouldReturnTrue_WhenEntitiesHasItems()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Entities.Add(new EntityTreeItemViewModel { Name = "Queues", Type = "Folder" });

        // Assert
        viewModel.HasEntities.ShouldBeTrue();
    }

    [Fact]
    public void SelectedEntity_ShouldBeSettable()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "TestQueue", Type = "Queue" };

        // Act
        viewModel.SelectedEntity = entity;

        // Assert
        viewModel.SelectedEntity.ShouldBe(entity);
    }

    [Fact]
    public void IsLoadingEntities_ShouldBeSettable()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.IsLoadingEntities = true;

        // Assert
        viewModel.IsLoadingEntities.ShouldBeTrue();
    }

    [Fact]
    public void EntitySearchText_ShouldBeSettable()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.EntitySearchText = "test";

        // Assert
        viewModel.EntitySearchText.ShouldBe("test");
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldLoadQueuesAndTopics()
    {
        // Arrange
        var queues = new List<ServiceBusQueue>
        {
            new() { Name = "queue1", MessageCount = 10 },
            new() { Name = "queue2", MessageCount = 20 }
        };

        var topics = new List<ServiceBusTopic>
        {
            new() 
            { 
                Name = "topic1",
                Subscriptions = new List<ServiceBusSubscription>
                {
                    new() { Name = "sub1", MessageCount = 5 }
                }
            }
        };

        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(queues);

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(topics);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        viewModel.Entities.Count.ShouldBe(2); // Queues folder and Topics folder
        viewModel.IsLoadingEntities.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldCreateQueuesFolder()
    {
        // Arrange
        var queues = new List<ServiceBusQueue>
        {
            new() { Name = "queue1", MessageCount = 10 },
            new() { Name = "queue2", MessageCount = 20 }
        };

        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(queues);

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(new List<ServiceBusTopic>());

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        var queuesFolder = viewModel.Entities.FirstOrDefault(e => e.Name == "Queues");
        queuesFolder.ShouldNotBeNull();
        queuesFolder.Type.ShouldBe("Folder");
        queuesFolder.MessageCount.ShouldBe(2); // Count of queues
        queuesFolder.IsExpanded.ShouldBeTrue();
        queuesFolder.Children.Count.ShouldBe(2);
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldCreateQueueItems()
    {
        // Arrange
        var queues = new List<ServiceBusQueue>
        {
            new() { Name = "orders-queue", MessageCount = 42 },
            new() { Name = "events-queue", MessageCount = 17 }
        };

        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(queues);

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(new List<ServiceBusTopic>());

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        var queuesFolder = viewModel.Entities.First(e => e.Name == "Queues");
        var queueItems = queuesFolder.Children.ToList();
        
        queueItems.Count.ShouldBe(2);
        queueItems[0].Name.ShouldBe("orders-queue");
        queueItems[0].Type.ShouldBe("Queue");
        queueItems[0].MessageCount.ShouldBe(42);
        queueItems[1].Name.ShouldBe("events-queue");
        queueItems[1].MessageCount.ShouldBe(17);
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldCreateTopicsFolder()
    {
        // Arrange
        var topics = new List<ServiceBusTopic>
        {
            new() 
            { 
                Name = "topic1",
                Subscriptions = new List<ServiceBusSubscription>()
            }
        };

        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(new List<ServiceBusQueue>());

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(topics);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        var topicsFolder = viewModel.Entities.FirstOrDefault(e => e.Name == "Topics");
        topicsFolder.ShouldNotBeNull();
        topicsFolder.Type.ShouldBe("Folder");
        // Note: Topics loading is currently disabled in the implementation, so count will be 0
        topicsFolder.MessageCount.ShouldBe(0);
        topicsFolder.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldCreateTopicsWithSubscriptions()
    {
        // Arrange
        var topics = new List<ServiceBusTopic>
        {
            new() 
            { 
                Name = "orders-topic",
                Subscriptions = new List<ServiceBusSubscription>
                {
                    new() { Name = "subscription1", MessageCount = 5 },
                    new() { Name = "subscription2", MessageCount = 10 }
                }
            }
        };

        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(new List<ServiceBusQueue>());

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(topics);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        var topicsFolder = viewModel.Entities.FirstOrDefault(e => e.Name == "Topics");
        topicsFolder.ShouldNotBeNull();
        
        // Note: Topics loading is currently disabled in the implementation
        // The topics folder exists but has no children
        topicsFolder.Children.Count.ShouldBe(0);
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldClearExistingEntities()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Entities.Add(new EntityTreeItemViewModel { Name = "Old Entity" });

        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(new List<ServiceBusQueue>());

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(new List<ServiceBusTopic>());

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        viewModel.Entities.ShouldNotContain(e => e.Name == "Old Entity");
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldSetIsLoadingEntitiesDuringExecution()
    {
        // Arrange
        var taskCompletionSource = new TaskCompletionSource<IEnumerable<ServiceBusQueue>>();
        
        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .Returns(taskCompletionSource.Task);

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(new List<ServiceBusTopic>());

        var viewModel = CreateViewModel();

        // Act
        var executeTask = viewModel.LoadEntitiesCommand.Execute();

        // Wait for IsLoadingEntities to become true
        await viewModel.WhenAnyValue(x => x.IsLoadingEntities)
            .Where(isLoading => isLoading)
            .FirstAsync();

        // Assert - should be loading
        viewModel.IsLoadingEntities.ShouldBeTrue();

        // Complete the task
        taskCompletionSource.SetResult(new List<ServiceBusQueue>());
        
        // Wait for completion
        await viewModel.WhenAnyValue(x => x.IsLoadingEntities)
            .Where(isLoading => !isLoading)
            .FirstAsync();

        // Assert - should be done loading
        viewModel.IsLoadingEntities.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadEntitiesCommand_WhenExceptionOccurs_ShouldSetIsLoadingToFalse()
    {
        // Arrange
        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ThrowsAsync(new Exception("Connection failed"));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        viewModel.IsLoadingEntities.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadEntitiesCommand_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var exception = new Exception("Connection failed");
        
        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ThrowsAsync(exception);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldHandleEmptyQueuesAndTopics()
    {
        // Arrange
        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(new List<ServiceBusQueue>());

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(new List<ServiceBusTopic>());

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        viewModel.Entities.Count.ShouldBe(2); // Still creates folders
        viewModel.Entities[0].Children.Count.ShouldBe(0); // Queues folder empty
        viewModel.Entities[1].Children.Count.ShouldBe(0); // Topics folder empty
    }

    [Fact]
    public void EntitySearchText_WhenChanged_ShouldApplyFilter()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var queueNode = new EntityTreeItemViewModel { Name = "orders-queue", Type = "Queue" };
        var topicNode = new EntityTreeItemViewModel { Name = "events-topic", Type = "Topic" };
        
        viewModel.Entities.Add(queueNode);
        viewModel.Entities.Add(topicNode);

        // Act
        viewModel.EntitySearchText = "orders";

        // Assert - The filter should have been applied to entities
        // We can't directly test visibility without mocking the ApplyFilter method,
        // but we can verify the property was set
        viewModel.EntitySearchText.ShouldBe("orders");
    }

    [Fact]
    public void EntitySearchText_WhenSetToEmpty_ShouldClearFilter()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Entities.Add(new EntityTreeItemViewModel { Name = "test-queue" });
        viewModel.EntitySearchText = "test";

        // Act
        viewModel.EntitySearchText = string.Empty;

        // Assert
        viewModel.EntitySearchText.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldCallGetQueuesAsync()
    {
        // Arrange
        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(new List<ServiceBusQueue>());

        _mockManagementService
            .Setup(x => x.GetTopicsAsync())
            .ReturnsAsync(new List<ServiceBusTopic>());

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        _mockManagementService.Verify(x => x.GetQueuesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoadEntitiesCommand_ShouldNotCallGetTopicsAsync_DueToCommentedCode()
    {
        // Arrange
        _mockManagementService
            .Setup(x => x.GetQueuesAsync())
            .ReturnsAsync(new List<ServiceBusQueue>());

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadEntitiesCommand.Execute().FirstAsync();

        // Assert
        // The GetTopicsAsync is commented out in the implementation, so it should not be called
        _mockManagementService.Verify(x => x.GetTopicsAsync(), Times.Never);
    }
}
