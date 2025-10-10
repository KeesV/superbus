using BusOps.Core.Models;

namespace BusOps.Design;

public class DesignServiceBusMessage : ServiceBusMessage
{
    public DesignServiceBusMessage()
    {
        MessageId = Guid.NewGuid().ToString();
        Body = """
               {
                   "orderId": "ORD-2025-001",
                   "customerId": "CUST-12345",
                   "orderDate": "2025-10-11T10:30:00Z",
                   "items": [
                       {
                           "productId": "PROD-001",
                           "productName": "Sample Product",
                           "quantity": 2,
                           "price": 29.99
                       },
                       {
                           "productId": "PROD-002",
                           "productName": "Another Product",
                           "quantity": 1,
                           "price": 49.99
                       }
                   ],
                   "totalAmount": 109.97,
                   "status": "Pending",
                   "shippingAddress": {
                       "street": "123 Main St",
                       "city": "Seattle",
                       "state": "WA",
                       "zipCode": "98101"
                   }
               }
               """;
        EnqueuedTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        Properties = new Dictionary<string, object>
        {
            { "CustomProperty1", "Value1" },
            { "CustomProperty2", 12345 },
            { "CustomProperty3", true },
            { "CustomProperty4", DateTime.UtcNow.ToString("o") },
            { "CustomProperty5", 45.67 },
            { "CustomProperty6", "AnotherValue"},
            { "CustomProperty7", "YetAnotherValue" },
            { "CustomProperty8", "MoreData" },
            { "CustomProperty9", "SampleText" },
            { "CustomProperty10", "FinalValue" }
        };

    }
}