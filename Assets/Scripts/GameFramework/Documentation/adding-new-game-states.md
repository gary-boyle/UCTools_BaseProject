# Adding New Game States

This guide walks you through the complete process of adding new game states to your Unity project using the Game Framework's state management system. We'll use a simple "Dummy" state to demonstrate the essential steps without complex implementation details.

## Overview

Adding a new game state involves several coordinated steps:

1. **Define the State Type** - Add to the GameStateType enum
2. **Create the State Class** - Implement your state logic
3. **Register the State** - Add to dependency injection system
4. **Define Transitions** - Specify valid state flows
5. **Test Integration** - Verify proper functionality

## Step-by-Step Implementation

### Step 1: Define the State Type

First, add your new state to the `GameStateType` enum:

```csharp
// In GameStateType enum
public enum GameStateType
{
    Bootstrap,
    Splash,
    MainMenu,
    Loading,
    NewGame,
    Playing,
    Dummy,         // ← New state added here
    Credits,
    GameOver,
    Victory,
    Quit
}
```

### Step 2: Create the State Class

Create your state class inheriting from `BaseGameState`:

```csharp
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.StateMachine;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// A simple dummy state for testing or demonstration purposes.
    /// Shows the minimal implementation required for a game state.
    /// </summary>
    public class DummyState : BaseGameState
    {
        public DummyState(GameContext context, IGameStateMachine stateMachine) 
            : base(GameStateType.Dummy, context, stateMachine)
        {
        }

        /// <summary>
        /// Called when entering the dummy state.
        /// </summary>
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            Debug.Log("[DummyState] Entering Dummy State");
            
            // Minimal implementation - just log entry
            // In a real state, you might:
            // - Show UI screens
            // - Subscribe to events  
            // - Initialize state-specific systems
            // - Set input contexts
        }

        /// <summary>
        /// Called every frame while in dummy state.
        /// </summary>
        public override void Update()
        {
            base.Update();
            
            // Handle basic input - press Space to exit
            if (InputManager.GetKeyDown("Space"))
            {
                Debug.Log("[DummyState] Space pressed, returning to MainMenu");
                _ = TransitionToStateAsync(GameStateType.MainMenu);
            }
        }

        /// <summary>
        /// Called when exiting the dummy state.
        /// </summary>
        public override async Task ExitAsync()
        {
            Debug.Log("[DummyState] Exiting Dummy State");
            
            // Minimal cleanup
            // In a real state, you might:
            // - Hide UI screens
            // - Unsubscribe from events
            // - Save state data
            // - Restore input contexts
            
            await base.ExitAsync();
        }
    }
}
```

### Step 3: Register the State

Add your new state to the dependency injection system in two places:

#### A. Register as Transient Service
In `GameManager.RegisterGameStates()`:

```csharp
private void RegisterGameStates()
{
    Debug.Log("[GameManager] Registering game states...");
    
    // Register all game states as transient
    _container.RegisterTransient<BootstrapState, BootstrapState>();
    _container.RegisterTransient<SplashState, SplashState>();
    _container.RegisterTransient<MainMenuState, MainMenuState>();
    _container.RegisterTransient<LoadingState, LoadingState>();
    _container.RegisterTransient<NewGameState, NewGameState>();
    _container.RegisterTransient<PlayingState, PlayingState>();
    _container.RegisterTransient<DummyState, DummyState>();            // ← Add new state
    _container.RegisterTransient<CreditsState, CreditsState>();
    _container.RegisterTransient<GameOverState, GameOverState>();
    _container.RegisterTransient<VictoryState, VictoryState>();
    _container.RegisterTransient<QuitState, QuitState>();
}
```

#### B. Register in State Machine
In `GameStateMachine.RegisterStates()`:

```csharp
private void RegisterStates()
{
    try
    {
        // Use DI container to create states with all dependencies injected
        RegisterState(_container.Resolve<BootstrapState>());
        RegisterState(_container.Resolve<SplashState>());
        RegisterState(_container.Resolve<MainMenuState>());
        RegisterState(_container.Resolve<LoadingState>());
        RegisterState(_container.Resolve<NewGameState>());
        RegisterState(_container.Resolve<PlayingState>());
        RegisterState(_container.Resolve<DummyState>());              // ← Add new state
        RegisterState(_container.Resolve<CreditsState>());
        RegisterState(_container.Resolve<GameOverState>());
        RegisterState(_container.Resolve<VictoryState>());
        RegisterState(_container.Resolve<QuitState>());
    }
    catch (Exception e)
    {
        Debug.LogError($"[GameStateMachine] Error registering states: {e}");
        throw;
    }
}
```

### Step 4: Define Valid Transitions

Add transition rules in `GameStateMachine.DefineStateTransitions()`:

