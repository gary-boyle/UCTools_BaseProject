using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Clean GameDataService that manages unified GameSession data
    /// Single source of truth for all game state information
    /// </summary>
    public class GameDataService : IGameDataService, IUpdatable
    {
        public bool IsInitialized { get; private set; }
        public GameSession CurrentSession { get; private set; }
        public LoadingConfiguration CurrentLoadingConfig { get; set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly ISaveService _saveService;
        
        // Events
        public event Action<GameSession> OnSessionCreated;
        public event Action<GameSession> OnSessionLoaded;
        public event Action OnSessionCleared;

        public GameDataService(IEventSystem eventSystem, ISaveService saveService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[GameDataService] Initializing game data service...");
            
            // Subscribe to scene events to keep session updated
            _eventSystem.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            IsInitialized = true;
            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            _eventSystem?.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
            ClearSession();
            CurrentLoadingConfig = null;
            IsInitialized = false;
        }

        public void Update()
        {
            UpdateSession();
        }
        
        #region GameSession Management
        
        /// <summary>
        /// Creates a new game session from loading configuration
        /// </summary>
        public void CreateNewGameSession(LoadingConfiguration config)
        {
            CurrentSession = GameSession.CreateNewGame(
                config.PlayerName, 
                config.GameData.ContainsKey("difficulty") ? config.GameData["difficulty"].ToString() : "Normal",
                config.SceneName
            );
            
            // Apply any custom data from loading config
            foreach (var kvp in config.GameData)
            {
                CurrentSession.customData[kvp.Key] = kvp.Value;
            }
            
            Debug.Log($"[GameDataService] Created new game session for player '{CurrentSession.playerName}'");
            OnSessionCreated?.Invoke(CurrentSession);
        }
        
        /// <summary>
        /// Loads existing game session
        /// </summary>
        public void LoadGameSession(GameSession session)
        {
            CurrentSession = session ?? throw new ArgumentNullException(nameof(session));
            
            Debug.Log($"[GameDataService] Loaded game session for player '{CurrentSession.playerName}'");
            OnSessionLoaded?.Invoke(CurrentSession);
        }
        
        /// <summary>
        /// Clears the current session
        /// </summary>
        public void ClearSession()
        {
            CurrentSession = null;
            OnSessionCleared?.Invoke();
        }
        
        /// <summary>
        /// Updates current session with play time and handles auto-save
        /// </summary>
        public void UpdateSession()
        {
            if (CurrentSession == null) return;
            
            CurrentSession.UpdatePlayTime();
            
            // Auto-save check (every 5 minutes)
            var timeSinceLastSave = DateTime.Now - CurrentSession.lastSaveTime;
            if (timeSinceLastSave.TotalMinutes >= 5)
            {
                _ = SaveCurrentSessionAsync("AutoSave");
            }
        }
        
        /// <summary>
        /// Saves the current session
        /// </summary>
        public async Task<bool> SaveCurrentSessionAsync(string saveName = null)
        {
            if (CurrentSession == null)
            {
                Debug.LogWarning("[GameDataService] No active session to save");
                return false;
            }
            
            CurrentSession.lastSaveTime = DateTime.Now;
            CurrentSession.UpdatePlayTime();
            
            return await _saveService.SaveGameSessionAsync(CurrentSession, saveName);
        }
        
        /// <summary>
        /// Loads a session from save file
        /// </summary>
        public async Task<bool> LoadSessionAsync(string saveName)
        {
            var session = await _saveService.LoadGameSessionAsync(saveName);
            if (session != null)
            {
                LoadGameSession(session);
                return true;
            }
            return false;
        }
        
        #endregion

        #region Data Access Convenience Methods
        
        /// <summary>
        /// Gets the current player state - throws if no active session
        /// </summary>
        public PlayerState GetPlayerState() 
        {
            if (CurrentSession?.player == null)
                throw new InvalidOperationException("No active game session or player state");
            return CurrentSession.player;
        }
        
        /// <summary>
        /// Gets the current game progress - throws if no active session  
        /// </summary>
        public GameProgress GetGameProgress() 
        {
            if (CurrentSession?.progress == null)
                throw new InvalidOperationException("No active game session or progress data");
            return CurrentSession.progress;
        }
        
        /// <summary>
        /// Gets custom data from current session
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default) 
        {
            if (CurrentSession?.customData.ContainsKey(key) == true)
            {
                try { return (T)CurrentSession.customData[key]; }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Sets custom data in current session
        /// </summary>
        public void SetCustomData<T>(string key, T value)
        {
            if (CurrentSession != null)
            {
                CurrentSession.customData[key] = value;
            }
        }
        
        /// <summary>
        /// Checks if there's an active game session
        /// </summary>
        public bool HasActiveSession() => CurrentSession != null;
        
        /// <summary>
        /// Gets loading configuration data
        /// </summary>
        public T GetLoadingData<T>(string key, T defaultValue = default)
        {
            if (CurrentLoadingConfig?.GameData?.ContainsKey(key) == true)
            {
                try { return (T)CurrentLoadingConfig.GameData[key]; }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// Updates current scene when scene loads
        /// </summary>
        private void OnSceneLoaded(SceneLoadedEvent evt)
        {
            if (CurrentSession != null)
            {
                CurrentSession.currentScene = evt.SceneName;
            }
        }
        
        #endregion


    }
}
