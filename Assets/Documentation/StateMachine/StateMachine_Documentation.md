# StateMachine Documentation

## Overview

The StateMachine system provides robust state management for Unity games using a hierarchical finite state machine pattern with dependency injection. It manages game flow transitions, UI lifecycle, event coordination, and maintains clean separation between state logic and system services.

## Core Architecture

The StateMachine operates through three main components working together:

```mermaid
graph TD
    A[GameStateMachine] --> B[State Registry]
    A --> C[Transition Validation]
    A --> D[Event Publishing]
    
    B --> E[BaseGameState Instances]
    C --> F[Valid Transition Rules]
    D --> G[GameStateChangeEvent]
    
    E --> H[Concrete State Classes]
    F --> I[Transition Safety]
    G --> J[System Notifications]
```

## State Lifecycle Management

Each game state follows a strict lifecycle pattern with dependency injection:

```mermaid
graph TD
    A[State Creation] --> B[Constructor Injection]
    B --> C[State Registration]
    C --> D[EnterAsync Called]
    D --> E[Setup Phase]
    E --> F[Active Phase]
    F --> G[Update/FixedUpdate Loop]
    G --> H{Exit Requested?}
    H -->|No| G
    H -->|Yes| I[ExitAsync Called]
    I --> J[Cleanup Phase]
    J --> K[State Inactive]
    
    E --> E1[Subscribe to Events]
    E --> E2[Initialize UI]
    E --> E3[Configure Input Context]
    
    J --> J1[Unsubscribe from Events]
    J --> J2[Cleanup UI]
    J --> J3[Reset Input Context]
```

## State Transition System

The state machine enforces valid transitions through a predefined transition map:

```mermaid
graph TD
    A[ChangeStateAsync Called] --> B{Valid Transition?}
    B -->|No| C[Log Error & Return]
    B -->|Yes| D[Store Previous State]
    D --> E[Exit Current State]
    E --> F[Publish GameStateChangeEvent]
    F --> G[Enter New State]
    G --> H[Update Current State Reference]
    
    E --> E1[Cleanup Resources]
    E --> E2[Unsubscribe Events]
    G --> G1[Initialize Resources]
    G --> G2[Subscribe Events]
```

## Event-Driven State Communication

States communicate through the EventSystem, maintaining loose coupling:

```mermaid
graph TD
    A[UI Interaction] --> B[Event Published]
    B --> C[State Event Handler]
    C --> D{Valid for Current State?}
    D -->|No| E[Log Warning/Ignore]
    D -->|Yes| F[Process Event]
    F --> G{Requires State Change?}
    G -->|No| H[Handle Within State]
    G -->|Yes| I[Transition to New State]
    
    H --> J[Update UI/Logic]
    I --> K[Call ChangeStateAsync]
```

## Game State Hierarchy

The system defines a complete set of game states with clear purposes:

```mermaid
graph TD
    A[Game States] --> B[Bootstrap]
    A --> C[Splash]
    A --> D[MainMenu]
    A --> E[NewGame]
    A --> F[Loading]
    A --> G[Playing]
    A --> H[GameOver]
    A --> I[Victory]
    A --> J[Credits]
    A --> K[Quit]
    
    B --> B1[System Initialization]
    C --> C1[Logo Display]
    D --> D1[Menu Navigation]
    E --> E1[Game Setup]
    F --> F1[Scene Loading]
    G --> G1[Active Gameplay]
    H --> H1[Failure Handling]
    I --> I1[Success Handling]
    J --> J1[Credits Display]
    K --> K1[Application Shutdown]
```

## Valid State Transitions

The system maintains strict transition rules for game flow integrity:

