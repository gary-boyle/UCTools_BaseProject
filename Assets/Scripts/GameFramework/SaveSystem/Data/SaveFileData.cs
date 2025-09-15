using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.SaveSystem.Data
{
        [System.Serializable]
    public class SaveFileData
    {
        #region Serialized Fields
        [SerializeField] private long saveTimeTicks;
        [SerializeField] private bool wasAutoSave;
        [SerializeField] private List<SavedObjectEntry> savedObjectsList;
        #endregion

        #region Public Properties
        /// <summary>
        /// Save timestamp as ticks for precise serialization
        /// </summary>
        public long SaveTimeTicks 
        { 
            get => saveTimeTicks; 
            set => saveTimeTicks = value; 
        }
        
        /// <summary>
        /// Indicates if this was an automatic save
        /// </summary>
        public bool WasAutoSave 
        { 
            get => wasAutoSave; 
            set => wasAutoSave = value; 
        }
        
        /// <summary>
        /// Helper property to get DateTime from ticks
        /// </summary>
        public DateTime SaveTime 
        { 
            get => new DateTime(saveTimeTicks);
            set => saveTimeTicks = value.Ticks;
        }

        /// <summary>
        /// Dictionary of saved object data keyed by SaveKey
        /// Note: JsonUtility doesn't serialize Dictionary, so we use a List internally
        /// </summary>
        public Dictionary<string, SavedObjectData> SavedObjects 
        { 
            get 
            {
                var dict = new Dictionary<string, SavedObjectData>();
                if (savedObjectsList != null)
                {
                    foreach (var entry in savedObjectsList)
                    {
                        dict[entry.Key] = entry.Value;
                    }
                }
                return dict;
            }
            set 
            {
                savedObjectsList = new List<SavedObjectEntry>();
                if (value != null)
                {
                    foreach (var kvp in value)
                    {
                        savedObjectsList.Add(new SavedObjectEntry { Key = kvp.Key, Value = kvp.Value });
                    }
                }
            }
        }
        #endregion

        public SaveFileData()
        {
            savedObjectsList = new List<SavedObjectEntry>();
        }
        
        #region Helper Methods
        /// <summary>
        /// Adds a saved object directly to avoid dictionary overhead
        /// </summary>
        public void AddSavedObject(string key, SavedObjectData data)
        {
            if (savedObjectsList == null)
                savedObjectsList = new List<SavedObjectEntry>();

            // Remove existing entry with same key
            for (int i = savedObjectsList.Count - 1; i >= 0; i--)
            {
                if (savedObjectsList[i].Key == key)
                {
                    savedObjectsList.RemoveAt(i);
                    break;
                }
            }

            // Add new entry
            savedObjectsList.Add(new SavedObjectEntry { Key = key, Value = data });
        }

        /// <summary>
        /// Gets a saved object by key
        /// </summary>
        public SavedObjectData GetSavedObject(string key)
        {
            if (savedObjectsList == null) return null;

            foreach (var entry in savedObjectsList)
            {
                if (entry.Key == key)
                    return entry.Value;
            }
            return null;
        }
        #endregion

    }


}