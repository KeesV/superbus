using BusOps.Core.Models;

namespace BusOps.Core.Interfaces;

public interface IServiceBusConnectionService
{
    Task<IEnumerable<ServiceBusConnection>> GetConnectionsAsync();
    Task<ServiceBusConnection?> GetConnectionAsync(string id);
    Task<ServiceBusConnection> SaveConnectionAsync(ServiceBusConnection connection);
    Task DeleteConnectionAsync(string id);
    Task<bool> TestConnectionAsync(string connectionString);
    Task<IEnumerable<DiscoveredServiceBusNamespace>> DiscoverNamespacesAsync();

}