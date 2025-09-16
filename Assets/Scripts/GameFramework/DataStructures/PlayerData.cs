using GameFramework.SaveSystem.Data;
using UnityEngine;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Utilities;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Player data with unique ID for identification and single auto-save per player
    /// </summary>
    [System.Serializable]
    public class PlayerData : ISaveable
    {
        #region ISaveable Implementation
        public string SaveKey => "PlayerData";
        public string TypeName => typeof(PlayerData).Name;
        #endregion

        #region Private Fields
        [SerializeField] private string _uniqueID;      // Unique identifier for this player instance
        [SerializeField] private string _playerName;
        [SerializeField] private Vector3 _position;
        [SerializeField] private Vector3 _rotation;
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
        
        public string PlayerName 
        { 
            get => _playerName; 
            set => _playerName = value; 
        }
        
        public Vector3 Position 
        { 
            get => _position; 
            set => _position = value; 
        }
        
        public Vector3 Rotation 
        { 
            get => _rotation; 
            set => _rotation = value; 
        }
        #endregion

        #region ISaveable Methods
        public object GetSaveData()
        {
            return new PlayerSaveData
            {
                uniqueID = _uniqueID,
                playerName = _playerName,
                Position = new Vector3(_position.x, _position.y, _position.z),
                Rotation = new Vector3(_rotation.x, _rotation.y, _rotation.z)
            };
        }

        public void LoadSaveData(object data)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerData] Cannot load null save data");
                return;
            }

            try
            {
                if (data is PlayerData directData)
                {
                    _uniqueID = directData._uniqueID;
                    _playerName = directData._playerName;
                    _position = directData._position;
                    _rotation = directData._rotation;
                }
                else
                {
                    var json = JsonUtility.ToJson(data);
                    var loadedData = JsonUtility.FromJson<PlayerSaveData>(json);
            
                    _uniqueID = loadedData.uniqueID;
                    _playerName = loadedData.playerName;
                    _position = loadedData.Position;
                    _rotation = loadedData.Rotation;
                }
        
                // IMPORTANT: Always update the public property when loading
                UniqueID = _uniqueID;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerData] Failed to load save data: {ex.Message}");
            }
        }
        #endregion

        #region Constructors
        public PlayerData() 
        {
            GenerateUniqueId();
        }

        public PlayerData(string playerName, Vector3 position, Vector3 rotation)
        {
            GenerateUniqueId();
            this.PlayerName = playerName;
            this.Position = position;
            this.Rotation = rotation;
        }

        /// <summary>
        /// Constructor for loading existing player with known ID
        /// </summary>
        public PlayerData(string uniqueID, string playerName, Vector3 position, Vector3 rotation)
        {
            this.UniqueID = uniqueID;
            this.PlayerName = playerName;
            this.Position = position;
            this.Rotation = rotation;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Generates a new unique ID for this player
        /// </summary>
        private void GenerateUniqueId()
        {
            UniqueID = UniqueIDGenerator.GenerateUniqueID("player");
        }
        #endregion
    }
}
