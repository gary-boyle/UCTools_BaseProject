using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// UI input events for menu navigation and interface interaction
    /// Handles mouse, keyboard, and controller input for UI systems
    /// </summary>
    
    public class UINavigateInputEvent
    {
        public Vector2 NavigationVector { get; }
        
        public UINavigateInputEvent(Vector2 navigationVector)
        {
            NavigationVector = navigationVector;
        }
    }
    
    public class UISubmitInputEvent { }

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
    
    public class UIRightClickInputEvent { }
    
    public class UIMiddleClickInputEvent { }
    
    public class UIScrollWheelInputEvent
    {
        public Vector2 ScrollDelta { get; }
        
        public UIScrollWheelInputEvent(Vector2 scrollDelta)
        {
            ScrollDelta = scrollDelta;
        }
    }

    public class UICancelInputEvent { }

}