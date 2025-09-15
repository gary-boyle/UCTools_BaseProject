using System;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for the Save Registry Service that manages all ISaveable objects in the game
    /// Provides centralized registration, unregistration, and querying of saveable objects
    /// Extends IGameService for consistent service lifecycle management
    /// </summary>
    public interface ISaveRegistryService : IGameService
    {
        #region Registration Management
        
        /// <summary>
        /// Registers a saveable object with the save system
        /// Thread-safe operation with duplicate checking
        /// </summary>
        /// <param name="saveable">The saveable object to register</param>
        /// <returns>True if registration was successful, false if already registered</returns>
        bool RegisterSaveable(ISaveable saveable);
        
        /// <summary>
        /// Unregisters a saveable object from the save system
        /// Thread-safe operation
        /// </summary>
        /// <param name="saveable">The saveable object to unregister</param>
        /// <returns>True if unregistration was successful, false if not found</returns>
        bool UnregisterSaveable(ISaveable saveable);
        
        /// <summary>
        /// Unregisters a saveable object by its SaveId
        /// </summary>
        /// <param name="saveId">The SaveId of the object to unregister</param>
        /// <returns>True if unregistration was successful, false if not found</returns>
        bool UnregisterSaveable(string saveId);
        
        /// <summary>
        /// Unregisters all currently registered saveable objects
        /// Used during shutdown or when clearing the registry
        /// </summary>
        void UnregisterAllSaveables();
        
        #endregion
        
        #region Query Operations
        
        /// <summary>
        /// Gets a registered saveable object by its SaveId
        /// Thread-safe operation
        /// </summary>
        /// <param name="saveId">The SaveId to search for</param>
        /// <returns>The saveable object, or null if not found</returns>
        ISaveable GetSaveable(string saveId);
        
        /// <summary>
        /// Gets a registered saveable object of a specific type by its SaveId
        /// Thread-safe operation with type casting
        /// </summary>
        /// <typeparam name="T">The expected type of the saveable object</typeparam>
        /// <param name="saveId">The SaveId to search for</param>
        /// <returns>The saveable object cast to type T, or null if not found or wrong type</returns>
        T GetSaveable<T>(string saveId) where T : class, ISaveable;
        
        /// <summary>
        /// Gets all registered saveable objects
        /// Returns a copy to prevent external modification
        /// </summary>
        /// <returns>Array of all registered saveable objects</returns>
        ISaveable[] GetAllSaveables();
        
        /// <summary>
        /// Gets all registered saveable objects of a specific type
        /// </summary>
        /// <typeparam name="T">The type of saveable objects to retrieve</typeparam>
        /// <returns>Array of saveable objects of type T</returns>
        T[] GetSaveablesOfType<T>() where T : class, ISaveable;
        
        /// <summary>
        /// Checks if a saveable object is registered
        /// </summary>
        /// <param name="saveId">The SaveId to check</param>
        /// <returns>True if registered, false otherwise</returns>
        bool IsRegistered(string saveId);
        
        /// <summary>
        /// Gets the count of registered saveable objects
        /// </summary>
        int RegisteredCount { get; }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Gets debug information about all registered saveables
        /// Useful for debugging and monitoring the save system state
        /// </summary>
        /// <returns>Debug string with registration information</returns>
        string GetDebugInfo();
        
        #endregion
    }
}
