using BusOps.Core.Models;
using BusOps.ViewModels;
using DynamicData;

namespace BusOps.Design;

public class DesignMainWindowViewModel : MainWindowViewModel
{
    public DesignMainWindowViewModel() : base(null!, null!, null!, null!)
    {
        Connections.AddRange([
            new ConnectionItemViewModel(new ServiceBusConnection
            {
                CreatedAt = DateTimeOffset.Now,
                Description = "sb-013-hub-nonprod",
                Id = "sb-013-hub-nonprod",
                IsActive = true,
                LastConnected = DateTimeOffset.Now.AddHours(-1),
            }, this)
        ]);
        Entities.AddRange([
            new EntityTreeItemViewModel
            {
                Name = "Queues",
                Type = "Folder",
                MessageCount = 79,
            }
        ]);
        Entities[0].Children.AddRange([
            new EntityTreeItemViewModel
            {
                Name = "my-queue-1",
                Type = "Queue",
                MessageCount = 42,
            },
            new EntityTreeItemViewModel
            {
                Name = "my-queue-2",
                Type = "Queue",
                MessageCount = 0,
            },
            new EntityTreeItemViewModel
            {
                Name = "my-queue-3",
                Type = "Queue",
                MessageCount = 37,
            }
        ]);
        SelectedEntity = Entities[0].Children[0];
        Messages.AddRange(SampleMessages.GenerateSampleMessages());
        IsLoadingEntities = false;
        IsLoadingMessages = false;
    }
}