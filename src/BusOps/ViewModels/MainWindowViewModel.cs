using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace BusOps.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private string _greeting = "Welcome to BusOps!";

    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }

    public ReactiveCommand<Unit, Unit> AddConnectionCommand { get; }
    
    // This will be set by the view
    public System.Func<Task>? ShowAddConnectionDialog { get; set; }

    public MainWindowViewModel()
    {
        AddConnectionCommand = ReactiveCommand.CreateFromTask(OnAddConnectionAsync);
    }

    private async Task OnAddConnectionAsync()
    {
        if (ShowAddConnectionDialog != null)
        {
            await ShowAddConnectionDialog();
        }
    }
}