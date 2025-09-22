// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
//
// namespace GameFramework.Components.Controllers
// {
//     /// <summary>
//     /// Manager for handling multiple pre-configured controller prefabs and runtime switching between controller types.
//     /// Works with the new MonoBehaviour-based controller system where each controller type is a prefab variant.
//     /// Useful for games that need to switch between different camera/movement modes at runtime.
//     /// </summary>
//     public class ControllerManager : MonoBehaviour
//     {
//         #region Serialized Fields
//         [Header("Controller Management")]
//         [SerializeField] private ControllerType _defaultControllerType = ControllerType.FirstPerson;
//         [SerializeField] private bool _initializeOnStart = true;
//         [SerializeField] private bool _allowRuntimeSwitching = true;
//
//         [Header("Prefab Variants - Assign your pre-configured controller prefabs")]
//         [SerializeField] private GameObject _firstPersonPrefab;
//         [SerializeField] private GameObject _thirdPersonPrefab;
//         [SerializeField] private GameObject _rtsPrefab;
//         [SerializeField] private GameObject _isometricPrefab;
//
//         [Header("Runtime Switching")]
//         [SerializeField] private KeyCode _switchKey = KeyCode.Tab;
//         [SerializeField] private bool _cycleControllers = false;
//         [SerializeField] private ControllerType[] _availableTypes = { ControllerType.FirstPerson, ControllerType.ThirdPerson };
//
//         [Header("Debug")]
//         [SerializeField] private bool _showDebugInfo = false;
//         #endregion
//
//         #region Private Fields
//         private BasePlayerController _currentController;
//         private ControllerType _currentControllerType;
//         private int _currentTypeIndex = 0;
//         #endregion
//
//         #region Events
//         public System.Action<ControllerType, ControllerType> OnControllerSwitched;
//         public System.Action<BasePlayerController> OnControllerChanged;
//         #endregion
//
//         #region Properties
//         public BasePlayerController CurrentController => _currentController;
//         public ControllerType CurrentControllerType => _currentControllerType;
//         public bool AllowRuntimeSwitching 
//         { 
//             get => _allowRuntimeSwitching; 
//             set => _allowRuntimeSwitching = value; 
//         }
//         #endregion
//
//         #region Unity Lifecycle
//         private void Awake()
//         {
//             InitializeConfigurations();
//         }
//
//         private void Start()
//         {
//             if (_initializeOnStart)
//             {
//                 InitializeDefaultController();
//             }
//         }
//
//         private void Update()
//         {
//             // Handle manual controller switching for debugging
//             if (_allowRuntimeSwitching && UnityEngine.Input.GetKeyDown(_switchKey))
//             {
//                 if (_cycleControllers)
//                 {
//                     CycleToNextController();
//                 }
//             }
//         }
//         #endregion
//
//         #region Initialization
//         private void InitializeConfigurations()
//         {
//             // Validate that required prefabs are assigned
//             ValidatePrefabAssignments();
//         }
//         
//         private void ValidatePrefabAssignments()
//         {
//             if (_availableTypes != null)
//             {
//                 foreach (var type in _availableTypes)
//                 {
//                     GameObject prefab = GetPrefabForControllerType(type);
//                     if (prefab == null)
//                     {
//                         Debug.LogWarning($"[ControllerManager] No prefab assigned for {type} controller type, but it's listed in available types.");
//                     }
//                     else
//                     {
//                         // Validate the prefab has the correct controller component
//                         BasePlayerController controller = prefab.GetComponent<BasePlayerController>();
//                         if (controller == null)
//                         {
//                             Debug.LogError($"[ControllerManager] Prefab {prefab.name} for {type} does not have a BasePlayerController component!");
//                         }
//                     }
//                 }
//             }
//         }
//
//         private void InitializeDefaultController()
//         {
//             SwitchToController(_defaultControllerType);
//         }
//         #endregion
//
//         #region Controller Switching
//         /// <summary>
//         /// Switch to a specific controller type using pre-configured prefab variants
//         /// </summary>
//         public bool SwitchToController(ControllerType controllerType)
//         {
//             if (_currentController != null && _currentControllerType == controllerType)
//             {
//                 if (_showDebugInfo)
//                     Debug.Log($"[ControllerManager] Already using {controllerType} controller.");
//                 return true;
//             }
//
//             // Get the prefab for the controller type
//             GameObject prefabToUse = GetPrefabForControllerType(controllerType);
//             if (prefabToUse != null)
//             {
//                 return SwitchToPrefabVariant(controllerType, prefabToUse);
//             }
//             else
//             {
//                 Debug.LogError($"[ControllerManager] No prefab assigned for {controllerType} controller type. Please assign the prefab in the ControllerManager inspector.");
//                 return false;
//             }
//         }
//
//         /// <summary>
//         /// Switch to the next available controller type
//         /// </summary>
//         public bool CycleToNextController()
//         {
//             if (_availableTypes == null || _availableTypes.Length == 0)
//             {
//                 Debug.LogWarning("[ControllerManager] No available controller types for cycling.");
//                 return false;
//             }
//
//             _currentTypeIndex = (_currentTypeIndex + 1) % _availableTypes.Length;
//             ControllerType nextType = _availableTypes[_currentTypeIndex];
//             
//             return SwitchToController(nextType);
//         }
//
//
//         private bool SwitchToPrefabVariant(ControllerType controllerType, GameObject prefab)
//         {
//             if (prefab == null)
//             {
//                 Debug.LogError($"[ControllerManager] Prefab for {controllerType} is null.");
//                 return false;
//             }
//
//             ControllerType previousType = _currentControllerType;
//             
//             // Store current position and rotation
//             Vector3 position = transform.position;
//             Quaternion rotation = transform.rotation;
//             
//             // Instantiate new prefab
//             GameObject newInstance = Instantiate(prefab, position, rotation);
//             
//             // Copy name and any important references
//             newInstance.name = gameObject.name;
//             
//             // Get the controller component
//             var newController = newInstance.GetComponent<BasePlayerController>();
//             if (newController == null)
//             {
//                 Debug.LogError($"[ControllerManager] Prefab {prefab.name} does not have a BasePlayerController component.");
//                 Destroy(newInstance);
//                 return false;
//             }
//
//             // Initialize the new controller
//             newController.Initialize();
//             
//             // Update references
//             _currentController = newController;
//             _currentControllerType = controllerType;
//             
//             // Update current type index for cycling
//             UpdateCurrentTypeIndex();
//             
//             // Destroy the current object and transfer control
//             var controllerManager = newInstance.GetComponent<ControllerManager>();
//             if (controllerManager != null)
//             {
//                 // Transfer settings to the new manager
//                 controllerManager.CopySettingsFrom(this);
//             }
//             
//             // Raise events before destroying
//             OnControllerSwitched?.Invoke(previousType, controllerType);
//             OnControllerChanged?.Invoke(newController);
//             
//             if (_showDebugInfo)
//                 Debug.Log($"[ControllerManager] Switched to prefab variant: {controllerType}");
//             
//             // Destroy current object
//             Destroy(gameObject);
//             
//             return true;
//         }
//
//         private void UpdateCurrentTypeIndex()
//         {
//             if (_availableTypes != null && _availableTypes.Length > 0)
//             {
//                 for (int i = 0; i < _availableTypes.Length; i++)
//                 {
//                     if (_availableTypes[i] == _currentControllerType)
//                     {
//                         _currentTypeIndex = i;
//                         break;
//                     }
//                 }
//             }
//         }
//
//         private GameObject GetPrefabForControllerType(ControllerType controllerType)
//         {
//             switch (controllerType)
//             {
//                 case ControllerType.FirstPerson:
//                     return _firstPersonPrefab;
//                 case ControllerType.ThirdPerson:
//                     return _thirdPersonPrefab;
//                 case ControllerType.RTS:
//                     return _rtsPrefab;
//                 case ControllerType.Isometric:
//                     return _isometricPrefab;
//                 default:
//                     return null;
//             }
//         }
//         #endregion
//
//         #region Settings Management
//         /// <summary>
//         /// Copy settings from another ControllerManager
//         /// </summary>
//         public void CopySettingsFrom(ControllerManager other)
//         {
//             if (other == null) return;
//
//             _allowRuntimeSwitching = other._allowRuntimeSwitching;
//             _switchKey = other._switchKey;
//             _cycleControllers = other._cycleControllers;
//             _availableTypes = other._availableTypes?.ToArray();
//             _currentTypeIndex = other._currentTypeIndex;
//             _showDebugInfo = other._showDebugInfo;
//             
//             // Copy prefab references
//             _firstPersonPrefab = other._firstPersonPrefab;
//             _thirdPersonPrefab = other._thirdPersonPrefab;
//             _rtsPrefab = other._rtsPrefab;
//             _isometricPrefab = other._isometricPrefab;
//         }
//         #endregion
//
//         #region UI Methods (for external UI integration)
//         /// <summary>
//         /// Show controller switch UI (override in derived classes or use events)
//         /// </summary>
//         protected virtual void ShowControllerSwitchUI()
//         {
//             if (_showDebugInfo)
//             {
//                 Debug.Log("[ControllerManager] Switch UI requested. Available types: " + 
//                          string.Join(", ", _availableTypes.Select(t => t.ToString())));
//             }
//         }
//
//         /// <summary>
//         /// Get available controller types for UI
//         /// </summary>
//         public ControllerType[] GetAvailableControllerTypes()
//         {
//             return _availableTypes?.ToArray() ?? new ControllerType[0];
//         }
//
//         /// <summary>
//         /// Check if a controller type is available and has a prefab assigned
//         /// </summary>
//         public bool IsControllerTypeAvailable(ControllerType controllerType)
//         {
//             bool isInAvailableTypes = _availableTypes?.Contains(controllerType) ?? false;
//             bool hasPrefab = GetPrefabForControllerType(controllerType) != null;
//             return isInAvailableTypes && hasPrefab;
//         }
//
//         /// <summary>
//         /// Get all controller types that have prefabs assigned
//         /// </summary>
//         public ControllerType[] GetControllerTypesWithPrefabs()
//         {
//             var typesWithPrefabs = new List<ControllerType>();
//             
//             foreach (ControllerType type in System.Enum.GetValues(typeof(ControllerType)))
//             {
//                 if (GetPrefabForControllerType(type) != null)
//                 {
//                     typesWithPrefabs.Add(type);
//                 }
//             }
//             
//             return typesWithPrefabs.ToArray();
//         }
//         #endregion
//
//         #region Prefab Management
//         /// <summary>
//         /// Set prefab variant for a controller type
//         /// </summary>
//         public void SetPrefabVariant(ControllerType controllerType, GameObject prefab)
//         {
//             switch (controllerType)
//             {
//                 case ControllerType.FirstPerson:
//                     _firstPersonPrefab = prefab;
//                     break;
//                 case ControllerType.ThirdPerson:
//                     _thirdPersonPrefab = prefab;
//                     break;
//                 case ControllerType.RTS:
//                     _rtsPrefab = prefab;
//                     break;
//                 case ControllerType.Isometric:
//                     _isometricPrefab = prefab;
//                     break;
//             }
//             
//             if (_showDebugInfo)
//                 Debug.Log($"[ControllerManager] Prefab variant set for {controllerType}: {prefab?.name ?? "null"}");
//         }
//
//         /// <summary>
//         /// Get prefab variant for a controller type
//         /// </summary>
//         public GameObject GetPrefabVariant(ControllerType controllerType)
//         {
//             return GetPrefabForControllerType(controllerType);
//         }
//         #endregion
//
//         #region Debug
//         private void OnGUI()
//         {
//             if (!_showDebugInfo) return;
//
//             GUILayout.BeginArea(new Rect(10, 10, 350, 250));
//             
//             // Current status
//             GUILayout.Label($"Current Controller: {_currentControllerType}");
//             GUILayout.Label($"Runtime Switching: {(_allowRuntimeSwitching ? "Enabled" : "Disabled")}");
//             GUILayout.Label($"Switch Key: {_switchKey}");
//             GUILayout.Space(10);
//             
//             // Cycle button
//             if (_allowRuntimeSwitching && _cycleControllers && GUILayout.Button("Cycle Controller"))
//             {
//                 CycleToNextController();
//             }
//             
//             GUILayout.Space(10);
//             GUILayout.Label("Available Controllers:");
//             
//             // Individual controller buttons
//             foreach (var type in _availableTypes ?? new ControllerType[0])
//             {
//                 bool hasPrefab = GetPrefabForControllerType(type) != null;
//                 bool isCurrent = _currentControllerType == type;
//                 
//                 GUI.enabled = _allowRuntimeSwitching && hasPrefab && !isCurrent;
//                 
//                 string buttonText = $"Switch to {type}";
//                 if (!hasPrefab) buttonText += " (No Prefab!)";
//                 if (isCurrent) buttonText += " (Current)";
//                 
//                 if (GUILayout.Button(buttonText))
//                 {
//                     SwitchToController(type);
//                 }
//             }
//             
//             GUI.enabled = true;
//             
//             // Prefab status
//             GUILayout.Space(10);
//             GUILayout.Label("Prefab Status:");
//             GUILayout.Label($"First Person: {(_firstPersonPrefab != null ? _firstPersonPrefab.name : "Not Assigned")}");
//             GUILayout.Label($"Third Person: {(_thirdPersonPrefab != null ? _thirdPersonPrefab.name : "Not Assigned")}");
//             GUILayout.Label($"RTS: {(_rtsPrefab != null ? _rtsPrefab.name : "Not Assigned")}");
//             GUILayout.Label($"Isometric: {(_isometricPrefab != null ? _isometricPrefab.name : "Not Assigned")}");
//             
//             GUILayout.EndArea();
//         }
//         #endregion
//     }
// }
