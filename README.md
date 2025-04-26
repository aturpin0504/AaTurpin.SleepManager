# AaTurpin.SleepManager

A lightweight C# NuGet package for managing Windows system sleep settings.

## Overview

AaTurpin.SleepManager is a .NET NuGet package that provides a simple interface to control the Windows system's sleep behavior. It allows your applications to temporarily prevent the system from entering sleep mode or turning off the display, which is particularly useful for long-running operations, presentations, or media playback scenarios.

The package includes and depends on RunLog for comprehensive logging functionality.

## Features

- Prevent system sleep with a single method call
- Optionally keep the display on while preventing sleep
- Temporarily prevent sleep for a specific duration
- Check the current sleep prevention status
- Comprehensive logging integration

## Requirements

- .NET Framework 4.7.2 or higher
- Windows operating system (uses Windows API P/Invoke calls)

## Installation

Install the SleepManager NuGet package using the Package Manager Console:

```
Install-Package AaTurpin.SleepManager
```

Or using the .NET CLI:

```
dotnet add package AaTurpin.SleepManager
```

Package URL: [NuGet Gallery](https://www.nuget.org/packages/AaTurpin.SleepManager)

The package automatically includes RunLog as a dependency, so you don't need to install it separately.

## Usage

### Basic Usage

```csharp
// Initialize logging first
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Logger = logger;

// Prevent the system from sleeping
SleepManager.PreventSleep();

// Do work that shouldn't be interrupted by sleep...

// Allow system sleep again when done
SleepManager.AllowSleep();
```

### In a Console Application

```csharp
using AaTurpin.SleepManager;
using RunLog;
using System;

class Program
{
    static void Main(string[] args)
    {
        // Configure logging
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
            
        Log.Logger = logger;
        
        // Prevent sleep while application runs
        SleepManager.PreventSleep();
        Console.WriteLine("Sleep prevented. Press any key to exit and restore sleep settings.");
        
        // Wait for user input
        Console.ReadKey();
        
        // Restore sleep settings
        SleepManager.AllowSleep();
        Console.WriteLine("Sleep settings restored.");
    }
}

### Keep Display On

```csharp
// Prevent sleep and keep the display on
SleepManager.PreventSleep(keepDisplayOn: true);
```

### Temporary Prevention

```csharp
// Prevent sleep for 30 minutes
await SleepManager.PreventSleepTemporarilyAsync(TimeSpan.FromMinutes(30));

// Prevent sleep and keep display on for 5 minutes
await SleepManager.PreventSleepTemporarilyAsync(
    TimeSpan.FromMinutes(5), 
    keepDisplayOn: true);
```

### Check Current State

```csharp
// Check if sleep is currently being prevented
bool isSleepPrevented = SleepManager.IsSleepCurrentlyPrevented();

// Check if display power-off is currently being prevented
bool isDisplayPrevented = SleepManager.IsDisplayCurrentlyPrevented();
```

### Custom Logger

```csharp
// Set a custom logger instance for the SleepManager
var customLogger = new LoggerConfiguration()
    .WriteTo.File("sleep-manager-logs.txt")
    .CreateLogger();

SleepManager.SetLogger(customLogger);
```

## How It Works

SleepManager uses the Windows API function `SetThreadExecutionState` to control the system's sleep behavior. This is a thread-specific setting, so the prevention only remains active as long as the thread that called the function is running.

When sleep prevention is activated, the system will not automatically enter sleep mode due to user inactivity. However, the user can still manually put the system to sleep.

## Best Practices

1. Always call `AllowSleep()` when your application is done with operations that required sleep prevention.
2. For long-running applications, consider using `PreventSleepTemporarilyAsync()` instead of managing the state manually.
3. Only prevent sleep when absolutely necessary to conserve energy.
4. Remember that sleep prevention is tied to the thread that activated it. Ensure your application architecture accounts for this.

## Logging

SleepManager integrates with the RunLog logging library and logs the following events:

- When sleep prevention is activated or deactivated
- When temporary sleep prevention begins and ends
- When sleep prevention state is checked
- Errors that occur during operation

## License

MIT