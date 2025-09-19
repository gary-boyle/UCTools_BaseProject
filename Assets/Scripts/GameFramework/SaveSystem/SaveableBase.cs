using UnityEngine;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Utilities;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Attributes;
using GameFramework.Core;
using System.Threading.Tasks;
using System.Reflection;

namespace GameFramework.SaveSystem
{
    /// <summary>
    /// Base class for MonoBehaviours that work with the new clean save system.
    /// Provides RuntimeObjectSaveData support and automatic prefab registry integration.
    /// Objects that inherit from this will automatically work with the V2 save/load system.
    /// </summary>
    public abstract class SaveableBase : MonoBehaviour, ISaveable
    {
    #region ISaveable Implementation
    public virtual string SaveKey => $"{GetSaveableTypeName()}_{UniqueID}";
    public virtual string TypeName => GetSaveableTypeName();
    #endregion

        #region Private Fields
        [SerializeField] private string _uniqueID;
        [SerializeField] private string _prefabGUID; // New: GUID of the source prefab
        
        private ISaveDataRegistry _saveDataRegistry;
        private bool _isRegisteredWithSaveSystem = false;
        #endregion

        #region Public Properties
        public string UniqueID
        {
            get => _uniqueID;
            private set
            {
                if (string.IsNullOrEmpty(value) || !UniqueIDGenerator.IsValidUniqueID(value))
                {
                    Debug.LogError($"[{GetType().Name}] Invalid UniqueID assigned: {value}");
                    return;
                }
                _uniqueID = value;
            }
        }
        
        /// <summary>
        /// GUID of the prefab this object was instantiated from
        /// Used by the new instantiation system
        /// </summary>
        public string PrefabGUID
        {
            get => _prefabGUID;
            set => _prefabGUID = value;
        }
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            Debug.Log($"!!! - {gameObject.name}");
            // Only generate unique ID at runtime when object is actually instantiated in a scene
            // This prevents prefabs and prefab variants from sharing the same ID
            if (string.IsNullOrEmpty(_uniqueID) && IsRuntimeInstance())
            {
                GenerateUniqueId();
            }
            
            // Try to determine prefab GUID if not set
            if (string.IsNullOrEmpty(_prefabGUID) && IsRuntimeInstance())
            {
                DeterminePrefabGUID();
            }
            
            // Call virtual method for additional Awake logic
            OnAwakeCustom();
        }

