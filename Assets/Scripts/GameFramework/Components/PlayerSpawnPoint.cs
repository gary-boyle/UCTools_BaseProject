using UnityEngine;

namespace GameFramework.Components
{
    /// <summary>
    /// Marks a GameObject as a player spawn point for new games.
    /// The InstantiationService will look for this component to determine where to spawn the player.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Spawn Point Configuration")]
        [Tooltip("Optional name for this spawn point (useful when multiple spawn points exist in different scenes)")]
        [SerializeField] private string _spawnPointName = "Default Spawn";
        
        [Tooltip("Whether this spawn point should be used for new games")]
        [SerializeField] private bool _isActive = true;
        
        [Header("Gizmo Settings")]
        [Tooltip("Color of the gizmo in the editor")]
        [SerializeField] private Color _gizmoColor = Color.green;
        
        [Tooltip("Size of the spawn point gizmo")]
        [SerializeField] private float _gizmoSize = 1.0f;
        
        [Tooltip("Whether to draw the player direction arrow")]
        [SerializeField] private bool _showDirectionArrow = true;
        
        [Tooltip("Length of the direction arrow")]
        [SerializeField] private float _arrowLength = 2.0f;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the name of this spawn point
        /// </summary>
        public string SpawnPointName => _spawnPointName;
        
        /// <summary>
        /// Gets whether this spawn point is active and should be used
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// Gets the spawn position (transform position)
        /// </summary>
        public Vector3 SpawnPosition => transform.position;
        
        /// <summary>
        /// Gets the spawn rotation (transform rotation)
        /// </summary>
        public Vector3 SpawnRotation => transform.rotation.eulerAngles;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Ensure the spawn point name is not empty
            if (string.IsNullOrEmpty(_spawnPointName))
            {
                _spawnPointName = $"Spawn Point ({gameObject.name})";
            }
        }

        private void Start()
        {
            // Log spawn point registration for debugging
            if (_isActive)
            {
                Debug.Log($"[PlayerSpawnPoint] Active spawn point registered: {_spawnPointName} at {transform.position}");
            }
        }
        #endregion

        #region Editor Gizmos
        /// <summary>
        /// Draws gizmos in the editor to visualize the spawn point
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_isActive) return;

            // Set gizmo color
            Gizmos.color = _gizmoColor;
            
            // Draw sphere at spawn position
            Gizmos.DrawSphere(transform.position, _gizmoSize * 0.5f);
            
            // Draw wireframe sphere for better visibility
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, _gizmoSize);
            
            if (_showDirectionArrow)
            {
                // Draw direction arrow
                Gizmos.color = _gizmoColor;
                Vector3 forward = transform.forward * _arrowLength;
                Vector3 arrowHead1 = transform.position + forward + (transform.right * -0.5f * _arrowLength * 0.3f) + (transform.up * -0.5f * _arrowLength * 0.3f);
                Vector3 arrowHead2 = transform.position + forward + (transform.right * 0.5f * _arrowLength * 0.3f) + (transform.up * -0.5f * _arrowLength * 0.3f);
                Vector3 arrowTip = transform.position + forward;
                
                // Draw arrow shaft
                Gizmos.DrawLine(transform.position, arrowTip);
                
                // Draw arrow head
                Gizmos.DrawLine(arrowTip, arrowHead1);
                Gizmos.DrawLine(arrowTip, arrowHead2);
            }
        }

        /// <summary>
        /// Draws gizmos when the GameObject is selected
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!_isActive) return;

            // Draw a more prominent gizmo when selected
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _gizmoSize * 1.5f);
            
            // Draw coordinate axes
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.right * _gizmoSize);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * _gizmoSize);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * _gizmoSize);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets whether this spawn point is active
        /// </summary>
        /// <param name="active">Whether the spawn point should be active</param>
        public void SetActive(bool active)
        {
            _isActive = active;
        }
        
        /// <summary>
        /// Sets the spawn point name
        /// </summary>
        /// <param name="name">New name for the spawn point</param>
        public void SetSpawnPointName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _spawnPointName = name;
            }
        }
        #endregion

        #region Validation
        /// <summary>
        /// Validates the spawn point configuration
        /// </summary>
        /// <returns>True if the spawn point is valid</returns>
        public bool IsValid()
        {
            return _isActive && !string.IsNullOrEmpty(_spawnPointName);
        }
        
        /// <summary>
        /// Gets validation error messages if any
        /// </summary>
        /// <returns>Error message or null if valid</returns>
        public string GetValidationError()
        {
            if (!_isActive) return "Spawn point is not active";
            if (string.IsNullOrEmpty(_spawnPointName)) return "Spawn point name is empty";
            return null;
        }
        #endregion
    }
}
