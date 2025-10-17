using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using BusOps.ViewModels;
using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using ReactiveUI;
using System.Reactive.Linq;

namespace BusOps.Tests.ViewModels;

public class MessageManagementViewModelTests
{
    private readonly Mock<IServiceBusMessageService> _mockMessageService;
    private readonly Mock<ILogger<MessageManagementViewModel>> _mockLogger;

    public MessageManagementViewModelTests()
    {
        _mockMessageService = new Mock<IServiceBusMessageService>();
        _mockLogger = new Mock<ILogger<MessageManagementViewModel>>();
    }

    private MessageManagementViewModel CreateViewModel()
    {
        return new MessageManagementViewModel(_mockMessageService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SelectedEntity.ShouldBeNull();
        viewModel.IsLoadingMessages.ShouldBeFalse();
        viewModel.SelectedMessage.ShouldBeNull();
        viewModel.Messages.ShouldBeEmpty();
        viewModel.HasMessages.ShouldBeFalse();
        viewModel.MaxMessagesToShow.ShouldBe(100);
        viewModel.SelectedEntityIsManageable.ShouldBeFalse();
    }

    [Fact]
    public void MaxMessagesToShowOptions_ShouldContainExpectedValues()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.MaxMessagesToShowOptions.ShouldContain(25);
        viewModel.MaxMessagesToShowOptions.ShouldContain(50);
        viewModel.MaxMessagesToShowOptions.ShouldContain(100);
        viewModel.MaxMessagesToShowOptions.ShouldContain(200);
        viewModel.MaxMessagesToShowOptions.ShouldContain(500);
        viewModel.MaxMessagesToShowOptions.ShouldContain(1000);
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
    public void IsLoadingMessages_ShouldBeSettable()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.IsLoadingMessages = true;

        // Assert
        viewModel.IsLoadingMessages.ShouldBeTrue();
    }

    [Fact]
    public void MaxMessagesToShow_ShouldBeSettable()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.MaxMessagesToShow = 500;

