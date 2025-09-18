using System;
using System.Threading.Tasks;
using GameFramework.Components;
using GameFramework.DataStructures;
using UnityEngine;
using GameFramework.GameData.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Data;
using GameFramework.Services.Interfaces;

namespace GameFramework.Services
{
    /// <summary>
    /// Central service for managing current game data (GameSessionData and PlayerData)
    /// Provides controlled access to game state and integrates with save system
    /// Uses EventSystem for change notifications and automatic save system registration
    /// </summary>
    public class GameDataService : IGameDataService
    {
        #region Private Fields
        private GameSessionData _currentGameSession;
        private PlayerData _currentPlayerData;
        private PlayerSaveData _pendingPlayerSaveData; // Stores player data from save files before instantiation
        private ISaveDataRegistry _saveDataRegistry;
        private IEventSystem _eventSystem;
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }
        
        // constructor
        public GameDataService(
            IEventSystem eventSystem,
            ISaveDataRegistry saveDataRegistry
            )
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveDataRegistry = saveDataRegistry ?? throw new ArgumentNullException(nameof(saveDataRegistry));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            // Initialize with default data
            InitializeDefaultGameData();

            IsInitialized = true;
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            // Deregister from save system if registry is available
            if (_saveDataRegistry != null)
            {
                UnregisterFromSaveSystem();
            }

            // Clear references
            _currentGameSession = null;
            _currentPlayerData = null;
            _pendingPlayerSaveData = null;
            _saveDataRegistry = null;
            _eventSystem = null;

