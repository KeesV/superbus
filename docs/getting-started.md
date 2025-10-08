# Getting Started

This document provides information on how to set up and run BusOps locally.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An Azure Service Bus namespace with a connection string
- Visual Studio Code (recommended) or Visual Studio

## Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd busops
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   dotnet run --project src/BusOps
   ```

## Project Structure

### BusOps (Main UI Project)
- **Views/**: XAML user interface files
- **ViewModels/**: MVVM view models using ReactiveUI
- **Controls/**: Custom Avalonia controls
- **Services/**: UI-specific services
- **Converters/**: Value converters for data binding

### BusOps.Core (Business Logic)
- **Models/**: Domain models and DTOs
- **Interfaces/**: Service contracts and abstractions
- **Services/**: Business logic services
- **Extensions/**: Helper extensions

### BusOps.Azure (Azure Integration)
- **Services/**: Azure Service Bus implementation
- **Models/**: Azure-specific models
- **Extensions/**: Azure SDK extensions

## Testing

### Unit Tests
```bash
dotnet test tests/BusOps.Tests
```

### Integration Tests
```bash
dotnet test tests/BusOps.IntegrationTests
```

## Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ServiceBus": {
    "DefaultTimeout": "00:01:00",
    "MaxRetryAttempts": 3
  }
}
```

## Debugging

1. Open the solution in Visual Studio Code
2. Set breakpoints in your code
3. Press F5 to start debugging
4. The application will launch with the debugger attached

## Architecture Notes

- The application follows MVVM pattern with ReactiveUI
- Dependency injection is used throughout for loose coupling
- Azure Service Bus operations include retry policies with Polly
- Logging is structured using Serilog
- UI follows Fluent Design principles