using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using BusOps.Core.Models;

namespace BusOps.Controls;

public partial class ServiceBusMessagesGrid : UserControl
{
    public static readonly DirectProperty<ServiceBusMessagesGrid, IEnumerable<ServiceBusMessage>> MessagesProperty =
        AvaloniaProperty.RegisterDirect<ServiceBusMessagesGrid, IEnumerable<ServiceBusMessage>>(
            nameof(Messages),
            o => o.Messages,
            (o, v) => o.Messages = v);
    
    public static readonly DirectProperty<ServiceBusMessagesGrid, ServiceBusMessage?> SelectedMessageProperty =
        AvaloniaProperty.RegisterDirect<ServiceBusMessagesGrid, ServiceBusMessage?>(
            nameof(SelectedMessage),
            o => o.SelectedMessage,
            (o, v) => o.SelectedMessage = v,
            defaultBindingMode: BindingMode.TwoWay);

    private IEnumerable<ServiceBusMessage> _messages = new AvaloniaList<ServiceBusMessage>();
    private ServiceBusMessage? _selectedMessage;
    private bool _selectAllChecked;

    public bool SelectAllChecked
    {
        get => _selectAllChecked;
        set
        {
            _selectAllChecked = value;
            ToggleSelectAllMessages(value);
        }
    }

    public IEnumerable<ServiceBusMessage> Messages
    {
        get => _messages;
        set => SetAndRaise(MessagesProperty, ref _messages, value);
    }
    
    public ServiceBusMessage? SelectedMessage
    {
        get => _selectedMessage;
        set => SetAndRaise(SelectedMessageProperty, ref _selectedMessage, value);
    }

    public ServiceBusMessagesGrid()
    {
        InitializeComponent();
    }
    
    private void ToggleSelectAllMessages(bool selectAll)
    {
        if (selectAll)
        {
            // Clear existing selections if any
            foreach (var message in Messages)
            {
                message.IsSelected = false;
            }

            // Select all messages
            foreach (var message in Messages)
            {
                message.IsSelected = true;
            }
        }
        else
        {
            // Deselect all messages
            foreach (var message in Messages)
            {
                message.IsSelected = false;
            }
        }
    }
}
