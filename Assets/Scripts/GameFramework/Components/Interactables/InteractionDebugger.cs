using UnityEngine;
using GameFramework.Components.Controllers;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Core;

namespace GameFramework.Components.Interactables
{
    /// <summary>
    /// Temporary debugging helper for interaction system.
    /// Attach to player controller to debug interaction issues.
    /// </summary>
    public class InteractionDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebug = true;
        [SerializeField] private KeyCode _manualTestKey = KeyCode.T;
        
        private BasePlayerController _controller;
        private IEventSystem _eventSystem;
        
        void Start()
        {
            _controller = GetComponent<BasePlayerController>();
            _eventSystem = GameManager.GetService<IEventSystem>();
            
            if (_eventSystem != null && _enableDebug)
            {
                // Subscribe to interaction events to see what's happening
                _eventSystem.Subscribe<InteractionAvailableEvent>(OnInteractionAvailable);
                _eventSystem.Subscribe<InteractionUnavailableEvent>(OnInteractionUnavailable);
                _eventSystem.Subscribe<InteractionPerformedEvent>(OnInteractionPerformed);
                _eventSystem.Subscribe<PlayerInteractInputEvent>(OnPlayerInteractInput);
            }
        }
        
        void Update()
        {
            if (!_enableDebug) return;
            
            // Manual test
            if (Input.GetKeyDown(_manualTestKey))
            {
                Debug.Log("[InteractionDebugger] Manual interaction test triggered");
                _controller?.InteractionDetector?.TriggerInteraction();
            }
            
            // Show current interactable
            var currentInteractable = _controller?.InteractionDetector?.CurrentInteractable;
            if (currentInteractable != null)
            {
                Debug.Log($"[InteractionDebugger] Current interactable: {currentInteractable.Transform.name}");
            }
        }
        
        private void OnPlayerInteractInput(PlayerInteractInputEvent inputEvent)
        {
            Debug.Log($"[InteractionDebugger] PlayerInteractInputEvent received! Phase: {inputEvent.Phase}");
        }
        
        private void OnInteractionAvailable(InteractionAvailableEvent evt)
        {
            Debug.Log($"[InteractionDebugger] Interaction AVAILABLE: {evt.Interactable.Transform.name} for {evt.ControllerType}");
        }
        
        private void OnInteractionUnavailable(InteractionUnavailableEvent evt)
        {
            Debug.Log($"[InteractionDebugger] Interaction UNAVAILABLE: {evt.Interactable.Transform.name}");
        }
        
        private void OnInteractionPerformed(InteractionPerformedEvent evt)
        {
            Debug.Log($"[InteractionDebugger] Interaction PERFORMED: {evt.Interactable.Transform.name}!");
        }
        
        void OnDestroy()
        {
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<InteractionAvailableEvent>(OnInteractionAvailable);
                _eventSystem.Unsubscribe<InteractionUnavailableEvent>(OnInteractionUnavailable);
                _eventSystem.Unsubscribe<InteractionPerformedEvent>(OnInteractionPerformed);
                _eventSystem.Unsubscribe<PlayerInteractInputEvent>(OnPlayerInteractInput);
            }
        }
        
        void OnDrawGizmos()
        {
            if (!_enableDebug || _controller?.InteractionDetector == null) return;
            
            // Let the interaction detector draw its gizmos
            _controller.InteractionDetector.DrawDebugGizmos();
        }
    }
}
