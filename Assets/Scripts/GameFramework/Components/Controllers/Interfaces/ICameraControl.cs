using UnityEngine;
using GameFramework.EventSystem.Events;

namespace GameFramework.Components.Controllers.Interfaces
{
    /// <summary>
    /// Interface for different camera control behaviors.
    /// Allows composition of different camera styles without inheritance.
    /// </summary>
    public interface ICameraControl
    {
        /// <summary>
        /// Initialize the camera component
        /// </summary>
        void Initialize();

        /// <summary>
        /// Clean up the camera component
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Handle look input from the input system
        /// </summary>
        void HandleLookInput(PlayerLookInputEvent inputEvent);

        /// <summary>
        /// Update camera logic (called from Update)
        /// </summary>
        void UpdateCamera();
        
        /// <summary>
        /// Whether the camera component is currently paused
        /// </summary>
        bool IsPaused { get; }
        
        /// <summary>
        /// Enable or disable camera input processing
        /// </summary>
        void SetInputEnabled(bool enabled);
    }
}
