using BusOps.Core.Models;
using ReactiveUI;
using System.Reactive;

namespace BusOps.ViewModels;

/// <summary>
/// ViewModel for individual connection list items
/// </summary>
public class ConnectionItemViewModel : ReactiveObject
{
    private readonly ServiceBusConnection _connection;

    public string Id => _connection.Id;
    public string Name => _connection.Name;
    public string? Description => _connection.Description;
    public bool IsActive => _connection.IsActive;
    public DateTimeOffset? LastConnected => _connection.LastConnected;

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public ConnectionItemViewModel(ServiceBusConnection connection)
    {
        _connection = connection;
        ConnectCommand = ReactiveCommand.Create(OnConnect);
    }

    private void OnConnect()
    {
        // TODO: Implement connection logic
        // This will be implemented later
    }

    public ServiceBusConnection GetConnection() => _connection;
}

