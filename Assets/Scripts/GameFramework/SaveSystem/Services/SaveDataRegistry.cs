using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameFramework.Services.Interfaces;
using GameFramework.SaveSystem.Interfaces;

namespace GameFramework.SaveSystem.Services
{
    /// <summary>
    /// Registry service for managing ISaveable objects
    /// Provides centralized registration/deregistration with lifecycle management
    /// Implements IGameService for proper initialization and cleanup
    /// </summary>
    public class SaveDataRegistry : ISaveDataRegistry
    {
        #region Private Fields
        private readonly Dictionary<string, ISaveable> _registeredObjects;
        private readonly HashSet<string> _saveKeys; // For duplicate key detection
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[SaveDataRegistry] Initializing save data registry...");
            
            // Registry is ready immediately, no async operations needed
            IsInitialized = true;
            
            Debug.Log($"[SaveDataRegistry] Registry initialized. Ready to register saveable objects.");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[SaveDataRegistry] Shutting down save data registry...");
            
            _registeredObjects.Clear();
            _saveKeys.Clear();
            IsInitialized = false;
            
            Debug.Log("[SaveDataRegistry] Registry shutdown complete.");
        }
        #endregion

        #region Constructor
        public SaveDataRegistry()
        {
            _registeredObjects = new Dictionary<string, ISaveable>();
            _saveKeys = new HashSet<string>();
        }
        #endregion

        #region Registration Methods
        /// <summary>
        /// Registers an ISaveable object with the registry
        /// Validates unique save keys to prevent conflicts
        /// </summary>
        public bool RegisterSaveable(ISaveable saveable)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[SaveDataRegistry] Cannot register saveable - registry not initialized");
                return false;
            }

            if (saveable == null)
            {
                Debug.LogError("[SaveDataRegistry] Cannot register null saveable object");
                return false;
            }

            if (string.IsNullOrEmpty(saveable.SaveKey))
            {
                Debug.LogError($"[SaveDataRegistry] Cannot register saveable with null or empty SaveKey. Type: {saveable.GetType().Name}");
                return false;
            }

            if (_saveKeys.Contains(saveable.SaveKey))
            {
                Debug.LogError($"[SaveDataRegistry] SaveKey '{saveable.SaveKey}' already registered. Registration failed.");
                return false;
            }

            _registeredObjects[saveable.SaveKey] = saveable;
            _saveKeys.Add(saveable.SaveKey);
            
            Debug.Log($"[SaveDataRegistry] Registered saveable: {saveable.SaveKey} (Type: {saveable.TypeName})");
            return true;
        }

        /// <summary>
        /// Deregisters an ISaveable object from the registry
        /// </summary>
        public bool DeregisterSaveable(ISaveable saveable)
        {
            if (saveable == null || string.IsNullOrEmpty(saveable.SaveKey))
                return false;

            return DeregisterSaveable(saveable.SaveKey);
        }

        /// <summary>
        /// Deregisters an ISaveable object by save key
        /// </summary>
        public bool DeregisterSaveable(string saveKey)
        {
            if (string.IsNullOrEmpty(saveKey))
                return false;

            bool removed = _registeredObjects.Remove(saveKey);
            _saveKeys.Remove(saveKey);
            
            if (removed)
            {
                Debug.Log($"[SaveDataRegistry] Deregistered saveable: {saveKey}");
            }
            
            return removed;
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Gets all registered ISaveable objects
        /// Returns read-only collection to prevent external modification
        /// </summary>
        public IReadOnlyDictionary<string, ISaveable> GetAllSaveableObjects()
        {
            return _registeredObjects;
        }

        /// <summary>
        /// Gets a specific ISaveable object by save key
        /// </summary>
        public ISaveable GetSaveable(string saveKey)
        {
            _registeredObjects.TryGetValue(saveKey, out ISaveable saveable);
            return saveable;
        }

        /// <summary>
        /// Checks if a save key is registered
        /// </summary>
        public bool IsRegistered(string saveKey)
        {
            return !string.IsNullOrEmpty(saveKey) && _saveKeys.Contains(saveKey);
        }

        /// <summary>
        /// Gets count of registered objects
        /// </summary>
        public int RegisteredCount => _registeredObjects.Count;
        #endregion
    }
}
