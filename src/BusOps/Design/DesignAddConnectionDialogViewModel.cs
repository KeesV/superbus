using BusOps.Core.Models;
using BusOps.ViewModels;
using DynamicData;

namespace BusOps.Design;

public class DesignAddConnectionDialogViewModel : AddConnectionDialogViewModel
{
    public DesignAddConnectionDialogViewModel() : base(null!, null, false)
    {
        DiscoveredNamespaces.AddRange([
            new DiscoveredServiceBusNamespace
            {
                Name = "sb-013-hub-nonprod",
                FullyQualifiedNamespace = "sb-013-hub-nonprod.servicebus.windows.net",
                SubscriptionId = "sub-1234",
                SubscriptionName = "Contoso NonProd",
                ResourceGroup = "rg-013-hub-nonprod",
                Location = "East US",
                Sku = "Standard",
                Status = "Active"
            },
            new DiscoveredServiceBusNamespace
            {
                Name = "sb-001-hub-prod",
                FullyQualifiedNamespace = "sb-001-hub-prod.servicebus.windows.net",
                SubscriptionId = "sub-5678",
                SubscriptionName = "Contoso Prod",
                ResourceGroup = "rg-001-hub-prod",
                Location = "East US 2",
                Sku = "Premium",
                Status = "Active"
            },
            new DiscoveredServiceBusNamespace
            {
                Name = "sb-002-hub-test",
                FullyQualifiedNamespace = "sb-002-hub-test.servicebus.windows.net",
                SubscriptionId = "sub-9101",
                SubscriptionName = "Contoso Test",
                ResourceGroup = "rg-002-hub-test",
                Location = "West US",
                Sku = "Basic",
                Status = "Disabled"
            }
        ]);
        
        ConnectionName = "sb-013-hub-nonprod";
        Description = "Service Bus connection for non-production workloads";
        
        IsDiscovering = true;
    }
}