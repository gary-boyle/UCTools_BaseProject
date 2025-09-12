using UnityEngine.InputSystem;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Console and debug system events
    /// Handles developer console input and interaction
    /// </summary>
    
    public class ConsoleToggleInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleToggleInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }

    public class ConsoleSubmitInputEvent 
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleSubmitInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }

    public class ConsoleTabCompleteInputEvent 
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleTabCompleteInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }

    public class ConsoleHistoryUpInputEvent 
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleHistoryUpInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }

    public class ConsoleHistoryDownInputEvent 
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleHistoryDownInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }
}