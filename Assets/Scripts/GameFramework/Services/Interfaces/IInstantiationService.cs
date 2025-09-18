using System;
using System.Threading.Tasks;
using UnityEngine;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for instantiation service that handles prefab instantiation during game loading
    /// </summary>
    public interface IInstantiationService : IGameService
    {
        /// <summary>
        /// Instantiates the player prefab at the specified position and rotation
        /// </summary>
        /// <param name="position">World position to instantiate the player</param>
        /// <param name="rotation">World rotation to instantiate the player</param>
        /// <returns>The instantiated player GameObject</returns>
        Task<GameObject> InstantiatePlayerAsync(Vector3 position, Vector3 rotation);
        
        /// <summary>
        /// Instantiates the player prefab using default spawn settings
        /// </summary>
        /// <returns>The instantiated player GameObject</returns>
        Task<GameObject> InstantiatePlayerAsync();
        
        /// <summary>
        /// Gets the current player instance if it exists
        /// </summary>
        /// <returns>The current player GameObject, or null if no player exists</returns>
        GameObject GetCurrentPlayer();
        
        /// <summary>
        /// Destroys the current player instance if it exists
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task DestroyPlayerAsync();
        
        /// <summary>
        /// Sets the player prefab to be instantiated
        /// </summary>
        /// <param name="playerPrefab">The prefab to use for player instantiation</param>
        void SetPlayerPrefab(GameObject playerPrefab);
        
        /// <summary>
        /// Sets the default spawn position and rotation for fallback scenarios
        /// </summary>
        /// <param name="position">Default spawn position</param>
        /// <param name="rotation">Default spawn rotation</param>
        void SetDefaultSpawnSettings(Vector3 position, Vector3 rotation);
    }
}
