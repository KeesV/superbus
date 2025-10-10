using BusOps.Core.Models;

namespace BusOps.Design;

public static class SampleMessages
{
    private static readonly Random Random = new();
    
    private static readonly string[] SampleBodies = 
    {
        "{\"orderId\": \"ORD-12345\", \"customerId\": \"CUST-789\", \"amount\": 299.99, \"status\": \"pending\"}",
        "{\"eventType\": \"UserRegistered\", \"userId\": \"user-456\", \"email\": \"user@example.com\", \"timestamp\": \"2025-10-09T10:30:00Z\"}",
        "{\"productId\": \"PROD-001\", \"name\": \"Wireless Mouse\", \"stock\": 150, \"price\": 29.99}",
        "{\"notification\": \"Payment processed\", \"transactionId\": \"TXN-98765\", \"success\": true}",
        "{\"sensorId\": \"SENSOR-42\", \"temperature\": 22.5, \"humidity\": 65, \"timestamp\": \"2025-10-09T14:15:00Z\"}",
        "{\"taskId\": \"TASK-555\", \"description\": \"Process batch job\", \"priority\": \"high\", \"assignedTo\": \"worker-3\"}",
        "{\"logLevel\": \"Warning\", \"message\": \"High memory usage detected\", \"source\": \"MonitoringService\"}",
        "{\"invoiceId\": \"INV-2025-001\", \"dueDate\": \"2025-11-09\", \"totalAmount\": 1599.00, \"isPaid\": false}",
        "{\"shippingId\": \"SHIP-777\", \"carrier\": \"FastShip\", \"trackingNumber\": \"FS123456789\", \"status\": \"in_transit\"}",
        "{\"reportId\": \"RPT-2025-10\", \"type\": \"monthly_sales\", \"generatedBy\": \"system\", \"recordCount\": 15420}"
    };
    
    private static readonly string[] SampleLabels = 
    {
        "OrderCreated", "UserEvent", "InventoryUpdate", "PaymentNotification", "SensorData",
        "TaskAssignment", "SystemLog", "InvoiceGenerated", "ShippingUpdate", "ReportReady"
    };
    
    private static readonly string[] SampleCorrelationIds = 
    {
        "correlation-abc-123", "correlation-def-456", "correlation-ghi-789",
        "correlation-jkl-012", "correlation-mno-345", "correlation-pqr-678",
        "correlation-stu-901", "correlation-vwx-234", "correlation-yza-567",
        "correlation-bcd-890"
    };

    public static List<ServiceBusMessage> GenerateSampleMessages()
    {
        var messages = new List<ServiceBusMessage>();
        
        for (int i = 0; i < 10; i++)
        {
            var message = new ServiceBusMessage
            {
                MessageId = $"msg-{Guid.NewGuid()}",
                CorrelationId = SampleCorrelationIds[i],
                SessionId = Random.Next(0, 100) > 70 ? $"session-{Random.Next(1, 5)}" : null,
                Label = SampleLabels[i],
                To = Random.Next(0, 100) > 50 ? $"queue-{Random.Next(1, 4)}" : null,
                ReplyTo = Random.Next(0, 100) > 70 ? $"reply-queue-{Random.Next(1, 3)}" : null,
                TimeToLive = TimeSpan.FromHours(Random.Next(1, 49)),
                ScheduledEnqueueTime = Random.Next(0, 100) > 80 
                    ? DateTime.UtcNow.AddMinutes(Random.Next(5, 120)) 
                    : null,
                Body = SampleBodies[i],
                Properties = GenerateRandomProperties(i),
                EnqueuedTime = DateTimeOffset.UtcNow.AddMinutes(-Random.Next(0, 1440)),
                DeliveryCount = Random.Next(0, 3),
                SequenceNumber = 1000 + i * Random.Next(1, 100)
            };
            
            messages.Add(message);
        }
        
        return messages;
    }
    
    private static Dictionary<string, object> GenerateRandomProperties(int seed)
    {
        var properties = new Dictionary<string, object>
        {
            { "Priority", Random.Next(1, 6) },
            { "Source", $"Service-{Random.Next(1, 10)}" },
            { "Environment", Random.Next(0, 100) > 50 ? "Production" : "Staging" },
            { "Version", $"{Random.Next(1, 4)}.{Random.Next(0, 10)}.{Random.Next(0, 20)}" }
        };
        
        // Add some random optional properties
        if (Random.Next(0, 100) > 50)
        {
            properties.Add("Region", Random.Next(0, 100) > 50 ? "US-East" : "EU-West");
        }
        
        if (Random.Next(0, 100) > 60)
        {
            properties.Add("RetryCount", Random.Next(0, 3));
        }
        
        if (Random.Next(0, 100) > 70)
        {
            properties.Add("IsProcessed", false);
        }
        
        return properties;
    }
}