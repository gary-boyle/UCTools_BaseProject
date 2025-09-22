using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.Input;
using GameFramework.Input.Interfaces;

namespace GameFramework.Components.Controllers
{
    /// <summary>
    /// Base class for all player controllers that combines movement and camera components.
    /// Uses composition over inheritance for maximum flexibility.
    /// </summary>
    public abstract class BasePlayerController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Base Controller Settings")]
        [SerializeField] protected bool _initializeOnStart = true;
        [SerializeField] protected bool _showDebugInfo = false;
        
        [SerializeField] protected LayerMask _interactionLayerMask = -1;

        
        [Header("Input Context")]
        [SerializeField] protected InputContext _requiredInputContext = InputContext.Player;
        #endregion

        #region Protected Fields
        protected IEventSystem _eventSystem;
        protected IInputManager _inputManager;
        protected IPauseService _pauseService;
        
        // Component instances
        protected IPlayerMovement _movementComponent;
        protected ICameraControl _cameraComponent;
        
        // State
        protected bool _isInitialized = false;
        protected bool _isEnabled = true;
        #endregion

        
        #region Public Properties
        public IPlayerMovement MovementComponent => _movementComponent;
        public ICameraControl CameraComponent => _cameraComponent;
        public bool IsInitialized => _isInitialized;
        public bool IsEnabled => _isEnabled;
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            // Get services early
            _eventSystem = GameManager.GetService<IEventSystem>();
            _inputManager = GameManager.GetService<IInputManager>();
            _pauseService = GameManager.GetService<IPauseService>();
            
