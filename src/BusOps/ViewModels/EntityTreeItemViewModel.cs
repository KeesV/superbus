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
    private bool _isVisible = true;

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

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public ObservableCollection<EntityTreeItemViewModel> Children { get; } = new();

    public string DisplayText => $"{Name} ({MessageCount})";

    /// <summary>
    /// Applies a search filter to this node and its children recursively
    /// </summary>
    /// <param name="searchText">The search text to filter by (case-insensitive)</param>
    /// <returns>True if this node or any of its children match the search</returns>
    public bool ApplyFilter(string searchText)
    {
        // If search text is empty, show everything
        if (string.IsNullOrWhiteSpace(searchText))
        {
            IsVisible = true;
            foreach (var child in Children)
            {
                child.ApplyFilter(searchText);
            }
            return true;
        }

        // Check if this node matches the search text
        var matchesSearch = Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);

        // Check if any children match (recursively)
        var anyChildMatches = false;
        foreach (var child in Children)
        {
            if (child.ApplyFilter(searchText))
            {
                anyChildMatches = true;
            }
        }

        // This node is visible if it matches or any of its children match
        IsVisible = matchesSearch || anyChildMatches;

        // If this node matches, expand it to show the match
        if (matchesSearch && !string.IsNullOrWhiteSpace(searchText))
        {
            IsExpanded = true;
        }

        return IsVisible;
    }
}
