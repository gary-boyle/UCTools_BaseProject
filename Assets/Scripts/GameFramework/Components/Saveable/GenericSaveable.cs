using GameFramework.SaveSystem;
using UnityEngine;

namespace GameFramework.Components.Saveable
{
    public class GenericSaveable : SaveableBase
    {
        #region Required SaveableBase Implementation
        public override object GetSaveData()
        {
            // Create and return save data object
            return new GenericSaveableData
            {
                uniqueID = UniqueID,
                Position = transform.position,
                Rotation = transform.rotation.eulerAngles
            };
        }

        private void Start()
        {
            base.Start();
            
        }
        public override void LoadSaveData(object data)
        {
            if (data == null)
            {
                Debug.LogWarning($"[SaveableExample] Cannot load null save data for {gameObject.name}");
                return;
            }

            GenericSaveableData saveData;
            
            // Handle different data types
            if (data is GenericSaveableData directData)
            {
                saveData = directData;
            }
            else
            {
                // Try JSON conversion as fallback
                try
                {
                    var json = JsonUtility.ToJson(data);
                    saveData = JsonUtility.FromJson<GenericSaveableData>(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SaveableExample] Failed to deserialize save data: {ex.Message}");
                    return;
                }
            }

            // Apply loaded data
            SetUniqueID(saveData.uniqueID); // Update the UniqueID if it changed
            transform.position = saveData.Position;
            transform.rotation = Quaternion.Euler(saveData.Rotation);
        }
        #endregion
    }
}