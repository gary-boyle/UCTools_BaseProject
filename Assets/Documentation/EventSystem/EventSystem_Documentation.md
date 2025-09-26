# EventSystem Documentation

## Overview

The EventSystem provides a type-safe, decoupled communication mechanism for Unity game systems. It implements a publish-subscribe pattern that allows components to communicate without direct dependencies, promoting loose coupling and maintainable architecture.

## Core Architecture

The EventSystem operates as a centralized message broker that manages event subscriptions and publications. It supports both parameterized events (with data) and parameterless events (simple notifications).

```mermaid
graph TD
    A[EventSystem Core] --> B[Subscription Management]
    A --> C[Publication System]
    A --> D[Handler Storage]
    A --> E[Error Handling]
    
    B --> B1[Type-Based Registration]
    C --> C1[Type-Safe Publishing]
    D --> D2[Dictionary<Type, List<Delegate>>]
    E --> E1[Exception Isolation]
```

## Event Registration Flow

The subscription system maintains handlers in type-indexed collections:

```mermaid
graph TD
    A[Subscribe<T> Called] --> B{Event Type Exists?}
    B -->|No| C[Create New Handler List]
    B -->|Yes| D[Get Existing Handler List]
    C --> E[Add to _handlers Dictionary]
    D --> F[Add Handler to List]
    E --> F
    F --> G[Handler Registered Successfully]
```

## Event Publication Process

Event publishing follows a safe iteration pattern with error isolation:

```mermaid
graph TD
    A[Publish<T> Called] --> B{Handlers Exist for Type?}
    B -->|No| C[Silent Return - No Action]
    B -->|Yes| D[Get Handler List]
    D --> E[Iterate Handlers Backwards]
    E --> F{More Handlers?}
    F -->|Yes| G[Invoke Next Handler]
    F -->|No| H[Publication Complete]
    
    G --> I{Handler Throws Exception?}
    I -->|Yes| J[Log Error & Continue]
    I -->|No| K[Handler Success]
    J --> E
    K --> E
```

## Event Lifecycle Management

The system provides complete lifecycle control for event handlers:

```mermaid
graph TD
    A[Component Lifecycle] --> B[Subscribe to Events]
    B --> C[Event Publishing/Handling]
    C --> D{Component Destroying?}
    D -->|No| C
    D -->|Yes| E[Unsubscribe from Events]
    E --> F[Handler Removed]
    F --> G{Handler List Empty?}
    G -->|Yes| H[Remove Type Entry]
    G -->|No| I[Keep Type Entry]
```

## Event Type System

The EventSystem supports multiple event patterns organized by domain:

```mermaid
graph TD
    A[Event Types] --> B[Game State Events]
    A --> C[Audio Events]
    A --> D[Save/Load Events]
    A --> E[UI Events]
    A --> F[Player Input Events]
    A --> G[Notification Events]
    
    B --> B1[GameStateChangeEvent]
    B --> B2[GamePausedEvent]
    B --> B3[GameResumedEvent]
    
    C --> C1[PlayMusicEvent]
    C --> C2[PlaySoundEvent]
    C --> C3[StopMusicEvent]
    
    D --> D1[SaveRequestedEvent]
    D --> D2[SaveCompletedEvent]
    D --> D3[LoadCompletedEvent]
```

## Handler Invocation Strategy

The system uses backwards iteration to handle dynamic handler modification:

```mermaid
graph TD
    A[Handler List] --> B[Start from Last Index]
    B --> C[Invoke Handler at Index i]
    C --> D{Handler Modified List?}
    D -->|Yes| E[Safe - Already Processed]
    D -->|No| F[Continue Normal Flow]
    E --> G[Decrement Index]
    F --> G
    G --> H{Index >= 0?}
    H -->|Yes| C
    H -->|No| I[Iteration Complete]
```

## Event Pattern Types

### Parameterized Events
Events that carry data payloads for rich communication:

```mermaid
graph LR
    A[Publisher] --> B[Create Event Data]
    B --> C[Publish<EventType>(data)]
    C --> D[EventSystem Routes]
    D --> E[Handler Receives Data]
    E --> F[Process Event Data]
```

### Parameterless Events
Simple notification events without data:

```mermaid
graph LR
    A[Publisher] --> B[Publish<EventType>()]
    B --> C[EventSystem Routes]
    C --> D[Handler Receives Notification]
    D --> E[Perform Action]
```

## Error Handling and Resilience

The EventSystem implements robust error handling to prevent cascading failures:

```mermaid
graph TD
    A[Handler Invocation] --> B{Exception Thrown?}
    B -->|No| C[Handler Success]
    B -->|Yes| D[Catch Exception]
    D --> E[Log Error with Context]
    E --> F[Continue to Next Handler]
    F --> G[System Remains Stable]
    
    C --> H[Continue Processing]
    G --> H
```

## Event Categories and Domains

### Game State Management
Handles core game flow and state transitions:

```mermaid
graph TD
    A[Game State Events] --> B[State Changes]
    A --> C[Lifecycle Events]
    A --> D[Scene Management]
    
    B --> B1[GameStateChangeEvent]
    C --> C1[GamePausedEvent]
    C --> C2[GameResumedEvent]
    D --> D1[SceneLoadedEvent]
```

### Audio System Integration
Manages audio playback requests and control:

