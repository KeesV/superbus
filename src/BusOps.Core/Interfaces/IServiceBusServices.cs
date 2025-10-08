using BusOps.Core.Models;

namespace BusOps.Core.Interfaces;

public interface IServiceBusManagementService
{
    Task<bool> ConnectAsync(string connectionString);
    Task DisconnectAsync();
    Task<IEnumerable<ServiceBusQueue>> GetQueuesAsync();
    Task<IEnumerable<ServiceBusTopic>> GetTopicsAsync();
    Task<IEnumerable<ServiceBusSubscription>> GetSubscriptionsAsync(string topicName);
    Task<ServiceBusQueue> CreateQueueAsync(string queueName, QueueOptions? options = null);
    Task<ServiceBusTopic> CreateTopicAsync(string topicName, TopicOptions? options = null);
    Task<ServiceBusSubscription> CreateSubscriptionAsync(string topicName, string subscriptionName, SubscriptionOptions? options = null);
    Task DeleteQueueAsync(string queueName);
    Task DeleteTopicAsync(string topicName);
    Task DeleteSubscriptionAsync(string topicName, string subscriptionName);
}

public interface IServiceBusMessageService
{
    Task SendMessageAsync(string queueOrTopicName, ServiceBusMessage message);
    Task<IEnumerable<ServiceBusMessage>> ReceiveMessagesAsync(string queueName, int maxMessages = 10, bool peekOnly = true);
    Task<IEnumerable<ServiceBusMessage>> ReceiveSubscriptionMessagesAsync(string topicName, string subscriptionName, int maxMessages = 10, bool peekOnly = true);
    Task<IEnumerable<ServiceBusMessage>> GetDeadLetterMessagesAsync(string queueOrSubscriptionPath, int maxMessages = 10);
    Task CompleteMessageAsync(string queueOrSubscriptionPath, ServiceBusMessage message);
    Task AbandonMessageAsync(string queueOrSubscriptionPath, ServiceBusMessage message);
    Task DeadLetterMessageAsync(string queueOrSubscriptionPath, ServiceBusMessage message, string reason);
    Task PurgeQueueAsync(string queueName);
    Task PurgeSubscriptionAsync(string topicName, string subscriptionName);
}

public class QueueOptions
{
    public TimeSpan? LockDuration { get; set; }
    public TimeSpan? DefaultMessageTimeToLive { get; set; }
    public int? MaxDeliveryCount { get; set; }
    public bool? RequiresSession { get; set; }
    public bool? RequiresDuplicateDetection { get; set; }
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; }
    public bool? EnableBatchedOperations { get; set; }
    public bool? EnablePartitioning { get; set; }
}

public class TopicOptions
{
    public TimeSpan? DefaultMessageTimeToLive { get; set; }
    public bool? RequiresDuplicateDetection { get; set; }
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; }
    public bool? EnableBatchedOperations { get; set; }
    public bool? EnablePartitioning { get; set; }
}

public class SubscriptionOptions
{
    public TimeSpan? LockDuration { get; set; }
    public TimeSpan? DefaultMessageTimeToLive { get; set; }
    public int? MaxDeliveryCount { get; set; }
    public bool? RequiresSession { get; set; }
    public bool? EnableBatchedOperations { get; set; }
    public string? ForwardTo { get; set; }
    public string? ForwardDeadLetteredMessagesTo { get; set; }
}