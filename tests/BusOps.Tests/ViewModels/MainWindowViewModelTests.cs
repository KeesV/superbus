using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using BusOps.Services;
using BusOps.ViewModels;
using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reactive.Linq;

namespace BusOps.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<IServiceBusConnectionService> _mockConnectionService;
    private readonly Mock<IServiceBusManagementService> _mockManagementService;
    private readonly Mock<IServiceBusMessageService> _mockMessageService;
    private readonly Mock<IErrorDialogService> _mockErrorDialogService;
    private readonly Mock<ILogger<MainWindowViewModel>> _mockLogger;
    private readonly Mock<ILogger<EntitiesTreeViewModel>> _mockEntitiesTreeLogger;
    private readonly Mock<ILogger<MessageManagementViewModel>> _mockMessageManagementLogger;

    public MainWindowViewModelTests()
    {
        _mockConnectionService = new Mock<IServiceBusConnectionService>();
        _mockManagementService = new Mock<IServiceBusManagementService>();
        _mockMessageService = new Mock<IServiceBusMessageService>();
        _mockErrorDialogService = new Mock<IErrorDialogService>();
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();
        _mockEntitiesTreeLogger = new Mock<ILogger<EntitiesTreeViewModel>>();
        _mockMessageManagementLogger = new Mock<ILogger<MessageManagementViewModel>>();
    }

    private MainWindowViewModel CreateViewModel()
    {
        var entitiesTreeViewModel = new EntitiesTreeViewModel(
            _mockEntitiesTreeLogger.Object,
            _mockManagementService.Object);
        
        var messageManagementViewModel = new MessageManagementViewModel(
            _mockMessageService.Object,
            _mockMessageManagementLogger.Object);

        return new MainWindowViewModel(
            _mockConnectionService.Object,
            _mockManagementService.Object,
            _mockMessageService.Object,
            _mockErrorDialogService.Object,
            _mockLogger.Object,
            entitiesTreeViewModel,
            messageManagementViewModel);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.StatusText.ShouldBe("Ready");
        viewModel.ConnectionStatus.ShouldBe("No active connections");
        viewModel.IsConnected.ShouldBeFalse();
        viewModel.MaxMessagesToShow.ShouldBe(100);
        viewModel.IsLoadingMessages.ShouldBeFalse();
        viewModel.SelectedMessage.ShouldBeNull();
        viewModel.SelectedEntity.ShouldBeNull();
        viewModel.Messages.ShouldBeEmpty();
        viewModel.Connections.ShouldNotBeNull();
        viewModel.HasSelectedMessage.ShouldBeFalse();
        viewModel.HasMessages.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_ShouldInitializeCommands()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.AddConnectionCommand.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeChildViewModels()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.EntitiesTreeViewModel.ShouldNotBeNull();
        viewModel.MessageManagementViewModel.ShouldNotBeNull();
    }

    [Fact]
    public void MessageLimitOptions_ShouldContainExpectedValues()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.MessageLimitOptions.ShouldContain(10);
        viewModel.MessageLimitOptions.ShouldContain(25);
        viewModel.MessageLimitOptions.ShouldContain(50);
        viewModel.MessageLimitOptions.ShouldContain(100);
        viewModel.MessageLimitOptions.ShouldContain(250);
        viewModel.MessageLimitOptions.ShouldContain(500);
        viewModel.MessageLimitOptions.ShouldContain(1000);
    }

    [Fact]
    public void StatusText_ShouldBeSettable()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        var viewModel = CreateViewModel();

        // Act
        viewModel.StatusText = "Connected";

        // Assert
        viewModel.StatusText.ShouldBe("Connected");
    }

    [Fact]
    public void ConnectionStatus_ShouldBeSettable()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        var viewModel = CreateViewModel();

        // Act
        viewModel.ConnectionStatus = "Connected to test";

        // Assert
        viewModel.ConnectionStatus.ShouldBe("Connected to test");
    }

    [Fact]
    public void IsConnected_ShouldBeSettable()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        var viewModel = CreateViewModel();

        // Act
        viewModel.IsConnected = true;

        // Assert
        viewModel.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void MaxMessagesToShow_ShouldBeSettable()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
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
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        var viewModel = CreateViewModel();
        var message = new ServiceBusMessage { MessageId = "test-123" };

        // Act
        viewModel.SelectedMessage = message;

        // Assert
        viewModel.SelectedMessage.ShouldBe(message);
    }

    [Fact]
    public void HasSelectedMessage_ShouldReturnTrue_WhenMessageIsSelected()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        var viewModel = CreateViewModel();
        var message = new ServiceBusMessage { MessageId = "test-123" };

        // Act
        viewModel.SelectedMessage = message;

        // Assert
        viewModel.HasSelectedMessage.ShouldBeTrue();
    }

    [Fact]
    public void HasSelectedMessage_ShouldReturnFalse_WhenNoMessageIsSelected()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.HasSelectedMessage.ShouldBeFalse();
    }

    [Fact]
    public void HasMessages_ShouldReturnTrue_WhenMessagesExist()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        var viewModel = CreateViewModel();

        // Act
        viewModel.Messages.Add(new ServiceBusMessage { MessageId = "test" });

        // Assert
        viewModel.HasMessages.ShouldBeTrue();
    }

    [Fact]
    public void HasMessages_ShouldReturnFalse_WhenNoMessagesExist()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.HasMessages.ShouldBeFalse();
    }

    [Fact]
    public async Task Constructor_ShouldLoadConnections()
    {
        // Arrange
        var connections = new List<ServiceBusConnection>
        {
            new() { Id = "1", Name = "Connection 1", ConnectionString = "test1" },
            new() { Id = "2", Name = "Connection 2", ConnectionString = "test2" }
        };

        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(connections);

        // Act
        var viewModel = CreateViewModel();
        await Task.Delay(100); // Give async load time to complete

        // Assert
        viewModel.Connections.Count.ShouldBe(2);
        viewModel.Connections[0].Name.ShouldBe("Connection 1");
        viewModel.Connections[1].Name.ShouldBe("Connection 2");
    }

    [Fact]
    public async Task Constructor_WhenLoadConnectionsFails_ShouldLogError()
    {
        // Arrange
        var exception = new Exception("Failed to load");
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ThrowsAsync(exception);

        // Act
        var viewModel = CreateViewModel();
        await Task.Delay(100);

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
    public async Task ConnectAsync_ShouldUpdateStatusDuringConnection()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        
        _mockManagementService
            .Setup(x => x.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        _mockMessageService
            .Setup(x => x.Initialize(It.IsAny<string>()));

        var viewModel = CreateViewModel();
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test";
        var connectionName = "Test Connection";

        // Act
        await viewModel.ConnectAsync(connectionString, connectionName);

        // Assert
        viewModel.StatusText.ShouldBe("Ready");
        viewModel.ConnectionStatus.ShouldBe("Connected to Test Connection");
        viewModel.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task ConnectAsync_ShouldCallManagementServiceConnect()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        
        _mockManagementService
            .Setup(x => x.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        _mockMessageService
            .Setup(x => x.Initialize(It.IsAny<string>()));

        var viewModel = CreateViewModel();
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test";

        // Act
        await viewModel.ConnectAsync(connectionString, "Test");

        // Assert
        _mockManagementService.Verify(x => x.ConnectAsync(connectionString), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_ShouldInitializeMessageService()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        
        _mockManagementService
            .Setup(x => x.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        _mockMessageService
            .Setup(x => x.Initialize(It.IsAny<string>()));

        var viewModel = CreateViewModel();
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test";

        // Act
        await viewModel.ConnectAsync(connectionString, "Test");

        // Assert
        _mockMessageService.Verify(x => x.Initialize(connectionString), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WhenFails_ShouldUpdateStatusAndShowError()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());
        
        var exception = new Exception("Connection failed");
        _mockManagementService
            .Setup(x => x.ConnectAsync(It.IsAny<string>()))
            .ThrowsAsync(exception);

        var viewModel = CreateViewModel();
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test";

        // Act
        await viewModel.ConnectAsync(connectionString, "Test");

        // Assert
        viewModel.StatusText.ShouldBe("Connection failed");
        viewModel.ConnectionStatus.ShouldBe("Connection failed");
        _mockErrorDialogService.Verify(
            x => x.ShowErrorDialog("Connection Error", exception, It.IsAny<Avalonia.Controls.Window>()),
            Times.Once);
    }

    [Fact]
    public async Task AddConnectionCommand_ShouldCallShowAddConnectionDialog()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        var viewModel = CreateViewModel();
        var dialogCalled = false;
        viewModel.ShowAddConnectionDialog = () =>
        {
            dialogCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await viewModel.AddConnectionCommand.Execute().FirstAsync();

        // Assert
        dialogCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task AddConnectionCommand_ShouldReloadConnectionsAfterDialog()
    {
        // Arrange
        var initialConnections = new List<ServiceBusConnection>();
        var updatedConnections = new List<ServiceBusConnection>
        {
            new() { Id = "1", Name = "New Connection", ConnectionString = "test" }
        };

        _mockConnectionService
            .SetupSequence(x => x.GetConnectionsAsync())
            .ReturnsAsync(initialConnections)
            .ReturnsAsync(updatedConnections);

        var viewModel = CreateViewModel();
        await Task.Delay(100); // Let initial load complete

        viewModel.ShowAddConnectionDialog = () => Task.CompletedTask;

        // Act
        await viewModel.AddConnectionCommand.Execute().FirstAsync();
        await Task.Delay(100); // Let reload complete

        // Assert
        viewModel.Connections.Count.ShouldBe(1);
        viewModel.Connections[0].Name.ShouldBe("New Connection");
    }

    [Fact]
    public void EntitiesTreeViewModel_SelectedEntity_ShouldSyncWithMainViewModel()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        var viewModel = CreateViewModel();
        var entity = new EntityTreeItemViewModel { Name = "test-queue", Type = "Queue" };

        // Act
        viewModel.EntitiesTreeViewModel.SelectedEntity = entity;

        // Assert
        viewModel.SelectedEntity.ShouldBe(entity);
        viewModel.MessageManagementViewModel.SelectedEntity.ShouldBe(entity);
    }

    [Fact]
    public void SelectedMessage_WhenChanged_ShouldUpdateHasSelectedMessage()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        var viewModel = CreateViewModel();
        var hasSelectedMessageChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.HasSelectedMessage))
                hasSelectedMessageChanged = true;
        };

        // Act
        viewModel.SelectedMessage = new ServiceBusMessage { MessageId = "test" };

        // Assert
        hasSelectedMessageChanged.ShouldBeTrue();
    }

    [Fact]
    public void Messages_WhenChanged_ShouldUpdateHasMessages()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        var viewModel = CreateViewModel();
        var hasMessagesChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.HasMessages))
                hasMessagesChanged = true;
        };

        // Act
        viewModel.Messages.Add(new ServiceBusMessage { MessageId = "test" });

        // Assert
        hasMessagesChanged.ShouldBeTrue();
    }

    [Fact]
    public void Properties_ShouldRaisePropertyChangedEvents()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.GetConnectionsAsync())
            .ReturnsAsync(new List<ServiceBusConnection>());

        var viewModel = CreateViewModel();
        var statusTextChanged = false;
        var connectionStatusChanged = false;
        var isConnectedChanged = false;
        var maxMessagesToShowChanged = false;
        var isLoadingMessagesChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.StatusText))
                statusTextChanged = true;
            if (args.PropertyName == nameof(MainWindowViewModel.ConnectionStatus))
                connectionStatusChanged = true;
            if (args.PropertyName == nameof(MainWindowViewModel.IsConnected))
                isConnectedChanged = true;
            if (args.PropertyName == nameof(MainWindowViewModel.MaxMessagesToShow))
                maxMessagesToShowChanged = true;
            if (args.PropertyName == nameof(MainWindowViewModel.IsLoadingMessages))
                isLoadingMessagesChanged = true;
        };

        // Act
        viewModel.StatusText = "Test";
        viewModel.ConnectionStatus = "Test Status";
        viewModel.IsConnected = true;
        viewModel.MaxMessagesToShow = 200;
        viewModel.IsLoadingMessages = true;

        // Assert
        statusTextChanged.ShouldBeTrue();
        connectionStatusChanged.ShouldBeTrue();
        isConnectedChanged.ShouldBeTrue();
        maxMessagesToShowChanged.ShouldBeTrue();
        isLoadingMessagesChanged.ShouldBeTrue();
    }
}

