using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using UnityEngine;
using GameFramework.GameData.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.Services.Interfaces;

namespace GameFramework.GameData.Services
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
            _saveDataRegistry = saveDataRegistry ?? throw new ArgumentNullException(nameof(_saveDataRegistry));
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
        public void UpdateGameSession(string difficulty = null, string currentScene = null, float? gameTime = null)
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
                if (previousPlayer != null)
                {
                    _saveDataRegistry.DeregisterSaveable(previousPlayer);
                }
                _saveDataRegistry.RegisterSaveable(_currentPlayerData);
            }

            // Publish change event through EventSystem
            _eventSystem?.Publish(new PlayerDataChangedEvent(_currentPlayerData));
        }

        /// <summary>
        /// Updates specific player properties
        /// </summary>
        public void UpdatePlayer(string playerName = null, Vector3? position = null, Vector3? rotation = null)
        {
            if (!IsInitialized || _currentPlayerData == null)
            {
                Debug.LogError("[GameDataService] Cannot update player - service not initialized or no current player");
                return;
            }

            bool changed = false;

            if (!string.IsNullOrEmpty(playerName) && _currentPlayerData.PlayerName != playerName)
            {
                _currentPlayerData.PlayerName = playerName;
                changed = true;
            }

            if (position.HasValue && _currentPlayerData.Position != position.Value)
            {
                _currentPlayerData.Position = position.Value;
                changed = true;
            }

            if (rotation.HasValue && _currentPlayerData.Rotation != rotation.Value)
            {
                _currentPlayerData.Rotation = rotation.Value;
                changed = true;
            }

            if (changed)
            {
                // Publish change event through EventSystem
                _eventSystem?.Publish(new PlayerDataChangedEvent(_currentPlayerData));
            }
        }
        #endregion

        #region Data Lifecycle
        /// <summary>
        /// Creates a new game session with default or specified parameters
        /// </summary>
        public void StartNewGame(string playerName = "Player", string difficulty = "Normal", string startingScene = "MainMenu")
        {
            if (!IsInitialized)
            {
                Debug.LogError("[GameDataService] Cannot start new game - service not initialized");
                return;
            }
            
            // Create new game session
            var newGameSession = new GameSessionData(difficulty, startingScene, 0f);
            SetGameSessionData(newGameSession);

            // Create new player
            var newPlayerData = new PlayerData(playerName, Vector3.zero, Vector3.zero);
            SetPlayerData(newPlayerData);

            // Publish new game started event
            _eventSystem?.Publish(new NewGameStartedEvent(newGameSession, newPlayerData));
        }

        /// <summary>
        /// Loads game data from provided data objects (used by load system)
        /// </summary>
        public void LoadGameData(GameSessionData gameSessionData, PlayerData playerData)
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

            if (playerData != null)
            {
                SetPlayerData(playerData);
            }

            // Publish game data loaded event
            _eventSystem?.Publish(new GameDataLoadedEvent(gameSessionData, playerData));
        }

        /// <summary>
        /// Resets all game data to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[GameDataService] Cannot reset - service not initialized");
                return;
            }

            InitializeDefaultGameData();

            // Re-register with save system if available
            if (_saveDataRegistry != null)
            {
                RegisterWithSaveSystem();
            }

            // Publish change events for the reset data
            _eventSystem?.Publish(new GameSessionDataChangedEvent(_currentGameSession));
            _eventSystem?.Publish(new PlayerDataChangedEvent(_currentPlayerData));
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initializes default game data objects
        /// </summary>
        private void InitializeDefaultGameData()
        {
            // Create default game session
            _currentGameSession = new GameSessionData("Normal", "MainMenu", 0f);

            // Create default player
            _currentPlayerData = new PlayerData("Player", Vector3.zero, Vector3.zero);
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
