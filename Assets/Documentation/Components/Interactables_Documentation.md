# Interactables Documentation

## Overview

The Interactables system provides a flexible object interaction framework that adapts its detection method based on the active player controller type. It supports distance-based detection for character controllers (FPS, Third-Person, Isometric) and mouse-hover detection for RTS controllers, with visual feedback and event integration.

## Core Architecture

The Interactables system operates through a component-based architecture with adaptive detection strategies:

```mermaid
graph TD
    A[Interactables System] --> B[IInteractable Interface]
    A --> C[InteractionDetector]
    A --> D[OutlineRenderer]
    A --> E[Event Integration]
    
    B --> F[BaseInteractable]
    F --> G[Concrete Implementations]
    G --> H[ExampleInteractable]
    G --> I[Custom Interactables]
    
    C --> J[Detection Strategies]
    J --> K[Distance-Based Detection]
    J --> L[Mouse-Based Detection]
    
    D --> M[Visual Feedback]
    M --> N[Material Swapping]
    M --> O[Outline Effects]
    
    E --> P[InteractionAvailableEvent]
    E --> Q[InteractionUnavailableEvent]
    E --> R[InteractionPerformedEvent]
```

## Detection Strategy Architecture

The system uses different detection methods based on the active controller type:

```mermaid
graph TD
    A[InteractionDetector.UpdateDetection] --> B{Controller Type?}
    
    B -->|FPS| C[Distance + Facing Detection]
    B -->|ThirdPerson| C
    B -->|Isometric| C
    B -->|RTS| D[Mouse Hover Detection]
    
    C --> E[Physics.OverlapSphere]
    E --> F[Distance Validation]
    F --> G[Facing Angle Check]
    G --> H[Score Calculation]
    H --> I[Select Best Interactable]
    
    D --> J[Camera.ScreenPointToRay]
    J --> K[Physics.Raycast]
    K --> L[Hit Validation]
    L --> M[Set Hovered Interactable]
    
    I --> N[SetCurrentInteractable]
    M --> N
    N --> O[Update Visual Feedback]
    N --> P[Publish Events]
```

## Interaction Lifecycle

Each interactable object follows a complete lifecycle with visual and event feedback:

```mermaid
graph TD
    A[Interactable in Scene] --> B[Detection Update Loop]
    B --> C{Within Detection Range?}
    
    C -->|No| D[Not Available State]
    C -->|Yes| E[Validation Checks]
    
    E --> F{Passes All Checks?}
    F -->|No| D
    F -->|Yes| G[OnInteractionAvailable]
    
    G --> H[Show Outline Effect]
    G --> I[Publish InteractionAvailableEvent]
    H --> J[Available State]
    I --> J
    
    J --> K{Player Input?}
    K -->|No| L{Still Valid?}
    K -->|Yes| M[OnInteract Called]
    
    L -->|No| N[OnInteractionUnavailable]
    L -->|Yes| J
    
    M --> O[Execute Interaction Logic]
    M --> P[Publish InteractionPerformedEvent]
    
    N --> Q[Hide Outline Effect]
    N --> R[Publish InteractionUnavailableEvent]
    Q --> D
    R --> D
```

## Distance-Based Detection System

For character-based controllers (FPS, Third-Person, Isometric):

```mermaid
graph TD
    A[Distance Detection Update] --> B[Physics.OverlapSphere]
    B --> C[Detection Radius Check]
    C --> D[For Each Collider]
    
    D --> E[Get IInteractable Component]
    E --> F{Component Exists?}
    F -->|No| G[Skip Collider]
    F -->|Yes| H[Check CanInteract]
    
    H --> I{Can Interact?}
    I -->|No| G
    I -->|Yes| J[Distance Check]
    
    J --> K{Within Interaction Range?}
    K -->|No| G
    K -->|Yes| L[Facing Angle Check]
    
    L --> M{Within Facing Threshold?}
    M -->|No| G
    M -->|Yes| N[Calculate Score]
    
    N --> O[Distance Weight + Angle Weight]
    O --> P{Best Score So Far?}
    P -->|Yes| Q[Update Best Candidate]
    P -->|No| R[Continue to Next]
    
    Q --> R
    G --> R
    R --> S{More Colliders?}
    S -->|Yes| D
    S -->|No| T[Set Best as Current]
```

