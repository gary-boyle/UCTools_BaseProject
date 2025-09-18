using GameFramework.SaveSystem.Data;
using UnityEngine;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Utilities;

namespace GameFramework.Components
{
    /// <summary>
    /// Player data MonoBehaviour with unique ID for identification and single auto-save per player.
    /// Attach this to a PlayerPrefab to be instantiated during game loading.
    /// </summary>
    public class PlayerData : MonoBehaviour, ISaveable
    {
        #region ISaveable Implementation
        public string SaveKey => "PlayerData";
        public string TypeName => typeof(PlayerData).Name;
        #endregion

        #region Private Fields
        [SerializeField] private string _uniqueID;      // Unique identifier for this player instance
        [SerializeField] private string _playerName;
        private Vector3 _position;
        private Vector3 _rotation;
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
        
        // public Vector3 Position 
        // { 
        //     get => _position; 
        //     set => _position = value; 
        // }
        //
        // public Vector3 Rotation 
        // { 
        //     get => _rotation; 
        //     set => _rotation = value; 
        // }
        #endregion

        #region ISaveable Methods
        public object GetSaveData()
        {
            // Ensure we have the latest position data before saving
            SyncFromTransform();

            var position = transform.position;
            var rotation = transform.rotation.eulerAngles;
            
            return new PlayerSaveData
            {
                uniqueID = _uniqueID,
                playerName = _playerName,
                Position = new Vector3(position.x, position.y, position.z),
                Rotation = new Vector3(rotation.x, rotation.y, rotation.z)
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
                else if (data is PlayerSaveData saveData)
                {
                    _uniqueID = saveData.uniqueID;
                    _playerName = saveData.playerName;
                    _position = saveData.Position;
                    _rotation = saveData.Rotation;
                }
                else
                {
                    // Try JSON conversion as fallback
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
        }

        private void Awake()
        {
            GenerateUniqueId();
        }
        
        /// <summary>
        /// Forces immediate sync from transform (used before saving)
        /// </summary>
        public void SyncFromTransform()
        {
            if (transform != null)
            {
                _position = transform.position;
                _rotation = transform.rotation.eulerAngles;
            }
        }
        public PlayerData(string playerName, Vector3 position, Vector3 rotation)
        {
            this.PlayerName = playerName;
        }

        /// <summary>
        /// Constructor for loading existing player with known ID
        /// </summary>
        public PlayerData(string uniqueID, string playerName, Vector3 position, Vector3 rotation)
        {
            this.UniqueID = uniqueID;
            this.PlayerName = playerName;
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
