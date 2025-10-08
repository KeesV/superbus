using ReactiveUI;

namespace BusOps.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private string _greeting = "Welcome to BusOps!";

    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }
}