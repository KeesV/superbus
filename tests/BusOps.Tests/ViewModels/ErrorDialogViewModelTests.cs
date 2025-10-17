using BusOps.ViewModels;
using Shouldly;

namespace BusOps.Tests.ViewModels;

public class ErrorDialogViewModelTests
{
    [Fact]
    public void DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var viewModel = new ErrorDialogViewModel();

        // Assert
        viewModel.Title.ShouldBe("Error");
        viewModel.ErrorDetails.ShouldBe(string.Empty);
    }

    [Fact]
    public void ParameterizedConstructor_ShouldSetProperties()
    {
        // Arrange
        var title = "Connection Error";
        var details = "Failed to connect to service bus";

        // Act
        var viewModel = new ErrorDialogViewModel(title, details);

        // Assert
        viewModel.Title.ShouldBe(title);
        viewModel.ErrorDetails.ShouldBe(details);
    }

    [Fact]
    public void Title_ShouldBeSettable()
    {
        // Arrange
        var viewModel = new ErrorDialogViewModel();

        // Act
        viewModel.Title = "Custom Error";

        // Assert
        viewModel.Title.ShouldBe("Custom Error");
    }

    [Fact]
    public void ErrorDetails_ShouldBeSettable()
    {
        // Arrange
        var viewModel = new ErrorDialogViewModel();

        // Act
        viewModel.ErrorDetails = "Error details here";

        // Assert
        viewModel.ErrorDetails.ShouldBe("Error details here");
    }

    [Fact]
    public void FromException_ShouldCreateViewModelWithExceptionDetails()
    {
        // Arrange
        var title = "Test Error";
        var exception = new InvalidOperationException("Something went wrong");

        // Act
        var viewModel = ErrorDialogViewModel.FromException(title, exception);

        // Assert
        viewModel.Title.ShouldBe(title);
        viewModel.ErrorDetails.ShouldContain("Message: Something went wrong");
        viewModel.ErrorDetails.ShouldContain("Type: System.InvalidOperationException");
        viewModel.ErrorDetails.ShouldContain("Stack Trace:");
    }

    [Fact]
    public void FromException_WithInnerException_ShouldIncludeInnerExceptionDetails()
    {
        // Arrange
        var title = "Test Error";
        var innerException = new ArgumentException("Inner error");
        var exception = new InvalidOperationException("Outer error", innerException);

        // Act
        var viewModel = ErrorDialogViewModel.FromException(title, exception);

        // Assert
        viewModel.Title.ShouldBe(title);
        viewModel.ErrorDetails.ShouldContain("Message: Outer error");
        viewModel.ErrorDetails.ShouldContain("Inner Exception: Inner error");
        viewModel.ErrorDetails.ShouldContain("Stack Trace:");
    }

    [Fact]
    public void FromException_WithoutInnerException_ShouldNotIncludeInnerExceptionSection()
    {
        // Arrange
        var title = "Test Error";
        var exception = new InvalidOperationException("Error without inner exception");

        // Act
        var viewModel = ErrorDialogViewModel.FromException(title, exception);

        // Assert
        viewModel.ErrorDetails.ShouldNotContain("Inner Exception:");
    }

    [Fact]
    public void FromException_ShouldIncludeExceptionTypeName()
    {
        // Arrange
        var title = "Test Error";
        var exception = new ArgumentNullException(nameof(title), "Parameter cannot be null");

        // Act
        var viewModel = ErrorDialogViewModel.FromException(title, exception);

        // Assert
        viewModel.ErrorDetails.ShouldContain("Type: System.ArgumentNullException");
    }

    [Fact]
    public void Properties_ShouldRaisePropertyChangedEvents()
    {
        // Arrange
        var viewModel = new ErrorDialogViewModel();
        var titleChanged = false;
        var errorDetailsChanged = false;

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ErrorDialogViewModel.Title))
                titleChanged = true;
            if (args.PropertyName == nameof(ErrorDialogViewModel.ErrorDetails))
                errorDetailsChanged = true;
        };

        // Act
        viewModel.Title = "New Title";
        viewModel.ErrorDetails = "New Details";

        // Assert
        titleChanged.ShouldBeTrue();
        errorDetailsChanged.ShouldBeTrue();
    }

    [Fact]
    public void FromException_ShouldFormatDetailsWithLineBreaks()
    {
        // Arrange
        var title = "Test Error";
        var exception = new InvalidOperationException("Test message");

        // Act
        var viewModel = ErrorDialogViewModel.FromException(title, exception);

        // Assert
        viewModel.ErrorDetails.ShouldContain("\n\n"); // Should have line breaks between sections
    }
}