```csharp
private void DefineStateTransitions()
{
    // ... existing transitions ...
    
    // Dummy state transitions - allow from MainMenu and back
    _validTransitions.Add((GameStateType.MainMenu, GameStateType.Dummy));
    _validTransitions.Add((GameStateType.Dummy, GameStateType.MainMenu));
    
    // ... rest of existing transitions ...
}
```

### Step 5: Update AllGameStates Array

Add your new state to the static array used for Unity compatibility:

```csharp
private static readonly GameStateType[] AllGameStates = new GameStateType[]
{
    GameStateType.Bootstrap,
    GameStateType.Splash,
    GameStateType.MainMenu,
    GameStateType.Loading,
    GameStateType.NewGame,
    GameStateType.Playing,
    GameStateType.Dummy,         // ← Add new state
    GameStateType.Credits,
    GameStateType.GameOver,
    GameStateType.Victory,
    GameStateType.Quit
};
```

## Basic State Template

Here's a minimal template for creating new states:

```csharp
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.StateMachine;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    public class YourNewState : BaseGameState
    {
        public YourNewState(GameContext context, IGameStateMachine stateMachine) 
            : base(GameStateType.YourNewState, context, stateMachine)
        {
        }

        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            Debug.Log($"[{StateType}] Entering state");
            
            // Your enter logic here
        }

        public override void Update()
        {
            base.Update();
            
            // Your update logic here
        }

        public override async Task ExitAsync()
        {
            Debug.Log($"[{StateType}] Exiting state");
            
            // Your exit logic here
            
            await base.ExitAsync();
        }
    }
}
```

## Common Usage Patterns

### Simple Menu State
```csharp
public override async Task EnterAsync(GameContext context)
{
    await base.EnterAsync(context);
    
    // Show UI and handle input
    UIService.ShowScreen("MyScreen");
    InputManager.SetInputContext("Menu");
}

public override async Task ExitAsync()
{
    // Clean up
    UIService.HideScreen("MyScreen");
    InputManager.RestorePreviousInputContext();
    
    await base.ExitAsync();
}
```


## Testing Your New State

### Quick Test Method
Add this to your DummyState to test transitions:

```csharp
public override void Update()
{
    base.Update();
    
    // Test basic transitions
    if (InputManager.GetKeyDown("Space"))
        _ = TransitionToStateAsync(GameStateType.MainMenu);
    
    if (InputManager.GetKeyDown("Escape"))
        _ = TransitionToStateAsync(GameStateType.Quit);
}
```

## Troubleshooting

### Common Issues

#### ❌ **State Not Found**
```
[GameStateMachine] State Dummy not registered!
```
**Fix**: Check both registration locations in Step 3.

#### ❌ **Invalid Transition**
```
[GameStateMachine] Invalid transition from MainMenu to Dummy
```
**Fix**: Add transition rule in Step 4.

#### ❌ **Constructor Error**
```
ArgumentNullException: context cannot be null
```
**Fix**: Ensure state is registered as transient in DI container.

### Quick Checklist

- [ ] Added to GameStateType enum
- [ ] Created state class inheriting BaseGameState
- [ ] Registered in GameManager.RegisterGameStates()
- [ ] Registered in GameStateMachine.RegisterStates()
- [ ] Added transitions in DefineStateTransitions()
- [ ] Updated AllGameStates array
- [ ] Tested basic enter/exit functionality

## Best Practices

### 🏗️ **Keep It Simple**
Start with minimal implementation and add complexity gradually:

```csharp
// ✅ Good: Start simple
public override async Task EnterAsync(GameContext context)
{
    await base.EnterAsync(context);
    Debug.Log("Entered state");
}

// ❌ Avoid: Complex logic in first version
public override async Task EnterAsync(GameContext context)
{
    await base.EnterAsync(context);
    // 50 lines of complex initialization...
}
```

### 🔄 **Follow the Pattern**
Always call base methods and maintain consistent structure:

```csharp
// ✅ Good: Consistent pattern
public override async Task EnterAsync(GameContext context)
{
    await base.EnterAsync(context);    // Always call base first
    // Your logic here
}

public override async Task ExitAsync()
{
    // Your cleanup here
    await base.ExitAsync();            // Always call base last
}
```

### 🎯 **Single Responsibility**
Each state should handle one clear purpose:

```csharp
// ✅ Good: Clear, focused state
public class PauseState : BaseGameState
{
    // Handles only pause menu functionality
}

// ❌ Avoid: States that do too much
public class GameplayAndInventoryAndSettingsState : BaseGameState
{
    // Trying to handle multiple concerns
}
```

This simplified approach gives you a solid foundation for adding new states. Once you're comfortable with the basic pattern, you can add more sophisticated features like UI management, event handling, and complex state logic.