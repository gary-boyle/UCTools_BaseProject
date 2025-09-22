using UnityEngine;
using GameFramework.EventSystem.Events;

namespace GameFramework.Components.Controllers.Interfaces
{
    /// <summary>
    /// Interface for different player movement behaviors.
    /// Allows composition of different movement styles without inheritance.
    /// </summary>
    public interface IPlayerMovement
    {
        /// <summary>
        /// Initialize the movement component
        /// </summary>
        void Initialize();

        /// <summary>
        /// Clean up the movement component
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Handle movement input from the input system
        /// </summary>
        void HandleMoveInput(PlayerMoveInputEvent inputEvent);

        /// <summary>
        /// Handle jump input from the input system
        /// </summary>
        void HandleJumpInput(PlayerJumpInputEvent inputEvent);

        /// <summary>
        /// Handle sprint input from the input system
        /// </summary>
        void HandleSprintInput(PlayerSprintInputEvent inputEvent);

        /// <summary>
        /// Handle crouch input from the input system
        /// </summary>
        void HandleCrouchInput(PlayerCrouchInputEvent inputEvent);

        /// <summary>
        /// Update movement logic (called from Update)
        /// </summary>
        void UpdateMovement();

        /// <summary>
        /// Fixed update for physics-based movement
        /// </summary>
        void FixedUpdateMovement();

        /// <summary>
        /// Stop all movement (useful for pause/cutscenes)
        /// </summary>
        void StopMovement();

        /// <summary>
        /// Whether the movement component is currently paused
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// The transform being moved by this component
        /// </summary>
        Transform MovementTransform { get; }

        /// <summary>
        /// Current movement vector
        /// </summary>
        Vector3 CurrentVelocity { get; }

        /// <summary>
        /// Is the player currently grounded (if applicable)
        /// </summary>
        bool IsGrounded { get; }
    }
}
