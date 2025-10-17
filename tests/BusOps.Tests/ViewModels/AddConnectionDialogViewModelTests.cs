using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using BusOps.ViewModels;
using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reactive.Linq;

namespace BusOps.Tests.ViewModels;

public class AddConnectionDialogViewModelTests
{
    private readonly Mock<IServiceBusConnectionService> _mockConnectionService;
    private readonly Mock<ILogger<AddConnectionDialogViewModel>> _mockLogger;

    public AddConnectionDialogViewModelTests()
    {
        _mockConnectionService = new Mock<IServiceBusConnectionService>();
        _mockLogger = new Mock<ILogger<AddConnectionDialogViewModel>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Assert
        viewModel.DiscoveredNamespaces.ShouldBeEmpty();
        viewModel.SelectedNamespace.ShouldBeNull();
        viewModel.IsDiscovering.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldBeEmpty();
        viewModel.ConnectionName.ShouldBeEmpty();
        viewModel.Description.ShouldBeEmpty();
        viewModel.CustomConnectionString.ShouldBeEmpty();
        viewModel.UseCustomConnectionString.ShouldBeFalse();
        viewModel.CreatedConnection.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeCommands()
    {
        // Arrange & Act
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Assert
        viewModel.DiscoverNamespacesCommand.ShouldNotBeNull();
        viewModel.AddCommand.ShouldNotBeNull();
        viewModel.CancelCommand.ShouldNotBeNull();
    }

    [Fact]
    public async Task DiscoverNamespacesAsync_ShouldPopulateDiscoveredNamespaces()
    {
        // Arrange
        var namespaces = new List<DiscoveredServiceBusNamespace>
        {
            new() { Name = "namespace1", FullyQualifiedNamespace = "namespace1.servicebus.windows.net" },
            new() { Name = "namespace2", FullyQualifiedNamespace = "namespace2.servicebus.windows.net" }
        };

        _mockConnectionService
            .Setup(x => x.DiscoverNamespacesAsync())
            .ReturnsAsync(namespaces);

        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Act
        await viewModel.DiscoverNamespacesCommand.Execute().FirstAsync();

        // Assert
        viewModel.DiscoveredNamespaces.Count.ShouldBe(2);
        viewModel.DiscoveredNamespaces[0].Name.ShouldBe("namespace1");
        viewModel.DiscoveredNamespaces[1].Name.ShouldBe("namespace2");
        viewModel.IsDiscovering.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldBeEmpty();
    }

    [Fact]
    public async Task DiscoverNamespacesAsync_WhenNoNamespacesFound_ShouldSetErrorMessage()
    {
        // Arrange
        _mockConnectionService
            .Setup(x => x.DiscoverNamespacesAsync())
            .ReturnsAsync(new List<DiscoveredServiceBusNamespace>());

        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Act
        await viewModel.DiscoverNamespacesCommand.Execute().FirstAsync();

        // Assert
        viewModel.DiscoveredNamespaces.ShouldBeEmpty();
        viewModel.ErrorMessage.ShouldContain("No Service Bus namespaces found");
        viewModel.IsDiscovering.ShouldBeFalse();
    }

    [Fact]
    public async Task DiscoverNamespacesAsync_WhenExceptionThrown_ShouldSetErrorMessage()
    {
        // Arrange
        var exceptionMessage = "Authentication failed";
        _mockConnectionService
            .Setup(x => x.DiscoverNamespacesAsync())
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Act
        await viewModel.DiscoverNamespacesCommand.Execute().FirstAsync();

        // Assert
        viewModel.ErrorMessage.ShouldContain("Failed to discover namespaces");
        viewModel.ErrorMessage.ShouldContain(exceptionMessage);
        viewModel.IsDiscovering.ShouldBeFalse();
    }

    [Fact]
    public async Task DiscoverNamespacesAsync_ShouldClearPreviousResults()
    {
        // Arrange
        var firstNamespaces = new List<DiscoveredServiceBusNamespace>
        {
            new() { Name = "namespace1", FullyQualifiedNamespace = "namespace1.servicebus.windows.net" }
        };

        var secondNamespaces = new List<DiscoveredServiceBusNamespace>
        {
            new() { Name = "namespace2", FullyQualifiedNamespace = "namespace2.servicebus.windows.net" }
        };

        _mockConnectionService
            .SetupSequence(x => x.DiscoverNamespacesAsync())
            .ReturnsAsync(firstNamespaces)
            .ReturnsAsync(secondNamespaces);

        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Act
        await viewModel.DiscoverNamespacesCommand.Execute().FirstAsync();
        await viewModel.DiscoverNamespacesCommand.Execute().FirstAsync();

        // Assert
        viewModel.DiscoveredNamespaces.Count.ShouldBe(1);
        viewModel.DiscoveredNamespaces[0].Name.ShouldBe("namespace2");
    }

    [Fact]
    public void SelectedNamespace_WhenSet_ShouldAutoPopulateConnectionName()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        var ns = new DiscoveredServiceBusNamespace 
        { 
            Name = "test-namespace",
            FullyQualifiedNamespace = "test-namespace.servicebus.windows.net" 
        };

        // Act
        viewModel.SelectedNamespace = ns;

        // Assert
        viewModel.ConnectionName.ShouldBe("test-namespace");
    }

