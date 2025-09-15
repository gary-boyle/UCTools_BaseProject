// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using GameFramework.Core;
// using GameFramework.DataStructures;
// using GameFramework.EventSystem.Events;
// using GameFramework.EventSystem.Interfaces;
// using GameFramework.Services.Interfaces;
// using GameFramework.StateMachine.Data;
// using GameFramework.StateMachine.Interfaces;
// using UnityEngine;
//
// namespace GameFramework.Services
// {
//     /// <summary>
//     /// GameDataService manages GameSession lifecycle and provides session creation utilities
//     /// 
//     /// Intent: Single source of truth for game state with session creation and management
//     /// 
//     /// Design:
//     /// - Handles all GameSession creation logic (moved from GameSession)
//     /// - Uses TimeService for all time-related operations
//     /// - Manages session lifecycle and save timing
//     /// - Uses EventSystem for communication
//     /// </summary>
//     public class GameDataService_old : IGameDataService
//     {
//         public bool IsInitialized { get; private set; }
//         
//         public GameSessionData CurrentSessionData { get; private set; }
//         public LoadingConfiguration CurrentLoadingConfig { get; set; }
//         
//         private readonly IEventSystem _eventSystem;
//         
//         public GameDataService_old(
//             IEventSystem eventSystem)
//         {
//             _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
//         }
//
//         public async Task InitializeAsync()
//         {
//             if (IsInitialized) return;
//             
//             // Subscribe to scene events to keep session updated
//             _eventSystem.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
//             
//             IsInitialized = true;
//             await Task.CompletedTask;
//         }
//
//         public void Shutdown()
//         {
//             _eventSystem?.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
//             
//             ClearSession();
//             CurrentLoadingConfig = null;
//             IsInitialized = false;
//         }
//         
//         #region GameSession Creation
//         
//         /// <summary>
//         /// Creates a new game session from loading configuration
//         /// TimeService will handle playtime tracking automatically
//         /// Publishes SessionCreatedEvent through EventSystem
//         /// </summary>
//         public void CreateNewGameSession(LoadingConfiguration config)
//         {
//             string difficulty = "Normal";
//             
//             // Extract difficulty from config if available
//             if (config.GameData.ContainsKey("difficulty"))
//             {
//                 difficulty = config.GameData["difficulty"].ToString();
//             }
//             
//             CurrentSessionData = CreateNewGameSession(
//                 config.PlayerName, 
//                 difficulty,
//                 config.SceneName
//             );
//             // Publish session created event through EventSystem
//             _eventSystem.Publish(new SessionCreatedEvent(CurrentSessionData));
//         }
//         
//         /// <summary>
//         /// Creates a new GameSession with specified parameters (moved from GameSession static method)
//         /// </summary>
//         public GameSessionData CreateNewGameSession(string playerName, string difficulty, string startingScene)
//         {
//             var now = DateTime.Now;
//             var session = new GameSessionData
//             {
//                 Difficulty = difficulty,
//                 CurrentScene = startingScene,
//                 SessionStartTime = now,
//                 LastSaveTime = now,
//                 WasAutoSave = false
//             };
//             
//             // Initialize time data
//             session.SetSavedTimeData(0f);
//             
//             return session;
//         }
//         
//         #endregion
//         
//         #region GameSession Management
//         
//         /// <summary>
//         /// Loads existing game session - TimeService handles playtime restoration
//         /// Publishes SessionLoadedEvent through EventSystem
//         /// </summary>
//         public void LoadGameSession(GameSessionData sessionDataOld)
//         {
//             CurrentSessionData = sessionDataOld ?? throw new ArgumentNullException(nameof(sessionDataOld));
//             
//             // Publish session loaded event through EventSystem
//             _eventSystem.Publish(new SessionLoadedEvent(CurrentSessionData));
//         }
//         
//         /// <summary>
//         /// Clears the current session
//         /// Publishes SessionClearedEvent through EventSystem
//         /// </summary>
//         public void ClearSession()
//         {
//             string playerName = null;
//             
//             if (CurrentSessionData != null)
//             {
//                 playerName = CurrentSessionData.PlayerName;
//             }
//             
//             CurrentSessionData = null;
//             
//             // Publish session cleared event through EventSystem
//             _eventSystem.Publish(new SessionClearedEvent(playerName));
//         }
//         
//         #endregion
//         
//         #region Data Access Convenience Methods
//         
//         /// <summary>
//         /// Checks if there's an active game session
//         /// </summary>
//         public bool HasActiveSession() => CurrentSessionData != null;
//         
//         /// <summary>
//         /// Gets loading configuration data
//         /// </summary>
//         public T GetLoadingData<T>(string key, T defaultValue = default)
//         {
//             if (CurrentLoadingConfig?.GameData?.ContainsKey(key) == true)
//             {
//                 try { return (T)CurrentLoadingConfig.GameData[key]; }
//                 catch { return defaultValue; }
//             }
//             return defaultValue;
//         }
//         
//         #endregion
//
//         #region Event Handlers
//         
//         /// <summary>
//         /// Updates current scene when scene loads
//         /// </summary>
//         private void OnSceneLoaded(SceneLoadedEvent evt)
//         {
//             CurrentSessionData?.SetCurrentScene(evt.SceneName);
//         }
//         
//         #endregion
//     }
// }
