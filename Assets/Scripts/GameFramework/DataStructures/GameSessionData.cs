using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using UnityEngine;
using GameFramework.SaveSystem.Interfaces;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Game session data implementation of ISaveable with unique ID
    /// Stores difficulty, scene, and game time information
    /// </summary>
    [System.Serializable]
    public class GameSessionData : ISaveable
    {
        #region ISaveable Implementation
        //public string UniqueID { get; private set; }
        public string SaveKey => "GameSessionData";
        public string TypeName => typeof(GameSessionData).Name;
        #endregion

        #region Private Fields
        [SerializeField] private string _uniqueID;
        [SerializeField] private string _difficulty = "Normal";
        [SerializeField] private string _currentScene = "MainMenu";
        [SerializeField] private long _gameTime = 0;
        #endregion

        #region Public Properties
        public string UniqueID
        {
            get => _uniqueID;
            private set
            {
                if (string.IsNullOrEmpty(value) || !UniqueIDGenerator.IsValidUniqueID(value))
                {
                    Debug.LogError($"[GameSessionData] Invalid UniqueID assigned: {value}");
                    return;
                }
                _uniqueID = value;
            }
        }
        
        public string Difficulty 
        { 
            get => _difficulty; 
            set => _difficulty = value; 
        }
        
        public string CurrentScene 
        { 
            get => _currentScene; 
            set => _currentScene = value; 
        }
        
        
        /// <summary>
        /// Game time with double precision - TimeService updates this directly
        /// </summary>
        public long GameTime 
        { 
            get => _gameTime; 
            set => _gameTime = value; 
        }
        #endregion

        #region ISaveable Methods
        /// <summary>
        /// Gets serializable data for save operations
        /// Always returns current time since TimeService updates _gameTime directly
        /// </summary>
        public object GetSaveData()
        {
            return new GameSessionSaveData
            {
                uniqueID = _uniqueID,
                difficulty = _difficulty,
                currentScene = _currentScene,
                gameTime = _gameTime
            };
        }

        /// <summary>
        /// Restores state from saved data
        /// </summary>
        public void LoadSaveData(object data)
        {
            if (data == null)
            {
                Debug.LogWarning("[GameSessionData] Cannot load null save data");
                return;
            }

            try
            {
                if (data is GameSessionData directData)
                {
                    _uniqueID = directData._uniqueID;
                    _difficulty = directData._difficulty;
                    _currentScene = directData._currentScene;
                    _gameTime = directData._gameTime;
                }
                else
                {
                    var json = JsonUtility.ToJson(data);
                    var loadedData = JsonUtility.FromJson<GameSessionSaveData>(json);
            
                    _uniqueID = loadedData.uniqueID;
                    _difficulty = loadedData.difficulty;
                    _currentScene = loadedData.currentScene;
                    _gameTime = loadedData.gameTime;
                }
        
                // IMPORTANT: Always update the public property when loading
                UniqueID = _uniqueID;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameSessionData] Failed to load save data: {ex.Message}");
            }
        }
        #endregion

        #region Constructors
        
        /// <summary>
        /// Constructor for loading existing session with known ID
        /// </summary>
        public GameSessionData(string difficulty, string currentScene, long gameTime)
        {
            this.UniqueID = GenerateUniqueId();
            this.Difficulty = difficulty;
            this.CurrentScene = currentScene;
            this.GameTime = gameTime;
        }
        
        public GameSessionData(string gameSessionID, string difficulty, string currentScene, long gameTime)
        {
            this.UniqueID = gameSessionID;
            this.Difficulty = difficulty;
            this.CurrentScene = currentScene;
            this.GameTime = gameTime;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Generates a new unique ID for this game session
        /// </summary>
        private string GenerateUniqueId()
        {
            return UniqueIDGenerator.GenerateUniqueID("session");
        }
        #endregion
    }
}
