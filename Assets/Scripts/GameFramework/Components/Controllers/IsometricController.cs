using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Components.Controllers.Animation;
using GameFramework.Components.Controllers.Enum;
using GameFramework.EventSystem.Events;

namespace GameFramework.Components.Controllers
{
    /// <summary>
    /// Isometric controller that combines top-down character movement with fixed-angle isometric camera.
    /// Suitable for isometric RPGs, puzzle games, and top-down action games.
    /// Uses Cinemachine 3.1+ for enhanced camera management.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class IsometricController : BasePlayerController
    {
        #region Serialized Fields
        [Header("Isometric Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        
        [Header("Character Model")]
        [SerializeField] private GameObject _characterModel;
        
        [Header("Grid Movement")]
        [SerializeField] private bool _useGridMovement = false; // Disabled for smooth classic isometric movement
        [SerializeField] private float _gridSize = 1.0f;
        [SerializeField] private float _gridMoveSpeed = 5.0f;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            // Set cursor lock requirement for isometric controllers (never lock cursor)
            _cursorLockRequirement = CursorLockRequirement.Never;
            
            base.Awake();
            
        }

        #endregion

        #region Component Management
        protected override void CreateComponents()
        {


            // Find and assign movement component
            var movementComponent = GetComponent<IsometricMovement>();
            if (movementComponent == null)
            {
                Debug.LogError($"[{GetType().Name}] IsometricMovement component not found on {gameObject.name}");
                return;
            }
            
            // Find and assign camera component  
            var cameraComponent = GetComponent<IsometricCameraControl>();
            if (cameraComponent == null)
            {
                Debug.LogError($"[{GetType().Name}] IsometricCameraControl component not found on {gameObject.name}");
                return;
            }

            // Assign to base class fields
            _movementComponent = movementComponent;
            _cameraComponent = cameraComponent;
            
            // Configure for grid movement if enabled
            if (_useGridMovement && movementComponent != null)
            {
                movementComponent.ConfigureGridMovement(_useGridMovement, _gridSize, _gridMoveSpeed, _interactionLayerMask);
            }
        }
        
        protected override PlayerPrefabType GetControllerType()
        {
            return PlayerPrefabType.Isometric;
        }
        #endregion


        #region Debug
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            if (!_showDebugInfo) return;
            
            if (_movementComponent is IsometricMovement isometricMovement)
            {
                isometricMovement.DrawGridDebugGizmos(_showDebugInfo);
            }
        }
        #endregion
    }
}