## Mouse-Based Detection System

For RTS controllers with mouse interaction:

```mermaid
graph TD
    A[Mouse Detection Update] --> B[Get Mouse Position]
    B --> C[Camera.ScreenPointToRay]
    C --> D[Physics.Raycast]
    D --> E{Hit Detected?}
    
    E -->|No| F[Clear Current Interactable]
    E -->|Yes| G[Get Hit Collider]
    
    G --> H[Get IInteractable Component]
    H --> I{Component Exists?}
    I -->|No| F
    I -->|Yes| J{Can Interact?}
    
    J -->|No| F
    J -->|Yes| K[Set as Current Interactable]
    
    F --> L[SetCurrentInteractable(null)]
    K --> M[SetCurrentInteractable(found)]
    
    L --> N[Update Visual Feedback]
    M --> N
```

## Visual Feedback System

The OutlineRenderer provides flexible visual feedback for interaction availability:

```mermaid
graph TD
    A[OutlineRenderer] --> B[Initialization]
    B --> C[Store Original Material]
    C --> D[Create/Assign Outline Material]
    
    D --> E[ShowOutline Request]
    E --> F{Use Outline Material?}
    F -->|Yes| G[Swap to Outline Material]
    F -->|No| H[Modify Current Material Color]
    
    G --> I[Visual Outline Active]
    H --> I
    
    I --> J[HideOutline Request]
    J --> K[Restore Original Material]
    K --> L[Visual Outline Disabled]
    
    M[Cleanup] --> N[Restore Materials]
    N --> O[Release Resources]
```

## IInteractable Interface Contract

The interface defines the complete interaction contract:

```mermaid
graph TD
    A[IInteractable Interface] --> B[Core Properties]
    A --> C[Interaction Methods]
    A --> D[State Properties]
    
    B --> E[CanInteract]
    B --> F[InteractionRange]
    B --> G[Transform Reference]
    
    C --> H[OnInteractionAvailable]
    C --> I[OnInteractionUnavailable]
    C --> J[OnInteract]
    
    D --> K[Current State Tracking]
    D --> L[Controller Type Awareness]
```

## Event Integration Pattern

The system publishes events for external system coordination:

```mermaid
graph TD
    A[Interaction State Change] --> B[Event Publishing]
    B --> C[InteractionAvailableEvent]
    B --> D[InteractionUnavailableEvent]
    B --> E[InteractionPerformedEvent]
    
    C --> F[UI System Updates]
    C --> G[Audio Feedback]
    C --> H[Animation Triggers]
    
    D --> I[Clear UI Prompts]
    D --> J[Stop Audio Feedback]
    
    E --> K[Execute Game Logic]
    E --> L[Update Statistics]
    E --> M[Save Progress]
```

## BaseInteractable Implementation

The base implementation provides common functionality and extension points:

```mermaid
graph TD
    A[BaseInteractable] --> B[Common Properties]
    A --> C[Outline Integration]
    A --> D[Interaction Counting]
    A --> E[Debug Support]
    
    B --> F[_canInteract Flag]
    B --> G[_interactionRange]
    
    C --> H[OutlineRenderer Instance]
    C --> I[Automatic Outline Management]
    
    D --> J[Interaction Statistics]
    D --> K[Usage Tracking]
    
    E --> L[Debug Gizmos]
    E --> M[Debug Logging]
    E --> N[Range Visualization]
```

## Interaction Scoring System

For distance-based detection, the system uses a scoring algorithm to select the best interactable:

