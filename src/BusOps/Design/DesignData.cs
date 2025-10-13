using BusOps.Core.Models;
using BusOps.ViewModels;

namespace BusOps.Design;

public static class DesignData
{
    public static IEnumerable<ConnectionItemViewModel> SampleConnections =>
    [
        new ConnectionItemViewModel(new ServiceBusConnection
        {
            CreatedAt = DateTimeOffset.Now,
            Description = "sb-013-hub-nonprod",
            Id = "sb-013-hub-nonprod",
            IsActive = true,
            LastConnected = DateTimeOffset.Now.AddHours(-1),
        }, null!)
    ];

    public static IEnumerable<EntityTreeItemViewModel> SampleEntities
    {
        get
        {
            var entities = new List<EntityTreeItemViewModel>();
            entities.AddRange([
                new EntityTreeItemViewModel
                {
                    Name = "Queues",
                    Type = "Folder",
                    MessageCount = 100,
                }
            ]);

            for (var i = 0; i < 100; i++)
            {
                entities[0].Children.Add(new EntityTreeItemViewModel
                {
                    Name = $"my-queue-{i + 1}",
                    Type = "Queue",
                    MessageCount = Random.Shared.Next(0, 1000),
                });
            }
            
            return entities;
        }
    }
}