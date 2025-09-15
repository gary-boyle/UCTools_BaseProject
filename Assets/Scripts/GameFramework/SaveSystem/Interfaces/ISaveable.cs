using System;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for objects that can be saved and loaded
    /// Provides a contract for serializable game objects
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique identifier for this saveable object
        /// Used to identify the object during save/load operations
        /// </summary>
        string SaveId { get; }
        
        /// <summary>
        /// Gets the current save data as a serializable object
        /// This should return all data needed to restore the object's state
        /// </summary>
        /// <returns>Serializable data object</returns>
        object GetSaveData();
        
        /// <summary>
        /// Restores the object's state from save data
        /// </summary>
        /// <param name="saveData">Previously saved data object</param>
        void LoadSaveData(object saveData);
    }
}