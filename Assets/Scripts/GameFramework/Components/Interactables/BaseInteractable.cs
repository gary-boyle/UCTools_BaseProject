using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.Components.Controllers.Enum;

namespace GameFramework.Components.Interactables
{
    /// <summary>
    /// Base implementation of IInteractable that handles common interaction logic.
    /// Provides outline rendering and basic interaction counting for demonstration.
    /// </summary>
    public class BaseInteractable : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Interaction Settings")]
        [SerializeField] private bool _canInteract = true;
        [SerializeField] private float _interactionRange = 3f;
        
        [Header("Outline Settings")]
        [SerializeField] private OutlineRenderer _outlineRenderer = new OutlineRenderer();
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion
        
        #region Private Fields
        private int _interactionCount = 0;
        private bool _isOutlined = false;
        private Renderer _renderer;
        #endregion
        
        #region Public Properties
        public bool CanInteract => _canInteract;
        public float InteractionRange => _interactionRange;
        public Transform Transform => transform;
        public int InteractionCount => _interactionCount;
        #endregion
        
        #region Unity Lifecycle
        protected virtual void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }
            
            if (_renderer != null)
            {
                _outlineRenderer.Initialize(_renderer);
            }
            else if (_showDebugInfo)
            {
                Debug.LogWarning($"[BaseInteractable] No Renderer found on {gameObject.name}. Outline effects will not work.");
            }
        }
        
        protected virtual void OnDestroy()
        {
            _outlineRenderer?.Cleanup();
        }
        #endregion
        
        #region IInteractable Implementation
        public virtual void OnInteractionAvailable(PlayerPrefabType controllerType)
        {
            if (!_isOutlined)
            {
                _outlineRenderer?.ShowOutline();
                _isOutlined = true;
                
                if (_showDebugInfo)
                    Debug.Log($"[BaseInteractable] Interaction available on {gameObject.name} for {controllerType}");
            }
        }
        
        public virtual void OnInteractionUnavailable(PlayerPrefabType controllerType)
        {
            if (_isOutlined)
            {
                _outlineRenderer?.HideOutline();
                _isOutlined = false;
                
                if (_showDebugInfo)
                    Debug.Log($"[BaseInteractable] Interaction unavailable on {gameObject.name} for {controllerType}");
            }
        }
        
        public virtual void OnInteract(PlayerPrefabType controllerType)
        {
            if (!_canInteract) return;
            
            _interactionCount++;
            
            if (_showDebugInfo)
                Debug.Log($"[BaseInteractable] Interacted with {gameObject.name} ({_interactionCount} times) via {controllerType}");
        }
        #endregion
        
        #region Debug
        private void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;
            
            // Draw interaction range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
            
            // Draw interaction state
            if (_isOutlined)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, Vector3.one * 0.2f);
            }
        }
        #endregion
    }
}
