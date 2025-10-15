using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusOps.ViewModels;

namespace BusOps.Views;

public partial class ErrorDialog : Window
{
    private readonly ErrorDialogViewModel _viewModel;

    public ErrorDialog(ErrorDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        
        InitializeComponent();
        DataContext = _viewModel;
        
        // Wire up button events
        var okButton = this.FindControl<Button>("OkButton");
        var copyButton = this.FindControl<Button>("CopyButton");
        
        if (okButton != null)
        {
            okButton.Click += OkButton_Click;
        }
        
        if (copyButton != null)
        {
            copyButton.Click += CopyButton_Click;
        }
    }

    public ErrorDialog() : this(new ErrorDialogViewModel())
    {
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void CopyButton_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(_viewModel.ErrorDetails);
        }
    }
}

