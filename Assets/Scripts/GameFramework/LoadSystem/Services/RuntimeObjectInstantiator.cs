using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.LoadSystem.Interfaces;
using UnityEngine;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.Services.Interfaces;

namespace GameFramework.LoadSystem.Services
{


    /// <summary>
    /// Service responsible for instantiating and configuring runtime objects from save data.
    /// Uses the PrefabRegistry to map GUIDs to prefab assets, eliminating Resources folder usage.
    /// Supports both object instantiation and in-place data loading for existing objects.
    /// </summary>
    public class RuntimeObjectInstantiator : IRuntimeObjectInstantiator
    {
        #region Private Fields
        private PrefabRegistry _prefabRegistry;
        private Dictionary<string, IObjectFactory> _objectFactories;
        private Transform _defaultParent;
        #endregion
        
        #region IGameService Implementation
        public bool IsInitialized { get; private set; }
        
        public RuntimeObjectInstantiator(PrefabRegistry prefabRegistry)
        {
            _prefabRegistry = prefabRegistry ?? throw new ArgumentNullException(nameof(prefabRegistry));
            _objectFactories = new Dictionary<string, IObjectFactory>();
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[RuntimeObjectInstantiator] Initializing runtime object instantiator...");
            
            // Create default parent for instantiated objects
            var parentGO = new GameObject("_RuntimeObjects");
            _defaultParent = parentGO.transform;
            UnityEngine.Object.DontDestroyOnLoad(parentGO);
            
            // Register built-in object factories
            RegisterBuiltInFactories();
            
            IsInitialized = true;
            Debug.Log("[RuntimeObjectInstantiator] Runtime object instantiator initialized");
            
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[RuntimeObjectInstantiator] Shutting down runtime object instantiator...");
            
            _objectFactories?.Clear();
            
            if (_defaultParent != null)
            {
                UnityEngine.Object.Destroy(_defaultParent.gameObject);
                _defaultParent = null;
            }
            
            IsInitialized = false;
            Debug.Log("[RuntimeObjectInstantiator] Runtime object instantiator shutdown complete");
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Instantiates a runtime object from save data
        /// </summary>
        /// <param name="saveData">The save data containing object information</param>
        /// <param name="parent">Optional parent transform (uses default if null)</param>
        /// <returns>The instantiated GameObject, or null if failed</returns>
        public async Task<GameObject> InstantiateObjectAsync(RuntimeObjectSaveData saveData, Transform parent = null)
        {
            //var tmp = this;
            
            // if (!IsInitialized)
            // {
            //     Debug.LogError("[RuntimeObjectInstantiator] Cannot instantiate object - service not initialized");
            //     return null;
            // }
            
            if (saveData == null)
            {
                Debug.LogError("[RuntimeObjectInstantiator] Cannot instantiate object from null save data");
                return null;
            }
            
            if (string.IsNullOrEmpty(saveData.prefabGUID))
            {
                Debug.LogError($"[RuntimeObjectInstantiator] Cannot instantiate object {saveData.uniqueID} - no prefab GUID specified");
                return null;
            }
            
            try
            {
                // Get prefab from registry
                GameObject prefab = _prefabRegistry.GetPrefab(saveData.prefabGUID);
                if (prefab == null)
                {
                    Debug.LogError($"[RuntimeObjectInstantiator] Prefab not found for GUID: {saveData.prefabGUID}");
                    return null;
                }
                
                // Determine parent
                Transform targetParent = parent ?? _defaultParent;
                
                // Instantiate the prefab
                GameObject instance = UnityEngine.Object.Instantiate(prefab, targetParent);
                if (instance == null)
                {
                    Debug.LogError($"[RuntimeObjectInstantiator] Failed to instantiate prefab: {prefab.name}");
                    return null;
                }
                
                // Apply transform data
                instance.transform.position = saveData.position;
                instance.transform.rotation = Quaternion.Euler(saveData.rotation);
                instance.transform.localScale = saveData.scale;
                instance.SetActive(saveData.isActive);
                
                // Configure the object with save data
                await ConfigureObjectAsync(instance, saveData);
                
                Debug.Log($"[RuntimeObjectInstantiator] Successfully instantiated object: {saveData.uniqueID} ({saveData.typeName})");
                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RuntimeObjectInstantiator] Error instantiating object {saveData.uniqueID}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Configures an existing object with save data (for objects that already exist in the scene)
        /// </summary>
        /// <param name="gameObject">The existing GameObject to configure</param>
        /// <param name="saveData">The save data to apply</param>
        /// <returns>True if configuration was successful</returns>
        public async Task<bool> ConfigureObjectAsync(GameObject gameObject, RuntimeObjectSaveData saveData)
        {
            if (gameObject == null || saveData == null)
            {
                Debug.LogError("[RuntimeObjectInstantiator] Cannot configure object - null gameObject or saveData");
                return false;
            }
            
            try
            {
                // Try to find a specific factory for this object type
                if (_objectFactories.TryGetValue(saveData.typeName, out IObjectFactory factory))
                {
                    return await factory.ConfigureObjectAsync(gameObject, saveData);
                }
                
                // Fall back to generic ISaveable configuration
                var saveable = gameObject.GetComponent<ISaveable>();
                if (saveable != null)
                {
                    // Set the unique ID if possible
                    if (saveable is MonoBehaviour mb && mb.GetType().GetMethod("SetUniqueID") != null)
                    {
                        mb.GetType().GetMethod("SetUniqueID").Invoke(mb, new object[] { saveData.uniqueID });
                    }
                    
                    // Load the save data
                    saveable.LoadSaveData(saveData);
                    
                    Debug.Log($"[RuntimeObjectInstantiator] Configured object using ISaveable interface: {saveData.uniqueID}");
                    return true;
                }
                
                Debug.LogWarning($"[RuntimeObjectInstantiator] No factory or ISaveable component found for object type: {saveData.typeName}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RuntimeObjectInstantiator] Error configuring object {saveData.uniqueID}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Registers a custom object factory for a specific type
        /// </summary>
        /// <param name="typeName">The type name to handle</param>
        /// <param name="factory">The factory implementation</param>
        public void RegisterObjectFactory(string typeName, IObjectFactory factory)
        {
            if (string.IsNullOrEmpty(typeName) || factory == null)
            {
                Debug.LogError("[RuntimeObjectInstantiator] Cannot register null factory or empty type name");
                return;
            }
            
            _objectFactories[typeName] = factory;
            Debug.Log($"[RuntimeObjectInstantiator] Registered object factory for type: {typeName}");
        }
        
        /// <summary>
        /// Unregisters an object factory
        /// </summary>
        /// <param name="typeName">The type name to unregister</param>
        public void UnregisterObjectFactory(string typeName)
        {
            if (_objectFactories.ContainsKey(typeName))
            {
                _objectFactories.Remove(typeName);
                Debug.Log($"[RuntimeObjectInstantiator] Unregistered object factory for type: {typeName}");
            }
        }
        #endregion
        
        #region Private Methods
        private void RegisterBuiltInFactories()
        {
            // Register factory for ClickableCube
            RegisterObjectFactory("ClickableCube", new ClickableCubeFactory());
            
            // Register factory for TestGenericSaveable
            RegisterObjectFactory("TestGenericSaveable", new TestGenericSaveableFactory());
        }
        #endregion
    }
    
    /// <summary>
    /// Interface for object-specific factories
    /// </summary>
    public interface IObjectFactory
    {
        /// <summary>
        /// Configures a GameObject with save data
        /// </summary>
        /// <param name="gameObject">The GameObject to configure</param>
        /// <param name="saveData">The save data to apply</param>
        /// <returns>True if configuration was successful</returns>
        Task<bool> ConfigureObjectAsync(GameObject gameObject, RuntimeObjectSaveData saveData);
    }
    
    /// <summary>
    /// Factory for ClickableCube objects
    /// </summary>
    public class ClickableCubeFactory : IObjectFactory
    {
        public async Task<bool> ConfigureObjectAsync(GameObject gameObject, RuntimeObjectSaveData saveData)
        {
            if (saveData is not ClickableCubeRuntimeSaveData cubeData)
            {
                Debug.LogError("[ClickableCubeFactory] Save data is not ClickableCubeRuntimeSaveData");
                return false;
            }
            
            var clickableCube = gameObject.GetComponent<GameFramework.Components.ClickableCube>();
            if (clickableCube == null)
            {
                Debug.LogError("[ClickableCubeFactory] GameObject does not have ClickableCube component");
                return false;
            }
            
            try
            {
                // Set the unique ID
                clickableCube.SetUniqueID(cubeData.uniqueID);
                
                // Configure the cube with the save data
                clickableCube.SetValues(cubeData.cubeValue, cubeData.cubeColor);
                
                Debug.Log($"[ClickableCubeFactory] Configured ClickableCube: {cubeData.uniqueID}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClickableCubeFactory] Error configuring ClickableCube: {ex.Message}");
                return false;
            }
        }
    }
    
    /// <summary>
    /// Factory for TestGenericSaveable objects
    /// </summary>
    public class TestGenericSaveableFactory : IObjectFactory
    {
        public async Task<bool> ConfigureObjectAsync(GameObject gameObject, RuntimeObjectSaveData saveData)
        {
            if (saveData is not TestGenericRuntimeSaveData genericData)
            {
                Debug.LogError("[TestGenericSaveableFactory] Save data is not TestGenericRuntimeSaveData");
                return false;
            }
            
            var testGeneric = gameObject.GetComponent<GameFramework.SaveSystem.Examples.TestGenericSaveable>();
            if (testGeneric == null)
            {
                Debug.LogError("[TestGenericSaveableFactory] GameObject does not have TestGenericSaveable component");
                return false;
            }
            
            try
            {
                // Set the unique ID
                testGeneric.SetUniqueID(genericData.uniqueID);
                
                // Configure the object with the save data
                testGeneric.SetTestValue(genericData.testValue);
                testGeneric.SetTestString(genericData.testString);
                testGeneric.SetTestBool(genericData.testBool);
                
                Debug.Log($"[TestGenericSaveableFactory] Configured TestGenericSaveable: {genericData.uniqueID}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestGenericSaveableFactory] Error configuring TestGenericSaveable: {ex.Message}");
                return false;
            }
        }
    }
}
