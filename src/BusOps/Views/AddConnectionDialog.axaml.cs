using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusOps.ViewModels;

namespace BusOps.Views;

public partial class AddConnectionDialog : Window
{
    private readonly AddConnectionDialogViewModel? _viewModel;

    public AddConnectionDialog()
    {
        InitializeComponent();
    }
    
    public AddConnectionDialog(AddConnectionDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        
        InitializeComponent();
        DataContext = _viewModel;
        
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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var success = await _viewModel.SaveConnectionAsync();
        if (success)
        {
            Close(_viewModel.CreatedConnection);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
