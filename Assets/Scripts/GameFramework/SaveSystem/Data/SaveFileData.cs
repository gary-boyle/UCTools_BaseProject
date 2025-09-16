using System;
using UnityEngine;
using System.Reflection;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// Container for all save file data with clean JSON structure
    /// Uses direct field mapping for type safety and clean serialization
    /// Extensible by adding new fields for additional save data types
    /// </summary>
    [System.Serializable]
    public class SaveFileData
    {
        #region Serialized Fields
        [SerializeField] public long SaveTimeTicks;
        [SerializeField] public bool WasAutoSave;
        
        // Core game data (always present)
        [SerializeField] public GameSessionSaveData GameSessionData;
        [SerializeField] public PlayerSaveData PlayerData;
        
        // Future extensions: Add new fields here as needed
        // [SerializeField] public List<EnemySaveData> Enemies;
        // [SerializeField] public InventorySaveData Inventory;
        #endregion

        #region Public Properties
        /// <summary>
        /// Helper property to get DateTime from ticks
        /// </summary>
        public DateTime SaveTime 
        { 
            get => new DateTime(SaveTimeTicks);
            set => SaveTimeTicks = value.Ticks;
        }
        #endregion

        #region Constructor
        public SaveFileData()
        {
            SaveTimeTicks = DateTime.Now.Ticks;
            WasAutoSave = false;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Uses reflection to set save data field by save key
        /// Enables dynamic assignment without hardcoded switch statements
        /// </summary>
        public bool SetSaveData(string saveKey, object data)
        {
            try
            {
                var field = typeof(SaveFileData).GetField(saveKey, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(this, data);
                    Debug.Log($"[SaveFileData] Set save data for key: {saveKey}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[SaveFileData] No field found for save key: {saveKey}. Add a field named '{saveKey}' to SaveFileData class.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileData] Failed to set save data for key {saveKey}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets save data by key using reflection
        /// </summary>
        public T GetSaveData<T>(string saveKey) where T : class
        {
            try
            {
                var field = typeof(SaveFileData).GetField(saveKey, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(this) as T;
                }
                else
                {
                    Debug.LogWarning($"[SaveFileData] No field found for save key: {saveKey}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileData] Failed to get save data for key {saveKey}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates that all expected save data is present
        /// </summary>
        public bool ValidateData()
        {
            bool isValid = true;

            if (GameSessionData == null)
            {
                Debug.LogError("[SaveFileData] GameSessionData is null");
                isValid = false;
            }

            if (PlayerData == null)
            {
                Debug.LogError("[SaveFileData] PlayerData is null");
                isValid = false;
            }

            return isValid;
        }
        #endregion
    }
}
