using Avalonia.Controls;
using BusOps.ViewModels;
using BusOps.Views;

namespace BusOps.Services;

public interface IErrorDialogService
{
    Task ShowErrorDialog(string title, Exception ex, Window? owner);
}

public class ErrorDialogService : IErrorDialogService
{
    public async Task ShowErrorDialog(string title, Exception ex, Window? owner)
    {
        if (owner == null)
            return;
        
        var dialogViewModel = ErrorDialogViewModel.FromException(title, ex);
        var dialog = new ErrorDialog(dialogViewModel);
        await dialog.ShowDialog(owner);
    }
    
}