```mermaid
graph TD
    Bootstrap --> Splash
    Splash --> MainMenu
    Splash --> Loading
    MainMenu --> NewGame
    MainMenu --> Loading
    MainMenu --> Credits
    MainMenu --> Quit
    NewGame --> Loading
    NewGame --> MainMenu
    NewGame --> Playing
    Loading --> Playing
    Loading --> MainMenu
    Loading --> GameOver
    Playing --> GameOver
    Playing --> Victory
    Playing --> MainMenu
    Playing --> Loading
    Playing --> Quit
    Credits --> MainMenu
    Credits --> Quit
    GameOver --> MainMenu
    GameOver --> NewGame
    GameOver --> Loading
    GameOver --> Quit
    Victory --> MainMenu
    Victory --> Credits
    Victory --> NewGame
    Victory --> Quit
```

## Dependency Injection Pattern

States receive all dependencies through constructor injection:

```mermaid
graph TD
    A[DiContainer] --> B[Create State Instance]
    B --> C[Inject GameContext]
    B --> D[Inject IGameStateMachine]
    C --> E[All Service Dependencies]
    D --> F[State Transition Capability]
    
    E --> E1[EventSystem]
    E --> E2[UIService]
    E --> E3[InputManager]
    E --> E4[LoadService]
    E --> E5[Other Services...]
    
    F --> G[TransitionToStateAsync Method]
```

## State Registration Process

The GameStateMachine creates and registers all states during initialization:

```mermaid
graph TD
    A[StateMachine.InitializeAsync] --> B[RegisterStates Method]
    B --> C[For Each State Type]
    C --> D[Resolve from DI Container]
    D --> E[Constructor Injection Occurs]
    E --> F[RegisterState Called]
    F --> G[State Added to Dictionary]
    G --> H{More States?}
    H -->|Yes| C
    H -->|No| I[All States Registered]
    I --> J[Define Transition Rules]
    J --> K[Ready for State Changes]
```

## Input Context Management

States manage input contexts based on their requirements:

```mermaid
graph TD
    A[State Enter] --> B{State Type?}
    B -->|MainMenu/UI| C[Set InputContext.UI]
    B -->|Playing| D[Set InputContext.Player]
    B -->|Loading| E[Set InputContext.UI]
    
    C --> F[Menu Navigation Enabled]
    D --> G[Player Controls Enabled]
    E --> H[Loading Interactions Only]
    
    I[State Exit] --> J[Reset Input Context]
    J --> K[Prepare for Next State]
```

## UI Lifecycle Management

Each state is responsible for its UI elements throughout its lifecycle:

```mermaid
graph TD
    A[State Enter] --> B[Show State Screen]
    B --> C[Subscribe to UI Events]
    C --> D[Handle User Interactions]
    D --> E{Show Popup Needed?}
    E -->|Yes| F[Show Popup]
    E -->|No| G[Continue State Logic]
    F --> H[Handle Popup Events]
    H --> I{Close Popup?}
    I -->|Yes| J[Hide Popup]
    I -->|No| H
    J --> G
    G --> K{State Exit Requested?}
    K -->|No| D
    K -->|Yes| L[Hide All Popups]
    L --> M[Hide State Screen]
    M --> N[State Cleanup Complete]
```

## Error Handling and Recovery

The state machine implements robust error handling:

```mermaid
graph TD
    A[State Operation] --> B{Exception Occurs?}
    B -->|No| C[Continue Normal Flow]
    B -->|Yes| D[Catch Exception]
    D --> E[Log Detailed Error]
    E --> F{Critical Error?}
    F -->|No| G[Attempt Recovery]
    F -->|Yes| H[Safe State Transition]
    G --> I[Retry Operation]
    H --> J[Fallback State]
    J --> K[User Notification]
    
    I --> L{Recovery Successful?}
    L -->|Yes| C
    L -->|No| H
```

## Load Game Integration

BaseGameState provides universal load game functionality:

