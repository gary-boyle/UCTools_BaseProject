using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using GameFramework.Services.Interfaces;

namespace GameFramework.Services
{
    /// <summary>
    /// Service responsible for managing and tracking all ISaveable objects in the game
    /// Uses Registry pattern to maintain a centralized collection of saveable objects
    /// Provides thread-safe registration/unregistration and query capabilities
    /// </summary>
    public class SaveRegistryService : ISaveRegistryService
    {
        #region Private Fields
        
        /// <summary>
        /// Dictionary storing all registered saveable objects by their SaveId
        /// Uses concurrent-safe operations for thread safety
        /// </summary>
        private readonly Dictionary<string, ISaveable> _registeredSaveables = new Dictionary<string, ISaveable>();
        
        /// <summary>
        /// Lock object for thread-safe operations on the registry
        /// </summary>
        private readonly object _registryLock = new object();
        
        /// <summary>
        /// Event fired when a new saveable object is registered
        /// </summary>
        public event Action<ISaveable> OnSaveableRegistered;
        
        /// <summary>
        /// Event fired when a saveable object is unregistered
        /// </summary>
        public event Action<ISaveable> OnSaveableUnregistered;
        
        #endregion
        
        #region IGameService Implementation
        
        /// <summary>
        /// Indicates whether the service has been initialized
        /// </summary>
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Initializes the save registry service
        /// </summary>
        /// <returns>Completed task</returns>
        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[SaveRegistryService] Service already initialized");
                return;
            }
            
            // Clear any existing registrations (safety measure)
            lock (_registryLock)
            {
                _registeredSaveables.Clear();
            }
            
