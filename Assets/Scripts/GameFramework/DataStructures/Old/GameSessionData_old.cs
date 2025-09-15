// using System;
// using System.Collections.Generic;
// using GameFramework.SaveSystem.Interfaces;
// using GameFramework.Services.Interfaces;
// using UnityEngine;
//
// namespace GameFramework.DataStructures
// {
//     /// <summary>
//     /// Game session data structure that implements ISaveable for integration with the save system
//     /// Contains all persistent game state data for a single gameplay session
//     /// 
//     /// Intent: Centralized session state that can be saved/loaded through the unified save system
//     /// Design: Implements ISaveable with consistent SaveId and proper data encapsulation
//     /// Pros: Clean integration with save registry, automatic save system participation
//     /// Cons: Slightly more complex due to ISaveable interface requirements
//     /// </summary>
//     [System.Serializable]
//     public class GameSessionData_old 
//     {
//         #region ISaveable Implementation
//         
//         /// <summary>
//         /// Unique identifier for this saveable object
//         /// Uses constant value since there should only be one active session
//         /// </summary>
//         public string SaveId => "GameSession";
//         
//         /// <summary>
//         /// Gets the current save data as a serializable object
//         /// Returns a deep copy to prevent external modification
//         /// </summary>
//         public object GetSaveData()
//         {
//             // Return a copy of this object for serialization
//             // This ensures the save system gets a snapshot at save time
//             return CreateSaveDataCopy();
//         }
//         
//         /// <summary>
//         /// Restores the object's state from save data
//         /// Validates and applies loaded data safely
//         /// </summary>
//         public void LoadSaveData(object saveData)
//         {
//             if (saveData is GameSessionData_old loadedSession)
//             {
//                 ApplyLoadedData(loadedSession);
//             }
//             else
//             {
//                 Debug.LogError($"[GameSessionData] Invalid save data type: {saveData?.GetType()}");
//             }
//         }
//         
//         #endregion
//         
//         #region Core Session Data
//         
//         [SerializeField] private string _playerName;
//         [SerializeField] private string _difficulty;
//         [SerializeField] private string _currentScene;
//         [SerializeField] private DateTime _sessionStartTime;
//         [SerializeField] private DateTime _lastSaveTime;
//         [SerializeField] private bool _wasAutoSave;
//         [SerializeField] private float _savedGameTime;
//         [SerializeField] private bool _hasSavedTimeData;
//         
//         // Public properties for access
//         public string PlayerName 
//         { 
//             get => _playerName; 
//             set => _playerName = value; 
//         }
//         
//         public string Difficulty 
//         { 
//             get => _difficulty; 
//             set => _difficulty = value; 
//         }
//         
//         public string CurrentScene 
//         { 
//             get => _currentScene; 
//             set => _currentScene = value; 
//         }
//         
//         public DateTime SessionStartTime 
//         { 
//             get => _sessionStartTime; 
//             set => _sessionStartTime = value; 
//         }
//         
//         public DateTime LastSaveTime 
//         { 
//             get => _lastSaveTime; 
//             set => _lastSaveTime = value; 
//         }
//         
//         public bool WasAutoSave 
//         { 
//             get => _wasAutoSave; 
//             set => _wasAutoSave = value; 
//         }
//         
//         public float SavedGameTime 
//         { 
//             get => _savedGameTime; 
//             private set => _savedGameTime = value; 
//         }
//         
//         public bool HasSavedTimeData 
//         { 
//             get => _hasSavedTimeData; 
//             private set => _hasSavedTimeData = value; 
//         }
//         
//         #endregion
//         
//         #region Constructors
//         
//         /// <summary>
//         /// Default constructor for serialization
//         /// </summary>
//         public GameSessionData_old()
//         {
//             // Initialize with default values
//             _sessionStartTime = DateTime.Now;
//             _lastSaveTime = DateTime.Now;
//             _savedGameTime = 0f;
//             _hasSavedTimeData = false;
//             _wasAutoSave = false;
//         }
//         
//         /// <summary>
//         /// Constructor for creating new game sessions
//         /// </summary>
//         public GameSessionData_old(string playerName, string difficulty, string startingScene)
//         {
//             _playerName = playerName;
//             _difficulty = difficulty;
//             _currentScene = startingScene;
//             _sessionStartTime = DateTime.Now;
//             _lastSaveTime = DateTime.Now;
//             _savedGameTime = 0f;
//             _hasSavedTimeData = false;
//             _wasAutoSave = false;
//         }
//         
//         #endregion
//         
//         #region Public Methods
//         
//         /// <summary>
//         /// Updates the current scene
//         /// </summary>
//         public void SetCurrentScene(string sceneName)
//         {
//             _currentScene = sceneName;
//         }
//         
//         /// <summary>
//         /// Sets the saved time data
//         /// </summary>
//         public void SetSavedTimeData(float gameTime)
//         {
//             _savedGameTime = gameTime;
//             _hasSavedTimeData = true;
//         }
//         
//         /// <summary>
//         /// Clears saved time data
//         /// </summary>
//         public void ClearSavedTimeData()
//         {
//             _savedGameTime = 0f;
//             _hasSavedTimeData = false;
//         }
//         
//         #endregion
//         
//         #region Private ISaveable Helper Methods
//         
//         /// <summary>
//         /// Creates a deep copy of this session for save data
//         /// </summary>
//         private GameSessionData_old CreateSaveDataCopy()
//         {
//             var copy = new GameSessionData_old
//             {
//                 _playerName = this._playerName,
//                 _difficulty = this._difficulty,
//                 _currentScene = this._currentScene,
//                 _sessionStartTime = this._sessionStartTime,
//                 _lastSaveTime = this._lastSaveTime,
//                 _wasAutoSave = this._wasAutoSave,
//                 _savedGameTime = this._savedGameTime,
//                 _hasSavedTimeData = this._hasSavedTimeData
//             };
//             
//             return copy;
//         }
//         
//         /// <summary>
//         /// Applies loaded data to this session
//         /// </summary>
//         private void ApplyLoadedData(GameSessionData_old loadedSession)
//         {
//             _playerName = loadedSession._playerName;
//             _difficulty = loadedSession._difficulty;
//             _currentScene = loadedSession._currentScene;
//             _sessionStartTime = loadedSession._sessionStartTime;
//             _lastSaveTime = loadedSession._lastSaveTime;
//             _wasAutoSave = loadedSession._wasAutoSave;
//             _savedGameTime = loadedSession._savedGameTime;
//             _hasSavedTimeData = loadedSession._hasSavedTimeData;
//         }
//         
//         #endregion
//     }
// }
