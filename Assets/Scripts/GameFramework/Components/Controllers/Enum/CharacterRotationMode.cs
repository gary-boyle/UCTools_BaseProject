using UnityEngine;

namespace GameFramework.Components.Controllers.Enum
{
    /// <summary>
    /// Defines how a character's rotation is controlled in third-person movement.
    /// </summary>
    public enum CharacterRotationMode
    {
        /// <summary>
        /// No automatic rotation. Character maintains current facing direction.
        /// Useful for stationary characters or when rotation is handled externally.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Character automatically rotates to face the direction they're moving.
        /// Left/Right input without forward movement provides direct rotation control.
        /// Diagonal or forward movement makes character face movement direction.
        /// Best for tank-like controls or traditional third-person movement.
        /// </summary>
        FaceMovementDirection = 1,
        
        /// <summary>
        /// Character rotation is directly controlled by mouse horizontal input.
        /// Provides immediate, precise rotation control independent of movement.
        /// Best for games requiring precise aiming or camera-relative movement.
        /// </summary>
        MouseControl = 2,
        
        /// <summary>
        /// Hybrid mode: Mouse controls rotation, but character smoothly turns toward 
        /// movement when no mouse input is detected for a specified duration.
        /// Combines precision of mouse control with convenience of auto-facing.
        /// Best for games that want both precise control and intuitive movement.
        /// </summary>
        MouseWithMovementFallback = 3
    }
    
    /// <summary>
    /// Configuration settings for character rotation behavior.
    /// Provides detailed control over rotation parameters for each mode.
    /// </summary>
    [System.Serializable]
    public class CharacterRotationSettings
    {
        [Header("Rotation Mode")]
        [UnityEngine.Tooltip("Primary rotation control method")]
        public CharacterRotationMode rotationMode = CharacterRotationMode.MouseControl;
        
        [Header("Movement Direction Rotation")]
        [UnityEngine.Tooltip("Speed of rotation for direct input (×100 = degrees/second). Only affects A/D key rotation speed.")]
        [UnityEngine.Range(0.0001f, 10f)]
        public float movementRotationSpeed = 1.0f;
        
        [Header("Mouse Control Rotation")]
        [UnityEngine.Tooltip("Sensitivity multiplier for mouse rotation")]
        [UnityEngine.Range(0.01f, 5f)]
        public float mouseRotationSensitivity = 1.0f;
        
        [Header("Hybrid Mode Settings")]
        [UnityEngine.Tooltip("Time in seconds without mouse input before falling back to movement direction")]
        [UnityEngine.Range(0.1f, 3f)]
        public float mouseInactivityThreshold = 0.5f;
    }
}
