using GameFramework.Services.Interfaces;
using System.Collections.Generic;

namespace GameFramework.SaveSystem.Interfaces
{

    /// <summary>
    /// Interface for the SaveDataRegistry, defining its public API.
    /// </summary>
    public interface ISaveDataRegistry : IGameService
    {
        /// <summary>
        /// Registers an ISaveable object with the registry.
        /// </summary>
        /// <param name="saveable">The ISaveable object to register.</param>
        /// <returns>True if registration is successful, false otherwise.</returns>
        bool RegisterSaveable(ISaveable saveable);

        /// <summary>
        /// Deregisters an ISaveable object from the registry.
        /// </summary>
        /// <param name="saveable">The ISaveable object to deregister.</param>
        /// <returns>True if deregistration is successful, false otherwise.</returns>
        bool DeregisterSaveable(ISaveable saveable);

        /// <summary>
        /// Deregisters an ISaveable object by its save key.
        /// </summary>
        /// <param name="saveKey">The save key of the object to deregister.</param>
        /// <returns>True if deregistration is successful, false otherwise.</returns>
        bool DeregisterSaveable(string saveKey);

        /// <summary>
        /// Gets all registered ISaveable objects.
        /// </summary>
        /// <returns>A read-only dictionary of save keys and their corresponding ISaveable objects.</returns>
        IReadOnlyDictionary<string, ISaveable> GetAllSaveableObjects();

        /// <summary>
        /// Gets a specific ISaveable object by its save key.
        /// </summary>
        /// <param name="saveKey">The save key of the object to retrieve.</param>
        /// <returns>The ISaveable object, or null if not found.</returns>
        ISaveable GetSaveable(string saveKey);

        /// <summary>
        /// Checks if a save key is registered.
        /// </summary>
        /// <param name="saveKey">The save key to check.</param>
        /// <returns>True if the save key is registered, false otherwise.</returns>
        bool IsRegistered(string saveKey);

        /// <summary>
        /// Gets the count of registered objects.
        /// </summary>
        int RegisteredCount { get; }
    }
}