            IsInitialized = true;
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Shuts down the service and unregisters all saveable objects
        /// </summary>
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[SaveRegistryService] Service not initialized");
                return;
            }
            
            Debug.Log("[SaveRegistryService] Shutting down Save Registry Service...");
            
            // Unregister all saveables
            UnregisterAllSaveables();
            
            IsInitialized = false;
        }
        
        #endregion
        
        #region Registration Management
        
        /// <summary>
        /// Registers a saveable object with the save system
        /// Thread-safe operation with duplicate checking
        /// </summary>
        /// <param name="saveable">The saveable object to register</param>
        /// <returns>True if registration was successful, false if already registered</returns>
        public bool RegisterSaveable(ISaveable saveable)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveRegistryService] Cannot register saveable - service not initialized");
                return false;
            }
            
            if (saveable == null)
            {
                Debug.LogError("[SaveRegistryService] Cannot register null saveable");
                return false;
            }
            
            if (string.IsNullOrEmpty(saveable.SaveId))
            {
                Debug.LogError($"[SaveRegistryService] Cannot register saveable with null/empty SaveId: {saveable.GetType().Name}");
                return false;
            }
            
            lock (_registryLock)
            {
                // Check if already registered
                if (_registeredSaveables.ContainsKey(saveable.SaveId))
                {
                    Debug.LogWarning($"[SaveRegistryService] Saveable with ID '{saveable.SaveId}' already registered");
                    return false;
                }
                
                // Register the saveable
                _registeredSaveables[saveable.SaveId] = saveable;
            }
            return true;
        }
        
        /// <summary>
        /// Unregisters a saveable object from the save system
        /// Thread-safe operation
        /// </summary>
        /// <param name="saveable">The saveable object to unregister</param>
        /// <returns>True if unregistration was successful, false if not found</returns>
        public bool UnregisterSaveable(ISaveable saveable)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveRegistryService] Cannot unregister saveable - service not initialized");
                return false;
            }
            
            if (saveable == null || string.IsNullOrEmpty(saveable.SaveId))
            {
                Debug.LogError("[SaveRegistryService] Cannot unregister null saveable or saveable with null/empty SaveId");
                return false;
            }
            
            bool wasRemoved = false;
            lock (_registryLock)
            {
                wasRemoved = _registeredSaveables.Remove(saveable.SaveId);
            }
            
            if (wasRemoved)
            {
                return true;
            }
            
            Debug.LogWarning($"[SaveRegistryService] Attempted to unregister unknown saveable: {saveable.SaveId}");
            return false;
        }
        
        /// <summary>
        /// Unregisters a saveable object by its SaveId
        /// </summary>
        /// <param name="saveId">The SaveId of the object to unregister</param>
        /// <returns>True if unregistration was successful, false if not found</returns>
        public bool UnregisterSaveable(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                Debug.LogError("[SaveRegistryService] Cannot unregister with null/empty SaveId");
                return false;
            }
            
            var saveable = GetSaveable(saveId);
            return saveable != null && UnregisterSaveable(saveable);
        }
        
        /// <summary>
        /// Unregisters all currently registered saveable objects
        /// Used during shutdown
        /// </summary>
        public void UnregisterAllSaveables()
        {
            List<ISaveable> saveablesToUnregister;
            
            // Get a copy of all saveables to avoid modification during iteration
            lock (_registryLock)
            {
                saveablesToUnregister = _registeredSaveables.Values.ToList();
            }
            
            // Unregister each saveable
            foreach (var saveable in saveablesToUnregister)
            {
                UnregisterSaveable(saveable);
            }
        }
        
        #endregion
        
        #region Query Operations
        
        /// <summary>
        /// Gets a registered saveable object by its SaveId
        /// Thread-safe operation
        /// </summary>
        /// <param name="saveId">The SaveId to search for</param>
        /// <returns>The saveable object, or null if not found</returns>
        public ISaveable GetSaveable(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
                return null;
            
            lock (_registryLock)
            {
                _registeredSaveables.TryGetValue(saveId, out var saveable);
                return saveable;
            }
        }
        
        /// <summary>
        /// Gets a registered saveable object of a specific type by its SaveId
        /// Thread-safe operation with type casting
        /// </summary>
        /// <typeparam name="T">The expected type of the saveable object</typeparam>
        /// <param name="saveId">The SaveId to search for</param>
        /// <returns>The saveable object cast to type T, or null if not found or wrong type</returns>
        public T GetSaveable<T>(string saveId) where T : class, ISaveable
        {
            return GetSaveable(saveId) as T;
        }
        
        /// <summary>
        /// Gets all registered saveable objects
        /// Returns a copy to prevent external modification
        /// </summary>
        /// <returns>Array of all registered saveable objects</returns>
        public ISaveable[] GetAllSaveables()
        {
            lock (_registryLock)
            {
                return _registeredSaveables.Values.ToArray();
            }
        }
        
        /// <summary>
        /// Gets all registered saveable objects of a specific type
        /// </summary>
        /// <typeparam name="T">The type of saveable objects to retrieve</typeparam>
        /// <returns>Array of saveable objects of type T</returns>
        public T[] GetSaveablesOfType<T>() where T : class, ISaveable
        {
            lock (_registryLock)
            {
                return _registeredSaveables.Values.OfType<T>().ToArray();
            }
        }
        
        /// <summary>
        /// Checks if a saveable object is registered
        /// </summary>
        /// <param name="saveId">The SaveId to check</param>
        /// <returns>True if registered, false otherwise</returns>
        public bool IsRegistered(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
                return false;
            
            lock (_registryLock)
            {
                return _registeredSaveables.ContainsKey(saveId);
            }
        }
        
        /// <summary>
        /// Gets the count of registered saveable objects
        /// </summary>
        public int RegisteredCount
        {
            get
            {
                lock (_registryLock)
                {
                    return _registeredSaveables.Count;
                }
            }
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Gets debug information about all registered saveables
        /// </summary>
        /// <returns>Debug string with registration information</returns>
        public string GetDebugInfo()
        {
            lock (_registryLock)
            {
                var info = $"SaveRegistryService - Registered Count: {_registeredSaveables.Count}\n";
                foreach (var kvp in _registeredSaveables)
                {
                    info += $"  - {kvp.Key}: {kvp.Value.GetType().Name}\n";
                }
                return info;
            }
        }
        
        #endregion
    }
}
