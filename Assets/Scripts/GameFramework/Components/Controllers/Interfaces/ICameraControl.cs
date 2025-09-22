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
        /// Set the target transform to follow/look at
        /// </summary>
        void SetTarget(Transform target);

        /// <summary>
        /// Get the camera transform controlled by this component
        /// </summary>
        Transform GetCameraTransform();

        /// <summary>
        /// Whether the camera component is currently paused
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Current look input being processed
        /// </summary>
        Vector2 CurrentLookInput { get; }

        /// <summary>
        /// Mouse sensitivity multiplier specific to this camera
        /// </summary>
        float MouseSensitivityMultiplier { get; set; }

        /// <summary>
        /// Enable or disable camera input processing
        /// </summary>
        void SetInputEnabled(bool enabled);
    }
}
