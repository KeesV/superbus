using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusOps.ViewModels;

namespace BusOps.Views;

public partial class ConfirmationDialog : Window
{
    private readonly ConfirmationDialogViewModel _viewModel;

    public ConfirmationDialog(ConfirmationDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        
        InitializeComponent();
        DataContext = _viewModel;
        
        // Wire up button events
        var confirmButton = this.FindControl<Button>("ConfirmButton");
        var cancelButton = this.FindControl<Button>("CancelButton");
        
        if (confirmButton != null)
        {
            confirmButton.Click += ConfirmButton_Click;
        }
        
        if (cancelButton != null)
        {
            cancelButton.Click += CancelButton_Click;
        }
    }

    public ConfirmationDialog() : this(new ConfirmationDialogViewModel())
    {
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

