// using GameFramework.Components.Saveable;
// using UnityEngine;
// using GameFramework.SaveSystem;
//
// namespace GameFramework.SaveSystem.Examples
// {
//     /// <summary>
//     /// Example implementation of SaveableBase showing how to extend the base class
//     /// for custom saving and loading functionality.
//     /// 
//     /// This example demonstrates:
//     /// - Basic save/load implementation
//     /// - Custom initialization logic
//     /// - Extension points usage
//     /// - Custom save data class
//     /// </summary>
//     public class SaveableExample : SaveableBase
//     {
//         #region Private Fields
//         [Header("Saveable Example Settings")]
//         [SerializeField] private float _health = 100f;
//         [SerializeField] private Vector3 _spawnPosition;
//         [SerializeField] private string _playerName = "Player";
//         [SerializeField] private bool _isActive = true;
//         #endregion
//
//         #region Public Properties
//         public float Health
//         {
//             get => _health;
//             set => _health = Mathf.Clamp(value, 0f, 100f);
//         }
//
//         public Vector3 SpawnPosition
//         {
//             get => _spawnPosition;
//             set => _spawnPosition = value;
//         }
//
//         public string PlayerName
//         {
//             get => _playerName;
//             set => _playerName = value;
//         }
//
//         public bool IsActive
//         {
//             get => _isActive;
//             set => _isActive = value;
//         }
//         #endregion
//
//         #region SaveableBase Extension Points
//         protected override void OnAwakeCustom()
//         {
//             // Custom initialization logic
//             if (_spawnPosition == Vector3.zero)
//             {
//                 _spawnPosition = transform.position;
//             }
//             
//             Debug.Log($"[SaveableExample] {gameObject.name} initialized with spawn position: {_spawnPosition}");
//         }
//
//         protected override void OnStartCustom()
//         {
//             // Custom start logic before save system registration
//             Debug.Log($"[SaveableExample] {gameObject.name} starting with health: {_health}");
//         }
//
//         protected override void OnBeforeSave()
//         {
//             // Logic to execute before saving
//             Debug.Log($"[SaveableExample] Preparing to save {gameObject.name}");
//             
//             // Example: Update spawn position to current position before saving
//             _spawnPosition = transform.position;
//         }
//
//         protected override void OnAfterLoad()
//         {
//             // Logic to execute after loading
//             Debug.Log($"[SaveableExample] Finished loading {gameObject.name}");
//             
//             // Example: Apply loaded position to transform
//             transform.position = _spawnPosition;
//             gameObject.SetActive(_isActive);
//         }
//
//         protected override void OnSaveError(System.Exception exception)
//         {
//             // Custom save error handling
//             Debug.LogError($"[SaveableExample] Custom save error handling for {gameObject.name}: {exception.Message}");
//             
//             // Call base implementation for standard error logging
//             base.OnSaveError(exception);
//         }
//
//         protected override void OnLoadError(System.Exception exception)
//         {
//             // Custom load error handling
//             Debug.LogError($"[SaveableExample] Custom load error handling for {gameObject.name}: {exception.Message}");
//             
//             // Call base implementation for standard error logging  
//             base.OnLoadError(exception);
//             
//             // Example: Reset to default values on load error
//             ResetToDefaults();
//         }
//
//         protected override string GetUniqueIdPrefix()
//         {
//             // Custom prefix for unique ID generation
//             return "example";
//         }
//         #endregion
//
//         #region Required SaveableBase Implementation
//         public override object GetSaveData()
//         {
//             // Create and return save data object
//             return new SaveableExampleData
//             {
//                 uniqueID = UniqueID,
//                 health = _health,
//                 spawnPosition = _spawnPosition,
//                 playerName = _playerName,
//                 isActive = _isActive
//             };
//         }
//
//         public override void LoadSaveData(object data)
//         {
//             if (data == null)
//             {
//                 Debug.LogWarning($"[SaveableExample] Cannot load null save data for {gameObject.name}");
//                 return;
//             }
//
//             SaveableExampleData saveData;
//             
//             // Handle different data types
//             if (data is SaveableExampleData directData)
//             {
//                 saveData = directData;
//             }
//             else
//             {
//                 // Try JSON conversion as fallback
//                 try
//                 {
//                     var json = JsonUtility.ToJson(data);
//                     saveData = JsonUtility.FromJson<SaveableExampleData>(json);
//                 }
//                 catch (System.Exception ex)
//                 {
//                     Debug.LogError($"[SaveableExample] Failed to deserialize save data: {ex.Message}");
//                     return;
//                 }
//             }
//
//             // Apply loaded data
//             SetUniqueID(saveData.uniqueID); // Update the UniqueID if it changed
//             _health = saveData.health;
//             _spawnPosition = saveData.spawnPosition;
//             _playerName = saveData.playerName;
//             _isActive = saveData.isActive;
//         }
//         #endregion
//
//         #region Public Methods
//         /// <summary>
//         /// Example method to modify state (for testing save/load)
//         /// </summary>
//         public void TakeDamage(float damage)
//         {
//             Health -= damage;
//             Debug.Log($"[SaveableExample] {gameObject.name} took {damage} damage. Health: {Health}");
//             
//             if (Health <= 0)
//             {
//                 Die();
//             }
//         }
//
//         /// <summary>
//         /// Example method that changes multiple properties
//         /// </summary>
//         public void Respawn()
//         {
//             Health = 100f;
//             transform.position = _spawnPosition;
//             IsActive = true;
//             Debug.Log($"[SaveableExample] {gameObject.name} respawned at {_spawnPosition}");
//         }
//
//         /// <summary>
//         /// Reset to default values
//         /// </summary>
//         public void ResetToDefaults()
//         {
//             _health = 100f;
//             _spawnPosition = transform.position;
//             _playerName = "Player";
//             _isActive = true;
//         }
//
//         private void Die()
//         {
//             IsActive = false;
//             gameObject.SetActive(false);
//             Debug.Log($"[SaveableExample] {gameObject.name} died");
//         }
//         #endregion
//     }
//
//     /// <summary>
//     /// Serializable save data class for SaveableExample.
//     /// This is what gets saved to/loaded from the save file.
//     /// </summary>
//     [System.Serializable]
//     public class SaveableExampleData
//     {
//         public string uniqueID;
//         public float health;
//         public Vector3 spawnPosition;
//         public string playerName;
//         public bool isActive;
//     }
// }
