// using UnityEngine;
// using GameFramework.Core;
// using GameFramework.EventSystem.Interfaces;
// using GameFramework.EventSystem.Events;
// using GameFramework.Services.Interfaces;
// using GameFramework.GameData.Events;
// using GameFramework.Config.ScriptableObjects;
//
// namespace GameFramework.Components
// {
//     /// <summary>
//     /// Dedicated camera controller that handles mouse look and camera rotation.
//     /// Integrates with the game's event-driven input system.
//     /// Separates camera concerns from player movement for better modularity.
//     /// </summary>
//     public class CameraController : MonoBehaviour
//     {
//         #region Serialized Fields
//         [Header("Camera Settings")]
//         [SerializeField] private Transform _cameraTransform;
//         [SerializeField] private Transform _playerTransform;
//         
//         [Header("Look Settings")]
//         [SerializeField] private float _verticalLookRange = 80f;
//         [SerializeField] private float _mouseSensitivityMultiplier = 1.0f;
//         [SerializeField] private bool _lockCursor = true;
//         
//         [Header("Debug")]
//         [SerializeField] private bool _showDebugInfo = false;
//         #endregion
//
//         #region Private Fields
//         private IEventSystem _eventSystem;
//         private IGameDataService _gameDataService;
//         private IPauseService _pauseService;
//         private InputSettings_SO _inputSettings;
//         
//         // Cursor state management
//         private bool _wasLockedBeforePause = true;
//         
//         // Look state
//         private Vector2 _lookInput = Vector2.zero;
//         private float _verticalRotation = 0f;
//         
//         // Component state
//         private bool _isInitialized = false;
//         #endregion
//
//         #region Public Properties
//         public Vector2 LookInput => _lookInput;
//         public float VerticalRotation => _verticalRotation;
//         public Transform CameraTransform => _cameraTransform;
//         public bool IsInitialized => _isInitialized;
//         
//         /// <summary>
//         /// Local mouse sensitivity multiplier (applied on top of global settings)
//         /// </summary>
//         public float MouseSensitivityMultiplier
//         {
//             get => _mouseSensitivityMultiplier;
//             set => _mouseSensitivityMultiplier = Mathf.Clamp(value, 0.1f, 5.0f);
//         }
//         
//         /// <summary>
//         /// Get effective mouse sensitivity (global * local multiplier)
//         /// </summary>
//         public float EffectiveMouseSensitivity
//         {
//             get
//             {
//                 float globalSensitivity = _inputSettings?.GetMouseSensitivity() ?? 1.0f;
//                 return globalSensitivity * _mouseSensitivityMultiplier;
//             }
//         }
//         #endregion
//
//         #region Unity Lifecycle
//         private void Awake()
//         {
//             // Set up player transform if not assigned
//             if (_playerTransform == null)
//             {
//                 _playerTransform = transform;
//             }
//         }
//
//         private void Start()
//         {
//             InitializeController();
//         }
//
//         private void Update()
//         {
//             if (!_isInitialized) return;
//             
//             ProcessLook();
//         }
//
//         private void OnDestroy()
//         {
//             CleanupController();
//         }
//         #endregion
//
//         #region Initialization
//         /// <summary>
//         /// Initialize the CameraController and subscribe to input events
//         /// </summary>
//         private void InitializeController()
//         {
//             // Get EventSystem from GameContext
//             _eventSystem = GameManager.GetService<IEventSystem>();
//             if (_eventSystem == null)
//             {
//                 Debug.LogError($"[CameraController] EventSystem not available.");
//                 return;
//             }
//             
//             // Get GameDataService from GameContext
//             _gameDataService = GameManager.GetService<IGameDataService>();
//             if (_gameDataService == null)
//             {
//                 Debug.LogError($"[CameraController] GameDataService not available.");
//                 return;
//             }
//             
//             // Get InputSettings from SettingsRegistry
//             _inputSettings = SettingsRegistry.Get<InputSettings_SO>();
//             if (_inputSettings == null)
//             {
//                 Debug.LogError($"[CameraController] InputSettings not available.");
//                 return;
//             }
//             
//             // Get PauseService from GameContext
//             _pauseService = GameManager.GetService<IPauseService>();
//             if (_pauseService == null)
//             {
//                 Debug.LogError($"[CameraController] PauseService not available.");
//                 return;
//             }
//
//             // Subscribe to input and camera change events
//             _eventSystem.Subscribe<PlayerLookInputEvent>(OnPlayerLookInput);
//             _eventSystem.Subscribe<MainCameraChangedEvent>(OnMainCameraChanged);
//             _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
//             
//             // Subscribe to pause/resume events for cursor management
//             _eventSystem.Subscribe<GamePausedEvent>(OnGamePaused);
//             _eventSystem.Subscribe<GameResumedEvent>(OnGameResumed);
//             
//             // Get camera from GameDataService (will auto-detect if needed)
//             if (_cameraTransform == null)
//             {
//                 Camera mainCamera = _gameDataService.GetMainCamera();
//                 if (mainCamera != null)
//                 {
//                     _cameraTransform = mainCamera.transform;
//                     Debug.Log($"[CameraController] Using camera from GameDataService: {mainCamera.name}");
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"[CameraController] No main camera available from GameDataService");
//                 }
//             }
//             
//             // Lock cursor if requested
//             if (_lockCursor)
//             {
//                 Cursor.lockState = CursorLockMode.Locked;
//                 Cursor.visible = false;
//             }
//             
//             _isInitialized = true;
//             
//             if (_showDebugInfo)
//                 Debug.Log($"[CameraController] Initialized successfully on {gameObject.name}");
//         }
//
//         /// <summary>
//         /// Cleanup subscriptions when the controller is destroyed
//         /// </summary>
//         private void CleanupController()
//         {
//             if (_eventSystem != null)
//             {
//                 _eventSystem.Unsubscribe<PlayerLookInputEvent>(OnPlayerLookInput);
//                 _eventSystem.Unsubscribe<MainCameraChangedEvent>(OnMainCameraChanged);
//                 _eventSystem.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
//                 
//                 // Unsubscribe from pause/resume events
//                 _eventSystem.Unsubscribe<GamePausedEvent>(OnGamePaused);
//                 _eventSystem.Unsubscribe<GameResumedEvent>(OnGameResumed);
//             }
//             
//             // Clear references
//             _inputSettings = null;
//             _pauseService = null;
//             
//             if (_showDebugInfo)
//                 Debug.Log($"[CameraController] Cleaned up on {gameObject.name}");
//         }
//         #endregion
//
//         #region Input Event Handling
//         /// <summary>
//         /// Handle player look input events for camera/mouse look
//         /// Note: Mouse sensitivity is already applied by InputManager
//         /// </summary>
//         private void OnPlayerLookInput(PlayerLookInputEvent inputEvent)
//         {
//             if (!_isInitialized) return;
//             if (_pauseService != null && _pauseService.IsPaused) return;
//
//             _lookInput = inputEvent.LookDelta;
//
//             if (_showDebugInfo)
//             {
//                 Debug.Log($"[CameraController] Look Input: {_lookInput}, Phase: {inputEvent.Phase}");
//             }
//         }
//         
//         /// <summary>
//         /// Handle main camera changed events from GameDataService
//         /// </summary>
//         private void OnMainCameraChanged(MainCameraChangedEvent cameraEvent)
//         {
//             if (!_isInitialized) return;
//             
//             // Update camera reference if we don't have one assigned manually
//             if (_cameraTransform == null || _cameraTransform == cameraEvent.PreviousCamera?.transform)
//             {
//                 _cameraTransform = cameraEvent.NewCamera?.transform;
//                 
//                 // Reset vertical rotation for new camera
//                 if (_cameraTransform != null)
//                 {
//                     _verticalRotation = 0f;
//                     _cameraTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
//                 }
//                 
//                 if (_showDebugInfo)
//                 {
//                     Debug.Log($"[CameraController] Camera updated to: {cameraEvent.NewCamera?.name ?? "null"}");
//                 }
//             }
//         }
//         
//         /// <summary>
//         /// Handle options changed events to update input settings
//         /// </summary>
//         private void OnOptionsChanged(OptionsChangedEvent optionsEvent)
//         {
//             if (!_isInitialized) return;
//             
//             // Input settings are automatically updated in the ScriptableObject
//             // No need to re-fetch, just log for debugging
//             if (_showDebugInfo)
//             {
//                 Debug.Log($"[CameraController] Options changed - Global sensitivity: {_inputSettings?.GetMouseSensitivity() ?? 1.0f}, Local multiplier: {_mouseSensitivityMultiplier}, Effective: {EffectiveMouseSensitivity}");
//             }
//         }
//         
//         /// <summary>
//         /// Handle game paused events - unlock cursor for menu navigation
//         /// </summary>
//         private void OnGamePaused(GamePausedEvent pausedEvent)
//         {
//             if (!_isInitialized) return;
//             
//             // Clear any pending look input to prevent rotation during pause
//             _lookInput = Vector2.zero;
//             
//             // Store current cursor lock state before unlocking
//             _wasLockedBeforePause = Cursor.lockState == CursorLockMode.Locked;
//             
//             // Unlock cursor for menu navigation
//             Cursor.lockState = CursorLockMode.None;
//             Cursor.visible = true;
//             
//             if (_showDebugInfo)
//             {
//                 Debug.Log($"[CameraController] Game paused - cursor unlocked (was locked: {_wasLockedBeforePause})");
//             }
//         }
//         
//         /// <summary>
//         /// Handle game resumed events - restore cursor lock state
//         /// </summary>
//         private void OnGameResumed(GameResumedEvent resumedEvent)
//         {
//             if (!_isInitialized) return;
//             
//             // Restore cursor lock state if it was locked before pause
//             if (_wasLockedBeforePause && _lockCursor)
//             {
//                 Cursor.lockState = CursorLockMode.Locked;
//                 Cursor.visible = false;
//             }
//             
//             if (_showDebugInfo)
//             {
//                 Debug.Log($"[CameraController] Game resumed - cursor lock restored (locked: {_wasLockedBeforePause && _lockCursor})");
//             }
//         }
//         #endregion
//
//         #region Camera Processing
//         /// <summary>
//         /// Process look input for camera rotation
//         /// Applies local mouse sensitivity multiplier on top of InputManager's processed input
//         /// </summary>
//         private void ProcessLook()
//         {
//             if (_cameraTransform == null || _playerTransform == null) return;
//             if (_pauseService != null && _pauseService.IsPaused) return;
//             
//             // Only process if there's actual input
//             if (_lookInput.magnitude < 0.01f)
//             {
//                 return;
//             }
//
//             // Apply local mouse sensitivity multiplier (InputManager already applied global settings)
//             Vector2 processedLookInput = _lookInput * _mouseSensitivityMultiplier;
//             
//             // Apply look input with time delta
//             Vector2 lookDelta = processedLookInput * Time.deltaTime;
//
//             // Rotate player horizontally
//             _playerTransform.Rotate(Vector3.up, lookDelta.x, Space.World);
//
//             // Rotate camera vertically
//             _verticalRotation -= lookDelta.y;
//             _verticalRotation = Mathf.Clamp(_verticalRotation, -_verticalLookRange, _verticalLookRange);
//             _cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
//             
//             // IMPORTANT: Reset input after processing to prevent continuous rotation
//             _lookInput = Vector2.zero;
//             
//             if (_showDebugInfo)
//             {
//                 Debug.Log($"[CameraController] Applied rotation - Processed Input: {processedLookInput}, Final Delta: {lookDelta}, Vertical Rotation: {_verticalRotation}");
//             }
//         }
//         #endregion
//
//         #region Public Methods
//         /// <summary>
//         /// Manually set look input (useful for testing or external control)
//         /// </summary>
//         public void SetLookInput(Vector2 lookInput)
//         {
//             _lookInput = lookInput;
//         }
//
//         /// <summary>
//         /// Reset vertical rotation to center
//         /// </summary>
//         public void ResetVerticalRotation()
//         {
//             _verticalRotation = 0f;
//             if (_cameraTransform != null)
//             {
//                 _cameraTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
//             }
//         }
//
//         /// <summary>
//         /// Set the camera transform reference
//         /// Optionally updates GameDataService if the camera is different
//         /// </summary>
//         public void SetCameraTransform(Transform cameraTransform, bool updateGameDataService = false)
//         {
//             _cameraTransform = cameraTransform;
//             
//             // Optionally update GameDataService with new camera
//             if (updateGameDataService && _gameDataService != null && _cameraTransform != null)
//             {
//                 Camera camera = _cameraTransform.GetComponent<Camera>();
//                 if (camera != null)
//                 {
//                     _gameDataService.SetMainCamera(camera);
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Set the player transform reference
//         /// </summary>
//         public void SetPlayerTransform(Transform playerTransform)
//         {
//             _playerTransform = playerTransform;
//         }
//
//         /// <summary>
//         /// Toggle cursor lock state
//         /// </summary>
//         public void ToggleCursorLock()
//         {
//             _lockCursor = !_lockCursor;
//             if (_lockCursor)
//             {
//                 Cursor.lockState = CursorLockMode.Locked;
//                 Cursor.visible = false;
//             }
//             else
//             {
//                 Cursor.lockState = CursorLockMode.None;
//                 Cursor.visible = true;
//             }
//         }
//
//         /// <summary>
//         /// Set cursor lock state
//         /// </summary>
//         public void SetCursorLock(bool lockCursor)
//         {
//             _lockCursor = lockCursor;
//             if (_lockCursor)
//             {
//                 Cursor.lockState = CursorLockMode.Locked;
//                 Cursor.visible = false;
//             }
//             else
//             {
//                 Cursor.lockState = CursorLockMode.None;
//                 Cursor.visible = true;
//             }
//         }
//         #endregion
//
//         #region Debug
//         private void OnDrawGizmos()
//         {
//             if (!_showDebugInfo) return;
//
//             // Draw look direction if camera exists
//             if (_cameraTransform != null)
//             {
//                 Gizmos.color = Color.magenta;
//                 Gizmos.DrawRay(_cameraTransform.position, _cameraTransform.forward * 3f);
//                 
//                 // Draw camera frustum outline
//                 Gizmos.color = Color.cyan;
//                 Gizmos.matrix = Matrix4x4.TRS(_cameraTransform.position, _cameraTransform.rotation, Vector3.one);
//                 Gizmos.DrawFrustum(Vector3.zero, 60f, 5f, 0.1f, 1.0f);
//                 Gizmos.matrix = Matrix4x4.identity;
//             }
//             
//             // Show current look input as arrow
//             if (_lookInput.magnitude > 0.01f)
//             {
//                 Vector3 inputVisualization = new Vector3(_lookInput.x, _lookInput.y, 0) * 0.1f;
//                 Gizmos.color = Color.yellow;
//                 Gizmos.DrawRay(transform.position + Vector3.up * 2f, inputVisualization);
//             }
//         }
//         #endregion
//     }
// }