            if (_eventSystem == null)
            {
                Debug.LogError($"[{GetType().Name}] EventSystem not available. Controller cannot function.");
                enabled = false;
                return;
            }
        }

        protected virtual void Start()
        {
            if (_initializeOnStart)
            {
                Initialize();
            }
        }

        protected virtual void Update()
        {
            if (!_isInitialized || !_isEnabled || IsPaused) return;
            
            // Update components
            _movementComponent?.UpdateMovement();
            _cameraComponent?.UpdateCamera();
        }

        protected virtual void FixedUpdate()
        {
            if (!_isInitialized || !_isEnabled || IsPaused) return;
            
            // Fixed update for movement (physics)
            _movementComponent?.FixedUpdateMovement();
        }

        protected virtual void OnEnable()
        {
            if (_isInitialized)
            {
                SubscribeToEvents();
                SetInputContext();
            }
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
            ResetInputContext();
        }

        protected virtual void OnDestroy()
        {
            Cleanup();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the controller and its components
        /// </summary>
        public virtual void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"[{GetType().Name}] Already initialized.");
                return;
            }
            
            // Create components
            CreateComponents();
            
            // Initialize components
            _movementComponent?.Initialize();
            _cameraComponent?.Initialize();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Set input context
            SetInputContext();
            
            _isInitialized = true;
            
            if (_showDebugInfo)
                Debug.Log($"[{GetType().Name}] Initialized successfully on {gameObject.name}");
        }

        /// <summary>
        /// Cleanup the controller and its components
        /// </summary>
        public virtual void Cleanup()
        {
            if (!_isInitialized) return;
            
            UnsubscribeFromEvents();
            ResetInputContext();
            
            // Cleanup components
            _movementComponent?.Cleanup();
            _cameraComponent?.Cleanup();
            
            // Clear references
            _movementComponent = null;
            _cameraComponent = null;
            _eventSystem = null;
            _inputManager = null;
            _pauseService = null;
            
            _isInitialized = false;
            
            if (_showDebugInfo)
                Debug.Log($"[{GetType().Name}] Cleaned up on {gameObject.name}");
        }

        /// <summary>
        /// Enable or disable the controller
        /// </summary>
        public virtual void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            
            if (enabled)
            {
                SetInputContext();
            }
            else
            {
                ResetInputContext();
                StopAllMovement();
            }
        }

        /// <summary>
        /// Stop all movement and camera input
        /// </summary>
        public virtual void StopAllMovement()
        {
            _movementComponent?.StopMovement();
            _cameraComponent?.SetInputEnabled(false);
        }

        /// <summary>
        /// Resume all movement and camera input
        /// </summary>
        public virtual void ResumeAllMovement()
        {
            _cameraComponent?.SetInputEnabled(true);
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Create the movement and camera components for this controller type
        /// </summary>
        protected abstract void CreateComponents();
        #endregion

        #region Virtual Methods
        /// <summary>
        /// Subscribe to input and game events
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            if (_eventSystem == null) return;
            
            // Subscribe to input events
            _eventSystem.Subscribe<PlayerMoveInputEvent>(OnPlayerMoveInput);
            _eventSystem.Subscribe<PlayerLookInputEvent>(OnPlayerLookInput);
            _eventSystem.Subscribe<PlayerJumpInputEvent>(OnPlayerJumpInput);
            _eventSystem.Subscribe<PlayerSprintInputEvent>(OnPlayerSprintInput);
            _eventSystem.Subscribe<PlayerCrouchInputEvent>(OnPlayerCrouchInput);
            _eventSystem.Subscribe<PlayerAttackInputEvent>(OnPlayerAttackInput);
            _eventSystem.Subscribe<PlayerInteractInputEvent>(OnPlayerInteractInput);
            
            // Subscribe to game events
            _eventSystem.Subscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Subscribe<GameResumedEvent>(OnGameResumed);
        }

        /// <summary>
        /// Unsubscribe from input and game events
        /// </summary>
        protected virtual void UnsubscribeFromEvents()
        {
            if (_eventSystem == null) return;
            
            // Unsubscribe from input events
            _eventSystem.Unsubscribe<PlayerMoveInputEvent>(OnPlayerMoveInput);
            _eventSystem.Unsubscribe<PlayerLookInputEvent>(OnPlayerLookInput);
            _eventSystem.Unsubscribe<PlayerJumpInputEvent>(OnPlayerJumpInput);
            _eventSystem.Unsubscribe<PlayerSprintInputEvent>(OnPlayerSprintInput);
            _eventSystem.Unsubscribe<PlayerCrouchInputEvent>(OnPlayerCrouchInput);
            _eventSystem.Unsubscribe<PlayerAttackInputEvent>(OnPlayerAttackInput);
            _eventSystem.Unsubscribe<PlayerInteractInputEvent>(OnPlayerInteractInput);
            
            // Unsubscribe from game events
            _eventSystem.Unsubscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Unsubscribe<GameResumedEvent>(OnGameResumed);
        }

        /// <summary>
        /// Set the required input context for this controller
        /// </summary>
        protected virtual void SetInputContext()
        {
            if (_inputManager != null && _isEnabled)
            {
                _inputManager.SetInputContext(_requiredInputContext);
            }
        }

        /// <summary>
        /// Reset input context to None
        /// </summary>
        protected virtual void ResetInputContext()
        {
            if (_inputManager != null)
            {
                _inputManager.SetInputContext(InputContext.None);
            }
        }
        #endregion

        #region Input Event Handlers
        protected virtual void OnPlayerMoveInput(PlayerMoveInputEvent inputEvent)
        {
            Debug.Log("OnPlayerMoveInput recieved" + _isInitialized + " " + _isEnabled);
            if (!_isInitialized || !_isEnabled) return;
            _movementComponent?.HandleMoveInput(inputEvent);
        }

        protected virtual void OnPlayerLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized || !_isEnabled) return;
            _cameraComponent?.HandleLookInput(inputEvent);
        }

        protected virtual void OnPlayerJumpInput(PlayerJumpInputEvent inputEvent)
        {
            if (!_isInitialized || !_isEnabled) return;
            _movementComponent?.HandleJumpInput(inputEvent);
        }

        protected virtual void OnPlayerSprintInput(PlayerSprintInputEvent inputEvent)
        {
            if (!_isInitialized || !_isEnabled) return;
            _movementComponent?.HandleSprintInput(inputEvent);
        }

        protected virtual void OnPlayerCrouchInput(PlayerCrouchInputEvent inputEvent)
        {
            if (!_isInitialized || !_isEnabled) return;
            _movementComponent?.HandleCrouchInput(inputEvent);
        }

        protected virtual void OnPlayerAttackInput(PlayerAttackInputEvent inputEvent)
        {
            // Override in derived classes if attack functionality is needed
        }

        protected virtual void OnPlayerInteractInput(PlayerInteractInputEvent inputEvent)
        {
            // Override in derived classes if interaction functionality is needed
        }

        protected virtual void OnGamePaused(GamePausedEvent pausedEvent)
        {
            StopAllMovement();
        }

        protected virtual void OnGameResumed(GameResumedEvent resumedEvent)
        {
            ResumeAllMovement();
        }
        #endregion

        #region Debug
        protected virtual void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;
            
            // Draw controller info
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, GetType().Name);
#endif

            // Draw movement gizmos
            if (_movementComponent is MonoBehaviour movementMB)
            {
                var drawGizmosMethod = movementMB.GetType().GetMethod("OnDrawGizmos", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                drawGizmosMethod?.Invoke(movementMB, null);
            }
        }
        #endregion
    }
}
