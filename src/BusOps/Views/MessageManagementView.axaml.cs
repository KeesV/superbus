using Avalonia.Controls;
using BusOps.ViewModels;

namespace BusOps.Views;

public partial class MessageManagementView : UserControl
{
    public MessageManagementView()
    {
        InitializeComponent();
        
        // Wire up the confirmation dialog when DataContext changes
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MessageManagementViewModel viewModel)
        {
            viewModel.ShowConfirmationDialog = ShowConfirmationDialog;
        }
    }

    private async Task<bool> ShowConfirmationDialog(string title, string message)
    {
        var viewModel = new ConfirmationDialogViewModel
        {
            Title = title,
            Message = message,
            ConfirmButtonText = "Yes",
            CancelButtonText = "No"
        };

        var dialog = new ConfirmationDialog(viewModel);
        var topLevel = TopLevel.GetTopLevel(this);
        
        if (topLevel is Window window)
        {
            var result = await dialog.ShowDialog<bool>(window);
            return result;
        }

        return false;
    }
}