```mermaid
graph TD
    A[BeginLoadGameEvent Published] --> B[State Receives Event]
    B --> C[CanLoadFromCurrentState Check]
    C --> D{Loading Allowed?}
    D -->|No| E[Log Warning & Return]
    D -->|Yes| F[Close All Popups]
    F --> G[Transition to Loading State]
    G --> H[LoadingState Takes Over]
    
    C --> C1[Check Current State]
    C --> C2[Check LoadService Status]
    C --> C3[Validate Load Request]
```

## Performance Considerations

### State Creation and Management
- States are created once during initialization and reused
- Constructor injection happens only once per state
- State transitions avoid object creation overhead
- Dictionary lookup for state retrieval: O(1) complexity

### Memory Management
- States maintain minimal memory footprint when inactive
- Event subscriptions are managed per state lifecycle
- UI elements are created/destroyed as needed
- No garbage collection pressure during normal operation

### Update Loop Integration
- Only active state receives Update/FixedUpdate calls
- Passive states remain dormant to save CPU cycles
- State machine overhead is minimal per frame

## Thread Safety Considerations

The current StateMachine implementation has specific thread safety characteristics:

```mermaid
graph TD
    A[Threading Concerns] --> B[State Transitions]
    A --> C[Event Publishing]
    A --> D[UI Operations]
    
    B --> B1[Not Thread-Safe]
    C --> C1[EventSystem Dependent]
    D --> D1[Unity Main Thread Only]
    
    B1 --> E[Use from Main Thread Only]
    C1 --> F[Follow EventSystem Rules]
    D1 --> G[UI Thread Requirements]
```

## Service Integration Pattern

The StateMachine integrates with the broader service architecture:

```mermaid
graph TD
    A[Bootstrap Phase] --> B[Create DiContainer]
    B --> C[Register Services]
    C --> D[Register StateMachine]
    D --> E[Create GameContext]
    E --> F[Initialize StateMachine]
    F --> G[Register All States]
    G --> H[Begin State Flow]
    
    C --> C1[EventSystem]
    C --> C2[UIService]
    C --> C3[InputManager]
    C --> C4[Other Services...]
```

## Debugging and Monitoring

The system provides comprehensive logging and monitoring:

```mermaid
graph TD
    A[State Operations] --> B[Entry/Exit Logging]
    A --> C[Transition Validation]
    A --> D[Error Reporting]
    A --> E[Event Handling]
    
    B --> F[Debug.Log State Changes]
    C --> G[Invalid Transition Warnings]
    D --> H[Exception Context]
    E --> I[Event Processing Logs]
    
    F --> J[Development Debugging]
    G --> K[Flow Validation]
    H --> L[Error Diagnosis]
    I --> M[Event Flow Tracking]
```

## Best Practices

### State Design Guidelines
1. **Single Responsibility**: Each state handles one game mode or screen
2. **Event-Driven**: Use events for communication, avoid direct state references
3. **Lifecycle Management**: Always pair subscriptions with unsubscriptions
4. **Input Context**: Set appropriate input context for each state
5. **UI Ownership**: States own their UI elements' lifecycle

### Transition Design
1. **Validate Transitions**: Only allow logical state flows
2. **Clean Exits**: Ensure proper cleanup before transitions
3. **Error Recovery**: Handle failed transitions gracefully
4. **Event Publishing**: Always publish state change events

### Performance Optimization
1. **Minimize State Logic**: Keep Update methods lightweight
2. **Efficient Event Handling**: Unsubscribe when not needed
3. **UI Efficiency**: Cache UI references when possible
4. **Memory Management**: Clean up resources in ExitAsync

## Common Usage Patterns

### Simple State Transition
States can transition directly using the injected state machine reference.

### Event-Driven State Changes
External systems publish events that states listen to and react accordingly.

### Conditional State Logic
States can implement complex conditional logic based on game data and user actions.

### UI-State Coordination
States coordinate with UI systems to show/hide screens and handle user interactions.

The StateMachine system provides a robust foundation for managing complex game flow while maintaining clean separation of concerns and testability through dependency injection.
