using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// ScriptableObject that maintains a registry of all prefabs that can be instantiated at runtime.
    /// Maps prefab GUIDs to prefab assets, eliminating the need for Resources folder usage.
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabRegistry", menuName = "Game Framework/Save System/Prefab Registry", order = 1)]
    public class PrefabRegistry : ScriptableObject
    {
        [Header("Prefab Mappings")]
        [SerializeField] private List<PrefabEntry> _prefabEntries = new List<PrefabEntry>();
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int _totalPrefabs = 0;
        
        private Dictionary<string, GameObject> _prefabLookup;
        private Dictionary<GameObject, string> _reverseLookup;
        
        #region Public Properties
        /// <summary>
        /// Total number of registered prefabs
        /// </summary>
        public int TotalPrefabs => _prefabEntries?.Count ?? 0;
        
        /// <summary>
        /// All registered prefab GUIDs
        /// </summary>
        public string[] RegisteredGUIDs => _prefabLookup?.Keys.ToArray() ?? new string[0];
        #endregion
        
        #region Unity Lifecycle
        private void OnEnable()
        {
            BuildLookupTables();
            _totalPrefabs = TotalPrefabs;
        }
        
        private void OnValidate()
        {
            // Clean up null entries
            if (_prefabEntries != null)
            {
                _prefabEntries.RemoveAll(entry => entry.Prefab == null || string.IsNullOrEmpty(entry.GUID));
            }
            
            BuildLookupTables();
            _totalPrefabs = TotalPrefabs;
        }
        #endregion
        
        #region Lookup Methods
        /// <summary>
        /// Gets a prefab by its GUID
        /// </summary>
        /// <param name="guid">The prefab GUID to look up</param>
        /// <returns>The prefab GameObject, or null if not found</returns>
        public GameObject GetPrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;
                
            EnsureLookupTables();
            
            _prefabLookup.TryGetValue(guid, out GameObject prefab);
            return prefab;
        }
        
        /// <summary>
        /// Gets the GUID for a prefab
        /// </summary>
        /// <param name="prefab">The prefab GameObject</param>
        /// <returns>The GUID string, or null if not found</returns>
        public string GetGUID(GameObject prefab)
        {
            if (prefab == null)
                return null;
                
            EnsureLookupTables();
            
            _reverseLookup.TryGetValue(prefab, out string guid);
            return guid;
        }
        
        /// <summary>
        /// Checks if a GUID is registered
        /// </summary>
        /// <param name="guid">The GUID to check</param>
        /// <returns>True if the GUID is registered</returns>
        public bool IsRegistered(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return false;
                
            EnsureLookupTables();
            return _prefabLookup.ContainsKey(guid);
        }
        
        /// <summary>
        /// Checks if a prefab is registered
        /// </summary>
        /// <param name="prefab">The prefab to check</param>
        /// <returns>True if the prefab is registered</returns>
        public bool IsRegistered(GameObject prefab)
        {
            if (prefab == null)
                return false;
                
            EnsureLookupTables();
            return _reverseLookup.ContainsKey(prefab);
        }
        #endregion
        
        #region Registration Methods
        /// <summary>
        /// Registers a new prefab with a specific GUID
        /// </summary>
        /// <param name="guid">The GUID for the prefab</param>
        /// <param name="prefab">The prefab GameObject</param>
        /// <returns>True if registration was successful</returns>
        public bool RegisterPrefab(string guid, GameObject prefab)
        {
            if (string.IsNullOrEmpty(guid) || prefab == null)
            {
                Debug.LogError("[PrefabRegistry] Cannot register prefab with null/empty GUID or null prefab");
                return false;
            }
            
            // Check for existing GUID
            var existingEntry = _prefabEntries.FirstOrDefault(e => e.GUID == guid);
            if (existingEntry != null)
            {
                if (existingEntry.Prefab == prefab)
                {
                    Debug.LogWarning($"[PrefabRegistry] Prefab {prefab.name} with GUID {guid} is already registered");
                    return true;
                }
                else
                {
                    Debug.LogError($"[PrefabRegistry] GUID {guid} is already registered to different prefab: {existingEntry.Prefab?.name}");
                    return false;
                }
            }
            
            // Check for existing prefab with different GUID
            var existingPrefabEntry = _prefabEntries.FirstOrDefault(e => e.Prefab == prefab);
            if (existingPrefabEntry != null)
            {
                Debug.LogError($"[PrefabRegistry] Prefab {prefab.name} is already registered with GUID: {existingPrefabEntry.GUID}");
                return false;
            }
            
            // Add new entry
            var newEntry = new PrefabEntry
            {
                GUID = guid,
                Prefab = prefab,
                PrefabName = prefab.name
            };
            
            _prefabEntries.Add(newEntry);
            BuildLookupTables();
            
            Debug.Log($"[PrefabRegistry] Registered prefab: {prefab.name} with GUID: {guid}");
            return true;
        }
        
        /// <summary>
        /// Unregisters a prefab by GUID
        /// </summary>
        /// <param name="guid">The GUID to unregister</param>
        /// <returns>True if unregistration was successful</returns>
        public bool UnregisterPrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return false;
                
            var entry = _prefabEntries.FirstOrDefault(e => e.GUID == guid);
            if (entry != null)
            {
                _prefabEntries.Remove(entry);
                BuildLookupTables();
                Debug.Log($"[PrefabRegistry] Unregistered prefab with GUID: {guid}");
                return true;
            }
            
            return false;
        }
        #endregion
        
        #region Utility Methods
        /// <summary>
        /// Gets all prefab entries for debugging/editor purposes
        /// </summary>
        /// <returns>Array of all prefab entries</returns>
        public PrefabEntry[] GetAllEntries()
        {
            return _prefabEntries?.ToArray() ?? new PrefabEntry[0];
        }
        
        /// <summary>
        /// Validates all prefab entries and removes invalid ones
        /// </summary>
        /// <returns>Number of invalid entries removed</returns>
        public int ValidateAndCleanup()
        {
            if (_prefabEntries == null)
                return 0;
                
            int removedCount = _prefabEntries.RemoveAll(entry => 
                entry.Prefab == null || 
                string.IsNullOrEmpty(entry.GUID));
                
            if (removedCount > 0)
            {
                BuildLookupTables();
                Debug.Log($"[PrefabRegistry] Cleaned up {removedCount} invalid prefab entries");
            }
            
            return removedCount;
        }
        #endregion
        
        #region Private Methods
        private void EnsureLookupTables()
        {
            if (_prefabLookup == null || _reverseLookup == null)
                BuildLookupTables();
        }
        
        private void BuildLookupTables()
        {
            _prefabLookup = new Dictionary<string, GameObject>();
            _reverseLookup = new Dictionary<GameObject, string>();
            
            if (_prefabEntries != null)
            {
                foreach (var entry in _prefabEntries)
                {
                    if (entry.Prefab != null && !string.IsNullOrEmpty(entry.GUID))
                    {
                        // Update prefab name in case it changed
                        entry.PrefabName = entry.Prefab.name;
                        
                        // Add to lookup tables
                        if (!_prefabLookup.ContainsKey(entry.GUID))
                        {
                            _prefabLookup[entry.GUID] = entry.Prefab;
                            _reverseLookup[entry.Prefab] = entry.GUID;
                        }
                        else
                        {
                            Debug.LogWarning($"[PrefabRegistry] Duplicate GUID found: {entry.GUID}. Skipping prefab: {entry.Prefab.name}");
                        }
                    }
                }
            }
        }
        #endregion
        
        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to auto-generate GUIDs for prefabs that don't have them
        /// Uses Unity's AssetDatabase to get stable GUIDs
        /// </summary>
        [ContextMenu("Auto-Generate Missing GUIDs")]
        public void AutoGenerateMissingGUIDs()
        {
            if (_prefabEntries == null)
                return;
                
            int generatedCount = 0;
            
            foreach (var entry in _prefabEntries)
            {
                if (entry.Prefab != null && string.IsNullOrEmpty(entry.GUID))
                {
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(entry.Prefab);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        entry.GUID = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
                        entry.PrefabName = entry.Prefab.name;
                        generatedCount++;
                    }
                }
            }
            
            if (generatedCount > 0)
            {
                BuildLookupTables();
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"[PrefabRegistry] Auto-generated {generatedCount} missing GUIDs");
            }
        }
        
        /// <summary>
        /// Editor-only method to validate all GUIDs against Unity's AssetDatabase
        /// </summary>
        [ContextMenu("Validate GUIDs")]
        public void ValidateGUIDs()
        {
            if (_prefabEntries == null)
                return;
                
            int invalidCount = 0;
            
            foreach (var entry in _prefabEntries)
            {
                if (entry.Prefab != null && !string.IsNullOrEmpty(entry.GUID))
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(entry.GUID);
                    GameObject asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    
                    if (asset != entry.Prefab)
                    {
                        Debug.LogWarning($"[PrefabRegistry] GUID mismatch for prefab {entry.Prefab.name}. Expected GUID: {entry.GUID}");
                        invalidCount++;
                    }
                }
            }
            
            Debug.Log($"[PrefabRegistry] Validation complete. Found {invalidCount} GUID mismatches.");
        }
#endif
        #endregion
    }
    
    /// <summary>
    /// Individual entry in the prefab registry
    /// </summary>
    [System.Serializable]
    public class PrefabEntry
    {
        [SerializeField] public string GUID;
        [SerializeField] public GameObject Prefab;
        [SerializeField, ReadOnly] public string PrefabName; // For display purposes
    }
    //
    // /// <summary>
    // /// ReadOnly attribute for inspector display
    // /// </summary>
    // public class ReadOnlyAttribute : PropertyAttribute { }
}
