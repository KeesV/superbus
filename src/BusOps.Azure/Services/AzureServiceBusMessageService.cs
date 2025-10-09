using Azure.Messaging.ServiceBus;
using BusOps.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using System.Text;
using AzureServiceBusMessage = Azure.Messaging.ServiceBus.ServiceBusMessage;

namespace BusOps.Azure.Services;

/// <summary>
/// Azure implementation of IServiceBusMessageService for message operations
/// including send, receive, peek, complete, abandon, and dead letter operations.
/// </summary>
public class AzureServiceBusMessageService : IServiceBusMessageService, IDisposable
{
    private readonly ILogger<AzureServiceBusMessageService> _logger;
    private readonly IServiceBusClientFactory _clientFactory;
    private ServiceBusClient? _client;
    private readonly Dictionary<string, ServiceBusSender> _senders = new();
    private readonly Dictionary<string, ServiceBusReceiver> _receivers = new();
    private readonly ResiliencePipeline _resiliencePipeline;
    private bool _disposed;

    public AzureServiceBusMessageService(
        ILogger<AzureServiceBusMessageService> logger,
        IServiceBusClientFactory clientFactory)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        
        // Configure retry policy with Polly for transient failures
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<ServiceBusException>(),
                MaxRetryAttempts = 3,
                DelayGenerator = static args => 
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber));
                    return new ValueTask<TimeSpan?>(delay);
                }
            })
            .Build();
    }

    /// <summary>
    /// Initializes the message service with a connection string or namespace.
    /// Must be called before using any other methods.
    /// </summary>
    public void Initialize(string connectionStringOrNamespace)
    {
        try
        {
            // Use the factory to create the client
            _client = _clientFactory.CreateClient(connectionStringOrNamespace);
            
            var authMethod = ServiceBusClientFactory.IsConnectionString(connectionStringOrNamespace) 
                ? "connection string" 
                : "DefaultAzureCredential";
            
            _logger.LogInformation("Azure Service Bus Message Service initialized using {AuthMethod}", authMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Service Bus client");
            throw;
        }
    }

    public async Task SendMessageAsync(string queueOrTopicName, Core.Models.ServiceBusMessage message)
    {
        EnsureInitialized();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var sender = GetOrCreateSender(queueOrTopicName);
            
            var azureMessage = new AzureServiceBusMessage(Encoding.UTF8.GetBytes(message.Body))
            {
                MessageId = string.IsNullOrEmpty(message.MessageId) ? Guid.NewGuid().ToString() : message.MessageId,
                CorrelationId = message.CorrelationId,
                SessionId = message.SessionId,
                Subject = message.Label,
                To = message.To,
                ReplyTo = message.ReplyTo,
                TimeToLive = message.TimeToLive ?? TimeSpan.MaxValue
            };

            if (message.ScheduledEnqueueTime.HasValue)
            {
                azureMessage.ScheduledEnqueueTime = message.ScheduledEnqueueTime.Value;
            }

            // Add custom properties
            foreach (var prop in message.Properties)
            {
                azureMessage.ApplicationProperties[prop.Key] = prop.Value;
            }

            await sender.SendMessageAsync(azureMessage);
            
            _logger.LogInformation("Sent message {MessageId} to {QueueOrTopic}", 
                azureMessage.MessageId, queueOrTopicName);
        });
    }

    public async Task<IEnumerable<Core.Models.ServiceBusMessage>> ReceiveMessagesAsync(
        string queueName, int maxMessages = 10, bool peekOnly = true)
    {
        EnsureInitialized();
        
        return await _resiliencePipeline.ExecuteAsync(async (cancellationToken) =>
        {
            var messages = new List<Core.Models.ServiceBusMessage>();
            
            if (peekOnly)
            {
                var receiver = GetOrCreateReceiver(queueName, false);
                var peekedMessages = await receiver.PeekMessagesAsync(maxMessages, cancellationToken: cancellationToken);

                messages.AddRange(peekedMessages.Select(ConvertToServiceBusMessage));
            }
            else
            {
                var receiver = GetOrCreateReceiver(queueName, false);
                var receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(5), cancellationToken);

                messages.AddRange(receivedMessages.Select(ConvertToServiceBusMessage));
            }
            
            _logger.LogInformation("Retrieved {Count} messages from queue {QueueName} (peek: {PeekOnly})", 
                messages.Count, queueName, peekOnly);
            
            return messages;
        });
    }

    public async Task<IEnumerable<Core.Models.ServiceBusMessage>> ReceiveSubscriptionMessagesAsync(
        string topicName, string subscriptionName, int maxMessages = 10, bool peekOnly = true)
    {
        EnsureInitialized();
        
        return await _resiliencePipeline.ExecuteAsync(async (cancellationToken) =>
        {
            var messages = new List<Core.Models.ServiceBusMessage>();
            var entityPath = $"{topicName}/subscriptions/{subscriptionName}";
            
            if (peekOnly)
            {
                var receiver = GetOrCreateReceiver(topicName, subscriptionName, false);
                var peekedMessages = await receiver.PeekMessagesAsync(maxMessages, cancellationToken: cancellationToken);

                messages.AddRange(peekedMessages.Select(ConvertToServiceBusMessage));
            }
            else
            {
                var receiver = GetOrCreateReceiver(topicName, subscriptionName, false);
                var receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(5), cancellationToken);

                messages.AddRange(receivedMessages.Select(ConvertToServiceBusMessage));
            }
            
            _logger.LogInformation("Retrieved {Count} messages from subscription {TopicName}/{SubscriptionName} (peek: {PeekOnly})", 
                messages.Count, topicName, subscriptionName, peekOnly);
            
            return messages;
        });
    }

    public async Task<IEnumerable<Core.Models.ServiceBusMessage>> GetDeadLetterMessagesAsync(
        string queueOrSubscriptionPath, int maxMessages = 10)
    {
        EnsureInitialized();
        
        return await _resiliencePipeline.ExecuteAsync(async (cancellationToken) =>
        {
            var messages = new List<Core.Models.ServiceBusMessage>();
            
            // Determine if it's a queue or subscription path
            ServiceBusReceiver receiver;
            if (queueOrSubscriptionPath.Contains("/subscriptions/"))
            {
                // It's a subscription path (topicName/subscriptions/subscriptionName)
                var parts = queueOrSubscriptionPath.Split('/');
                var topicName = parts[0];
                var subscriptionName = parts[2];
                receiver = GetOrCreateReceiver(topicName, subscriptionName, true);
            }
            else
            {
                // It's a queue
                receiver = GetOrCreateReceiver(queueOrSubscriptionPath, true);
            }
            
            var peekedMessages = await receiver.PeekMessagesAsync(maxMessages, cancellationToken: cancellationToken);
            
            foreach (var msg in peekedMessages)
            {
                messages.Add(ConvertToServiceBusMessage(msg));
            }
            
            _logger.LogInformation("Retrieved {Count} dead letter messages from {Path}", 
                messages.Count, queueOrSubscriptionPath);
            
            return messages;
        });
    }

    public async Task CompleteMessageAsync(string queueOrSubscriptionPath, Core.Models.ServiceBusMessage message)
    {
        EnsureInitialized();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            // This requires the actual ServiceBusReceivedMessage which we don't have
            // In a real implementation, we'd need to store received messages with their lock tokens
            _logger.LogWarning("CompleteMessageAsync not fully implemented - requires lock token management");
            await Task.CompletedTask;
        });
    }

    public async Task AbandonMessageAsync(string queueOrSubscriptionPath, Core.Models.ServiceBusMessage message)
    {
        EnsureInitialized();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            // This requires the actual ServiceBusReceivedMessage which we don't have
            // In a real implementation, we'd need to store received messages with their lock tokens
            _logger.LogWarning("AbandonMessageAsync not fully implemented - requires lock token management");
            await Task.CompletedTask;
        });
    }

    public async Task DeadLetterMessageAsync(string queueOrSubscriptionPath, Core.Models.ServiceBusMessage message, string reason)
    {
        EnsureInitialized();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            // This requires the actual ServiceBusReceivedMessage which we don't have
            // In a real implementation, we'd need to store received messages with their lock tokens
            _logger.LogWarning("DeadLetterMessageAsync not fully implemented - requires lock token management");
            await Task.CompletedTask;
        });
    }

    public async Task PurgeQueueAsync(string queueName)
    {
        EnsureInitialized();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var receiver = GetOrCreateReceiver(queueName, false);
            var deletedCount = 0;
            
            // Receive and complete messages in batches until queue is empty
            while (true)
            {
                var messages = await receiver.ReceiveMessagesAsync(100, TimeSpan.FromSeconds(1));
                
                if (messages.Count == 0)
                    break;
                
                foreach (var message in messages)
                {
                    await receiver.CompleteMessageAsync(message);
                    deletedCount++;
                }
            }
            
            _logger.LogInformation("Purged {Count} messages from queue {QueueName}", 
                deletedCount, queueName);
        });
    }

    public async Task PurgeSubscriptionAsync(string topicName, string subscriptionName)
    {
        EnsureInitialized();
        
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var receiver = GetOrCreateReceiver(topicName, subscriptionName, false);
            var deletedCount = 0;
            
            // Receive and complete messages in batches until subscription is empty
            while (true)
            {
                var messages = await receiver.ReceiveMessagesAsync(100, TimeSpan.FromSeconds(1));
                
                if (messages.Count == 0)
                    break;
                
                foreach (var message in messages)
                {
                    await receiver.CompleteMessageAsync(message);
                    deletedCount++;
                }
            }
            
            _logger.LogInformation("Purged {Count} messages from subscription {TopicName}/{SubscriptionName}", 
                deletedCount, topicName, subscriptionName);
        });
    }

    private ServiceBusSender GetOrCreateSender(string queueOrTopicName)
    {
        if (!_senders.TryGetValue(queueOrTopicName, out var sender))
        {
            sender = _client!.CreateSender(queueOrTopicName);
            _senders[queueOrTopicName] = sender;
        }
        
        return sender;
    }

    private ServiceBusReceiver GetOrCreateReceiver(string queueName, bool isDeadLetter)
    {
        var key = isDeadLetter ? $"{queueName}/$DeadLetterQueue" : queueName;
        
        if (!_receivers.TryGetValue(key, out var receiver))
        {
            var options = new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };
            
            if (isDeadLetter)
            {
                options.SubQueue = SubQueue.DeadLetter;
            }
            
            receiver = _client!.CreateReceiver(queueName, options);
            _receivers[key] = receiver;
        }
        
        return receiver;
    }

    private ServiceBusReceiver GetOrCreateReceiver(string topicName, string subscriptionName, bool isDeadLetter)
    {
        var key = isDeadLetter 
            ? $"{topicName}/subscriptions/{subscriptionName}/$DeadLetterQueue" 
            : $"{topicName}/subscriptions/{subscriptionName}";
        
        if (!_receivers.TryGetValue(key, out var receiver))
        {
            var options = new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };
            
            if (isDeadLetter)
            {
                options.SubQueue = SubQueue.DeadLetter;
            }
            
            receiver = _client!.CreateReceiver(topicName, subscriptionName, options);
            _receivers[key] = receiver;
        }
        
        return receiver;
    }

    private Core.Models.ServiceBusMessage ConvertToServiceBusMessage(ServiceBusReceivedMessage azureMessage)
    {
        var message = new Core.Models.ServiceBusMessage
        {
            MessageId = azureMessage.MessageId,
            CorrelationId = azureMessage.CorrelationId,
            SessionId = azureMessage.SessionId,
            Label = azureMessage.Subject,
            To = azureMessage.To,
            ReplyTo = azureMessage.ReplyTo,
            TimeToLive = azureMessage.TimeToLive,
            Body = Encoding.UTF8.GetString(azureMessage.Body),
            EnqueuedTime = azureMessage.EnqueuedTime,
            DeliveryCount = azureMessage.DeliveryCount,
            SequenceNumber = azureMessage.SequenceNumber
        };

        if (azureMessage.ScheduledEnqueueTime != default)
        {
            message.ScheduledEnqueueTime = azureMessage.ScheduledEnqueueTime.DateTime;
        }

        // Copy application properties
        foreach (var prop in azureMessage.ApplicationProperties)
        {
            message.Properties[prop.Key] = prop.Value;
        }

        return message;
    }

    private void EnsureInitialized()
    {
        if (_client == null)
        {
            throw new InvalidOperationException(
                "Service Bus client is not initialized. Call Initialize() first.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var sender in _senders.Values)
        {
            sender.DisposeAsync().AsTask().Wait();
        }
        _senders.Clear();

        foreach (var receiver in _receivers.Values)
        {
            receiver.DisposeAsync().AsTask().Wait();
        }
        _receivers.Clear();

        _client?.DisposeAsync().AsTask().Wait();
        _client = null;

        _disposed = true;
        
        _logger.LogInformation("Azure Service Bus Message Service disposed");
    }
}
