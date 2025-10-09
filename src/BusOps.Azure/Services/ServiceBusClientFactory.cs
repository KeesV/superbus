using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;

namespace BusOps.Azure.Services;

/// <summary>
/// Factory class for creating Azure Service Bus clients with support for both
/// connection string and Azure AD authentication.
/// </summary>
public class ServiceBusClientFactory : IServiceBusClientFactory
{
    private readonly ILogger<ServiceBusClientFactory> _logger;

    public ServiceBusClientFactory(ILogger<ServiceBusClientFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a ServiceBusClient from either a connection string or FQDN.
    /// </summary>
    public ServiceBusClient CreateClient(string connectionStringOrNamespace)
    {
        if (IsConnectionString(connectionStringOrNamespace))
        {
            _logger.LogDebug("Creating ServiceBusClient using connection string");
            return new ServiceBusClient(connectionStringOrNamespace);
        }
        else
        {
            var fullyQualifiedNamespace = EnsureFullyQualifiedNamespace(connectionStringOrNamespace);
            var credential = new DefaultAzureCredential();
            
            _logger.LogDebug("Creating ServiceBusClient using DefaultAzureCredential for namespace: {Namespace}", 
                fullyQualifiedNamespace);
            
            return new ServiceBusClient(fullyQualifiedNamespace, credential);
        }
    }

    /// <summary>
    /// Creates a ServiceBusAdministrationClient from either a connection string or FQDN.
    /// </summary>
    public ServiceBusAdministrationClient CreateAdministrationClient(string connectionStringOrNamespace)
    {
        if (IsConnectionString(connectionStringOrNamespace))
        {
            _logger.LogDebug("Creating ServiceBusAdministrationClient using connection string");
            return new ServiceBusAdministrationClient(connectionStringOrNamespace);
        }
        else
        {
            var fullyQualifiedNamespace = EnsureFullyQualifiedNamespace(connectionStringOrNamespace);
            var credential = new DefaultAzureCredential();
            
            _logger.LogDebug("Creating ServiceBusAdministrationClient using DefaultAzureCredential for namespace: {Namespace}", 
                fullyQualifiedNamespace);
            
            return new ServiceBusAdministrationClient(fullyQualifiedNamespace, credential);
        }
    }

    /// <summary>
    /// Determines if the input string is a connection string or a namespace identifier.
    /// </summary>
    public static bool IsConnectionString(string input)
    {
        // Connection strings typically contain "Endpoint=" and "SharedAccessKeyName=" or "SharedAccessKey="
        return input.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase) &&
               (input.Contains("SharedAccessKeyName=", StringComparison.OrdinalIgnoreCase) ||
                input.Contains("SharedAccessKey=", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ensures the namespace is in fully qualified format (e.g., namespace.servicebus.windows.net).
    /// </summary>
    public static string EnsureFullyQualifiedNamespace(string input)
    {
        // Strip port number if present (e.g., "myservicebus.servicebus.windows.net:5671" -> "myservicebus.servicebus.windows.net")
        var namespaceWithoutPort = input;
        var portIndex = input.LastIndexOf(':');
        if (portIndex > 0)
        {
            // Ensure it's actually a port number and not part of a URL scheme
            var afterColon = input.Substring(portIndex + 1);
            if (int.TryParse(afterColon, out _))
            {
                namespaceWithoutPort = input.Substring(0, portIndex);
            }
        }
        
        // If it's already a full FQDN (e.g., myservicebus.servicebus.windows.net), return as-is
        if (namespaceWithoutPort.EndsWith(".servicebus.windows.net", StringComparison.OrdinalIgnoreCase))
        {
            return namespaceWithoutPort;
        }
        
        // If it's just the namespace name (e.g., myservicebus), append the domain
        if (!namespaceWithoutPort.Contains('.'))
        {
            return $"{namespaceWithoutPort}.servicebus.windows.net";
        }
        
        // Otherwise, return as-is (might be a custom domain or already formatted)
        return namespaceWithoutPort;
    }
}

