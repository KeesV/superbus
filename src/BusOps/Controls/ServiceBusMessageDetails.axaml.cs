using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using BusOps.Core.Models;
using Microsoft.Azure.Amqp.Framing;
using TextMateSharp.Grammars;

namespace BusOps.Controls;

public partial class ServiceBusMessageDetails : UserControl
{
    private TextMate.Installation? _tm;

    public ServiceBusMessageDetails()
    {
        InitializeComponent();
        
        // Make it behave like a viewer
        BodyViewer.IsReadOnly = true;
        BodyViewer.Focusable = false;

        // Install TextMate highlighting
        var registry = new RegistryOptions(ThemeName.LightPlus); // or DarkPlus
        _tm = BodyViewer.InstallTextMate(registry);

        // Pick JSON grammar
        _tm.SetGrammar(registry.GetScopeByLanguageId("json"));

        // Subscribe to DataContext changes
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ServiceBusMessage message)
        {
            BodyViewer.Text = message.Body != null ? PrettyPrintJson(message.Body) : string.Empty;
        }
        else
        {
            BodyViewer.Text = string.Empty;
        }
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