using BusOps.ViewModels;
using Shouldly;

namespace BusOps.Tests.ViewModels;

public class EntityTreeItemViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Act
        var viewModel = new EntityTreeItemViewModel();

        // Assert
        viewModel.Name.ShouldBe(string.Empty);
        viewModel.Type.ShouldBe(string.Empty);
        viewModel.MessageCount.ShouldBe(0L);
        viewModel.IsExpanded.ShouldBeFalse();
        viewModel.IsVisible.ShouldBeTrue();
        viewModel.Parent.ShouldBeNull();
        viewModel.Children.ShouldBeEmpty();
    }

    [Fact]
    public void Name_ShouldBeSettable()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel();

        // Act
        viewModel.Name = "TestQueue";

        // Assert
        viewModel.Name.ShouldBe("TestQueue");
    }

    [Fact]
    public void Type_ShouldBeSettable()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel();

        // Act
        viewModel.Type = "Queue";

        // Assert
        viewModel.Type.ShouldBe("Queue");
    }

    [Fact]
    public void MessageCount_ShouldBeSettable()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel();

        // Act
        viewModel.MessageCount = 42L;

        // Assert
        viewModel.MessageCount.ShouldBe(42L);
    }

    [Fact]
    public void IsExpanded_ShouldBeSettable()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel();

        // Act
        viewModel.IsExpanded = true;

        // Assert
        viewModel.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void IsVisible_ShouldBeSettable()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel();

        // Act
        viewModel.IsVisible = false;

        // Assert
        viewModel.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void Parent_ShouldBeSettable()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel();
        var parent = new EntityTreeItemViewModel { Name = "ParentNode" };

        // Act
        viewModel.Parent = parent;

        // Assert
        viewModel.Parent.ShouldBe(parent);
        viewModel.Parent.Name.ShouldBe("ParentNode");
    }

    [Fact]
    public void Children_ShouldBeAddable()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel { Name = "Parent" };
        var child1 = new EntityTreeItemViewModel { Name = "Child1" };
        var child2 = new EntityTreeItemViewModel { Name = "Child2" };

        // Act
        viewModel.Children.Add(child1);
        viewModel.Children.Add(child2);

        // Assert
        viewModel.Children.Count.ShouldBe(2);
        viewModel.Children[0].ShouldBe(child1);
        viewModel.Children[1].ShouldBe(child2);
    }

    [Fact]
    public void DisplayText_ShouldFormatNameAndMessageCount()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel
        {
            Name = "orders-queue",
            MessageCount = 123
        };

        // Act
        var displayText = viewModel.DisplayText;

        // Assert
        displayText.ShouldBe("orders-queue (123)");
    }

    [Fact]
    public void DisplayText_ShouldUpdateWhenNameChanges()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel
        {
            Name = "queue1",
            MessageCount = 10
        };

        // Act
        viewModel.Name = "queue2";

        // Assert
        viewModel.DisplayText.ShouldBe("queue2 (10)");
    }

    [Fact]
    public void DisplayText_ShouldUpdateWhenMessageCountChanges()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel
        {
            Name = "queue1",
            MessageCount = 10
        };

        // Act
        viewModel.MessageCount = 50;

        // Assert
        viewModel.DisplayText.ShouldBe("queue1 (50)");
    }

    [Fact]
    public void ApplyFilter_WithEmptySearchText_ShouldShowAllNodes()
    {
        // Arrange
        var parent = new EntityTreeItemViewModel { Name = "Parent" };
        var child1 = new EntityTreeItemViewModel { Name = "Child1" };
        var child2 = new EntityTreeItemViewModel { Name = "Child2" };
        parent.Children.Add(child1);
        parent.Children.Add(child2);

        // Act
        var result = parent.ApplyFilter(string.Empty);

        // Assert
        result.ShouldBeTrue();
        parent.IsVisible.ShouldBeTrue();
        child1.IsVisible.ShouldBeTrue();
        child2.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilter_WithWhitespaceSearchText_ShouldShowAllNodes()
    {
        // Arrange
        var parent = new EntityTreeItemViewModel { Name = "Parent" };
        var child = new EntityTreeItemViewModel { Name = "Child" };
        parent.Children.Add(child);

        // Act
        var result = parent.ApplyFilter("   ");

        // Assert
        result.ShouldBeTrue();
        parent.IsVisible.ShouldBeTrue();
        child.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilter_WhenNodeMatches_ShouldBeVisible()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel { Name = "orders-queue" };

        // Act
        var result = viewModel.ApplyFilter("orders");

        // Assert
        result.ShouldBeTrue();
        viewModel.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilter_WhenNodeDoesNotMatch_ShouldBeHidden()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel { Name = "orders-queue" };

        // Act
        var result = viewModel.ApplyFilter("events");

        // Assert
        result.ShouldBeFalse();
        viewModel.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void ApplyFilter_ShouldBeCaseInsensitive()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel { Name = "OrdersQueue" };

        // Act
        var result1 = viewModel.ApplyFilter("orders");
        viewModel.IsVisible = true; // Reset
        var result2 = viewModel.ApplyFilter("ORDERS");
        viewModel.IsVisible = true; // Reset
        var result3 = viewModel.ApplyFilter("OrDeRs");

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeTrue();
        result3.ShouldBeTrue();
        viewModel.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilter_WhenChildMatches_ParentShouldBeVisible()
    {
        // Arrange
        var parent = new EntityTreeItemViewModel { Name = "Queues" };
        var child = new EntityTreeItemViewModel { Name = "orders-queue" };
        parent.Children.Add(child);

        // Act
        var result = parent.ApplyFilter("orders");

        // Assert
        result.ShouldBeTrue();
        parent.IsVisible.ShouldBeTrue();
        child.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilter_WhenNoChildMatches_ParentShouldBeHidden()
    {
        // Arrange
        var parent = new EntityTreeItemViewModel { Name = "Queues" };
        var child1 = new EntityTreeItemViewModel { Name = "orders-queue" };
        var child2 = new EntityTreeItemViewModel { Name = "events-queue" };
        parent.Children.Add(child1);
        parent.Children.Add(child2);

        // Act
        var result = parent.ApplyFilter("notifications");

        // Assert
        result.ShouldBeFalse();
        parent.IsVisible.ShouldBeFalse();
        child1.IsVisible.ShouldBeFalse();
        child2.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void ApplyFilter_ShouldWorkRecursively()
    {
        // Arrange
        var grandparent = new EntityTreeItemViewModel { Name = "Topics" };
        var parent = new EntityTreeItemViewModel { Name = "orders-topic" };
        var child = new EntityTreeItemViewModel { Name = "subscription1" };
        grandparent.Children.Add(parent);
        parent.Children.Add(child);

        // Act
        var result = grandparent.ApplyFilter("subscription");

        // Assert
        result.ShouldBeTrue();
        grandparent.IsVisible.ShouldBeTrue();
        parent.IsVisible.ShouldBeTrue();
        child.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilter_WhenMatchFound_ShouldExpandNode()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel 
        { 
            Name = "orders-queue",
            IsExpanded = false
        };

        // Act
        viewModel.ApplyFilter("orders");

        // Assert
        viewModel.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilter_WhenMatchFoundInChild_ParentShouldNotAutoExpand()
    {
        // Arrange
        var parent = new EntityTreeItemViewModel 
        { 
            Name = "Queues",
            IsExpanded = false
        };
        var child = new EntityTreeItemViewModel { Name = "orders-queue" };
        parent.Children.Add(child);

        // Act
        parent.ApplyFilter("orders");

        // Assert
        parent.IsExpanded.ShouldBeFalse(); // Parent doesn't match, so it shouldn't expand
        child.IsExpanded.ShouldBeTrue(); // Child matches, so it should expand
    }

    [Fact]
    public void ApplyFilter_WithPartialMatch_ShouldWork()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel { Name = "orders-processing-queue" };

        // Act
        var result = viewModel.ApplyFilter("process");

        // Assert
        result.ShouldBeTrue();
        viewModel.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilter_WithMultipleChildren_ShouldFilterCorrectly()
    {
        // Arrange
        var parent = new EntityTreeItemViewModel { Name = "Queues" };
        var child1 = new EntityTreeItemViewModel { Name = "orders-queue" };
        var child2 = new EntityTreeItemViewModel { Name = "events-queue" };
        var child3 = new EntityTreeItemViewModel { Name = "notifications-queue" };
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        parent.Children.Add(child3);

        // Act
        var result = parent.ApplyFilter("events");

        // Assert
        result.ShouldBeTrue();
        parent.IsVisible.ShouldBeTrue(); // Parent visible because child matches
        child1.IsVisible.ShouldBeFalse(); // Doesn't match
        child2.IsVisible.ShouldBeTrue(); // Matches
        child3.IsVisible.ShouldBeFalse(); // Doesn't match
    }

    [Fact]
    public void ApplyFilter_WhenEmptyString_ShouldNotExpand()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel 
        { 
            Name = "orders-queue",
            IsExpanded = false
        };

        // Act
        viewModel.ApplyFilter(string.Empty);

        // Assert
        viewModel.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void ApplyFilter_ComplexHierarchy_ShouldFilterCorrectly()
    {
        // Arrange - Build a tree: Topics -> topic1 -> sub1, sub2
        var topicsFolder = new EntityTreeItemViewModel { Name = "Topics" };
        var topic1 = new EntityTreeItemViewModel { Name = "orders-topic" };
        var topic2 = new EntityTreeItemViewModel { Name = "events-topic" };
        var sub1 = new EntityTreeItemViewModel { Name = "orders-subscription" };
        var sub2 = new EntityTreeItemViewModel { Name = "events-subscription" };
        
        topicsFolder.Children.Add(topic1);
        topicsFolder.Children.Add(topic2);
        topic1.Children.Add(sub1);
        topic2.Children.Add(sub2);

        // Act - Search for "orders"
        var result = topicsFolder.ApplyFilter("orders");

        // Assert
        result.ShouldBeTrue();
        topicsFolder.IsVisible.ShouldBeTrue(); // Has matching descendants
        topic1.IsVisible.ShouldBeTrue(); // Matches
        topic2.IsVisible.ShouldBeFalse(); // Doesn't match
        sub1.IsVisible.ShouldBeTrue(); // Matches
        sub2.IsVisible.ShouldBeFalse(); // Doesn't match
    }

    [Fact]
    public void Properties_ShouldRaisePropertyChangedEvents()
    {
        // Arrange
        var viewModel = new EntityTreeItemViewModel();
        var nameChanged = false;
        var typeChanged = false;
        var messageCountChanged = false;
        var isExpandedChanged = false;
        var isVisibleChanged = false;

        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(EntityTreeItemViewModel.Name))
                nameChanged = true;
            if (args.PropertyName == nameof(EntityTreeItemViewModel.Type))
                typeChanged = true;
            if (args.PropertyName == nameof(EntityTreeItemViewModel.MessageCount))
                messageCountChanged = true;
            if (args.PropertyName == nameof(EntityTreeItemViewModel.IsExpanded))
                isExpandedChanged = true;
            if (args.PropertyName == nameof(EntityTreeItemViewModel.IsVisible))
                isVisibleChanged = true;
        };

        // Act
        viewModel.Name = "TestQueue";
        viewModel.Type = "Queue";
        viewModel.MessageCount = 100;
        viewModel.IsExpanded = true;
        viewModel.IsVisible = false;

        // Assert
        nameChanged.ShouldBeTrue();
        typeChanged.ShouldBeTrue();
        messageCountChanged.ShouldBeTrue();
        isExpandedChanged.ShouldBeTrue();
        isVisibleChanged.ShouldBeTrue();
    }


}

