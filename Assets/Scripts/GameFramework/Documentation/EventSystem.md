# EventSystem Documentation

## Overview

The `EventSystem` is a lightweight, generic event management system designed for Unity game projects. It provides a decoupled communication mechanism between different game components using a publish-subscribe pattern, enabling loose coupling and improved code maintainability.

## Key Features

- **Type-Safe Events**: Uses generic type parameters to ensure compile-time type safety
- **Dual Event Types**: Supports both parameterized events (with data) and parameterless events
- **Error Resilience**: Handles exceptions in event handlers gracefully without breaking the event chain
- **Lifecycle Management**: Proper initialization and shutdown procedures
- **Memory Efficient**: Automatic cleanup of empty handler lists
- **Thread-Safe Ready**: Async initialization support for future extensibility

## Architecture

The EventSystem uses a dictionary-based storage mechanism where:
- **Key**: `Type` representing the event type
- **Value**: `List<Delegate>` containing all registered handlers for that event type

```
EventType → [Handler1, Handler2, Handler3, ...]
```

## Usage Examples

### Basic Event Handling

#### 1. Parameterless Events

```csharp
// Define an event marker class
public class GameStarted { }

// Subscribe to the event
eventSystem.Subscribe<GameStarted>(() => {
    Debug.Log("Game has started!");
});

// Publish the event
eventSystem.Publish<GameStarted>();
```

#### 2. Events with Data

```csharp
// Define an event data class
public class PlayerDied
{
    public string PlayerName { get; set; }
    public Vector3 DeathPosition { get; set; }
}

// Subscribe to the event
eventSystem.Subscribe<PlayerDied>(data => {
    Debug.Log($"Player {data.PlayerName} died at {data.DeathPosition}");
});

// Publish the event with data
eventSystem.Publish(new PlayerDied 
{ 
    PlayerName = "Hero", 
    DeathPosition = transform.position 
});
```

### Lifecycle Management

```csharp
// Initialize the system
await eventSystem.InitializeAsync();

// Use the system...

// Clean shutdown
eventSystem.Shutdown();
```

## API Reference

### Initialization Methods

| Method | Description |
|--------|-------------|
| `InitializeAsync()` | Asynchronously initializes the event system |
| `Shutdown()` | Shuts down the system and clears all handlers |

### Subscription Methods

| Method | Description |
|--------|-------------|
| `Subscribe<T>(Action<T> handler)` | Subscribe to events with data of type T |
| `Subscribe<T>(Action handler)` | Subscribe to parameterless events of type T |

### Unsubscription Methods

| Method | Description |
|--------|-------------|
| `Unsubscribe<T>(Action<T> handler)` | Unsubscribe from events with data |
| `Unsubscribe<T>(Action handler)` | Unsubscribe from parameterless events |

### Publishing Methods

| Method | Description |
|--------|-------------|
| `Publish<T>(T eventData)` | Publish an event with data |
| `Publish<T>()` | Publish a parameterless event |

### Utility Methods

| Method | Description |
|--------|-------------|
| `Clear()` | Remove all event handlers |

## Best Practices

### Event Design

1. **Use Descriptive Names**: Create clear, intention-revealing event classes
   ```csharp
   public class PlayerLevelUp { } // Good
   public class Event1 { }        // Poor
   ```

2. **Keep Events Immutable**: Design event data classes as immutable structures
   ```csharp
   public class ScoreChanged
   {
       public int NewScore { get; }
       public int PreviousScore { get; }
       
       public ScoreChanged(int newScore, int previousScore)
       {
           NewScore = newScore;
           PreviousScore = previousScore;
       }
   }
   ```

### Memory Management

1. **Always Unsubscribe**: Prevent memory leaks by unsubscribing when objects are destroyed
   ```csharp
   private void OnDestroy()
   {
       eventSystem.Unsubscribe<PlayerDied>(OnPlayerDied);
   }
   ```

2. **Use Clear Responsibly**: Only call `Clear()` during major state transitions



## Thread Safety

⚠️ **Important**: The current implementation is **not thread-safe**. All operations should be performed on the main Unity thread. Future versions may include thread-safe variants.

## Example Integration

```csharp
public class GameManager : MonoBehaviour
{
    private EventSystem eventSystem;
    
    private async void Start()
    {
        eventSystem = new EventSystem();
        await eventSystem.InitializeAsync();
        
        // Subscribe to game events
        eventSystem.Subscribe<GameOver>(OnGameOver);
    }
    
    private void OnGameOver(GameOver data)
    {
        // Handle game over logic
    }
    
    private void OnDestroy()
    {
        eventSystem?.Shutdown();
    }
}
```
