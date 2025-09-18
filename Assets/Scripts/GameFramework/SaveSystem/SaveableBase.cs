using UnityEngine;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Utilities;
using GameFramework.Core;
using System.Threading.Tasks;

namespace GameFramework.SaveSystem
{
    /// <summary>
    /// Abstract base class for MonoBehaviours that need to be saved.
    /// Provides common ISaveable implementation with automatic save system registration,
    /// UniqueID management, and extension points for custom save/load logic.
    /// 
    /// To use: inherit from SaveableBase and implement GetSaveData() and LoadSaveData()
    /// </summary>
    public abstract class SaveableBase : MonoBehaviour, ISaveable
    {
        #region ISaveable Implementation
        public virtual string SaveKey => $"{GetType().Name}_{UniqueID}";
        public virtual string TypeName => GetType().Name;
        #endregion

        #region Private Fields
        [SerializeField] private string _uniqueID;
        
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
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            // Only generate unique ID at runtime when object is actually instantiated in a scene
            // This prevents prefabs and prefab variants from sharing the same ID
            if (string.IsNullOrEmpty(_uniqueID) && IsRuntimeInstance())
            {
                GenerateUniqueId();
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
                Debug.Log($"[{GetType().Name}] {gameObject.name} unregistered from save system");
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

        #region Abstract ISaveable Methods
        /// <summary>
        /// Gets the serializable data for this object.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <returns>Serializable data object</returns>
        public abstract object GetSaveData();

        /// <summary>
        /// Restores object state from saved data.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="data">The saved data to load</param>
        public abstract void LoadSaveData(object data);
        #endregion

        #region ISaveable Methods with Error Handling
        /// <summary>
        /// Template method for GetSaveData with error handling and extension points.
        /// Calls the abstract GetSaveData method with proper error handling.
        /// </summary>
        object ISaveable.GetSaveData()
        {
            try
            {
                OnBeforeSave();
                var data = GetSaveData();
                Debug.Log($"[{GetType().Name}] Save data collected for {gameObject.name}");
                return data;
            }
            catch (System.Exception ex)
            {
                OnSaveError(ex);
                throw; // Re-throw to maintain ISaveable contract
            }
        }

        /// <summary>
        /// Template method for LoadSaveData with error handling and extension points.
        /// Calls the abstract LoadSaveData method with proper error handling.
        /// </summary>
        void ISaveable.LoadSaveData(object data)
        {
            if (data == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Cannot load null save data for {gameObject.name}");
                return;
            }

            try
            {
                LoadSaveData(data);
                OnAfterLoad();
                Debug.Log($"[{GetType().Name}] Save data loaded for {gameObject.name}");
            }
            catch (System.Exception ex)
            {
                OnLoadError(ex);
                // Don't re-throw here as loading should be more tolerant of errors
            }
        }
        #endregion

        #region Utility Methods
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
            // Custom validation logic can be added here by derived classes
            // We intentionally do NOT generate UniqueIDs here to prevent prefab conflicts
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
