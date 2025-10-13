using BusOps.Core.Models;
using ReactiveUI;
using System.Reactive;

namespace BusOps.ViewModels;

/// <summary>
/// ViewModel for individual connection list items
/// </summary>
public class ConnectionItemViewModel : ViewModelBase
{
    private readonly ServiceBusConnection _connection;
    private readonly MainWindowViewModel _mainViewModel;

    public string Id => _connection.Id;
    public string Name => _connection.Name;
    public string? Description => _connection.Description;
    public bool IsActive => _connection.IsActive;
    public DateTimeOffset? LastConnected => _connection.LastConnected;

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public ConnectionItemViewModel(ServiceBusConnection connection, MainWindowViewModel mainViewModel)
    {
        _connection = connection;
        _mainViewModel = mainViewModel;
        ConnectCommand = ReactiveCommand.CreateFromTask(OnConnectAsync);
    }

    private async Task OnConnectAsync()
    {
        await _mainViewModel.ConnectAsync(_connection.ConnectionString, _connection.Name);
    }

    public ServiceBusConnection GetConnection() => _connection;
}