        // Assert
        viewModel.MaxMessagesToShow.ShouldBe(500);
    }

    [Fact]
    public void SelectedMessage_ShouldBeSettable()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var message = new ServiceBusMessage { MessageId = "test-123" };

        // Act
        viewModel.SelectedMessage = message;

        // Assert
        viewModel.SelectedMessage.ShouldBe(message);
    }

    [Fact]
    public void HasMessages_ShouldReturnFalse_WhenMessagesIsEmpty()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.HasMessages.ShouldBeFalse();
    }

    [Fact]
    public void HasMessages_ShouldReturnTrue_WhenMessagesHasItems()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var message = new ServiceBusMessage { MessageId = "test-123" };

        // Act
        viewModel.Messages.Add(message);

        // Assert
        viewModel.HasMessages.ShouldBeTrue();
    }

    [Fact]
    public void SelectedEntityIsManageable_ShouldReturnTrue_ForQueue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "TestQueue", Type = "Queue" };

        // Act
        viewModel.SelectedEntity = entity;

        // Assert
        viewModel.SelectedEntityIsManageable.ShouldBeTrue();
    }

    [Fact]
    public void SelectedEntityIsManageable_ShouldReturnTrue_ForTopic()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "TestTopic", Type = "Topic" };

        // Act
        viewModel.SelectedEntity = entity;

        // Assert
        viewModel.SelectedEntityIsManageable.ShouldBeTrue();
    }

    [Fact]
    public void SelectedEntityIsManageable_ShouldReturnFalse_ForFolder()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "Queues", Type = "Folder" };

        // Act
        viewModel.SelectedEntity = entity;

        // Assert
        viewModel.SelectedEntityIsManageable.ShouldBeFalse();
    }

    [Fact]
    public void SelectedEntityIsManageable_ShouldReturnFalse_WhenSelectedEntityIsNull()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedEntity = null;

        // Assert
        viewModel.SelectedEntityIsManageable.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadMessages_ForQueue_ShouldCallReceiveMessagesAsync()
    {
        // Arrange
        var messages = new List<ServiceBusMessage>
        {
            new() { MessageId = "msg-1", Body = "Body 1" },
            new() { MessageId = "msg-2", Body = "Body 2" }
        };

        var tcs = new TaskCompletionSource<IEnumerable<ServiceBusMessage>>();
        _mockMessageService
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(tcs.Task);

        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "orders-queue", Type = "Queue" };

        // Act
        viewModel.SelectedEntity = entity;
        tcs.SetResult(messages); // Complete the async operation
        
        // Wait for messages to be loaded by observing IsLoadingMessages
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages)
            .Where(isLoading => !isLoading)
            .FirstAsync();

        // Assert
        _mockMessageService.Verify(
            x => x.ReceiveMessagesAsync("orders-queue", 100, true),
            Times.Once);
    }

    [Fact]
    public async Task LoadMessages_ForQueue_ShouldPopulateMessagesCollection()
    {
        // Arrange
        var messages = new List<ServiceBusMessage>
        {
            new() { MessageId = "msg-1", Body = "Body 1" },
            new() { MessageId = "msg-2", Body = "Body 2" }
        };

        _mockMessageService
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(messages);

        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "orders-queue", Type = "Queue" };

        // Act
        viewModel.SelectedEntity = entity;
        
        // Wait for messages to be loaded
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages)
            .Where(isLoading => !isLoading)
            .FirstAsync();

        // Assert
        viewModel.Messages.Count.ShouldBe(2);
        viewModel.Messages[0].MessageId.ShouldBe("msg-1");
        viewModel.Messages[1].MessageId.ShouldBe("msg-2");
    }

    [Fact]
    public async Task LoadMessages_ForSubscription_ShouldCallReceiveSubscriptionMessagesAsync()
    {
        // Arrange
        var messages = new List<ServiceBusMessage>
        {
            new() { MessageId = "msg-1", Body = "Body 1" }
        };

        _mockMessageService
            .Setup(x => x.ReceiveSubscriptionMessagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(messages);

        var viewModel = CreateViewModel();
        var topic = new EntityTreeItemViewModel { Name = "orders-topic", Type = "Topic" };
        var subscription = new EntityTreeItemViewModel 
        { 
            Name = "orders-subscription", 
            Type = "Subscription",
            Parent = topic
        };

        // Act
        viewModel.SelectedEntity = subscription;
        
        // Wait for messages to be loaded
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages)
            .Where(isLoading => !isLoading)
            .FirstAsync();

        // Assert
        _mockMessageService.Verify(
            x => x.ReceiveSubscriptionMessagesAsync("orders-topic", "orders-subscription", 100, true),
            Times.Once);
    }

    [Fact]
    public async Task LoadMessages_ForFolder_ShouldClearMessages()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Messages.Add(new ServiceBusMessage { MessageId = "existing-msg" });
        
        var entity = new EntityTreeItemViewModel { Name = "Queues", Type = "Folder" };

        // Act
        viewModel.SelectedEntity = entity;
        
        // Wait a moment for reactive processing - no async work expected for folders
        await Task.Yield();

        // Assert
        viewModel.Messages.ShouldBeEmpty();
        _mockMessageService.Verify(
            x => x.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task LoadMessages_WhenExceptionOccurs_ShouldSetIsLoadingToFalse()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IEnumerable<ServiceBusMessage>>();
        _mockMessageService
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(tcs.Task);

        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "test-queue", Type = "Queue" };

        // Act
        viewModel.SelectedEntity = entity;
        tcs.SetException(new Exception("Connection failed"));
        
        // Wait for error handling to complete
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages)
            .Where(isLoading => !isLoading)
            .FirstAsync();

        // Assert
        viewModel.IsLoadingMessages.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadMessages_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var exception = new Exception("Connection failed");
        var tcs = new TaskCompletionSource<IEnumerable<ServiceBusMessage>>();
        _mockMessageService
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(tcs.Task);

        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "test-queue", Type = "Queue" };

        // Act
        viewModel.SelectedEntity = entity;
        tcs.SetException(exception);
        
        // Wait for error handling to complete
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages)
            .Where(isLoading => !isLoading)
            .FirstAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadMessages_ShouldClearPreviousMessages()
    {
        // Arrange
        var messages1 = new List<ServiceBusMessage>
        {
            new() { MessageId = "msg-1" }
        };
        var messages2 = new List<ServiceBusMessage>
        {
            new() { MessageId = "msg-2" },
            new() { MessageId = "msg-3" }
        };

        _mockMessageService
            .SetupSequence(x => x.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(messages1)
            .ReturnsAsync(messages2);

        var viewModel = CreateViewModel();
        var entity1 = new EntityTreeItemViewModel { Name = "queue1", Type = "Queue" };
        var entity2 = new EntityTreeItemViewModel { Name = "queue2", Type = "Queue" };

        // Act
        viewModel.SelectedEntity = entity1;
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages).Where(isLoading => !isLoading).FirstAsync();
        viewModel.Messages.Count.ShouldBe(1);

        viewModel.SelectedEntity = entity2;
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages).Where(isLoading => !isLoading).FirstAsync();

        // Assert
        viewModel.Messages.Count.ShouldBe(2);
        viewModel.Messages.ShouldNotContain(m => m.MessageId == "msg-1");
    }

    [Fact]
    public async Task MaxMessagesToShow_WhenChanged_ShouldReloadMessages()
    {
        // Arrange
        var messages = new List<ServiceBusMessage>
        {
            new() { MessageId = "msg-1" }
        };

        _mockMessageService
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(messages);

        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "test-queue", Type = "Queue" };
        viewModel.SelectedEntity = entity;
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages).Where(isLoading => !isLoading).FirstAsync();

        // Act
        viewModel.MaxMessagesToShow = 500;
        await viewModel.WhenAnyValue(x => x.IsLoadingMessages).Where(isLoading => !isLoading).FirstAsync();

        // Assert
        _mockMessageService.Verify(
            x => x.ReceiveMessagesAsync("test-queue", 500, true),
            Times.Once);
    }

    [Fact]
    public void Properties_ShouldRaisePropertyChangedEvents()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var selectedEntityChanged = false;
        var isLoadingMessagesChanged = false;
        var selectedMessageChanged = false;
        var maxMessagesToShowChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MessageManagementViewModel.SelectedEntity))
                selectedEntityChanged = true;
            if (args.PropertyName == nameof(MessageManagementViewModel.IsLoadingMessages))
                isLoadingMessagesChanged = true;
            if (args.PropertyName == nameof(MessageManagementViewModel.SelectedMessage))
                selectedMessageChanged = true;
            if (args.PropertyName == nameof(MessageManagementViewModel.MaxMessagesToShow))
                maxMessagesToShowChanged = true;
        };

        // Act
        viewModel.SelectedEntity = new EntityTreeItemViewModel { Name = "test" };
        viewModel.IsLoadingMessages = true;
        viewModel.SelectedMessage = new ServiceBusMessage { MessageId = "test" };
        viewModel.MaxMessagesToShow = 200;

        // Assert
        selectedEntityChanged.ShouldBeTrue();
        isLoadingMessagesChanged.ShouldBeTrue();
        selectedMessageChanged.ShouldBeTrue();
        maxMessagesToShowChanged.ShouldBeTrue();
    }

    [Fact]
    public void SelectedEntityChanged_ShouldRaiseSelectedEntityIsManageable()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var selectedEntityIsManageableChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MessageManagementViewModel.SelectedEntityIsManageable))
                selectedEntityIsManageableChanged = true;
        };

        // Act
        viewModel.SelectedEntity = new EntityTreeItemViewModel { Name = "test", Type = "Queue" };

        // Assert
        selectedEntityIsManageableChanged.ShouldBeTrue();
    }

    [Fact]
    public void Messages_WhenChanged_ShouldRaiseHasMessages()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var hasMessagesChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MessageManagementViewModel.HasMessages))
                hasMessagesChanged = true;
        };

        // Act
        viewModel.Messages.Add(new ServiceBusMessage { MessageId = "test" });

        // Assert
        hasMessagesChanged.ShouldBeTrue();
    }
}
