# Modular Player Controller System

A comprehensive, modular player controller system for Unity that supports multiple controller styles using composition over inheritance. Built with Cinemachine 3.1+ integration and designed for maximum flexibility and extensibility.

## Features

- **Multiple Controller Types**: First-Person, Third-Person, RTS, and Isometric
- **Composition-Based Architecture**: Easy to extend without inheritance complexity
- **Cinemachine 3.1+ Integration**: Modern camera system with advanced features
- **Event-Driven Input**: Integrates with existing input and event systems
- **Runtime Switching**: Change controller types during gameplay
- **Prefab Variants**: Support for prefab-based controller variations
- **Factory Pattern**: Easy controller creation and management
- **Pause-Aware**: Proper pause handling across all controller types
- **Debug Support**: Comprehensive debugging and visualization tools

## Architecture Overview

### Core Interfaces
- **`IPlayerMovement`**: Defines movement behavior contracts
- **`ICameraControl`**: Defines camera control behavior contracts

### Movement Components
- **`FirstPersonMovement`**: WASD movement with physics
- **`ThirdPersonMovement`**: Camera-relative character movement
- **`RTSMovement`**: Camera panning and zoom for strategy games
- **`IsometricMovement`**: Top-down movement with grid support

### Camera Components (Cinemachine 3.1+)
- **`FirstPersonCameraControl`**: Direct mouse look
- **`ThirdPersonCameraControl`**: Orbital camera with collision
- **`RTSCameraControl`**: Pan/zoom/rotate camera
- **`IsometricCameraControl`**: Fixed-angle following camera

### Composite Controllers
- **`FirstPersonController`**: FPS-style gameplay
- **`ThirdPersonController`**: Third-person adventure/action
- **`RTSController`**: Real-time strategy with unit selection
- **`IsometricController`**: Top-down RPG/puzzle games

### Management Classes
- **`ControllerFactory`**: Create and configure controllers
- **`ControllerManager`**: Runtime switching and prefab variants

## Quick Start

### 1. Basic Controller Creation

```csharp
// Create a first-person controller
var config = ControllerConfiguration.CreateDefault(ControllerType.FirstPerson);
var controller = ControllerFactory.CreateController(ControllerType.FirstPerson, playerGameObject, config);
```

### 2. Using ControllerManager for Runtime Switching

```csharp
// Add ControllerManager to your player GameObject
var manager = playerGameObject.AddComponent<ControllerManager>();

// Configure available controller types
manager.AvailableTypes = new[] { ControllerType.FirstPerson, ControllerType.ThirdPerson };

// Switch at runtime
manager.SwitchToController(ControllerType.ThirdPerson);
```

### 3. Creating Prefab Variants

1. Create a player GameObject with your desired controller
2. Configure all settings (movement speed, camera angles, etc.)
3. Save as a prefab variant
4. Assign to ControllerManager's prefab slots

## Controller Types

### First Person Controller
- **Best For**: FPS games, immersive experiences
- **Features**: Direct camera control, attack/interaction system, physics-based movement
- **Requirements**: Rigidbody, CapsuleCollider

```csharp
var fpController = gameObject.GetComponent<FirstPersonController>();
fpController.SetAttackEnabled(true);
fpController.SetInteractionRange(3.0f);
```

### Third Person Controller  
- **Best For**: Adventure games, action RPGs, platformers
- **Features**: Camera-relative movement, orbital camera, character animation support
- **Requirements**: Rigidbody, CapsuleCollider, optional Animator

```csharp
var tpController = gameObject.GetComponent<ThirdPersonController>();
tpController.SetAnimator(characterAnimator);
tpController.GetThirdPersonCamera().SetDistanceLimits(2f, 10f);
```

### RTS Controller
- **Best For**: Strategy games, simulation games
- **Features**: Camera movement, unit selection, box selection, command system
- **Requirements**: None (camera-only controller)

```csharp
var rtsController = gameObject.GetComponent<RTSController>();
rtsController.FocusOnPosition(Vector3.zero);
var selectedUnits = rtsController.GetSelectedUnits();
```

### Isometric Controller
- **Best For**: Top-down RPGs, puzzle games, retro games
- **Features**: Grid movement option, sprite support, item collection, 8-directional animation
- **Requirements**: Rigidbody, CapsuleCollider

```csharp
var isoController = gameObject.GetComponent<IsometricController>();
isoController.SetGridMovement(true, 1.0f); // Enable 1-unit grid movement
isoController.SetCollectionRange(2.0f);
```

## Configuration

### Creating Custom Configurations

```csharp
var config = new ControllerConfiguration
{
    StartEnabled = true,
    CreateCinemachineCamera = true,
    FieldOfView = 75f,
    AttackRange = 3.0f,
    InteractionRange = 2.5f,
    EnableAttack = true,
    EnableInteraction = true
};

var controller = ControllerFactory.CreateController(ControllerType.FirstPerson, player, config);
```

