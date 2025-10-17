using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using BusOps.Services;
using BusOps.ViewModels;
using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reactive.Linq;

namespace BusOps.Tests.ViewModels;

public class ConnectionItemViewModelTests
{
    private readonly Mock<IServiceBusConnectionService> _mockConnectionService;
    private readonly Mock<IServiceBusManagementService> _mockManagementService;
    private readonly Mock<IServiceBusMessageService> _mockMessageService;
    private readonly Mock<IErrorDialogService> _mockErrorDialogService;
    private readonly Mock<ILogger<MainWindowViewModel>> _mockLogger;
    private readonly Mock<ILogger<EntitiesTreeViewModel>> _mockEntitiesTreeLogger;
    private readonly Mock<ILogger<MessageManagementViewModel>> _mockMessageManagementLogger;

    public ConnectionItemViewModelTests()
    {
        _mockConnectionService = new Mock<IServiceBusConnectionService>();
        _mockManagementService = new Mock<IServiceBusManagementService>();
        _mockMessageService = new Mock<IServiceBusMessageService>();
        _mockErrorDialogService = new Mock<IErrorDialogService>();
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();
        _mockEntitiesTreeLogger = new Mock<ILogger<EntitiesTreeViewModel>>();
        _mockMessageManagementLogger = new Mock<ILogger<MessageManagementViewModel>>();
    }

    private MainWindowViewModel CreateMainWindowViewModel()
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
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = "Test Connection",
            Description = "Test Description",
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey",
            IsActive = true,
            LastConnected = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var mainViewModel = CreateMainWindowViewModel();

        // Act
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        // Assert
        viewModel.Id.ShouldBe("test-id");
        viewModel.Name.ShouldBe("Test Connection");
        viewModel.Description.ShouldBe("Test Description");
        viewModel.IsActive.ShouldBeTrue();
        viewModel.LastConnected.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldHandleNullDescription()
    {
        // Arrange
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = "Test Connection",
            Description = null,
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey",
            IsActive = false,
            LastConnected = null
        };
        var mainViewModel = CreateMainWindowViewModel();

        // Act
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        // Assert
        viewModel.Description.ShouldBeNull();
        viewModel.IsActive.ShouldBeFalse();
        viewModel.LastConnected.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeConnectCommand()
    {
        // Arrange
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = "Test Connection",
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey"
        };
        var mainViewModel = CreateMainWindowViewModel();

        // Act
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        // Assert
        viewModel.ConnectCommand.ShouldNotBeNull();
    }

    [Fact]
    public async Task ConnectCommand_ShouldCallMainViewModelConnectAsync()
    {
        // Arrange
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = "Test Connection",
            ConnectionString = connectionString
        };
        var mainViewModel = CreateMainWindowViewModel();
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        _mockManagementService
            .Setup(x => x.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        _mockMessageService
            .Setup(x => x.Initialize(It.IsAny<string>()));

        // Act
        await viewModel.ConnectCommand.Execute().FirstAsync();

        // Assert
        _mockManagementService.Verify(
            x => x.ConnectAsync(connectionString), 
            Times.Once);
    }

    [Fact]
    public async Task ConnectCommand_ShouldInitializeMessageService()
    {
        // Arrange
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = "Test Connection",
            ConnectionString = connectionString
        };
        var mainViewModel = CreateMainWindowViewModel();
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        _mockManagementService
            .Setup(x => x.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        _mockMessageService
            .Setup(x => x.Initialize(It.IsAny<string>()));

        // Act
        await viewModel.ConnectCommand.Execute().FirstAsync();

        // Assert
        _mockMessageService.Verify(
            x => x.Initialize(connectionString), 
            Times.Once);
    }

    [Fact]
    public void GetConnection_ShouldReturnOriginalConnection()
    {
        // Arrange
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = "Test Connection",
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey"
        };
        var mainViewModel = CreateMainWindowViewModel();
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        // Act
        var result = viewModel.GetConnection();

        // Assert
        result.ShouldBeSameAs(connection);
        result.Id.ShouldBe("test-id");
        result.Name.ShouldBe("Test Connection");
    }

    [Fact]
    public void Properties_ShouldReflectConnectionState()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-7);
        var lastConnected = DateTimeOffset.UtcNow.AddHours(-2);
        
        var connection = new ServiceBusConnection
        {
            Id = "test-id-123",
            Name = "Production Connection",
            Description = "Production Service Bus",
            ConnectionString = "Endpoint=sb://prod.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey",
            IsActive = true,
            CreatedAt = createdAt,
            LastConnected = lastConnected
        };
        var mainViewModel = CreateMainWindowViewModel();

        // Act
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        // Assert
        viewModel.Id.ShouldBe("test-id-123");
        viewModel.Name.ShouldBe("Production Connection");
        viewModel.Description.ShouldBe("Production Service Bus");
        viewModel.IsActive.ShouldBeTrue();
        viewModel.LastConnected.ShouldBe(lastConnected);
    }

    [Fact]
    public async Task ConnectCommand_ShouldUpdateConnectionStatus()
    {
        // Arrange
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        var connectionName = "Test Connection";
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = connectionName,
            ConnectionString = connectionString
        };
        var mainViewModel = CreateMainWindowViewModel();
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        _mockManagementService
            .Setup(x => x.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        _mockMessageService
            .Setup(x => x.Initialize(It.IsAny<string>()));

        // Act
        await viewModel.ConnectCommand.Execute().FirstAsync();

        // Assert
        mainViewModel.IsConnected.ShouldBeTrue();
        mainViewModel.ConnectionStatus.ShouldBe($"Connected to {connectionName}");
    }

    [Fact]
    public async Task ConnectCommand_WhenConnectionFails_ShouldUpdateStatus()
    {
        // Arrange
        var connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = "Test Connection",
            ConnectionString = connectionString
        };
        var mainViewModel = CreateMainWindowViewModel();
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        _mockManagementService
            .Setup(x => x.ConnectAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Connection failed"));

        // Act
        await viewModel.ConnectCommand.Execute().FirstAsync();

        // Assert
        mainViewModel.StatusText.ShouldBe("Connection failed");
        mainViewModel.ConnectionStatus.ShouldBe("Connection failed");
    }

    [Fact]
    public void Constructor_ShouldStoreMainViewModel()
    {
        // Arrange
        var connection = new ServiceBusConnection
        {
            Id = "test-id",
            Name = "Test Connection",
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey"
        };
        var mainViewModel = CreateMainWindowViewModel();

        // Act
        var viewModel = new ConnectionItemViewModel(connection, mainViewModel);

        // Assert - The viewModel should be able to execute commands using the main view model
        viewModel.ConnectCommand.ShouldNotBeNull();
        viewModel.ConnectCommand.CanExecute.Subscribe(_ => { }).Dispose();
    }
}
