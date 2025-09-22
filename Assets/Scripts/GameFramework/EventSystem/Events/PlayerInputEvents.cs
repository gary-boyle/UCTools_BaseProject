using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Player input events for game mechanics
    /// Handles movement, combat, and player interaction inputs
    /// </summary>
    
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
    
    public class PlayerJumpInputEvent    {
        public InputActionPhase Phase { get; }
        
        public PlayerJumpInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    public class PlayerPreviousInputEvent { }

    public class PlayerNextInputEvent { }
    
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
}
