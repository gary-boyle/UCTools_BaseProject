using UnityEngine;
using GameFramework.Components.Controllers.Enum;

namespace GameFramework.Components.Controllers.Interfaces
{
    /// <summary>
    /// Interface for objects that can be interacted with by players.
    /// Provides different interaction behaviors based on controller type.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called when the player can interact with this object (outline shown)
        /// </summary>
        void OnInteractionAvailable(PlayerPrefabType controllerType);
        
        /// <summary>
        /// Called when the player is no longer able to interact with this object (outline hidden)
        /// </summary>
        void OnInteractionUnavailable(PlayerPrefabType controllerType);
        
        /// <summary>
        /// Called when the player performs an interaction with this object
        /// </summary>
        void OnInteract(PlayerPrefabType controllerType);
        
        /// <summary>
        /// Whether this object can currently be interacted with
        /// </summary>
        bool CanInteract { get; }
        
        /// <summary>
        /// The interaction range for distance-based detection
        /// </summary>
        float InteractionRange { get; }
        
        /// <summary>
        /// The transform of this interactable object
        /// </summary>
        Transform Transform { get; }
    }
}
