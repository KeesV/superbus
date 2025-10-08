using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BusOps.ViewModels;

namespace BusOps.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        
        // Set the dialog opening delegate
        viewModel.ShowAddConnectionDialog = ShowAddConnectionDialog;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async System.Threading.Tasks.Task ShowAddConnectionDialog()
    {
        var dialog = new AddConnectionDialog();
        var result = await dialog.ShowDialog<bool?>(this);
        
        if (result == true)
        {
            // Connection was added successfully
            // TODO: Refresh the connections list
        }
    }
}