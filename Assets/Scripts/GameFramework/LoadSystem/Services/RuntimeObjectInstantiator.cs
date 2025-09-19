using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.LoadSystem.Interfaces;
using UnityEngine;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Utilities;
using GameFramework.SaveSystem;
using GameFramework.Services.Interfaces;

namespace GameFramework.LoadSystem.Services
{


    /// <summary>
    /// Service responsible for instantiating and configuring runtime objects from save data.
    /// Uses the PrefabRegistry to map GUIDs to prefab assets, eliminating Resources folder usage.
    /// Uses SaveableTypeRegistry for automatic type discovery, eliminating the need for ObjectFactories.
    /// Supports both object instantiation and in-place data loading for existing objects.
    /// </summary>
    public class RuntimeObjectInstantiator : IRuntimeObjectInstantiator
    {
        #region Private Fields

        private PrefabRegistry _prefabRegistry;
        private Transform _defaultParent;

        #endregion

        #region IGameService Implementation

        public bool IsInitialized { get; private set; }

        public RuntimeObjectInstantiator(PrefabRegistry prefabRegistry)
        {
            _prefabRegistry = prefabRegistry ?? throw new ArgumentNullException(nameof(prefabRegistry));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            Debug.Log("[RuntimeObjectInstantiator] Initializing runtime object instantiator...");

            // Create default parent for instantiated objects
            var parentGO = new GameObject("_RuntimeObjects");
            _defaultParent = parentGO.transform;
            UnityEngine.Object.DontDestroyOnLoad(parentGO);

            // Initialize SaveableTypeRegistry for automatic type discovery
            SaveableTypeRegistry.Initialize();

            IsInitialized = true;
            Debug.Log("[RuntimeObjectInstantiator] Runtime object instantiator initialized");

            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            Debug.Log("[RuntimeObjectInstantiator] Shutting down runtime object instantiator...");

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
        /// Instantiates a runtime object from save data using automatic SaveableBase configuration
        /// </summary>
        /// <param name="saveData">The save data containing object information</param>
        /// <param name="parent">Optional parent transform (uses default if null)</param>
        /// <returns>The instantiated GameObject, or null if failed</returns>
        public async Task<GameObject> InstantiateObjectAsync(RuntimeObjectSaveData saveData, Transform parent = null)
        {

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
                Debug.LogError(
                    $"[RuntimeObjectInstantiator] Cannot instantiate object {saveData.uniqueID} - no prefab GUID specified");
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

                Debug.Log(
                    $"[RuntimeObjectInstantiator] Successfully instantiated object: {saveData.uniqueID} ({saveData.typeName})");
                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[RuntimeObjectInstantiator] Error instantiating object {saveData.uniqueID}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Configures an existing object with save data using SaveableBase (no factories needed!)
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
                // Try to find SaveableBase component
                var saveableBase = gameObject.GetComponent<SaveableBase>();
                if (saveableBase != null)
                {
                    // Set the unique ID
                    saveableBase.SetUniqueID(saveData.uniqueID);

                    // Load the runtime save data using the new system
                    saveableBase.LoadRuntimeSaveData(saveData);

                    Debug.Log(
                        $"[RuntimeObjectInstantiator] Configured object using SaveableBase: {saveData.uniqueID} ({saveData.typeName})");
                    return true;
                }

                Debug.LogWarning(
                    $"[RuntimeObjectInstantiator] No SaveableBase or ISaveable component found for object type: {saveData.typeName}. " +
                    "Make sure your prefab has a SaveableBase component.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[RuntimeObjectInstantiator] Error configuring object {saveData.uniqueID}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Logs information about registered saveable types (for debugging)
        /// </summary>
        public void LogRegisteredTypes()
        {
            SaveableTypeRegistry.LogRegisteredTypes();
        }

        /// <summary>
        /// Validates that all registered types are properl configured
        /// </summary>
        /// <returns>True if all types are valid, false if there are issues</returns>
        public bool ValidateRegisteredTypes()
        {
            return SaveableTypeRegistry.ValidateRegisteredTypes();
        }

        #endregion
    }
}
