using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using BusOps.Core.Models;

namespace BusOps.Controls;

public partial class ServiceBusMessagePropertyList : UserControl
{
    public static readonly DirectProperty<ServiceBusMessagePropertyList, string> TitleProperty =
        AvaloniaProperty.RegisterDirect<ServiceBusMessagePropertyList, string>(
            nameof(Title),
            o => o.Title,
            (o, v) => o.Title = v,
            defaultBindingMode: BindingMode.OneWay);
    
    public static readonly DirectProperty<ServiceBusMessagePropertyList, IEnumerable<ServiceBusMessageProperty>> PropertiesProperty = 
        AvaloniaProperty.RegisterDirect<ServiceBusMessagePropertyList, IEnumerable<ServiceBusMessageProperty>>(
            nameof(Properties), 
            o => o.Properties,
            (o, v) => o.Properties = v,
            defaultBindingMode: BindingMode.OneWay);
    
    public ServiceBusMessagePropertyList()
    {
        InitializeComponent();
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetAndRaise(TitleProperty, ref _title, value);
    }

    private IEnumerable<ServiceBusMessageProperty> _properties = new AvaloniaList<ServiceBusMessageProperty>();
    public IEnumerable<ServiceBusMessageProperty> Properties
    {
        get => _properties;
        set => SetAndRaise(PropertiesProperty, ref _properties, value);
    }
}