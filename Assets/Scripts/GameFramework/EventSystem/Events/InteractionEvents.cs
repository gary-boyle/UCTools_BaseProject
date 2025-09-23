using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.Components.Controllers.Enum;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Event published when an interactable object becomes available for interaction
    /// </summary>
    public class InteractionAvailableEvent
    {
        public IInteractable Interactable { get; }
        public PlayerPrefabType ControllerType { get; }
        
        public InteractionAvailableEvent(IInteractable interactable, PlayerPrefabType controllerType)
        {
            Interactable = interactable;
            ControllerType = controllerType;
        }
    }
    
    /// <summary>
    /// Event published when an interactable object is no longer available for interaction
    /// </summary>
    public class InteractionUnavailableEvent
    {
        public IInteractable Interactable { get; }
        public PlayerPrefabType ControllerType { get; }
        
        public InteractionUnavailableEvent(IInteractable interactable, PlayerPrefabType controllerType)
        {
            Interactable = interactable;
            ControllerType = controllerType;
        }
    }
    
    /// <summary>
    /// Event published when an interaction is performed on an object
    /// </summary>
    public class InteractionPerformedEvent
    {
        public IInteractable Interactable { get; }
        public PlayerPrefabType ControllerType { get; }
        
        public InteractionPerformedEvent(IInteractable interactable, PlayerPrefabType controllerType)
        {
            Interactable = interactable;
            ControllerType = controllerType;
        }
    }
}
