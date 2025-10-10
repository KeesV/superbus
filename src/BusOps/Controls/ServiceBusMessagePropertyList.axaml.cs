using Avalonia;
using Avalonia.Controls;
using BusOps.Core.Models;

namespace BusOps.Controls;

public partial class ServiceBusMessagePropertyList : UserControl
{
    public static readonly StyledProperty<string> TitleProperty = 
        AvaloniaProperty.Register<ServiceBusMessagePropertyList, string>(nameof(Title), "Broker Properties");
    
    public static readonly StyledProperty<List<ServiceBusMessageProperty>> PropertiesProperty = 
        AvaloniaProperty.Register<ServiceBusMessagePropertyList, List<ServiceBusMessageProperty>>(nameof(Properties), new List<ServiceBusMessageProperty>());
    
    public ServiceBusMessagePropertyList()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public List<ServiceBusMessageProperty> Properties
    {
        get => GetValue(PropertiesProperty);
        set => SetValue(PropertiesProperty, value);
    }
}