            IsInitialized = false;
        }
        #endregion
        

        #region GameSessionData Access
        /// <summary>
        /// Gets the current GameSessionData (read-only access)
        /// </summary>
        public GameSessionData GetGameSessionData()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[GameDataService] Service not initialized, returning null");
                return null;
            }

            return _currentGameSession;
        }

        /// <summary>
        /// Sets new GameSessionData and publishes change event
        /// </summary>
        public void SetGameSessionData(GameSessionData gameSessionData)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[GameDataService] Cannot set game session data - service not initialized");
                return;
            }

            if (gameSessionData == null)
            {
                Debug.LogError("[GameDataService] Cannot set null GameSessionData");
                return;
            }

            var previousSession = _currentGameSession;
            _currentGameSession = gameSessionData;

            // Re-register with save system if registry is available
            if (_saveDataRegistry != null)
            {
                if (previousSession != null)
                {
                    _saveDataRegistry.DeregisterSaveable(previousSession);
                }
                _saveDataRegistry.RegisterSaveable(_currentGameSession);
            }

            // Publish change event through EventSystem
            _eventSystem?.Publish(new GameSessionDataChangedEvent(_currentGameSession));
        }

        /// <summary>
        /// Updates specific game session properties
        /// </summary>
        public void UpdateGameSession(string difficulty = null, string currentScene = null, long? gameTime = null)
        {
            if (!IsInitialized || _currentGameSession == null)
            {
                Debug.LogError("[GameDataService] Cannot update game session - service not initialized or no current session");
                return;
            }

            bool changed = false;

            if (!string.IsNullOrEmpty(difficulty) && _currentGameSession.Difficulty != difficulty)
            {
                _currentGameSession.Difficulty = difficulty;
                changed = true;
            }

            if (!string.IsNullOrEmpty(currentScene) && _currentGameSession.CurrentScene != currentScene)
            {
                _currentGameSession.CurrentScene = currentScene;
                changed = true;
            }

            if (gameTime.HasValue && Math.Abs(_currentGameSession.GameTime - gameTime.Value) > 0.001f)
            {
                _currentGameSession.GameTime = gameTime.Value;
                changed = true;
            }

            if (changed)
            {
                // Publish change event through EventSystem
                _eventSystem?.Publish(new GameSessionDataChangedEvent(_currentGameSession));
            }
        }

        public bool HasActiveSession()
        {
            return IsInitialized && _currentGameSession != null;
        }
        
        #endregion

        #region PlayerData Access
        /// <summary>
        /// Gets the current PlayerData (read-only access)
        /// </summary>
        public PlayerData GetPlayerData()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[GameDataService] Service not initialized, returning null");
                return null;
            }

            return _currentPlayerData;
        }

        /// <summary>
        /// Sets new PlayerData and publishes change event
        /// </summary>
        public void SetPlayerData(PlayerData playerData)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[GameDataService] Cannot set player data - service not initialized");
                return;
            }

            if (playerData == null)
            {
                Debug.LogError("[GameDataService] Cannot set null PlayerData");
                return;
            }

            var previousPlayer = _currentPlayerData;
            _currentPlayerData = playerData;

            // Re-register with save system if registry is available
            if (_saveDataRegistry != null)
            {
                // Force deregister any existing PlayerData with the same SaveKey
                _saveDataRegistry.DeregisterSaveable("PlayerData");
                
                if (previousPlayer != null)
                {
                    _saveDataRegistry.DeregisterSaveable(previousPlayer);
                }
                
                bool registered = _saveDataRegistry.RegisterSaveable(_currentPlayerData);
                if (!registered)
                {
                    Debug.LogError($"[GameDataService] Failed to register PlayerData with SaveKey: {_currentPlayerData.SaveKey}");
                }
            }

            // Publish change event through EventSystem
            _eventSystem?.Publish(new PlayerDataChangedEvent(_currentPlayerData));
        }

        /// <summary>
        /// Gets pending PlayerSaveData from loaded games (before player instantiation)
        /// </summary>
        public PlayerSaveData GetPendingPlayerSaveData()
        {
            return _pendingPlayerSaveData;
        }

        /// <summary>
        /// Stores PlayerSaveData from loaded games before player instantiation
        /// </summary>
        private void StorePendingPlayerData(PlayerSaveData playerSaveData)
        {
            _pendingPlayerSaveData = playerSaveData;
            Debug.Log($"[GameDataService] Stored pending player save data for: {playerSaveData?.playerName ?? "Unknown"}");
        }

        /// <summary>
        /// Clears pending PlayerSaveData (called after successful player instantiation)
        /// </summary>
        public void ClearPendingPlayerData()
        {
            _pendingPlayerSaveData = null;
        }

        /// <summary>
        /// Forces deregistration of a specific PlayerData from the save system
        /// Used when destroying player objects to prevent duplicate registrations
        /// </summary>
        public void ForceDeregisterPlayerData(PlayerData playerData)
        {
            if (playerData != null && _saveDataRegistry != null)
            {
                bool deregistered = _saveDataRegistry.DeregisterSaveable(playerData);
                Debug.Log($"[GameDataService] Force deregistered PlayerData: {deregistered}");
                
                // Clear current reference if it matches
                if (_currentPlayerData == playerData)
                {
                    _currentPlayerData = null;
                }
            }
        }
        #endregion

        #region Data Lifecycle

        /// <summary>
        /// Loads game data from provided data objects (used by load system)
        /// </summary>
        public void LoadGameData(GameSessionData gameSessionData, PlayerSaveData playerSaveData)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[GameDataService] Cannot load game data - service not initialized");
                return;
            }
            
            if (gameSessionData != null)
            {
                SetGameSessionData(gameSessionData);
            }
        
            if (playerSaveData != null)
            {
                // Store the player save data - actual PlayerData MonoBehaviour will be created by InstantiationService
                StorePendingPlayerData(playerSaveData);
            }
        
            // Publish game data loaded event
            _eventSystem?.Publish(new GameDataLoadedEvent(gameSessionData, playerSaveData));
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initializes default game data objects
        /// </summary>
        private void InitializeDefaultGameData()
        {
            // Create default game session
            _currentGameSession = new GameSessionData("Normal", "MainMenu", 0);

            // Create default player - this is a temporary solution for initialization
            // In practice, PlayerData MonoBehaviours should be created by InstantiationService
            _currentPlayerData = new GameObject("DefaultPlayer").AddComponent<PlayerData>();
            _currentPlayerData.PlayerName = "Player";
        }

        /// <summary>
        /// Registers current data objects with the save system
        /// </summary>
        private void RegisterWithSaveSystem()
        {
            if (_saveDataRegistry == null) return;

            bool sessionRegistered = _saveDataRegistry.RegisterSaveable(_currentGameSession);
            bool playerRegistered = _saveDataRegistry.RegisterSaveable(_currentPlayerData);

            if (!sessionRegistered && !playerRegistered)
            {
                Debug.LogWarning("[GameDataService] Failed to register some game data objects with save system");
            }
        }

        /// <summary>
        /// Unregisters current data objects from the save system
        /// </summary>
        private void UnregisterFromSaveSystem()
        {
            if (_saveDataRegistry == null) return;

            if (_currentGameSession != null)
            {
                _saveDataRegistry.DeregisterSaveable(_currentGameSession);
            }

            if (_currentPlayerData != null)
            {
                _saveDataRegistry.DeregisterSaveable(_currentPlayerData);
            }
        }
        #endregion

        #region Validation
        /// <summary>
        /// Validates that current game data is in a consistent state
        /// </summary>
        public bool ValidateGameData()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[GameDataService] Cannot validate - service not initialized");
                return false;
            }

            bool isValid = true;

            if (_currentGameSession == null)
            {
                Debug.LogError("[GameDataService] Validation failed - GameSessionData is null");
                isValid = false;
            }

            if (_currentPlayerData == null)
            {
                Debug.LogError("[GameDataService] Validation failed - PlayerData is null");
                isValid = false;
            }
            
            return isValid;
        }
        #endregion
    }
}
