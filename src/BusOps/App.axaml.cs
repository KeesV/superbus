using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BusOps.Views;
using BusOps.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using BusOps.Core.Interfaces;
using BusOps.Azure.Services;

namespace BusOps;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        // Logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/busops-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        services.AddLogging(builder => builder.AddSerilog());

        // Core Services
        services.AddSingleton<IServiceBusClientFactory, ServiceBusClientFactory>();
        services.AddSingleton<IServiceBusConnectionService, AzureServiceBusConnectionService>();
        services.AddSingleton<IServiceBusManagementService, AzureServiceBusManagementService>();
        services.AddSingleton<IServiceBusMessageService, AzureServiceBusMessageService>();

        // Views and ViewModels
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<AddConnectionDialogViewModel>();
        services.AddTransient<EntitiesTreeViewModel>();
        services.AddTransient<MessageManagementViewModel>();
    }
}