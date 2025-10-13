using ReactiveUI;

namespace BusOps.ViewModels;

public class ErrorDialogViewModel : ViewModelBase
{
    private string _title = "Error";
    private string _errorDetails = string.Empty;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string ErrorDetails
    {
        get => _errorDetails;
        set => this.RaiseAndSetIfChanged(ref _errorDetails, value);
    }

    public ErrorDialogViewModel()
    {
    }

    public ErrorDialogViewModel(string title, string errorDetails)
    {
        _title = title;
        _errorDetails = errorDetails;
    }

    public static ErrorDialogViewModel FromException(string title, Exception exception)
    {
        var details = $"Message: {exception.Message}\n\n";
        details += $"Type: {exception.GetType().FullName}\n\n";
        
        if (exception.InnerException != null)
        {
            details += $"Inner Exception: {exception.InnerException.Message}\n\n";
        }
        
        details += $"Stack Trace:\n{exception.StackTrace}";
        
        return new ErrorDialogViewModel(title, details);
    }
}
