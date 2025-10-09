using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Identity;
using BusOps.Core.Interfaces;
using BusOps.Core.Models;
using Microsoft.Extensions.Logging;
using Polly;
using System.Text.Json;

namespace BusOps.Azure.Services;

public class AzureServiceBusManagementService : IServiceBusManagementService, IDisposable
{
    private readonly ILogger<AzureServiceBusManagementService> _logger;
    private ServiceBusAdministrationClient? _adminClient;
    private ServiceBusClient? _client;
    private string? _connectionString;
    private readonly ResiliencePipeline _resiliencePipeline;

    public AzureServiceBusManagementService(ILogger<AzureServiceBusManagementService> logger)
    {
        _logger = logger;
        
        // Configure retry policy with Polly
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 3,
                DelayGenerator = static args => 
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber));
                    return new ValueTask<TimeSpan?>(delay);
                }
            })
            .Build();
    }

    public async Task<bool> ConnectAsync(string connectionString)
    {
        try
        {
            _connectionString = connectionString;
            
            // Determine if input is a connection string or FQDN
            if (IsConnectionString(connectionString))
            {
                // Traditional connection string authentication
                _adminClient = new ServiceBusAdministrationClient(connectionString);
                _client = new ServiceBusClient(connectionString);
                
                _logger.LogInformation("Connecting to Azure Service Bus using connection string");
            }
            else
            {
                // FQDN with DefaultAzureCredential
                var fullyQualifiedNamespace = EnsureFullyQualifiedNamespace(connectionString);
                var credential = new DefaultAzureCredential();
                
                _adminClient = new ServiceBusAdministrationClient(fullyQualifiedNamespace, credential);
                _client = new ServiceBusClient(fullyQualifiedNamespace, credential);
                
                _logger.LogInformation("Connecting to Azure Service Bus using DefaultAzureCredential for namespace: {Namespace}", 
                    fullyQualifiedNamespace);
            }

            // Test the connection by trying to get namespace info
            var namespaceProperties = await _adminClient.GetNamespacePropertiesAsync();
            
            _logger.LogInformation("Successfully connected to Azure Service Bus namespace: {NamespaceName}", 
                namespaceProperties.Value.Name);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Azure Service Bus");
            await DisconnectAsync();
            return false;
        }
    }

    private static bool IsConnectionString(string input)
    {
        // Connection strings typically contain "Endpoint=" and "SharedAccessKeyName=" or "SharedAccessKey="
        return input.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase) &&
               (input.Contains("SharedAccessKeyName=", StringComparison.OrdinalIgnoreCase) ||
                input.Contains("SharedAccessKey=", StringComparison.OrdinalIgnoreCase));
    }

    private static string EnsureFullyQualifiedNamespace(string input)
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

    public async Task DisconnectAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
            _client = null;
        }

        _adminClient = null;
        _connectionString = null;
        
        _logger.LogInformation("Disconnected from Azure Service Bus");
    }

    public async Task<IEnumerable<ServiceBusQueue>> GetQueuesAsync()
    {
        EnsureConnected();
        
        return await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var queues = new List<ServiceBusQueue>();
            
            await foreach (var queueProperties in _adminClient!.GetQueuesAsync())
            {
                var runtimeProperties = await _adminClient.GetQueueRuntimePropertiesAsync(queueProperties.Name);
                
                queues.Add(new ServiceBusQueue
                {
                    Name = queueProperties.Name,
                    MessageCount = runtimeProperties.Value.TotalMessageCount,
                    DeadLetterMessageCount = runtimeProperties.Value.DeadLetterMessageCount,
                    SizeInBytes = runtimeProperties.Value.SizeInBytes,
                    LockDuration = queueProperties.LockDuration,
                    DefaultMessageTimeToLive = queueProperties.DefaultMessageTimeToLive,
                    MaxDeliveryCount = queueProperties.MaxDeliveryCount,
                    RequiresSession = queueProperties.RequiresSession,
                    RequiresDuplicateDetection = queueProperties.RequiresDuplicateDetection,
                    CreatedAt = runtimeProperties.Value.CreatedAt,
                    UpdatedAt = runtimeProperties.Value.UpdatedAt
                });
            }
            
            return queues;
        });
    }

    public async Task<IEnumerable<ServiceBusTopic>> GetTopicsAsync()
    {
        EnsureConnected();
        
        return await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var topics = new List<ServiceBusTopic>();
            
            await foreach (var topicProperties in _adminClient!.GetTopicsAsync())
            {
                var runtimeProperties = await _adminClient.GetTopicRuntimePropertiesAsync(topicProperties.Name);
                var subscriptions = await GetSubscriptionsAsync(topicProperties.Name);
                
                topics.Add(new ServiceBusTopic
                {
                    Name = topicProperties.Name,
                    SizeInBytes = runtimeProperties.Value.SizeInBytes,
                    DefaultMessageTimeToLive = topicProperties.DefaultMessageTimeToLive,
                    RequiresDuplicateDetection = topicProperties.RequiresDuplicateDetection,
                    EnableBatchedOperations = topicProperties.EnableBatchedOperations,
                    CreatedAt = runtimeProperties.Value.CreatedAt,
                    UpdatedAt = runtimeProperties.Value.UpdatedAt,
                    Subscriptions = subscriptions.ToList()
                });
            }
            
            return topics;
        });
    }

    public async Task<IEnumerable<ServiceBusSubscription>> GetSubscriptionsAsync(string topicName)
    {
        EnsureConnected();
        
        return await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var subscriptions = new List<ServiceBusSubscription>();
            
            await foreach (var subscriptionProperties in _adminClient!.GetSubscriptionsAsync(topicName))
            {
                var runtimeProperties = await _adminClient.GetSubscriptionRuntimePropertiesAsync(topicName, subscriptionProperties.SubscriptionName);
                
                subscriptions.Add(new ServiceBusSubscription
                {
                    Name = subscriptionProperties.SubscriptionName,
                    TopicName = topicName,
                    MessageCount = runtimeProperties.Value.TotalMessageCount,
                    DeadLetterMessageCount = runtimeProperties.Value.DeadLetterMessageCount,
                    LockDuration = subscriptionProperties.LockDuration,
                    DefaultMessageTimeToLive = subscriptionProperties.DefaultMessageTimeToLive,
                    MaxDeliveryCount = subscriptionProperties.MaxDeliveryCount,
                    RequiresSession = subscriptionProperties.RequiresSession,
                    CreatedAt = runtimeProperties.Value.CreatedAt,
                    UpdatedAt = runtimeProperties.Value.UpdatedAt
                });
            }
            
            return subscriptions;
        });
    }

    public async Task<ServiceBusQueue> CreateQueueAsync(string queueName, QueueOptions? options = null)
    {
        EnsureConnected();
        
        return await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var createOptions = new CreateQueueOptions(queueName);
            
            if (options != null)
            {
                if (options.LockDuration.HasValue)
                    createOptions.LockDuration = options.LockDuration.Value;
                if (options.DefaultMessageTimeToLive.HasValue)
                    createOptions.DefaultMessageTimeToLive = options.DefaultMessageTimeToLive.Value;
                if (options.MaxDeliveryCount.HasValue)
                    createOptions.MaxDeliveryCount = options.MaxDeliveryCount.Value;
                if (options.RequiresSession.HasValue)
                    createOptions.RequiresSession = options.RequiresSession.Value;
                if (options.RequiresDuplicateDetection.HasValue)
                    createOptions.RequiresDuplicateDetection = options.RequiresDuplicateDetection.Value;
                if (options.EnableBatchedOperations.HasValue)
                    createOptions.EnableBatchedOperations = options.EnableBatchedOperations.Value;
                if (options.EnablePartitioning.HasValue)
                    createOptions.EnablePartitioning = options.EnablePartitioning.Value;
            }
            
            var response = await _adminClient!.CreateQueueAsync(createOptions);
            var queueProperties = response.Value;
            
            return new ServiceBusQueue
            {
                Name = queueProperties.Name,
                LockDuration = queueProperties.LockDuration,
                DefaultMessageTimeToLive = queueProperties.DefaultMessageTimeToLive,
                MaxDeliveryCount = queueProperties.MaxDeliveryCount,
                RequiresSession = queueProperties.RequiresSession,
                RequiresDuplicateDetection = queueProperties.RequiresDuplicateDetection,
                CreatedAt = DateTimeOffset.UtcNow, // Runtime properties not available immediately after creation
                UpdatedAt = DateTimeOffset.UtcNow
            };
        });
    }

    public async Task<ServiceBusTopic> CreateTopicAsync(string topicName, TopicOptions? options = null)
    {
        EnsureConnected();
        
        return await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var createOptions = new CreateTopicOptions(topicName);
            
            if (options != null)
            {
                if (options.DefaultMessageTimeToLive.HasValue)
                    createOptions.DefaultMessageTimeToLive = options.DefaultMessageTimeToLive.Value;
                if (options.RequiresDuplicateDetection.HasValue)
                    createOptions.RequiresDuplicateDetection = options.RequiresDuplicateDetection.Value;
                if (options.EnableBatchedOperations.HasValue)
                    createOptions.EnableBatchedOperations = options.EnableBatchedOperations.Value;
                if (options.EnablePartitioning.HasValue)
                    createOptions.EnablePartitioning = options.EnablePartitioning.Value;
            }
            
            var response = await _adminClient!.CreateTopicAsync(createOptions);
            var topicProperties = response.Value;
            
            return new ServiceBusTopic
            {
                Name = topicProperties.Name,
                DefaultMessageTimeToLive = topicProperties.DefaultMessageTimeToLive,
                RequiresDuplicateDetection = topicProperties.RequiresDuplicateDetection,
                EnableBatchedOperations = topicProperties.EnableBatchedOperations,
                CreatedAt = DateTimeOffset.UtcNow, // Runtime properties not available immediately after creation
                UpdatedAt = DateTimeOffset.UtcNow
            };
        });
    }

    public async Task<ServiceBusSubscription> CreateSubscriptionAsync(string topicName, string subscriptionName, SubscriptionOptions? options = null)
    {
        EnsureConnected();
        
        return await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var createOptions = new CreateSubscriptionOptions(topicName, subscriptionName);
            
            if (options != null)
            {
                if (options.LockDuration.HasValue)
                    createOptions.LockDuration = options.LockDuration.Value;
                if (options.DefaultMessageTimeToLive.HasValue)
                    createOptions.DefaultMessageTimeToLive = options.DefaultMessageTimeToLive.Value;
                if (options.MaxDeliveryCount.HasValue)
                    createOptions.MaxDeliveryCount = options.MaxDeliveryCount.Value;
                if (options.RequiresSession.HasValue)
                    createOptions.RequiresSession = options.RequiresSession.Value;
                if (options.EnableBatchedOperations.HasValue)
                    createOptions.EnableBatchedOperations = options.EnableBatchedOperations.Value;
                if (!string.IsNullOrEmpty(options.ForwardTo))
                    createOptions.ForwardTo = options.ForwardTo;
                if (!string.IsNullOrEmpty(options.ForwardDeadLetteredMessagesTo))
                    createOptions.ForwardDeadLetteredMessagesTo = options.ForwardDeadLetteredMessagesTo;
            }
            
            var response = await _adminClient!.CreateSubscriptionAsync(createOptions);
            var subscriptionProperties = response.Value;
            
            return new ServiceBusSubscription
            {
                Name = subscriptionProperties.SubscriptionName,
                TopicName = topicName,
                LockDuration = subscriptionProperties.LockDuration,
                DefaultMessageTimeToLive = subscriptionProperties.DefaultMessageTimeToLive,
                MaxDeliveryCount = subscriptionProperties.MaxDeliveryCount,
                RequiresSession = subscriptionProperties.RequiresSession,
                CreatedAt = DateTimeOffset.UtcNow, // Runtime properties not available immediately after creation
                UpdatedAt = DateTimeOffset.UtcNow
            };
        });
    }

    public async Task DeleteQueueAsync(string queueName)
    {
        EnsureConnected();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            await _adminClient!.DeleteQueueAsync(queueName);
            _logger.LogInformation("Deleted queue: {QueueName}", queueName);
        });
    }

    public async Task DeleteTopicAsync(string topicName)
    {
        EnsureConnected();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            await _adminClient!.DeleteTopicAsync(topicName);
            _logger.LogInformation("Deleted topic: {TopicName}", topicName);
        });
    }

    public async Task DeleteSubscriptionAsync(string topicName, string subscriptionName)
    {
        EnsureConnected();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            await _adminClient!.DeleteSubscriptionAsync(topicName, subscriptionName);
            _logger.LogInformation("Deleted subscription: {SubscriptionName} from topic: {TopicName}", 
                subscriptionName, topicName);
        });
    }

    private void EnsureConnected()
    {
        if (_adminClient == null || _client == null)
        {
            throw new InvalidOperationException("Not connected to Azure Service Bus. Call ConnectAsync first.");
        }
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}