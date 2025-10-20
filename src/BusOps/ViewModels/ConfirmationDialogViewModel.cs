using ReactiveUI;

namespace BusOps.ViewModels;

public class ConfirmationDialogViewModel : ViewModelBase
{
    private string _title = "Confirmation";
    private string _message = string.Empty;
    private string _confirmButtonText = "Yes";
    private string _cancelButtonText = "No";

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    public string ConfirmButtonText
    {
        get => _confirmButtonText;
        set => this.RaiseAndSetIfChanged(ref _confirmButtonText, value);
    }

    public string CancelButtonText
    {
        get => _cancelButtonText;
        set => this.RaiseAndSetIfChanged(ref _cancelButtonText, value);
    }
}

