# BusOps - Azure Service Bus Management

BusOps is a cross-platform desktop application for managing Azure Service Bus namespaces, queues, topics, and subscriptions.

## Features

- **Multi-Connection Management**: Connect to multiple Service Bus namespaces
- **Queue Operations**: Create, delete, send, receive, and monitor queue messages
- **Topic/Subscription Management**: Full CRUD operations for topics and subscriptions
- **Message Operations**: Send, receive, peek, and manage dead letter messages
- **Real-time Monitoring**: Live updates of message counts and queue depths
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Technology Stack

- **UI Framework**: Avalonia UI 11.x with Fluent Design
- **Backend**: .NET 9 with C#
- **Architecture**: MVVM with ReactiveUI
- **Azure Integration**: Azure.ServiceBus SDK
- **Logging**: Serilog with structured logging
- **Resilience**: Polly for retry policies

## Getting Started

### Prerequisites

- .NET 9 SDK
- Azure Service Bus namespace with connection string

### Building and Running

1. Clone the repository
2. Navigate to the project directory
3. Restore dependencies: `dotnet restore`
4. Build the solution: `dotnet build`
5. Run the application: `dotnet run --project src/BusOps`

### Configuration

1. Launch the application
2. Click "Add Connection" to configure your first Service Bus connection
3. Enter your Service Bus connection string
4. Start managing your Service Bus entities!

## Project Structure

```
BusOps/
├── src/
│   ├── BusOps/                 # Main Avalonia UI application
│   ├── BusOps.Core/            # Business logic and models
│   └── BusOps.Azure/           # Azure Service Bus integration
├── tests/
│   ├── BusOps.Tests/           # Unit tests
│   └── BusOps.IntegrationTests/ # Integration tests
├── docs/                       # Documentation
└── assets/                     # Application assets
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License.