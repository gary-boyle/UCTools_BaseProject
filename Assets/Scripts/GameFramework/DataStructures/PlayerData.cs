using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Interfaces;
using UnityEngine;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Player data implementation of ISaveable
    /// Stores player name, position, and rotation information
    /// </summary>
    [System.Serializable]
    public class PlayerData : ISaveable
    {
        #region ISaveable Implementation
        public string SaveKey => "PlayerData";
        public string TypeName => typeof(PlayerData).Name;
        #endregion

        #region Private Fields
        [SerializeField] private string playerName = "";
        [SerializeField] private Vector3 position = Vector3.zero;
        [SerializeField] private Vector3 rotation = Vector3.zero;
        #endregion

        #region Public Properties
        public string PlayerName 
        { 
            get => playerName; 
            set => playerName = value; 
        }
        
        public Vector3 Position 
        { 
            get => position; 
            set => position = value; 
        }
        
        public Vector3 Rotation 
        { 
            get => rotation; 
            set => rotation = value; 
        }
        #endregion

        #region ISaveable Methods
        /// <summary>
        /// Gets serializable data for save operations
        /// Creates nested structure matching JSON requirements
        /// </summary>
        public object GetSaveData()
        {
            return new PlayerSaveData
            {
                playerName = PlayerName,
                Position = Position,
                Rotation = Rotation
            };
        }

        /// <summary>
        /// Restores state from saved data
        /// Handles nested object structure safely
        /// </summary>
        public void LoadSaveData(object data)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerData] Cannot load null save data");
                return;
            }

            try
            {
                // Handle JsonUtility deserialization
                if (data is PlayerData directData)
                {
                    playerName = directData.playerName;
                    position = directData.position;
                    rotation = directData.rotation;
                }
                else
                {
                    // Handle dynamic object from JSON
                    var json = JsonUtility.ToJson(data);
                    var loadedData = JsonUtility.FromJson<PlayerData>(json);
                    
                    playerName = loadedData.playerName;
                    position = loadedData.position;
                    rotation = loadedData.rotation;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerData] Failed to load save data: {ex.Message}");
            }
        }
        #endregion

        #region Constructors
        public PlayerData() { }

        public PlayerData(string playerName, Vector3 position, Vector3 rotation)
        {
            this.playerName = playerName;
            this.position = position;
            this.rotation = rotation;
        }
        #endregion
    }
    
}
