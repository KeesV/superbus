using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BusOps.ViewModels;
using BusOps.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BusOps.Views;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider = null!;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider) : this()
    {
        _serviceProvider = serviceProvider;
        DataContext = viewModel;
        
        // Set the dialog opening delegate
        viewModel.ShowAddConnectionDialog = ShowAddConnectionDialog;
        viewModel.ShowErrorDialog = ShowErrorDialog;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async Task ShowAddConnectionDialog()
    {
        // Create the dialog with DI-injected ViewModel
        var dialogViewModel = _serviceProvider.GetRequiredService<AddConnectionDialogViewModel>();
        var dialog = new AddConnectionDialog(dialogViewModel);
        var result = await dialog.ShowDialog<ServiceBusConnection?>(this);
        
        if (result != null)
        {
            // Connection was added successfully
            // TODO: Refresh the connections list
        }
    }

    private async Task ShowErrorDialog(string title, Exception exception)
    {
        var dialogViewModel = ErrorDialogViewModel.FromException(title, exception);
        var dialog = new ErrorDialog(dialogViewModel);
        await dialog.ShowDialog(this);
    }
}
