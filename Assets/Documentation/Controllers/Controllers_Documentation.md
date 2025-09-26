# Controllers Documentation

## Overview

The Controllers system provides a flexible, component-based player control architecture supporting multiple gameplay perspectives (First-Person, Third-Person, Isometric, RTS). It uses composition over inheritance for maximum flexibility and integrates seamlessly with Unity's physics, input, and Cinemachine systems.

## Core Architecture

The Controllers system operates through a hierarchical composition pattern with clear separation of concerns:

```mermaid
graph TD
    A[BasePlayerController] --> B[IPlayerMovement]
    A --> C[ICameraControl]
    A --> D[InteractionDetector]
    A --> E[PlayerAnimatorController]
    
    B --> F[FirstPersonMovement]
    B --> G[ThirdPersonMovement]
    B --> H[IsometricMovement]
    
    C --> I[FirstPersonCameraControl]
    C --> J[ThirdPersonCameraControl]
    C --> K[IsometricCameraControl]
    C --> L[RTSCameraControl]
    
    A --> M[Concrete Controllers]
    M --> N[FirstPersonController]
    M --> O[ThirdPersonController]
    M --> P[IsometricController]
    M --> Q[RTSController]
```

## Controller Types and Characteristics

### Controller Comparison Matrix

| Controller Type | Movement Style | Camera Type | Cursor Lock | Interaction Method | Use Cases |
|----------------|---------------|-------------|-------------|-------------------|-----------|
| **FirstPerson** | Direct character control | Attached to character | Always locked | Distance + Facing | FPS games, immersive exploration |
| **ThirdPerson** | Character movement + rotation | Orbital around character | Gameplay only | Distance + Facing | Action RPGs, adventure games |
| **Isometric** | Top-down movement | Fixed angle overhead | Never locked | Distance + Facing | RPGs, puzzle games, strategy |
| **RTS** | Camera movement | Free camera control | Never locked | Mouse hover | Strategy games, city builders |

## BasePlayerController Architecture

The base controller provides a unified framework for all player control types:

```mermaid
graph TD
    A[BasePlayerController Initialization] --> B[Service Resolution]
    B --> C[Component Discovery]
    C --> D[Component Creation]
    D --> E[Component Initialization]
    E --> F[Event Subscription]
    F --> G[Interaction System Setup]
    G --> H[Controller Ready]
    
    B --> B1[EventSystem]
    B --> B2[InputManager]
    B --> B3[PauseService]
    
    C --> C1[Find Existing Components]
    C --> C2[Locate Animation Controller]
    
    D --> D1[Create Movement Component]
    D --> D2[Create Camera Component]
    
    E --> E1[Initialize Movement]
    E --> E2[Initialize Camera]
    E --> E3[Initialize Animation]
```

## Component Lifecycle Management

Each controller manages a complex lifecycle with multiple coordinated components:

```mermaid
graph TD
    A[Controller Start] --> B[Awake - Service Resolution]
    B --> C[Start - Initialize if Auto-init]
    C --> D[Component Creation & Setup]
    D --> E[Event Subscription]
    E --> F[Active State]
    
    F --> G[Update Loop]
    G --> H[Movement Update]
    G --> I[Animation Update]
    G --> J[Interaction Update]
    
    F --> K[Fixed Update Loop]
    K --> L[Camera Update]
    K --> M[Physics Movement]
    
    F --> N{Game Paused?}
    N -->|Yes| O[Stop All Movement]
    N -->|No| G
    
    P[Controller Destroy] --> Q[Cleanup Components]
    Q --> R[Unsubscribe Events]
    R --> S[Clear References]
```

## Input Flow Architecture

The system uses event-driven input processing with context-aware routing:

