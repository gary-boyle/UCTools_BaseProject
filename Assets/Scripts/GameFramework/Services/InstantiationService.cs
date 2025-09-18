using System;
using System.Threading.Tasks;
using GameFramework.Components;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Service responsible for instantiating game objects, particularly the player prefab.
    /// Integrates with the event system to notify about instantiation events.
    /// Works with GameDataService to configure PlayerData components from save data.
    /// </summary>
    public class InstantiationService : IInstantiationService
    {
        #region Private Fields
        private readonly IEventSystem _eventSystem;
        private readonly IGameDataService _gameDataService;
        private GameObject _playerPrefab;
        private GameObject _currentPlayer;
        private Vector3 _defaultSpawnPosition = Vector3.zero;
        private Vector3 _defaultSpawnRotation = Vector3.zero;
        private bool _isNewGameLoad = false; // Track if current load is a new game
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }

        public InstantiationService(IEventSystem eventSystem, IGameDataService gameDataService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            // Subscribe to relevant events
            _eventSystem.Subscribe<BeginNewGameLoadEvent>(OnBeginNewGameLoad);
            _eventSystem.Subscribe<LoadSaveFileEvent>(OnLoadSaveFile);
            _eventSystem.Subscribe<LoadingCompletedEvent>(OnLoadingCompleted);

            IsInitialized = true;

            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            // Unsubscribe from events
            _eventSystem.Unsubscribe<BeginNewGameLoadEvent>(OnBeginNewGameLoad);
            _eventSystem.Unsubscribe<LoadSaveFileEvent>(OnLoadSaveFile);
            _eventSystem.Unsubscribe<LoadingCompletedEvent>(OnLoadingCompleted);

            // Clean up current player if it exists
            if (_currentPlayer != null)
            {
                UnityEngine.Object.DestroyImmediate(_currentPlayer);
                _currentPlayer = null;
            }

            IsInitialized = false;
        }
        #endregion

        #region IInstantiationService Implementation
        public async Task<GameObject> InstantiatePlayerAsync(Vector3 position, Vector3 rotation)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[InstantiationService] Service not initialized");
                return null;
            }

            if (_playerPrefab == null)
            {
                Debug.LogError("[InstantiationService] Player prefab not set");
                return null;
            }

            try
            {
                // Destroy existing player if it exists
                if (_currentPlayer != null)
                {
                    await DestroyPlayerAsync();
                }

                // Instantiate new player
                _currentPlayer = UnityEngine.Object.Instantiate(_playerPrefab, position, Quaternion.Euler(rotation));
                
                if (_currentPlayer != null)
                {
                    // Configure PlayerData component with save data if available
                    var playerData = _currentPlayer.GetComponent<PlayerData>();
                    if (playerData != null)
                    {
                        ConfigurePlayerDataFromSaveData(playerData, position, rotation);
                        
                        // Register with GameDataService as current player
                        _gameDataService.SetPlayerData(playerData);
                        
                        // Clear pending save data since player is now instantiated
                        _gameDataService.ClearPendingPlayerData();
                    }
                    
                    // Publish instantiation event
                    _eventSystem.Publish(new PlayerInstantiatedEvent(_currentPlayer, position, rotation));
                    
                    return _currentPlayer;
                }
                else
                {
                    Debug.LogError("[InstantiationService] Failed to instantiate player");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InstantiationService] Error instantiating player: {ex.Message}");
                return null;
            }
        }

        public async Task<GameObject> InstantiatePlayerAsync()
        {
            return await InstantiatePlayerAsync(_defaultSpawnPosition, _defaultSpawnRotation);
        }

        public GameObject GetCurrentPlayer()
        {
            return _currentPlayer;
        }

        public async Task DestroyPlayerAsync()
        {
            if (_currentPlayer != null)
            {
                // Deregister PlayerData from save system before destroying
                var playerData = _currentPlayer.GetComponent<PlayerData>();
                if (playerData != null)
                {
                    // Force deregistration from GameDataService to avoid duplicate registrations
                    _gameDataService.ForceDeregisterPlayerData(playerData);
                }
                
                // Publish destruction event
                _eventSystem.Publish(new PlayerDestroyedEvent(_currentPlayer));
                
                UnityEngine.Object.DestroyImmediate(_currentPlayer);
                _currentPlayer = null;
            }

            await Task.CompletedTask;
        }

        public void SetPlayerPrefab(GameObject playerPrefab)
        {
            _playerPrefab = playerPrefab;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles new game load events - sets flag for new game instantiation
        /// </summary>
        private async void OnBeginNewGameLoad(BeginNewGameLoadEvent evt)
        {
            _isNewGameLoad = true;
            
            // Clear any pending player data from previous sessions for new games
            _gameDataService.ClearPendingPlayerData();
            
            // The actual instantiation will happen when LoadingCompleted is fired
        }

        /// <summary>
        /// Handles load save file events - sets flag for saved game instantiation
        /// </summary>
        private async void OnLoadSaveFile(LoadSaveFileEvent evt)
        {
            _isNewGameLoad = false;
            // The actual instantiation will happen when LoadingCompleted is fired
        }

        /// <summary>
        /// Handles loading completed events - actually instantiates the player
        /// </summary>
        private async void OnLoadingCompleted(LoadingCompletedEvent evt)
        {
            if (_playerPrefab == null)
            {
                Debug.LogError("[InstantiationService] Cannot instantiate player - prefab not set");
                return;
            }

            try
            {
                Vector3 spawnPosition;
                Vector3 spawnRotation;
                
                if (_isNewGameLoad)
                {
                    // For new games, find PlayerSpawnPoint in scene
                    var spawnPoint = FindPlayerSpawnPoint();
                    if (spawnPoint != null)
                    {
                        spawnPosition = spawnPoint.SpawnPosition;
                        spawnRotation = spawnPoint.SpawnRotation;
                        Debug.Log($"[InstantiationService] Using PlayerSpawnPoint '{spawnPoint.SpawnPointName}' - Position: {spawnPosition}, Rotation: {spawnRotation}");
                    }
                    else
                    {
                        // Fallback to default position if no spawn point found
                        spawnPosition = _defaultSpawnPosition;
                        spawnRotation = _defaultSpawnRotation;
                        Debug.LogWarning($"[InstantiationService] No PlayerSpawnPoint found in scene, using default spawn position: {spawnPosition}, rotation: {spawnRotation}");
                    }
                }
                else
                {
                    // For loaded games, use saved position/rotation
                    var pendingData = _gameDataService.GetPendingPlayerSaveData();
                    if (pendingData != null)
                    {
                        spawnPosition = pendingData.Position;
                        spawnRotation = pendingData.Rotation;
                        Debug.Log($"[InstantiationService] Using saved position: {spawnPosition}, rotation: {spawnRotation}");
                    }
                    else
                    {
                        // Fallback to default if no save data (shouldn't happen for loaded games)
                        spawnPosition = _defaultSpawnPosition;
                        spawnRotation = _defaultSpawnRotation;
                        Debug.LogWarning($"[InstantiationService] No saved data found for loaded game, using default spawn position: {spawnPosition}, rotation: {spawnRotation}");
                    }
                }
                
                // Instantiate the player
                var player = await InstantiatePlayerAsync(spawnPosition, spawnRotation);
                
                if (player != null)
                {
                    Debug.Log("[InstantiationService] Player successfully instantiated after loading");
                }
                else
                {
                    Debug.LogError("[InstantiationService] Failed to instantiate player after loading");
                }
                
                // Reset the flag for next load
                _isNewGameLoad = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InstantiationService] Error instantiating player after loading: {ex.Message}");
            }
        }
        #endregion

        #region Public Helper Methods
        /// <summary>
        /// Sets the default spawn position and rotation for new games
        /// </summary>
        /// <param name="position">Default spawn position</param>
        /// <param name="rotation">Default spawn rotation</param>
        public void SetDefaultSpawnSettings(Vector3 position, Vector3 rotation)
        {
            _defaultSpawnPosition = position;
            _defaultSpawnRotation = rotation;
            Debug.Log($"[InstantiationService] Default spawn settings updated: Position {position}, Rotation {rotation}");
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Configures a PlayerData MonoBehaviour with save data or default values
        /// </summary>
        private void ConfigurePlayerDataFromSaveData(PlayerData playerData, Vector3 spawnPosition, Vector3 spawnRotation)
        {
            try
            {
                Debug.Log($"[InstantiationService] ConfigurePlayerDataFromSaveData - _isNewGameLoad: {_isNewGameLoad}, spawnPosition: {spawnPosition}, spawnRotation: {spawnRotation}");
                
                // For new games, always use the provided spawn position (from PlayerSpawnPoint)
                // For loaded games, use save data and ignore the spawn position
                if (_isNewGameLoad)
                {
                    // Configure for new game - use spawn position (from PlayerSpawnPoint or default)
                    playerData.PlayerName = "Player"; // Default name
                    
                    // Update transform
                    playerData.transform.position = spawnPosition;
                    playerData.transform.rotation = Quaternion.Euler(spawnRotation);
                }
                else
                {
                    // For loaded games, try to get pending save data from GameDataService
                    var pendingData = _gameDataService.GetPendingPlayerSaveData();
                    if (pendingData != null)
                    {
                        // Configure from loaded save data
                        // Use LoadSaveData method to properly configure the PlayerData
                        playerData.LoadSaveData(pendingData);
                        
                        // Update transform to match saved data
                        playerData.transform.position = pendingData.Position;
                        playerData.transform.rotation = Quaternion.Euler(pendingData.Rotation);
                    }
                    else
                    {
                        throw new Exception("No pending PlayerSaveData available for loaded game");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InstantiationService] Error configuring PlayerData: {ex.Message}");
                
                // Fallback to basic configuration
                playerData.PlayerName = "Player";
                playerData.transform.position = spawnPosition;
                playerData.transform.rotation = Quaternion.Euler(spawnRotation);
            }
        }

        /// <summary>
        /// Finds a PlayerSpawnPoint component in the current scene
        /// </summary>
        /// <returns>The active PlayerSpawnPoint, or null if none found</returns>
        private PlayerSpawnPoint FindPlayerSpawnPoint()
        {
            try
            {
                // Find all PlayerSpawnPoint components in the scene
                var spawnPoints = UnityEngine.Object.FindObjectsOfType<PlayerSpawnPoint>();
                
                if (spawnPoints == null || spawnPoints.Length == 0)
                {
                    Debug.LogWarning("[InstantiationService] No PlayerSpawnPoint components found in scene");
                    return null;
                }
                
                // Filter to only active spawn points
                var activeSpawnPoints = new System.Collections.Generic.List<PlayerSpawnPoint>();
                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint.IsActive && spawnPoint.IsValid())
                    {
                        activeSpawnPoints.Add(spawnPoint);
                    }
                }
                
                if (activeSpawnPoints.Count == 0)
                {
                    Debug.LogWarning("[InstantiationService] No active PlayerSpawnPoint components found in scene");
                    return null;
                }
                
                if (activeSpawnPoints.Count > 1)
                {
                    // Build error message with all spawn point names
                    var spawnPointNames = new System.Collections.Generic.List<string>();
                    foreach (var sp in activeSpawnPoints)
                    {
                        spawnPointNames.Add($"'{sp.SpawnPointName}' on GameObject '{sp.gameObject.name}'");
                    }
                    
                    string errorMessage = $"[InstantiationService] Multiple active PlayerSpawnPoint components found in scene: {string.Join(", ", spawnPointNames)}. Please ensure only one PlayerSpawnPoint is active per scene.";
                    Debug.LogError(errorMessage);
                    
                    // Return the first one but log the error
                    Debug.LogWarning($"[InstantiationService] Using first spawn point: {activeSpawnPoints[0].SpawnPointName}");
                    return activeSpawnPoints[0];
                }
                
                // Return the single active spawn point
                var chosenSpawnPoint = activeSpawnPoints[0];
                return chosenSpawnPoint;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InstantiationService] Error finding PlayerSpawnPoint: {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}