```mermaid
graph TD
    A[Audio Events] --> B[Music Control]
    A --> C[Sound Effects]
    A --> D[UI Audio]
    
    B --> B1[PlayMusicEvent]
    B --> B2[StopMusicEvent]
    C --> C1[PlaySoundEvent]
    C --> C2[StopSoundEvent]
    D --> D1[UIAudioEvent]
```

### Save/Load Operations
Coordinates game persistence operations:

```mermaid
graph TD
    A[Save Events] --> B[Save Requests]
    A --> C[Save Results]
    A --> D[Save Types]
    
    B --> B1[SaveRequestedEvent]
    C --> C1[SaveCompletedEvent]
    C --> C2[SaveFailedEvent]
    D --> D1[Regular Save]
    D --> D2[Auto Save]
    D --> D3[Overwrite Save]
```

## Memory Management Strategy

The EventSystem uses efficient memory management for handler storage:

```mermaid
graph TD
    A[Handler Management] --> B{Subscribe Called?}
    B -->|Yes| C[Add to List]
    A --> D{Unsubscribe Called?}
    D -->|Yes| E[Remove from List]
    E --> F{List Empty?}
    F -->|Yes| G[Remove Dictionary Entry]
    F -->|No| H[Keep Dictionary Entry]
    
    C --> I[Memory Usage Increases]
    G --> J[Memory Usage Optimized]
    H --> K[Memory Usage Maintained]
```

## Thread Safety Considerations

The current EventSystem implementation is **not thread-safe**:

```mermaid
graph TD
    A[Threading Concerns] --> B[Handler Dictionary Access]
    A --> C[Handler List Modification]
    A --> D[Concurrent Publishing]
    
    B --> B1[Not Synchronized]
    C --> C1[Race Conditions Possible]
    D --> D1[Handler State Issues]
    
    B1 --> E[External Synchronization Required]
    C1 --> E
    D1 --> E
```

## Service Integration Pattern

The EventSystem integrates with the game's service architecture:

```mermaid
graph TD
    A[IGameService Interface] --> B[EventSystem Implementation]
    B --> C[InitializeAsync()]
    B --> D[Shutdown()]
    B --> E[IsInitialized Property]
    
    C --> F[Setup Complete]
    D --> G[Clear All Handlers]
    G --> H[Reset State]
```

## Best Practices and Usage Patterns

### Subscription Management
Components should manage their event subscriptions carefully:

```mermaid
graph TD
    A[Component Start] --> B[Subscribe to Events]
    B --> C[Component Active]
    C --> D[Handle Events]
    D --> E{Component Destroying?}
    E -->|No| D
    E -->|Yes| F[Unsubscribe All Events]
    F --> G[Component Destroyed]
```

### Event Design Guidelines
Events should be designed for clarity and maintainability:

```mermaid
graph TD
    A[Event Design] --> B[Clear Naming Convention]
    A --> C[Minimal Data Payload]
    A --> D[Immutable Properties]
    A --> E[Domain Grouping]
    
    B --> F[ActionSubjectEvent Pattern]
    C --> G[Essential Data Only]
    D --> H[Read-Only After Creation]
    E --> I[Organized by System]
```

## Performance Considerations

### Handler Invocation Overhead
- **Dictionary Lookup**: O(1) for type-based handler retrieval
- **Handler Iteration**: O(n) where n is the number of handlers
- **Exception Handling**: Minimal overhead for success cases
- **Memory Allocation**: No allocations during normal operation

### Optimization Strategies
- Use parameterless events when no data is needed
- Unsubscribe handlers when components are destroyed
- Group related events to minimize subscription overhead
- Consider batching frequent events if performance becomes critical

## Integration Examples

### Service Registration
The EventSystem registers as a singleton service:

```mermaid
graph TD
    A[Application Bootstrap] --> B[DIContainer Setup]
    B --> C[Register IEventSystem]
    C --> D[EventSystem Singleton]
    D --> E[Service Available Globally]
```

### Component Communication
Loose coupling between game systems:

```mermaid
graph TD
    A[Player Controller] --> B[Publish PlayerDeathEvent]
    B --> C[EventSystem Routes Event]
    C --> D[UI System Updates HUD]
    C --> E[Audio System Plays Sound]
    C --> F[Save System Records State]
    
    D --> G[No Direct Dependencies]
    E --> G
    F --> G
```

## Troubleshooting

### Common Issues
1. **Memory Leaks**: Forgetting to unsubscribe handlers
2. **Handler Exceptions**: Unhandled exceptions in event handlers
3. **Event Ordering**: Expecting specific handler execution order
4. **Thread Safety**: Using events across threads

### Debugging Strategies
- Use Unity's Debug.Log in handlers to trace event flow
- Implement event logging for subscription/unsubscription
- Monitor handler count growth for memory leak detection
- Use try-catch blocks in critical event handlers

### Error Patterns
```mermaid
graph TD
    A[Common Errors] --> B[Subscription Leaks]
    A --> C[Handler Exceptions]
    A --> D[Type Mismatches]
    A --> E[Timing Issues]
    
    B --> F[Use using/IDisposable Pattern]
    C --> G[Implement Error Boundaries]
    D --> H[Use Generic Constraints]
    E --> I[Consider Event Queuing]
```

The EventSystem provides a robust foundation for decoupled communication in Unity games, enabling clean separation of concerns and maintainable system architecture.
