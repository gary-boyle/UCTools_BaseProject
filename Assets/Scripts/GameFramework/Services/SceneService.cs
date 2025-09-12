using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;
using ISceneService = GameFramework.Services.Interfaces.ISceneService;

namespace GameFramework.Services
{
    /// <summary>
    /// Scene service implementation with constructor injection
    /// </summary>
    public class SceneService : ISceneService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public SceneService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            IsInitialized = false;
        }
        
        public async Task LoadSceneAsync(string sceneName)
        {
            var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            Debug.Log($"[SceneService] Loaded scene '{sceneName}'");
        }
        
        public async Task LoadSceneAdditiveAsync(string sceneName)
        {
            var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            Debug.Log($"[SceneService] Additively loaded scene '{sceneName}'");
        }
        
        public async Task UnloadSceneAsync(string sceneName)
        {
            var operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            Debug.Log($"[SceneService] Unloaded scene '{sceneName}'");
        }
        
        public string GetCurrentSceneName()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        
        public bool IsSceneLoaded(string sceneName)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }
    }
}