using GameFramework.SaveSystem.Data;
using UnityEngine;
using GameFramework.SaveSystem.Interfaces;
using UnityEngine.Serialization;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Game session data implementation of ISaveable
    /// Stores difficulty, scene, and game time information
    /// </summary>
    [System.Serializable]
    public class GameSessionData : ISaveable
    {
        #region ISaveable Implementation
        public string SaveKey => "GameSessionData";
        public string TypeName => typeof(GameSessionData).Name;
        #endregion

        #region Private Fields
        [SerializeField] private string _difficulty = "Normal";
        [SerializeField] private string _currentScene = "MainMenu";
        [SerializeField] private float _gameTime = 0f;
        #endregion

        #region Public Properties
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
        
        public float GameTime 
        { 
            get => _gameTime; 
            set => _gameTime = value; 
        }
        #endregion

        #region ISaveable Methods
        /// <summary>
        /// Gets serializable data for save operations
        /// Returns anonymous object matching JSON structure requirements
        /// </summary>
        public object GetSaveData()
        {
            return new GameSessionSaveData
            {
                difficulty = Difficulty,
                currentScene = CurrentScene,
                gameTime = GameTime
            };
        }

        /// <summary>
        /// Restores state from saved data
        /// Handles dynamic object deserialization safely
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
                // Handle JsonUtility deserialization
                if (data is GameSessionData directData)
                {
                    _difficulty = directData._difficulty;
                    _currentScene = directData._currentScene;
                    _gameTime = directData._gameTime;
                }
                else
                {
                    // Handle dynamic object from JSON
                    var json = JsonUtility.ToJson(data);
                    var loadedData = JsonUtility.FromJson<GameSessionData>(json);
                    
                    _difficulty = loadedData._difficulty;
                    _currentScene = loadedData._currentScene;
                    _gameTime = loadedData._gameTime;
                }
                
                Debug.Log($"[GameSessionData] Loaded save data - Difficulty: {_difficulty}, Scene: {_currentScene}, Time: {_gameTime}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameSessionData] Failed to load save data: {ex.Message}");
            }
        }
        #endregion

        #region Constructors
        public GameSessionData() { }

        public GameSessionData(string difficulty, string currentScene, float gameTime)
        {
            this._difficulty = difficulty;
            this._currentScene = currentScene;
            this._gameTime = gameTime;
        }
        
        

        #endregion
    }
}
