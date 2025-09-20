using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using ISceneService = GameFramework.Services.Interfaces.ISceneService;

namespace GameFramework.Services
{
    /// <summary>
    /// Scene service implementation with constructor injection and progress reporting support
    /// </summary>
    public class SceneService : ISceneService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly Dictionary<string, AsyncOperation> _preloadedScenes = new();
        
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
            return SceneManager.GetActiveScene().name;
        }
        
        public bool IsSceneLoaded(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }
        
        public async Task<bool> LoadSceneWithProgressAsync(string sceneName, Action<float> progressCallback = null)
        {
            try
            {
                Debug.Log($"[SceneService] Loading scene '{sceneName}' with progress reporting");
                
                var operation = SceneManager.LoadSceneAsync(sceneName);
                if (operation == null)
                {
                    Debug.LogError($"[SceneService] Failed to start loading scene '{sceneName}'");
                    return false;
                }
                
                while (!operation.isDone)
                {
                    // Unity's progress goes from 0 to 0.9, then jumps to 1
                    // We normalize it to provide smoother feedback
                    float normalizedProgress = Mathf.Clamp01(operation.progress / 0.9f);
                    progressCallback?.Invoke(normalizedProgress);
                    await Task.Yield();
                }
                
                // find any camera in the scene and delete them
                
                // Ensure final progress is reported
                progressCallback?.Invoke(1.0f);
                
                Debug.Log($"[SceneService] Successfully loaded scene '{sceneName}' with progress reporting");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneService] Error loading scene '{sceneName}': {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> PreloadSceneAsync(string sceneName, Action<float> progressCallback = null)
        {
            try
            {
                Debug.Log($"[SceneService] Preloading scene '{sceneName}'");
                
                // Clean up any existing preload for this scene
                if (_preloadedScenes.ContainsKey(sceneName))
                {
                    Debug.LogWarning($"[SceneService] Scene '{sceneName}' already preloaded, replacing...");
                    _preloadedScenes.Remove(sceneName);
                }
                
                var operation = SceneManager.LoadSceneAsync(sceneName);
                if (operation == null)
                {
                    Debug.LogError($"[SceneService] Failed to start preloading scene '{sceneName}'");
                    return false;
                }
                
                // Prevent the scene from activating immediately
                operation.allowSceneActivation = false;
                _preloadedScenes[sceneName] = operation;
                
                // Wait until scene is loaded but not activated (progress reaches 0.9)
                while (operation.progress < 0.9f)
                {
                    progressCallback?.Invoke(operation.progress / 0.9f);
                    await Task.Yield();
                }
                
                progressCallback?.Invoke(1.0f);
                Debug.Log($"[SceneService] Successfully preloaded scene '{sceneName}' (ready for activation)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneService] Error preloading scene '{sceneName}': {ex.Message}");
                _preloadedScenes.Remove(sceneName);
                return false;
            }
        }
        
        public async Task<bool> ActivatePreloadedSceneAsync(string sceneName)
        {
            try
            {
                if (!_preloadedScenes.TryGetValue(sceneName, out var operation))
                {
                    Debug.LogError($"[SceneService] Scene '{sceneName}' is not preloaded");
                    return false;
                }
                
                Debug.Log($"[SceneService] Activating preloaded scene '{sceneName}'");
                
                // Allow the scene to activate
                operation.allowSceneActivation = true;
                
                // Wait for activation to complete
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                // Clean up the preloaded scene reference
                _preloadedScenes.Remove(sceneName);
                
                Debug.Log($"[SceneService] Successfully activated preloaded scene '{sceneName}'");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneService] Error activating preloaded scene '{sceneName}': {ex.Message}");
                _preloadedScenes.Remove(sceneName);
                return false;
            }
        }
    }
}