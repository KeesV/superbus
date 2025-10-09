using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Azure.ResourceManager;
using Azure.ResourceManager.ServiceBus;
using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BusOps.Azure.Services;

/// <summary>
/// Azure implementation of IServiceBusConnectionService that manages Service Bus connections
/// with persistent storage and support for both connection strings and Azure AD authentication.
/// </summary>
public class AzureServiceBusConnectionService : IServiceBusConnectionService
{
    private readonly ILogger<AzureServiceBusConnectionService> _logger;
    private readonly string _storageFilePath;
    private List<ServiceBusConnection>? _connections;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public AzureServiceBusConnectionService(ILogger<AzureServiceBusConnectionService> logger)
    {
        _logger = logger;
        
        // Store connections in user's local app data folder
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "BusOps");
        Directory.CreateDirectory(appFolder);
        
        _storageFilePath = Path.Combine(appFolder, "connections.json");
        
        // Load existing connections
        //LoadConnectionsAsync().GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<ServiceBusConnection>> GetConnectionsAsync()
    {
        if (_connections is not null) return _connections;
        
        await _fileLock.WaitAsync();
        try
        {
            _connections = await LoadConnectionsAsync();
            return _connections.ToList();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<ServiceBusConnection?> GetConnectionAsync(string id)
    {
        if (_connections is not null) return _connections.FirstOrDefault(x => x.Id == id);
        
        await _fileLock.WaitAsync();
        try
        {
            _connections = await LoadConnectionsAsync();
            return _connections.FirstOrDefault(c => c.Id == id);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<ServiceBusConnection> SaveConnectionAsync(ServiceBusConnection connection)
    {
        _connections ??= await LoadConnectionsAsync();
        await _fileLock.WaitAsync();
        try
        {
            var existingIndex = _connections.FindIndex(c => c.Id == connection.Id);
            
            if (existingIndex >= 0)
            {
                // Update existing connection
                _connections[existingIndex] = connection;
                _logger.LogInformation("Updated connection: {ConnectionName} (ID: {ConnectionId})", 
                    connection.Name, connection.Id);
            }
            else
            {
                // Add new connection
                if (string.IsNullOrEmpty(connection.Id))
                {
                    connection.Id = Guid.NewGuid().ToString();
                }
                
                connection.CreatedAt = DateTimeOffset.UtcNow;
                _connections.Add(connection);
                _logger.LogInformation("Added new connection: {ConnectionName} (ID: {ConnectionId})", 
                    connection.Name, connection.Id);
            }
            
            await SaveConnectionsAsync();
            return connection;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task DeleteConnectionAsync(string id)
    {
        _connections ??= await LoadConnectionsAsync();
        
        await _fileLock.WaitAsync();
        try
        {
            var connection = _connections.FirstOrDefault(c => c.Id == id);
            if (connection != null)
            {
                _connections.Remove(connection);
                await SaveConnectionsAsync();
                _logger.LogInformation("Deleted connection: {ConnectionName} (ID: {ConnectionId})", 
                    connection.Name, id);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent connection with ID: {ConnectionId}", id);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            // Try to parse and determine if it's a connection string or namespace URL
            if (IsConnectionString(connectionString))
            {
                // Test with connection string
                await TestConnectionStringAsync(connectionString);
            }
            else
            {
                // Assume it's a fully qualified namespace and test with DefaultAzureCredential
                await TestNamespaceWithAzureAdAsync(connectionString);
            }
            
            _logger.LogInformation("Connection test succeeded");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Discovers all Azure Service Bus namespaces accessible via DefaultAzureCredential.
    /// This method queries all accessible Azure subscriptions and returns their Service Bus namespaces.
    /// </summary>
    /// <returns>A list of discovered Service Bus namespaces with their metadata.</returns>
    public async Task<IEnumerable<DiscoveredServiceBusNamespace>> DiscoverNamespacesAsync()
    {
        var discoveredNamespaces = new List<DiscoveredServiceBusNamespace>();

        try
        {
            _logger.LogInformation("Starting Service Bus namespace discovery using DefaultAzureCredential");

            // Create Azure Resource Manager client with DefaultAzureCredential
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = false,
                ExcludeWorkloadIdentityCredential = false,
                ExcludeManagedIdentityCredential = false,
                ExcludeSharedTokenCacheCredential = false,
                ExcludeVisualStudioCredential = false,
                ExcludeVisualStudioCodeCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeAzurePowerShellCredential = false,
                ExcludeAzureDeveloperCliCredential = false,
                ExcludeInteractiveBrowserCredential = false
            });

            var armClient = new ArmClient(credential);

            // Get all subscriptions the user has access to
            var subscriptions = armClient.GetSubscriptions();

            await foreach (var subscription in subscriptions)
            {
                try
                {
                    _logger.LogDebug("Scanning subscription: {SubscriptionName} ({SubscriptionId})", 
                        subscription.Data.DisplayName, subscription.Data.SubscriptionId);

                    // Get all Service Bus namespaces in this subscription
                    var namespaces = subscription.GetServiceBusNamespacesAsync();

                    await foreach (var namespaceResource in namespaces)
                    {
                        try
                        {
                            var data = namespaceResource.Data;
                            
                            var discoveredNamespace = new DiscoveredServiceBusNamespace
                            {
                                Name = data.Name,
                                FullyQualifiedNamespace = data.ServiceBusEndpoint?.Replace("https://", "").Replace("/", "") ?? 
                                                          $"{data.Name}.servicebus.windows.net",
                                SubscriptionId = subscription.Data.SubscriptionId,
                                SubscriptionName = subscription.Data.DisplayName ?? subscription.Data.SubscriptionId,
                                ResourceGroup = namespaceResource.Id.ResourceGroupName ?? "Unknown",
                                Location = data.Location.Name,
                                Sku = data.Sku?.Name.ToString() ?? "Unknown",
                                Status = data.Status ?? "Unknown"
                            };

                            discoveredNamespaces.Add(discoveredNamespace);
                            
                            _logger.LogDebug("Discovered namespace: {NamespaceName} in {ResourceGroup}", 
                                discoveredNamespace.Name, discoveredNamespace.ResourceGroup);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get details for namespace in subscription {SubscriptionId}", 
                                subscription.Data.SubscriptionId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to access subscription: {SubscriptionId}", 
                        subscription.Data.SubscriptionId);
                }
            }

            _logger.LogInformation("Discovery completed. Found {Count} Service Bus namespaces", 
                discoveredNamespaces.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Service Bus namespaces: {ErrorMessage}", ex.Message);
        }

        return discoveredNamespaces;
    }

    private async Task TestConnectionStringAsync(string connectionString)
    {
        var adminClient = new ServiceBusAdministrationClient(connectionString);
        
        // Try to get namespace properties to verify connection
        var namespaceProperties = await adminClient.GetNamespacePropertiesAsync();
        
        _logger.LogDebug("Connected to namespace: {NamespaceName}", namespaceProperties.Value.Name);
    }

    private async Task TestNamespaceWithAzureAdAsync(string fullyQualifiedNamespace)
    {
        // Ensure the namespace has the correct format
        if (!fullyQualifiedNamespace.EndsWith(".servicebus.windows.net"))
        {
            fullyQualifiedNamespace = $"{fullyQualifiedNamespace}.servicebus.windows.net";
        }
        
        // Use DefaultAzureCredential for Azure AD authentication
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeWorkloadIdentityCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeSharedTokenCacheCredential = false,
            ExcludeVisualStudioCredential = false,
            ExcludeVisualStudioCodeCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeAzurePowerShellCredential = false,
            ExcludeAzureDeveloperCliCredential = false,
            ExcludeInteractiveBrowserCredential = false
        });
        
        var adminClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, credential);
        
        // Try to get namespace properties to verify connection
        var namespaceProperties = await adminClient.GetNamespacePropertiesAsync();
        
        _logger.LogDebug("Connected to namespace using Azure AD: {NamespaceName}", namespaceProperties.Value.Name);
    }

    private static bool IsConnectionString(string value)
    {
        // Connection strings typically contain "Endpoint=" and "SharedAccessKeyName=" or "SharedAccessKey="
        return value.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase) &&
               (value.Contains("SharedAccessKeyName=", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("SharedAccessKey=", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<ServiceBusConnection>> LoadConnectionsAsync()
    {
        if (!File.Exists(_storageFilePath))
        {
            _logger.LogInformation("No existing connections file found. Starting with empty connections list.");
            return new List<ServiceBusConnection>();
        }
        
        await _fileLock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(_storageFilePath);
            var connections = JsonSerializer.Deserialize<List<ServiceBusConnection>>(json);
            
            if (connections != null)
            {
                _connections = connections;
                _logger.LogInformation("Loaded {Count} connections from storage", _connections.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load connections from file: {FilePath}", _storageFilePath);
            _connections = new List<ServiceBusConnection>();
        }
        finally
        {
            _fileLock.Release();
        }
        
        return _connections;
    }

    private async Task SaveConnectionsAsync()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var json = JsonSerializer.Serialize(_connections, options);
            await File.WriteAllTextAsync(_storageFilePath, json);
            
            _logger.LogDebug("Saved {Count} connections to storage", _connections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save connections to file: {FilePath}", _storageFilePath);
            throw;
        }
    }
}