### Configuration Properties

- **General**: `StartEnabled`, `CreateCinemachineCamera`, `CameraPriority`
- **Camera**: `FieldOfView`, `OrthographicSize`
- **Combat**: `EnableAttack`, `EnableInteraction`, `AttackRange`, `InteractionRange`
- **RTS**: `SelectionBoxColor`
- **Isometric**: `UseGridMovement`, `GridSize`, `CollectionRange`

## Cinemachine Integration

The system uses Cinemachine 3.1+ components:

- **`CinemachineCamera`**: Main camera component
- **`CinemachineOrbitalFollow`**: Third-person orbital movement
- **`CinemachinePositionComposer`**: Smart following with dead zones
- **`CinemachineCollider`**: Camera collision detection
- **`CinemachineHardLockToTarget`**: Direct camera control

### Setting up Cinemachine Cameras

Cameras are automatically created when using the factory, but you can also assign existing ones:

```csharp
// First Person
fpController.SetCinemachineCamera(existingCMCamera);

// Third Person  
tpController.SetCinemachineCamera(existingCMCamera);
```

## Input Integration

Controllers automatically integrate with your existing input system through events:

- `PlayerMoveInputEvent`
- `PlayerLookInputEvent` 
- `PlayerJumpInputEvent`
- `PlayerSprintInputEvent`
- `PlayerCrouchInputEvent`
- `PlayerAttackInputEvent`
- `PlayerInteractInputEvent`

## Extending the System

### Creating Custom Movement Components

```csharp
public class CustomMovement : IPlayerMovement
{
    public void Initialize() { /* Implementation */ }
    public void HandleMoveInput(PlayerMoveInputEvent inputEvent) { /* Implementation */ }
    // ... implement other interface methods
}
```

### Creating Custom Camera Components

```csharp
public class CustomCameraControl : ICameraControl
{
    public void Initialize() { /* Implementation */ }
    public void HandleLookInput(PlayerLookInputEvent inputEvent) { /* Implementation */ }
    // ... implement other interface methods
}
```

### Creating Custom Controllers

```csharp
public class CustomController : BasePlayerController
{
    protected override void CreateComponents()
    {
        _movementComponent = new CustomMovement(transform);
        _cameraComponent = new CustomCameraControl(cinemachineCamera);
    }
}
```

## Advanced Features

### Runtime Controller Switching

```csharp
// Using ControllerManager
manager.SwitchToController(ControllerType.ThirdPerson);
manager.CycleToNextController();

// Direct factory usage
ControllerFactory.SwitchController(gameObject, ControllerType.RTS);
```

### Pause Handling

All controllers automatically handle pause events:

```csharp
// Controllers pause automatically when GamePausedEvent is published
eventSystem.Publish(new GamePausedEvent());

// Manual pause control
controller.StopAllMovement();
controller.ResumeAllMovement();
```

### Debug Visualization

Enable debug info in inspector or code:

```csharp
controller.ShowDebugInfo = true; // Shows gizmos and debug logs
```

## Best Practices

1. **Use Factory for Creation**: Always use `ControllerFactory` for consistent setup
2. **Configure Before Use**: Set up configurations before creating controllers
3. **Prefab Variants**: Use prefab variants for complex, reusable controller setups
4. **Event-Driven**: Leverage the event system for loose coupling
5. **Composition**: Extend through composition, not inheritance
6. **Testing**: Use the debug features for testing and tuning

## Performance Considerations

- Movement components use efficient caching
- Camera components minimize allocations
- RTS selection uses spatial partitioning concepts
- Grid movement reduces physics calculations
- All Update loops include early returns for paused states

## Troubleshooting

### Common Issues

1. **Controller Not Responding**: Check if InputContext is set correctly
2. **Camera Not Following**: Ensure Follow/LookAt targets are assigned
3. **Movement Stuttering**: Check FixedUpdate vs Update usage for physics
4. **Input Not Working**: Verify event system subscriptions
5. **Cinemachine Errors**: Ensure you have Cinemachine 3.1+ package installed

### Debug Steps

1. Enable debug info on controllers
2. Check Unity Console for error messages
3. Verify all required components are present
4. Test with simple input first
5. Check event system is properly initialized

## Dependencies

- Unity 2022.3+
- Cinemachine 3.1+
- Your project's input and event systems
- GameFramework.Core services

## API Reference

See individual class documentation for detailed API information:

- `IPlayerMovement` - Movement behavior interface
- `ICameraControl` - Camera control interface  
- `BasePlayerController` - Base controller class
- `ControllerFactory` - Controller creation utilities
- `ControllerManager` - Runtime management
- Individual controller classes for specific implementations

---

*This system provides a solid foundation for diverse controller types while maintaining clean architecture and easy extensibility.*
