using System;
using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for scene management service with progress reporting support
    /// </summary>
    public interface ISceneService : IGameService
    {
        Task LoadSceneAsync(string sceneName);
        Task LoadSceneAdditiveAsync(string sceneName);
        Task UnloadSceneAsync(string sceneName);
        string GetCurrentSceneName();
        bool IsSceneLoaded(string sceneName);
        
        /// <summary>
        /// Loads a scene with progress reporting for loading systems
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="progressCallback">Optional callback to report loading progress (0.0f to 1.0f)</param>
        /// <returns>True if scene loaded successfully</returns>
        Task<bool> LoadSceneWithProgressAsync(string sceneName, Action<float> progressCallback = null);
        
        /// <summary>
        /// Preloads a scene without activating it, useful for seamless transitions
        /// </summary>
        /// <param name="sceneName">Name of the scene to preload</param>
        /// <param name="progressCallback">Optional callback to report loading progress (0.0f to 1.0f)</param>
        /// <returns>True if scene preloaded successfully</returns>
        Task<bool> PreloadSceneAsync(string sceneName, Action<float> progressCallback = null);
        
        /// <summary>
        /// Activates a preloaded scene
        /// </summary>
        /// <param name="sceneName">Name of the scene to activate</param>
        /// <returns>True if scene activated successfully</returns>
        Task<bool> ActivatePreloadedSceneAsync(string sceneName);
    }
}