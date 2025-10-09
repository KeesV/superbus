using BusOps.Core.Models;

namespace BusOps.Core.Interfaces;

public interface IServiceBusManagementService
{
    Task ConnectAsync(string connectionString);
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