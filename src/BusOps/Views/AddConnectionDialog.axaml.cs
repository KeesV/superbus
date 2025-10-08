using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusOps.ViewModels;

namespace BusOps.Views;

public partial class AddConnectionDialog : Window
{
    public AddConnectionDialog()
    {
        InitializeComponent();
        DataContext = new AddConnectionDialogViewModel();
        
        // Wire up button events
        var addButton = this.FindControl<Button>("AddButton");
        var cancelButton = this.FindControl<Button>("CancelButton");
        
        if (addButton != null)
        {
            addButton.Click += AddButton_Click;
        }
        
        if (cancelButton != null)
        {
            cancelButton.Click += CancelButton_Click;
        }
    }

    public AddConnectionDialog(AddConnectionDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Validate and save connection
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
