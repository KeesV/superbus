using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace BusOps.Azure.Services;

public interface IServiceBusClientFactory
{
    /// <summary>
    /// Creates a ServiceBusClient from either a connection string or FQDN.
    /// </summary>
    ServiceBusClient CreateClient(string connectionStringOrNamespace);

    /// <summary>
    /// Creates a ServiceBusAdministrationClient from either a connection string or FQDN.
    /// </summary>
    ServiceBusAdministrationClient CreateAdministrationClient(string connectionStringOrNamespace);
}