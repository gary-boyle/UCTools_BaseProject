using GameFramework.Components.Controllers.Animation;
using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Components.Controllers.Enum;
using GameFramework.Components.Interactables;

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
        
        [Header("Cursor Management")]
        [SerializeField] protected CursorLockRequirement _cursorLockRequirement = CursorLockRequirement.Never;
        
        [Header("Animation")]
        [SerializeField] protected PlayerAnimatorController _animatorController;
        #endregion

        #region Protected Fields
        protected IEventSystem _eventSystem;
        protected IInputManager _inputManager;
        protected IPauseService _pauseService;
        
        // Component instances
        protected IPlayerMovement _movementComponent;
        protected ICameraControl _cameraComponent;
        
        // Interaction system
        protected InteractionDetector _interactionDetector;
        
        // State
        protected bool _isInitialized = false;
        protected bool _isEnabled = true;
        #endregion

        
        #region Public Properties
        public IPlayerMovement MovementComponent => _movementComponent;
        public ICameraControl CameraComponent => _cameraComponent;
        public InteractionDetector InteractionDetector => _interactionDetector;
        public PlayerAnimatorController AnimatorController => _animatorController;
        public bool IsInitialized => _isInitialized;
        public bool IsEnabled => _isEnabled;
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public CursorLockRequirement CursorLockRequirement => _cursorLockRequirement;
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
            
            // Update animation
            _animatorController?.UpdateAnimations();
            
            // Update interaction detection
            _interactionDetector?.UpdateDetection();
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
                //SetInputContext();
            }
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
            //ResetInputContext();
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
            
            // Find and create components
            FindComponents();
            CreateComponents();
            
            // Initialize components
            _movementComponent?.Initialize();
            _cameraComponent?.Initialize();
            InitializeAnimation();
            
            // Initialize interaction system
            InitializeInteractionSystem();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Inform services about this controller's cursor requirements
            _eventSystem.Publish(new PlayerControllerActivatedEvent(_cursorLockRequirement, GetControllerType()));
            
            _isInitialized = true;
        }

        /// <summary>
        /// Cleanup the controller and its components
        /// </summary>
        public virtual void Cleanup()
        {
            if (!_isInitialized) return;
            
            UnsubscribeFromEvents();
            //ResetInputContext();
            
            // Cleanup components
            _movementComponent?.Cleanup();
            _cameraComponent?.Cleanup();
            
            // Clear references
            _movementComponent = null;
            _cameraComponent = null;
            _interactionDetector = null;
            _eventSystem = null;
            _inputManager = null;
            _pauseService = null;
            
            _isInitialized = false;
            
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
        /// Get the controller type for interaction system
        /// </summary>
        protected abstract PlayerPrefabType GetControllerType();
        #endregion
        
        #region Virtual Methods - Component Management
        /// <summary>
        /// Find components automatically - can be overridden for controller-specific finding
        /// </summary>
        protected virtual void FindComponents()
        {
            // Find animation controller
            if (_animatorController == null)
                _animatorController = GetComponentInChildren<PlayerAnimatorController>();
        }
        
        /// <summary>
        /// Create and assign components - override in derived classes
        /// </summary>
        protected virtual void CreateComponents()
        {
            // Base implementation - derived classes should override this
            // to assign their specific movement and camera components
        }
        
        /// <summary>
        /// Initialize animation system
        /// </summary>
        protected virtual void InitializeAnimation()
        {
            if (_animatorController != null && _movementComponent != null)
            {
                Animator animator = GetComponentInChildren<Animator>();
                _animatorController.Initialize(GetControllerType(), _movementComponent, animator);
            }
        }
        #endregion
        
        #region Animation Control
        /// <summary>
        /// Trigger attack animation
        /// </summary>
        public virtual void TriggerAttack()
        {
            _animatorController?.TriggerAttack();
        }
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
        /// Initialize the interaction system for this controller
        /// </summary>
        protected virtual void InitializeInteractionSystem()
        {
            // Get main camera for interaction detection
            var gameDataService = GameManager.GetService<IGameDataService>();
            UnityEngine.Camera mainCamera = gameDataService?.GetMainCamera();
            
            if (mainCamera == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Main camera not found. Interaction system may not work properly.");
            }
            
            // Create interaction detector
            _interactionDetector = new InteractionDetector(
                transform,
                mainCamera,
                GetControllerType(),
                _interactionLayerMask,
                _eventSystem
            );
        }
        
        #endregion

        #region Input Event Handlers
        protected virtual void OnPlayerMoveInput(PlayerMoveInputEvent inputEvent)
        {
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
            if (!_isInitialized || !_isEnabled) return;
            
            // Trigger interaction on key press
            if (inputEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                _interactionDetector?.TriggerInteraction();
            }
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
            
            // Draw interaction gizmos
            _interactionDetector?.DrawDebugGizmos();
        }
        #endregion
    }
}
