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
        /// Provides smooth, arcade-style movement with gradual rotation changes.
        /// Best for casual gameplay where movement feels natural and flowing.
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
        [UnityEngine.Tooltip("Speed of rotation when facing movement direction")]
        [UnityEngine.Range(0.01f, 10f)]
        public float movementRotationSpeed = 1.0f;
        
        [UnityEngine.Tooltip("Smoothing time for movement-based rotation")]
        [UnityEngine.Range(0.01f, 1f)]
        public float movementRotationSmoothTime = 0.1f;
        
        [Header("Mouse Control Rotation")]
        [UnityEngine.Tooltip("Sensitivity multiplier for mouse rotation")]
        [UnityEngine.Range(0.01f, 5f)]
        public float mouseRotationSensitivity = 1.0f;
        
        [UnityEngine.Tooltip("Whether to apply mouse rotation instantly or smooth it")]
        public bool smoothMouseRotation = false;
        
        [UnityEngine.Tooltip("Smoothing time for mouse rotation (if enabled)")]
        [UnityEngine.Range(0.01f, 0.5f)]
        public float mouseRotationSmoothTime = 0.05f;
        
        [Header("Hybrid Mode Settings")]
        [UnityEngine.Tooltip("Time in seconds without mouse input before falling back to movement direction")]
        [UnityEngine.Range(0.1f, 3f)]
        public float mouseInactivityThreshold = 0.5f;
        
        [UnityEngine.Tooltip("How quickly to blend from mouse control to movement direction")]
        [UnityEngine.Range(0.1f, 2f)]
        public float hybridBlendSpeed = 1.0f;
    }
}
