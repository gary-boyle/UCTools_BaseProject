using UnityEngine;
using System.Collections.Generic;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.Components.Controllers.Enum;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;

namespace GameFramework.Components.Interactables
{
    /// <summary>
    /// Handles detection of interactable objects based on controller type.
    /// Supports distance-based detection with facing requirements for character controllers and mouse-based detection for RTS.
    /// </summary>
    public class InteractionDetector
    {
        #region Private Fields
        private readonly Transform _playerTransform;
        private readonly Camera _camera;
        private readonly PlayerPrefabType _controllerType;
        private readonly LayerMask _interactionLayerMask;
        private readonly IEventSystem _eventSystem;
        
        private IInteractable _currentInteractable = null;
        private HashSet<IInteractable> _nearbyInteractables = new HashSet<IInteractable>();
        
        // Distance-based detection settings
        private readonly float _detectionRadius;
        private readonly float _facingAngleThreshold;
        
        // Mouse-based detection settings
        private readonly float _mouseRaycastDistance = 100f;
        #endregion
        
        #region Constructor
        public InteractionDetector(Transform playerTransform, Camera camera, PlayerPrefabType controllerType, 
            LayerMask interactionLayerMask, IEventSystem eventSystem, 
            float detectionRadius = 5f, float facingAngleThreshold = 60f)
        {
            _playerTransform = playerTransform;
            _camera = camera;
            _controllerType = controllerType;
            _interactionLayerMask = interactionLayerMask;
            _eventSystem = eventSystem;
            _detectionRadius = detectionRadius;
            _facingAngleThreshold = facingAngleThreshold;
        }
        #endregion
        
        #region Public Properties
        public IInteractable CurrentInteractable => _currentInteractable;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Update interaction detection based on controller type
        /// </summary>
        public void UpdateDetection()
        {
             switch (_controllerType)
             {
                 case PlayerPrefabType.RTS:
                     UpdateMouseBasedDetection();
                     break;
                 
                 case PlayerPrefabType.FPS:
                 case PlayerPrefabType.ThirdPerson:
                 case PlayerPrefabType.Isometric:
                     UpdateDistanceBasedDetection();
                     break;
             }
        }
        
        /// <summary>
        /// Handle mouse position input for RTS controllers
        /// </summary>
        public void HandleMousePosition(Vector2 mousePosition)
        {
             if (_controllerType == PlayerPrefabType.RTS && _camera != null)
            {
                UpdateMouseBasedDetection();
            }
        }
        
        /// <summary>
        /// Trigger interaction with the current interactable
        /// </summary>
        public void TriggerInteraction()
        {
            if (_currentInteractable?.CanInteract == true)
            {
                _currentInteractable.OnInteract(_controllerType);
                _eventSystem?.Publish(new InteractionPerformedEvent(_currentInteractable, _controllerType));
            }
        }
        #endregion
        
        #region Private Methods - Distance-based Detection
        private void UpdateDistanceBasedDetection()
        {
            IInteractable bestInteractable = null;
            float bestScore = float.MaxValue;
            
            // Find all interactables in range
            Collider[] colliders = Physics.OverlapSphere(_playerTransform.position, _detectionRadius, _interactionLayerMask);
            
            foreach (var collider in colliders)
            {
                var interactable = collider.GetComponent<IInteractable>();
                if (interactable == null || !interactable.CanInteract) continue;
                
                // Check distance
                Vector3 toInteractable = interactable.Transform.position - _playerTransform.position;
                float distance = toInteractable.magnitude;
                
                if (distance > interactable.InteractionRange) continue;
                
                 // Check facing direction
                 float angle = Vector3.Angle(_playerTransform.forward, toInteractable.normalized);
                 if (angle > _facingAngleThreshold) continue;
                
                 // Calculate score (closer and more centered is better)
                 float score = distance;
                 float angleWeight = Vector3.Angle(_playerTransform.forward, toInteractable.normalized) / _facingAngleThreshold;
                 score += angleWeight;
                
                if (score < bestScore)
                {
                    bestScore = score;
                    bestInteractable = interactable;
                }
            }
            
            SetCurrentInteractable(bestInteractable);
        }
        #endregion
        
        #region Private Methods - Mouse-based Detection
        private void UpdateMouseBasedDetection()
        {
            if (_camera == null) return;
            
            Vector2 mousePosition = UnityEngine.Input.mousePosition;
            Ray ray = _camera.ScreenPointToRay(mousePosition);
            
            IInteractable hoveredInteractable = null;
            
            if (Physics.Raycast(ray, out RaycastHit hit, _mouseRaycastDistance, _interactionLayerMask))
            {
                hoveredInteractable = hit.collider.GetComponent<IInteractable>();
                if (hoveredInteractable?.CanInteract != true)
                {
                    hoveredInteractable = null;
                }
            }
            
            SetCurrentInteractable(hoveredInteractable);
        }
        #endregion
        
        #region Private Methods - Common
        private void SetCurrentInteractable(IInteractable newInteractable)
        {
            if (_currentInteractable == newInteractable) return;
            
            // Clear previous interactable
            if (_currentInteractable != null)
            {
                _currentInteractable.OnInteractionUnavailable(_controllerType);
                _eventSystem?.Publish(new InteractionUnavailableEvent(_currentInteractable, _controllerType));
            }
            
            // Set new interactable
            _currentInteractable = newInteractable;
            
            if (_currentInteractable != null)
            {
                _currentInteractable.OnInteractionAvailable(_controllerType);
                _eventSystem?.Publish(new InteractionAvailableEvent(_currentInteractable, _controllerType));
            }
        }
        #endregion
        
        #region Debug
        public void DrawDebugGizmos()
        {
            if (_playerTransform == null) return;
            
            // Draw detection range for distance-based controllers
             if (_controllerType != PlayerPrefabType.RTS)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_playerTransform.position, _detectionRadius);
                
                 // Draw facing cone for all distance-based controllers
                 Gizmos.color = Color.cyan;
                 Vector3 forward = _playerTransform.forward * _detectionRadius;
                 Vector3 right = Quaternion.Euler(0, _facingAngleThreshold, 0) * forward;
                 Vector3 left = Quaternion.Euler(0, -_facingAngleThreshold, 0) * forward;
                 
                 Gizmos.DrawRay(_playerTransform.position, right);
                 Gizmos.DrawRay(_playerTransform.position, left);
            }
            
            // Draw current interactable
            if (_currentInteractable != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_currentInteractable.Transform.position, Vector3.one * 0.5f);
            }
        }
        #endregion
    }
}