    [Fact]
    public void SelectedNamespace_WhenConnectionNameAlreadySet_ShouldNotOverwrite()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        viewModel.ConnectionName = "Custom Name";
        var ns = new DiscoveredServiceBusNamespace 
        { 
            Name = "test-namespace",
            FullyQualifiedNamespace = "test-namespace.servicebus.windows.net" 
        };

        // Act
        viewModel.SelectedNamespace = ns;

        // Assert
        viewModel.ConnectionName.ShouldBe("Custom Name");
    }

    [Fact]
    public async Task SaveConnectionAsync_WithSelectedNamespace_ShouldSaveSuccessfully()
    {
        // Arrange
        var expectedConnection = new ServiceBusConnection
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Connection",
            ConnectionString = "test-namespace.servicebus.windows.net",
            Description = "Test Description",
            IsActive = true
        };

        _mockConnectionService
            .Setup(x => x.SaveConnectionAsync(It.IsAny<ServiceBusConnection>()))
            .ReturnsAsync(expectedConnection);

        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        viewModel.SelectedNamespace = new DiscoveredServiceBusNamespace 
        { 
            Name = "test-namespace",
            FullyQualifiedNamespace = "test-namespace.servicebus.windows.net" 
        };
        viewModel.ConnectionName = "Test Connection";
        viewModel.Description = "Test Description";

        // Act
        var result = await viewModel.SaveConnectionAsync();

        // Assert
        result.ShouldBeTrue();
        viewModel.CreatedConnection.ShouldNotBeNull();
        viewModel.CreatedConnection!.Name.ShouldBe("Test Connection");
        viewModel.ErrorMessage.ShouldBeEmpty();
        
        _mockConnectionService.Verify(x => x.SaveConnectionAsync(
            It.Is<ServiceBusConnection>(c => 
                c.Name == "Test Connection" &&
                c.ConnectionString == "test-namespace.servicebus.windows.net" &&
                c.Description == "Test Description" &&
                c.IsActive == true
            )), Times.Once);
    }

    [Fact]
    public async Task SaveConnectionAsync_WithCustomConnectionString_ShouldSaveSuccessfully()
    {
        // Arrange
        var customConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key";
        var expectedConnection = new ServiceBusConnection
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Custom Connection",
            ConnectionString = customConnectionString,
            IsActive = true
        };

        _mockConnectionService
            .Setup(x => x.SaveConnectionAsync(It.IsAny<ServiceBusConnection>()))
            .ReturnsAsync(expectedConnection);

        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        viewModel.UseCustomConnectionString = true;
        viewModel.CustomConnectionString = customConnectionString;
        viewModel.ConnectionName = "Custom Connection";

        // Act
        var result = await viewModel.SaveConnectionAsync();

        // Assert
        result.ShouldBeTrue();
        viewModel.CreatedConnection.ShouldNotBeNull();
        viewModel.CreatedConnection!.ConnectionString.ShouldBe(customConnectionString);
        
        _mockConnectionService.Verify(x => x.SaveConnectionAsync(
            It.Is<ServiceBusConnection>(c => 
                c.Name == "Custom Connection" &&
                c.ConnectionString == customConnectionString &&
                c.IsActive == true
            )), Times.Once);
    }

    [Fact]
    public async Task SaveConnectionAsync_WithoutNamespaceOrConnectionString_ShouldFail()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        viewModel.ConnectionName = "Test Connection";

        // Act
        var result = await viewModel.SaveConnectionAsync();

        // Assert
        result.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldContain("Please select a namespace or provide a connection string");
        viewModel.CreatedConnection.ShouldBeNull();
        
        _mockConnectionService.Verify(x => x.SaveConnectionAsync(It.IsAny<ServiceBusConnection>()), Times.Never);
    }

    [Fact]
    public async Task SaveConnectionAsync_WhenServiceThrowsException_ShouldReturnFalse()
    {
        // Arrange
        var exceptionMessage = "Database error";
        _mockConnectionService
            .Setup(x => x.SaveConnectionAsync(It.IsAny<ServiceBusConnection>()))
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        viewModel.SelectedNamespace = new DiscoveredServiceBusNamespace 
        { 
            Name = "test-namespace",
            FullyQualifiedNamespace = "test-namespace.servicebus.windows.net" 
        };
        viewModel.ConnectionName = "Test Connection";

        // Act
        var result = await viewModel.SaveConnectionAsync();

        // Assert
        result.ShouldBeFalse();
        viewModel.ErrorMessage.ShouldContain("Failed to save connection");
        viewModel.ErrorMessage.ShouldContain(exceptionMessage);
        viewModel.CreatedConnection.ShouldBeNull();
    }

    [Fact]
    public async Task AddCommand_WhenConnectionNameEmptyAndNamespaceSelected_ShouldBeEnabled()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        
        // Act - setting namespace auto-populates ConnectionName
        viewModel.SelectedNamespace = new DiscoveredServiceBusNamespace 
        { 
            Name = "test-namespace",
            FullyQualifiedNamespace = "test-namespace.servicebus.windows.net" 
        };

        // Assert - Wait for the observable to emit and test the value
        var canExecute = await viewModel.AddCommand.CanExecute.FirstAsync();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task AddCommand_WhenNoNamespaceAndNoConnectionName_ShouldBeDisabled()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Act & Assert - Wait for the observable to emit
        var canExecute = await viewModel.AddCommand.CanExecute.FirstAsync();
        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task AddCommand_WhenConnectionNameSetAndNamespaceSelected_ShouldBeEnabled()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        viewModel.SelectedNamespace = new DiscoveredServiceBusNamespace 
        { 
            Name = "test-namespace",
            FullyQualifiedNamespace = "test-namespace.servicebus.windows.net" 
        };
        viewModel.ConnectionName = "Test Connection";

        // Act & Assert - Wait for the observable to emit
        var canExecute = await viewModel.AddCommand.CanExecute.FirstAsync();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task AddCommand_WhenUsingCustomConnectionStringWithValidData_ShouldBeEnabled()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        viewModel.UseCustomConnectionString = true;
        viewModel.CustomConnectionString = "Endpoint=sb://test.servicebus.windows.net/";
        viewModel.ConnectionName = "Test Connection";

        // Act & Assert - Wait for the observable to emit
        var canExecute = await viewModel.AddCommand.CanExecute.FirstAsync();
        canExecute.ShouldBeTrue();
    }

    [Fact]
    public async Task AddCommand_WhenUsingCustomConnectionStringWithoutConnectionString_ShouldBeDisabled()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        viewModel.UseCustomConnectionString = true;
        viewModel.ConnectionName = "Test Connection";

        // Act & Assert - Wait for the observable to emit
        var canExecute = await viewModel.AddCommand.CanExecute.FirstAsync();
        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void PropertyChanges_ShouldRaisePropertyChangedEvents()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);
        var propertyChangedEvents = new List<string>();
        
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != null)
                propertyChangedEvents.Add(args.PropertyName);
        };

        // Act
        viewModel.ConnectionName = "Test";
        viewModel.Description = "Description";
        viewModel.CustomConnectionString = "Connection";
        viewModel.UseCustomConnectionString = true;
        viewModel.IsDiscovering = true;
        viewModel.ErrorMessage = "Error";

        // Assert
        propertyChangedEvents.ShouldContain("ConnectionName");
        propertyChangedEvents.ShouldContain("Description");
        propertyChangedEvents.ShouldContain("CustomConnectionString");
        propertyChangedEvents.ShouldContain("UseCustomConnectionString");
        propertyChangedEvents.ShouldContain("IsDiscovering");
        propertyChangedEvents.ShouldContain("ErrorMessage");
    }

    [Fact]
    public async Task Constructor_WithStartDiscoveryTrue_ShouldStartDiscoveryAutomatically()
    {
        // Arrange
        var namespaces = new List<DiscoveredServiceBusNamespace>
        {
            new() { Name = "namespace1", FullyQualifiedNamespace = "namespace1.servicebus.windows.net" }
        };

        var tcs = new TaskCompletionSource<IEnumerable<DiscoveredServiceBusNamespace>>();
        _mockConnectionService
            .Setup(x => x.DiscoverNamespacesAsync())
            .Returns(tcs.Task);

        // Act
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: true);
        
        // Complete the async operation
        tcs.SetResult(namespaces);
        
        // Wait for the command to complete by observing it
        await viewModel.DiscoverNamespacesCommand.IsExecuting.Where(isExecuting => !isExecuting).FirstAsync();

        // Assert
        _mockConnectionService.Verify(x => x.DiscoverNamespacesAsync(), Times.Once);
        viewModel.DiscoveredNamespaces.Count.ShouldBe(1);
    }

    [Fact]
    public void Constructor_WithStartDiscoveryFalse_ShouldNotStartDiscovery()
    {
        // Arrange & Act
        _ = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Assert
        _mockConnectionService.Verify(x => x.DiscoverNamespacesAsync(), Times.Never);
    }

    [Fact]
    public void CancelCommand_ShouldAlwaysBeExecutable()
    {
        // Arrange
        var viewModel = new AddConnectionDialogViewModel(_mockConnectionService.Object, _mockLogger.Object, startDiscovery: false);

        // Act & Assert
        var canExecute = viewModel.CancelCommand.CanExecute.FirstAsync().Wait();
        canExecute.ShouldBeTrue();
    }
}
