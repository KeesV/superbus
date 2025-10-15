using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using AvaloniaEdit.TextMate;
using BusOps.Core.Models;
using TextMateSharp.Grammars;

namespace BusOps.Views;

public partial class ServiceBusMessageView : UserControl
{
    public static readonly DirectProperty<ServiceBusMessageView, ServiceBusMessage?> MessageProperty =
        AvaloniaProperty.RegisterDirect<ServiceBusMessageView, ServiceBusMessage?>(
            nameof(Message),
            o => o.Message,
            (o, v) => o.Message = v,
            defaultBindingMode: BindingMode.OneWay);

    public ServiceBusMessage? Message
    {
        get => _message;
        set
        {
            SetAndRaise(MessageProperty, ref _message, value);
            SetBodyViewerText(value?.Body ?? string.Empty);
        }
    }

    private ServiceBusMessage? _message;

    public ServiceBusMessageView()
    {
        InitializeComponent();
        
        // Make it behave like a viewer
        BodyViewer.IsReadOnly = true;
        BodyViewer.Focusable = false;

        // Install TextMate highlighting
        var registry = new RegistryOptions(ThemeName.DarkPlus); // or DarkPlus
        var tm = BodyViewer.InstallTextMate(registry);

        // Pick JSON grammar
        tm.SetGrammar(registry.GetScopeByLanguageId("json"));
    }

    private void SetBodyViewerText(string rawText)
    {
        BodyViewer.Text = PrettyPrintJson(rawText);
    }
    
    private static string PrettyPrintJson(string raw)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(raw);
            return System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return raw; // fall back to original if invalid JSON
        }
    }
}