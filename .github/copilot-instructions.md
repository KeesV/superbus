# BusOps - Azure Service Bus Management Application

## Project Overview
Cross-platform desktop application for managing Azure Service Bus with a modern UI, built with C#.

## Tech Stack

### Primary Stack
- **UI Framework**: Avalonia UI 11.x
- **Backend**: .NET 9
- **Architecture**: MVVM with ReactiveUI or CommunityToolkit.Mvvm
- **Azure Integration**: Azure.ServiceBus NuGet package
- **Styling**: Fluent Design System (FluentAvalonia)

### Why Avalonia UI?
- True cross-platform support (Windows, macOS, Linux)
- Modern, performant XAML-based UI
- Excellent theming support with Fluent Design
- Growing ecosystem and active development
- Familiar to WPF developers
- Better desktop experience compared to .NET MAUI

## Complete Technology Dependencies

### Frontend
- Avalonia UI 11.x
- FluentAvalonia (for modern Fluent Design)
- Avalonia.ReactiveUI (for MVVM)

### Backend
- .NET 9 (C#)
- Azure.ServiceBus (official Azure SDK)
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging

### Architecture Patterns
- MVVM pattern
- Dependency Injection
- Repository pattern for Service Bus operations
- Command pattern for UI actions

### Additional Libraries
- System.Text.Json (JSON serialization)
- Serilog (structured logging)
- Polly (retry policies and resilience)

## Application Features

### Core Features
- **Connection Management**: Multiple Service Bus namespace connections
- **Queue Operations**: Create, delete, view messages, purge queues
- **Topic/Subscription Management**: Full CRUD operations
- **Message Operations**: Send, receive, peek, dead letter handling
- **Monitoring**: Queue depths, message counts, real-time metrics
- **Import/Export**: Message backup and restore functionality

### UI/UX Features
- Modern Fluent Design interface
- Dark/Light theme support
- Responsive layout
- Context menus and keyboard shortcuts
- Real-time updates and notifications

## Project Structure

```
busops/
├── BusOps.sln
├── src/
│   ├── BusOps/                 # Main Avalonia project
│   │   ├── Views/                 # XAML views
│   │   ├── ViewModels/            # View models (MVVM)
│   │   ├── Controls/              # Custom controls
│   │   ├── Converters/            # Value converters
│   │   ├── Services/              # UI services
│   │   ├── Styles/                # Custom styles
│   │   ├── App.axaml              # Application entry point
│   │   └── Program.cs             # Main program
│   ├── BusOps.Core/            # Business logic layer
│   │   ├── Services/              # Business services
│   │   ├── Models/                # Domain models
│   │   ├── Interfaces/            # Service contracts
│   │   └── Extensions/            # Helper extensions
│   └── BusOps.Azure/           # Azure Service Bus integration
│       ├── ServiceBusManager.cs   # Main Service Bus client
│       ├── Models/                # Azure-specific models
│       ├── Services/              # Azure services
│       └── Extensions/            # Azure extensions
├── tests/
│   ├── BusOps.Tests/           # Unit tests
│   └── BusOps.IntegrationTests/ # Integration tests
├── docs/                          # Documentation
└── assets/                        # Images, icons, etc.
```

## Development Guidelines

### Coding Standards
- Follow C# coding conventions
- Use async/await for all I/O operations
- Implement proper error handling and logging
- Write unit tests for business logic
- Use dependency injection throughout

### Azure Service Bus Best Practices
- Use connection pooling
- Implement retry policies with Polly
- Handle transient failures gracefully
- Use appropriate message sessions and locks
- Implement proper dispose patterns

### UI Best Practices
- Follow MVVM pattern strictly
- Use data binding for all UI updates
- Implement proper validation
- Provide visual feedback for long operations
- Support keyboard navigation and accessibility

## Configuration

### App Settings
- Support multiple connection strings
- Theme preferences
- Logging configuration
- Default timeout values
- Auto-refresh intervals

### Security
- Secure storage of connection strings
- Support for Azure AD authentication
- Connection string encryption at rest

## Deployment

### Target Platforms
- Windows 10/11 (x64, ARM64)
- macOS 10.15+ (x64, Apple Silicon)
- Linux (major distributions)

### Distribution
- Self-contained deployment
- Framework-dependent deployment options
- Installer packages for each platform