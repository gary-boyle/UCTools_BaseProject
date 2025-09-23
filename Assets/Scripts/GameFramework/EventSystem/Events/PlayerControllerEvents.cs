using GameFramework.Components.Controllers;
using GameFramework.Components.Controllers.Enum;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Event published when a player controller is instantiated or changed
    /// </summary>
    public class PlayerControllerChangedEvent
    {
        public BasePlayerController Controller { get; }
        public CursorLockRequirement CursorLockRequirement { get; }
        
        public PlayerControllerChangedEvent(BasePlayerController controller, CursorLockRequirement cursorLockRequirement)
        {
            Controller = controller;
            CursorLockRequirement = cursorLockRequirement;
        }
    }
}
