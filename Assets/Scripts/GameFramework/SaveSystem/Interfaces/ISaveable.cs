using System;

namespace GameFramework.SaveSystem.Interfaces
{
    /// <summary>
    /// Interface for objects that can be saved to persistent storage
    /// Provides serialization key and data for save operations
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique identifier for this saveable object in the save file
        /// </summary>
        string SaveKey { get; }
        
        /// <summary>
        /// Type name for deserialization purposes
        /// </summary>
        string TypeName { get; }
        
        /// <summary>
        /// Gets the serializable data for this object
        /// </summary>
        object GetSaveData();
        
        /// <summary>
        /// Restores object state from saved data
        /// </summary>
        void LoadSaveData(object data);
    }
}