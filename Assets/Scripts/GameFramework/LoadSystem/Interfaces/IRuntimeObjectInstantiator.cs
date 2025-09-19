using System.Threading.Tasks;
using GameFramework.SaveSystem.Data;
using GameFramework.Services.Interfaces;
using UnityEngine;

namespace GameFramework.LoadSystem.Interfaces
{
    public interface IRuntimeObjectInstantiator : IGameService
    {
        /// <summary>
        /// Instantiates a runtime object from save data
        /// </summary>
        /// <param name="saveData">The save data containing object information</param>
        /// <param name="parent">Optional parent transform (uses default if null)</param>
        /// <returns>The instantiated GameObject, or null if failed</returns>
        Task<GameObject> InstantiateObjectAsync(RuntimeObjectSaveData saveData, Transform parent = null);

        /// <summary>
        /// Configures an existing object with save data (for objects that already exist in the scene)
        /// </summary>
        /// <param name="gameObject">The existing GameObject to configure</param>
        /// <param name="saveData">The save data to apply</param>
        /// <returns>True if configuration was successful</returns>
        Task<bool> ConfigureObjectAsync(GameObject gameObject, RuntimeObjectSaveData saveData);

        /// <summary>
        /// Logs information about registered saveable types (for debugging)
        /// </summary>
        void LogRegisteredTypes();

        /// <summary>
        /// Validates that all registered types are properly configured
        /// </summary>
        /// <returns>True if all types are valid, false if there are issues</returns>
        bool ValidateRegisteredTypes();
    }
}