```mermaid
graph TD
    A[Interaction Scoring] --> B[Distance Component]
    A --> C[Angle Component]
    A --> D[Combined Score]
    
    B --> E[Closer = Better Score]
    C --> F[More Centered = Better Score]
    D --> G[Lowest Total Score Wins]
    
    E --> H[score += distance]
    F --> I[score += (angle / threshold)]
    H --> J[Final Score Calculation]
    I --> J
    J --> K[Compare Against Best]
```

## Debug and Visualization Features

The system provides comprehensive debug visualization:

```mermaid
graph TD
    A[Debug Features] --> B[InteractionDetector Gizmos]
    A --> C[BaseInteractable Gizmos]
    A --> D[Console Logging]
    
    B --> E[Detection Radius Sphere]
    B --> F[Facing Cone Visualization]
    B --> G[Current Interactable Highlight]
    
    C --> H[Interaction Range Sphere]
    C --> I[Availability State Indicator]
    
    D --> J[Interaction Events]
    D --> K[State Changes]
    D --> L[Error Conditions]
```

## Performance Considerations

The system includes several performance optimizations:

```mermaid
graph TD
    A[Performance Features] --> B[Efficient Detection]
    A --> C[Material Management]
    A --> D[Event Optimization]
    
    B --> B1[Physics Queries Only When Needed]
    B --> B2[Layermask Filtering]
    B --> B3[Range-Based Culling]
    
    C --> C1[Material Instance Reuse]
    C --> C2[Lazy Material Creation]
    C --> C3[Proper Cleanup]
    
    D --> D1[Event Publishing Only on Changes]
    D --> D2[Minimal Event Data]
```

## Integration with Controllers

The system adapts its behavior based on the active controller type:

```mermaid
graph TD
    A[Controller Integration] --> B{Controller Type}
    
    B -->|FPS| C[Character-Based Detection]
    B -->|ThirdPerson| C
    B -->|Isometric| C
    B -->|RTS| D[Mouse-Based Detection]
    
    C --> E[Distance + Facing Checks]
    E --> F[Physics Overlap Queries]
    F --> G[3D World Interaction]
    
    D --> H[Screen to World Ray]
    H --> I[Raycast Hit Detection]
    I --> J[Point-and-Click Interaction]
    
    G --> K[Natural Character Interaction]
    J --> L[Precise Cursor Interaction]
```

## Best Practices

### Interactable Design Guidelines
1. **Inherit from BaseInteractable** for standard functionality and consistent behavior
2. **Set Appropriate Ranges** for interaction distance based on object size and context
3. **Implement Visual Feedback** using OutlineRenderer or custom visualization methods
4. **Handle Controller Types** appropriately in interaction logic
5. **Use LayerMasks** to optimize detection performance

### Performance Optimization
1. **Layer Organization** - Use specific layers for interactable objects
2. **Range Tuning** - Set conservative interaction ranges to reduce overlap queries
3. **Material Management** - Reuse outline materials across similar objects
4. **Update Frequency** - Consider detection update frequency for performance
5. **Event Efficiency** - Only publish events when interaction state actually changes

### Integration Patterns
1. **Event-Driven Logic** - Use interaction events to trigger game logic
2. **UI Coordination** - Subscribe to interaction events for UI prompt updates
3. **Audio Integration** - Provide audio feedback for interaction availability
4. **Animation Triggers** - Use interaction events to trigger object animations
5. **Statistics Tracking** - Track interaction data for analytics and achievements

## Common Usage Patterns

### Basic Interactable Object
Inherit from BaseInteractable and override interaction methods for custom behavior.

### Complex Interaction Logic
Implement IInteractable directly for complete control over interaction behavior.

### Visual Feedback Customization
Configure OutlineRenderer settings or implement custom visual feedback systems.

### Controller-Specific Behavior
Use the controller type parameter to provide different interaction behaviors per controller.

The Interactables system provides a robust, flexible foundation for object interaction in Unity games while automatically adapting to different control schemes and maintaining high performance through intelligent detection strategies.