        protected virtual async void Start()
        {
            // Call virtual method for custom Start logic before registration
            OnStartCustom();
            
            // Only register with save system if we have a valid UniqueID
            if (!string.IsNullOrEmpty(_uniqueID))
            {
                await RegisterWithSaveSystemAsync();
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] {gameObject.name} has no UniqueID - skipping save system registration. " +
                                "This may happen if the object is not a valid runtime instance.");
            }
        }

        protected virtual void OnDestroy()
        {
            // Unregister from save system
            UnregisterFromSaveSystem();
            
            // Call virtual method for custom cleanup
            OnDestroyCustom();
        }
        #endregion

        #region Virtual Extension Points
        /// <summary>
        /// Called during Awake, after UniqueID generation but before any other initialization.
        /// Override for custom Awake logic.
        /// </summary>
        protected virtual void OnAwakeCustom() { }

        /// <summary>
        /// Called during Start, before save system registration.
        /// Override for custom Start logic.
        /// </summary>
        protected virtual void OnStartCustom() { }

        /// <summary>
        /// Called during OnDestroy, after save system unregistration.
        /// Override for custom cleanup logic.
        /// </summary>
        protected virtual void OnDestroyCustom() { }

        /// <summary>
        /// Called before saving data. Override to perform pre-save operations.
        /// </summary>
        protected virtual void OnBeforeSave() { }

        /// <summary>
        /// Called after loading data successfully. Override to perform post-load operations.
        /// </summary>
        protected virtual void OnAfterLoad() { }

        /// <summary>
        /// Called when save operation fails. Override for custom error handling.
        /// </summary>
        protected virtual void OnSaveError(System.Exception exception)
        {
            Debug.LogError($"[{GetType().Name}] Save error for {gameObject.name}: {exception.Message}");
        }

        /// <summary>
        /// Called when load operation fails. Override for custom error handling.
        /// </summary>
        protected virtual void OnLoadError(System.Exception exception)
        {
            Debug.LogError($"[{GetType().Name}] Load error for {gameObject.name}: {exception.Message}");
        }
        #endregion

        #region Save System Integration
        private async Task RegisterWithSaveSystemAsync()
        {
            try
            {
                // Get the SaveDataRegistry service
                _saveDataRegistry = await GameManager.GetServiceAsync<ISaveDataRegistry>();
                
                if (_saveDataRegistry != null && !_isRegisteredWithSaveSystem)
                {
                    bool registered = _saveDataRegistry.RegisterSaveable(this);
                    _isRegisteredWithSaveSystem = registered;
                    
                    if (registered)
                    {
                        Debug.Log($"[{GetType().Name}] {gameObject.name} registered with save system (Key: {SaveKey})");
                        OnSaveSystemRegistered();
                    }
                    else
                    {
                        Debug.LogWarning($"[{GetType().Name}] Failed to register {gameObject.name} with save system");
                        OnSaveSystemRegistrationFailed();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error registering with save system: {ex.Message}");
                OnSaveSystemRegistrationFailed();
            }
        }

        private void UnregisterFromSaveSystem()
        {
            if (_saveDataRegistry != null && _isRegisteredWithSaveSystem)
            {
                _saveDataRegistry.DeregisterSaveable(this);
                _isRegisteredWithSaveSystem = false;
                OnSaveSystemUnregistered();
            }
        }

        /// <summary>
        /// Called when successfully registered with save system. Override for custom logic.
        /// </summary>
        protected virtual void OnSaveSystemRegistered() { }

        /// <summary>
        /// Called when save system registration fails. Override for custom error handling.
        /// </summary>
        protected virtual void OnSaveSystemRegistrationFailed() { }

        /// <summary>
        /// Called when unregistered from save system. Override for custom cleanup.
        /// </summary>
        protected virtual void OnSaveSystemUnregistered() { }
        #endregion

        #region New Save System Methods
        /// <summary>
        /// Creates runtime object save data with transform and identity information.
        /// Derived classes should override CreateSpecificRuntimeSaveData() to provide type-specific data.
        /// </summary>
        /// <returns>RuntimeObjectSaveData for this object</returns>
        public virtual RuntimeObjectSaveData CreateRuntimeSaveData()
        {
            var saveData = CreateSpecificRuntimeSaveData();
            
            if (saveData != null)
            {
                // Populate common fields
                saveData.uniqueID = _uniqueID;
                saveData.prefabGUID = _prefabGUID;
                saveData.typeName = TypeName;
                
                // Populate transform data
                saveData.position = transform.position;
                saveData.rotation = transform.eulerAngles;
                saveData.scale = transform.localScale;
                saveData.isActive = gameObject.activeInHierarchy;
            }
            
            return saveData;
        }
        
        /// <summary>
        /// Creates the specific runtime save data type for this object.
        /// Must be implemented by derived classes to return their specific save data type.
        /// </summary>
        /// <returns>Specific runtime save data instance</returns>
        protected abstract RuntimeObjectSaveData CreateSpecificRuntimeSaveData();
        
        /// <summary>
        /// Loads runtime object save data and applies it to this object.
        /// Handles common fields (transform, etc.) and delegates to LoadSpecificRuntimeSaveData for type-specific data.
        /// </summary>
        /// <param name="saveData">The runtime save data to load</param>
        public virtual void LoadRuntimeSaveData(RuntimeObjectSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Cannot load null runtime save data for {gameObject.name}");
                return;
            }
            
            try
            {
                // Load common fields
                _uniqueID = saveData.uniqueID;
                _prefabGUID = saveData.prefabGUID;
                
                // Apply transform data
                transform.position = saveData.position;
                transform.rotation = Quaternion.Euler(saveData.rotation);
                transform.localScale = saveData.scale;
                gameObject.SetActive(saveData.isActive);
                
                // Load type-specific data
                LoadSpecificRuntimeSaveData(saveData);
                
                Debug.Log($"[{GetType().Name}] Loaded runtime save data for {gameObject.name}");
                OnAfterLoad();
            }
            catch (System.Exception ex)
            {
                OnLoadError(ex);
            }
        }
        
        /// <summary>
        /// Loads type-specific runtime save data.
        /// Must be implemented by derived classes to handle their specific data.
        /// </summary>
        /// <param name="saveData">The runtime save data containing type-specific information</param>
        protected abstract void LoadSpecificRuntimeSaveData(RuntimeObjectSaveData saveData);
        #endregion

        #region ISaveable Methods (Legacy Compatibility)
        /// <summary>
        /// Legacy ISaveable method - provides backwards compatibility by delegating to new methods.
        /// </summary>
        public virtual object GetSaveData()
        {
            return CreateRuntimeSaveData();
        }

        /// <summary>
        /// Legacy ISaveable method - provides backwards compatibility by delegating to new methods.
        /// </summary>
        public virtual void LoadSaveData(object data)
        {
            if (data == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Cannot load null save data for {gameObject.name}");
                return;
            }

            try
            {
                // Load as RuntimeObjectSaveData
                if (data is RuntimeObjectSaveData runtimeData)
                {
                    LoadRuntimeSaveData(runtimeData);
                    return;
                }
                
                Debug.LogWarning($"[{GetType().Name}] Unsupported save data type: {data?.GetType().Name}");
            }
            catch (System.Exception ex)
            {
                OnLoadError(ex);
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Gets the saveable type name for this object using the SaveableTypeRegistry.
        /// This automatically uses the SaveableType attribute if present, otherwise falls back to class name.
        /// </summary>
        /// <returns>The type name for this saveable object</returns>
        protected virtual string GetSaveableTypeName()
        {
            // Try to get the type name from the SaveableTypeRegistry first
            var registryTypeName = SaveableTypeRegistry.GetTypeName(SaveableTypeRegistry.GetSaveDataType(GetType()));
            if (!string.IsNullOrEmpty(registryTypeName))
            {
                return registryTypeName;
            }
            
            // Fall back to class name if no SaveableType attribute is found
            Debug.LogWarning($"[{GetType().Name}] No SaveableType attribute found. Consider adding [SaveableType(typeof(YourRuntimeSaveData))] to this class for better type management.");
            return GetType().Name;
        }

        /// <summary>
        /// Generates a new unique ID for this saveable object.
        /// Uses the class name as prefix by default. Override for custom prefixes.
        /// </summary>
        protected virtual void GenerateUniqueId()
        {
            string prefix = GetUniqueIdPrefix();
            UniqueID = UniqueIDGenerator.GenerateUniqueID(prefix);
        }

        /// <summary>
        /// Gets the prefix used for unique ID generation.
        /// Override to customize the prefix for your saveable type.
        /// </summary>
        protected virtual string GetUniqueIdPrefix()
        {
            return GetType().Name.ToLower();
        }

        /// <summary>
        /// Manually set the unique ID (useful for loading existing objects).
        /// Only use this if you know what you're doing!
        /// </summary>
        public void SetUniqueID(string uniqueId)
        {
            UniqueID = uniqueId;
        }

        /// <summary>
        /// Forces re-registration with the save system.
        /// Useful if the object needs to be re-registered after certain operations.
        /// </summary>
        public async Task ForceReregisterWithSaveSystem()
        {
            UnregisterFromSaveSystem();
            await RegisterWithSaveSystemAsync();
        }

        /// <summary>
        /// Checks if this object is currently registered with the save system.
        /// </summary>
        public bool IsRegisteredWithSaveSystem => _isRegisteredWithSaveSystem;
        
        /// <summary>
        /// Attempts to determine the prefab GUID for this object
        /// </summary>
        private void DeterminePrefabGUID()
        {
#if UNITY_EDITOR
            // In editor, we can try to determine the prefab GUID
            var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(this);
            if (prefabAsset != null)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(prefabAsset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    _prefabGUID = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
                    Debug.Log($"[{GetType().Name}] Auto-determined prefab GUID: {_prefabGUID}");
                }
            }
#endif
            // In builds, the prefab GUID should be set manually or through the instantiation system
        }
        #endregion

        #region Runtime Instance Detection
        /// <summary>
        /// Determines if this object is a runtime instance (not a prefab asset)
        /// UniqueIDs should only be generated for actual scene instances
        /// </summary>
        private bool IsRuntimeInstance()
        {
#if UNITY_EDITOR
            // In editor, check if this is a prefab asset or scene object
            if (!Application.isPlaying)
            {
                // During edit mode, don't generate IDs for prefab assets
                return false;
            }
            
            // During play mode, check if object is in a scene
            return gameObject.scene.IsValid() && !string.IsNullOrEmpty(gameObject.scene.name);
#else
            // In builds, if we reach Awake, we're a runtime instance
            return Application.isPlaying;
#endif
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Validates the component in the editor.
        /// Override to add custom validation logic.
        /// Note: Does NOT generate UniqueIDs in editor to prevent prefab ID conflicts
        /// </summary>
        protected virtual void OnValidate()
        {
            // Auto-determine prefab GUID if missing
            if (string.IsNullOrEmpty(_prefabGUID) && !Application.isPlaying)
            {
                DeterminePrefabGUID();
            }
        }
        
        /// <summary>
        /// Editor-only method to manually generate a UniqueID for testing
        /// Only use this for debugging purposes!
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void EditorGenerateUniqueID()
        {
            if (!Application.isPlaying)
            {
                GenerateUniqueId();
                Debug.LogWarning($"[{GetType().Name}] Editor-generated UniqueID: {_uniqueID}. This should only be used for testing!");
            }
        }
#endif
        #endregion
    }
}