```mermaid
graph TD
    A[Unity Input System] --> B[InputManager]
    B --> C[Player Input Events]
    C --> D[BasePlayerController]
    D --> E{Input Type?}
    
    E -->|Move| F[Movement Component]
    E -->|Look| G[Camera Component]
    E -->|Jump| F
    E -->|Sprint| F
    E -->|Crouch| F
    E -->|Interact| H[Interaction Detector]
    E -->|Attack| I[Animation Controller]
    
    F --> J[Physics-based Movement]
    G --> K[Camera Transform Update]
    H --> L[Interaction Processing]
    I --> M[Animation Triggers]
```

## Movement System Architecture

The movement system provides specialized movement behaviors through composition:

```mermaid
graph TD
    A[IPlayerMovement Interface] --> B[BaseMovementComponent]
    B --> C[Common Functionality]
    C --> D[Ground Detection]
    C --> E[Jump Mechanics]
    C --> F[Sprint/Crouch States]
    C --> G[Physics Integration]
    
    B --> H[Specialized Implementations]
    H --> I[FirstPersonMovement]
    H --> J[ThirdPersonMovement]
    H --> K[IsometricMovement]
    
    I --> L[Direct Character Control]
    J --> M[Character + Camera Rotation]
    K --> N[Top-down with Grid Support]
```

## Camera System Architecture

The camera system provides perspective-appropriate camera behaviors:

```mermaid
graph TD
    A[ICameraControl Interface] --> B[BaseCameraComponent]
    B --> C[Common Functionality]
    C --> D[Input Processing]
    C --> E[Smooth Transitions]
    C --> F[Pause Handling]
    
    B --> G[Specialized Implementations]
    G --> H[FirstPersonCameraControl]
    G --> I[ThirdPersonCameraControl]
    G --> J[IsometricCameraControl]
    G --> K[RTSCameraControl]
    
    H --> L[Direct View Control]
    I --> M[Orbital Camera System]
    J --> N[Fixed Angle Follow]
    K --> O[Free Camera Movement]
```

## Interaction System Integration

The interaction system provides context-sensitive object interaction:

```mermaid
graph TD
    A[InteractionDetector] --> B{Controller Type?}
    B -->|FPS/ThirdPerson| C[Distance + Facing Detection]
    B -->|RTS| D[Mouse Hover Detection]
    B -->|Isometric| E[Distance + Facing Detection]
    
    C --> F[Raycast from Camera]
    D --> G[Mouse Position Raycast]
    E --> H[Sphere Cast from Character]
    
    F --> I[Validate Interaction]
    G --> I
    H --> I
    I --> J[Trigger Interaction Event]
```

## Animation System Integration

The animation system coordinates with movement and input for responsive character animation:

```mermaid
graph TD
    A[PlayerAnimatorController] --> B[Movement State Tracking]
    B --> C[Animation Parameter Updates]
    C --> D{Movement Type?}
    
    D -->|Walking| E[Set Walk Animation]
    D -->|Running| F[Set Run Animation]
    D -->|Jumping| G[Trigger Jump]
    D -->|Crouching| H[Set Crouch Animation]
    D -->|Idle| I[Set Idle Animation]
    
    J[Input Events] --> K[Animation Triggers]
    K --> L[Attack Animation]
    K --> M[Interaction Animation]
```

## Prefab Management System

The PlayerPrefabSelector manages multiple controller prefabs with automatic loading:

```mermaid
graph TD
    A[PlayerPrefabSelector] --> B[Prefab Registration]
    B --> C[Lazy Loading System]
    C --> D{Prefab Requested?}
    D -->|First Time| E[Load Prefab References]
    D -->|Cached| F[Return Cached Prefab]
    
    E --> G[Build Lookup Dictionary]
    G --> H[Map Enum to GameObjects]
    H --> I[Validate Prefab References]
    I --> F
    
    F --> J[Instantiate Selected Prefab]
```

## Service Integration Pattern

Controllers integrate with multiple game services through dependency injection:

```mermaid
graph TD
    A[Controller Awake] --> B[Service Resolution]
    B --> C[EventSystem Integration]
    B --> D[InputManager Integration]
    B --> E[PauseService Integration]
    B --> F[GameDataService Integration]
    
    C --> G[Input Event Subscription]
    C --> H[Game Event Publishing]
    D --> I[Input Context Management]
    E --> J[Pause State Monitoring]
    F --> K[Camera Reference Access]
```

## Cursor Management System

Different controllers have different cursor requirements managed automatically:

```mermaid
graph TD
    A[Controller Activation] --> B{Controller Type?}
    B -->|FirstPerson| C[Always Lock Cursor]
    B -->|ThirdPerson| D[Lock During Gameplay Only]
    B -->|Isometric/RTS| E[Never Lock Cursor]
    
    C --> F[Publish CursorLockRequirement.DuringGameplay]
    D --> G[Publish CursorLockRequirement.DuringGameplayWithUIExceptions]
    E --> H[Publish CursorLockRequirement.Never]
    
    F --> I[UIService Handles Cursor State]
    G --> I
    H --> I
```

## Performance Optimization Features

The controller system includes several performance optimizations:

```mermaid
graph TD
    A[Performance Features] --> B[Component Caching]
    A --> C[Pause-Aware Updates]
    A --> D[Event-Driven Architecture]
    A --> E[Physics Optimization]
    
    B --> B1[Cache Component References]
    B --> B2[Avoid GetComponent Calls]
    
    C --> C1[Skip Updates When Paused]
    C --> C2[Separate Update/FixedUpdate Timing]
    
    D --> D1[Avoid Polling Input]
    D --> D2[Event-Based State Changes]
    
    E --> E1[Physics-Based Movement]
    E --> E2[Efficient Ground Detection]
```

## Error Handling and Recovery

The system implements robust error handling for component failures:

```mermaid
graph TD
    A[Component Error] --> B{Error Type?}
    B -->|Missing Component| C[Log Error + Disable]
    B -->|Service Unavailable| D[Graceful Degradation]
    B -->|Animation Failure| E[Continue Without Animation]
    
    C --> F[Prevent Null Reference Exceptions]
    D --> G[Fallback to Basic Functionality]
    E --> H[Movement Still Works]
    
    F --> I[Fail-Safe Mode]
    G --> I
    H --> I
```

## Best Practices

### Controller Design Guidelines
1. **Composition Over Inheritance**: Use interfaces for movement and camera components
2. **Event-Driven Communication**: All input and state changes through events
3. **Service Dependencies**: Resolve services in Awake, handle missing services gracefully
4. **Component Lifecycle**: Proper initialization, cleanup, and pause handling
5. **Performance Awareness**: Cache references, avoid expensive operations in Update

### Integration Patterns
1. **Service Integration**: Use GameManager.GetService for dependency resolution
2. **Event Publishing**: Publish controller activation events for cursor management
3. **Animation Coordination**: Coordinate movement state with animation parameters
4. **Interaction Setup**: Configure interaction detection based on controller type
5. **Physics Integration**: Use Rigidbody for movement, handle physics interactions properly

### Performance Optimization
1. **Component Caching**: Cache all component references during initialization
2. **Update Optimization**: Use pause-aware updates, separate Update/FixedUpdate concerns
3. **Memory Management**: Proper cleanup in OnDestroy, unsubscribe from events
4. **Physics Efficiency**: Use appropriate physics settings, optimize collision detection
5. **Input Efficiency**: Event-driven input handling instead of polling

## Common Usage Patterns

### Controller Instantiation
Controllers are typically instantiated through the InstantiationService using prefab selection:

### Input Handling
All input flows through the event system with automatic routing to appropriate components:

### Component Composition
Controllers combine movement, camera, animation, and interaction components for complete player control:

### Service Coordination
Controllers coordinate with multiple services for complete game integration:

The Controllers system provides a robust, flexible foundation for player control in Unity games while maintaining clean separation of concerns, high performance, and easy extensibility.
