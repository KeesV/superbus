using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BusOps.Core.Models;

public class ServiceBusConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastConnected { get; set; }
    public bool IsActive { get; set; }
}

public class DiscoveredServiceBusNamespace
{
    public string Name { get; set; } = string.Empty;
    public string FullyQualifiedNamespace { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ServiceBusQueue
{
    public string Name { get; set; } = string.Empty;
    public long MessageCount { get; set; }
    public long DeadLetterMessageCount { get; set; }
    public long SizeInBytes { get; set; }
    public TimeSpan? LockDuration { get; set; }
    public TimeSpan? DefaultMessageTimeToLive { get; set; }
    public int MaxDeliveryCount { get; set; }
    public bool RequiresSession { get; set; }
    public bool RequiresDuplicateDetection { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class ServiceBusTopic
{
    public string Name { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public TimeSpan? DefaultMessageTimeToLive { get; set; }
    public bool RequiresDuplicateDetection { get; set; }
    public bool EnableBatchedOperations { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<ServiceBusSubscription> Subscriptions { get; set; } = new();
}

public class ServiceBusSubscription
{
    public string Name { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public long MessageCount { get; set; }
    public long DeadLetterMessageCount { get; set; }
    public TimeSpan? LockDuration { get; set; }
    public TimeSpan? DefaultMessageTimeToLive { get; set; }
    public int MaxDeliveryCount { get; set; }
    public bool RequiresSession { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class ServiceBusMessageProperty
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ServiceBusMessage : INotifyPropertyChanged
{
    public List<ServiceBusMessageProperty> BrokerProperties =>
    [
        new() { Key = "MessageId", Value = MessageId },
        new() { Key = "CorrelationId", Value = CorrelationId ?? string.Empty },
        new() { Key = "SessionId", Value = SessionId ?? string.Empty },
        new() { Key = "Label", Value = Label ?? string.Empty },
        new() { Key = "To", Value = To ?? string.Empty },
        new() { Key = "ReplyTo", Value = ReplyTo ?? string.Empty },
        new() { Key = "TimeToLive", Value = TimeToLive?.ToString() ?? string.Empty },
        new() { Key = "ScheduledEnqueueTime", Value = ScheduledEnqueueTime?.ToString() ?? string.Empty },
        new() { Key = "EnqueuedTime", Value = EnqueuedTime.ToString() },
        new() { Key = "DeliveryCount", Value = DeliveryCount.ToString() },
        new() { Key = "SequenceNumber", Value = SequenceNumber.ToString() }
    ];
    
    public List<ServiceBusMessageProperty> CustomProperties =>
        Properties.Select(kv => new ServiceBusMessageProperty
        {
            Key = kv.Key,
            Value = kv.Value?.ToString() ?? string.Empty
        }).ToList();
    
    private bool _isSelected;

    public string MessageId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? Label { get; set; }
    public string? To { get; set; }
    public string? ReplyTo { get; set; }
    public TimeSpan? TimeToLive { get; set; }
    public DateTime? ScheduledEnqueueTime { get; set; }
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public DateTimeOffset EnqueuedTime { get; set; }
    public int DeliveryCount { get; set; }
    public long SequenceNumber { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}