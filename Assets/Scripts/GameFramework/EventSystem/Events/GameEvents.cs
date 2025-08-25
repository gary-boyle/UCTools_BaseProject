using GameFramework.Core;
using GameFramework.StateMachine.Enum;

using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.EventSystem.Events
{
    // Game Events
    public class GameStateChangeEvent
    {
        public GameStateType PreviousState { get; set; }
        public GameStateType NewState { get; set; }
        public GameContext Context { get; set; }
    }

    public class GamePausedEvent { }
    public class GameResumedEvent { }
    public class GameStartedEvent { }
    public class GameEndedEvent { }
    public class OptionsChangedEvent { }
    public class SaveGameEvent { }
    public class LoadGameEvent { }
    public class NewGameRequestedEvent { }
    public class LoadRequestedEvent { }
    public class OptionsRequestedEvent { }
    public class CreditsRequestedEvent { }
    public class QuitRequestedEvent { }
    public class PauseRequestedEvent { }
    public class ResumeRequestedEvent { }
    public class MainMenuRequestedEvent { }
    public class GameOverEvent { }
    public class VictoryEvent { }
    public class UICancelInputEvent { }

     #region Player Input Events
    
    public class PlayerMoveInputEvent
    {
        public Vector2 MovementVector { get; }
        public InputActionPhase Phase { get; }
        
        public PlayerMoveInputEvent(Vector2 movementVector, InputActionPhase phase)
        {
            MovementVector = movementVector;
            Phase = phase;
        }
    }
    
    public class PlayerLookInputEvent
    {
        public Vector2 LookDelta { get; }
        public InputActionPhase Phase { get; }
        
        public PlayerLookInputEvent(Vector2 lookDelta, InputActionPhase phase)
        {
            LookDelta = lookDelta;
            Phase = phase;
        }
    }
    
    public class PlayerAttackInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerAttackInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class PlayerInteractInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerInteractInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class PlayerCrouchInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerCrouchInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class PlayerJumpInputEvent
    {
        // Jump is typically just performed, no need for phase
        public PlayerJumpInputEvent() { }
    }
    
    public class PlayerPreviousInputEvent
    {
        // Previous is typically just performed
        public PlayerPreviousInputEvent() { }
    }
    
    public class PlayerNextInputEvent
    {
        // Next is typically just performed
        public PlayerNextInputEvent() { }
    }
    
    public class PlayerSprintInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerSprintInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class PlayerPauseInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerPauseInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }
    
    #endregion
    
    #region UI Input Events
    
    public class UINavigateInputEvent
    {
        public Vector2 NavigationVector { get; }
        
        public UINavigateInputEvent(Vector2 navigationVector)
        {
            NavigationVector = navigationVector;
        }
    }
    
    public class UISubmitInputEvent
    {
        // Submit is typically just performed
        public UISubmitInputEvent() { }
    }

    public class UIPointInputEvent
    {
        public Vector2 Position { get; }
        
        public UIPointInputEvent(Vector2 position)
        {
            Position = position;
        }
    }
    
    public class UIClickInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public UIClickInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class UIRightClickInputEvent
    {
        // Right click is typically just performed
        public UIRightClickInputEvent() { }
    }
    
    public class UIMiddleClickInputEvent
    {
        // Middle click is typically just performed
        public UIMiddleClickInputEvent() { }
    }
    
    public class UIScrollWheelInputEvent
    {
        public Vector2 ScrollDelta { get; }
        
        public UIScrollWheelInputEvent(Vector2 scrollDelta)
        {
            ScrollDelta = scrollDelta;
        }
    }
    
    public class UITrackedDevicePositionInputEvent
    {
        public Vector3 Position { get; }
        
        public UITrackedDevicePositionInputEvent(Vector3 position)
        {
            Position = position;
        }
    }
    
    public class UITrackedDeviceOrientationInputEvent
    {
        public Quaternion Orientation { get; }
        
        public UITrackedDeviceOrientationInputEvent(Quaternion orientation)
        {
            Orientation = orientation;
        }
    }
    
    #endregion
}