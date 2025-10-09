using ReactiveUI;
using System.Collections.ObjectModel;

namespace BusOps.ViewModels;

/// <summary>
/// ViewModel for tree view items representing Service Bus entities
/// </summary>
public class EntityTreeItemViewModel : ReactiveObject
{
    private string _name = string.Empty;
    private string _type = string.Empty;
    private long _messageCount;
    private bool _isExpanded;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    public long MessageCount
    {
        get => _messageCount;
        set => this.RaiseAndSetIfChanged(ref _messageCount, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public ObservableCollection<EntityTreeItemViewModel> Children { get; } = new();

    public string DisplayText => MessageCount > 0 
        ? $"{Name} ({MessageCount})" 
        : Name;
}

