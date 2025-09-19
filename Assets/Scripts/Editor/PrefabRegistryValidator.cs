using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem;

namespace GameFramework.Editor
{
    /// <summary>
    /// Automatic validation system for PrefabRegistry.
    /// Runs validation checks when assets are imported/reimported.
    /// </summary>
    public class PrefabRegistryValidator : AssetPostprocessor
    {
        private const string PREFAB_REGISTRY_PATH = "Assets/Resources/PrefabRegistry.asset";
        
        /// <summary>
        /// Called after assets are imported
        /// </summary>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Application.isPlaying) return;

            bool shouldValidate = false;
            
            // Check if any prefabs were imported/deleted/moved
            var allAssets = importedAssets.Concat(deletedAssets).Concat(movedAssets);
            foreach (string assetPath in allAssets)
            {
                if (assetPath.EndsWith(".prefab"))
                {
                    shouldValidate = true;
                    break;
                }
            }

            // Also validate if PrefabRegistry itself was modified
            if (importedAssets.Contains(PREFAB_REGISTRY_PATH) || movedAssets.Contains(PREFAB_REGISTRY_PATH))
            {
                shouldValidate = true;
            }

            if (shouldValidate)
            {
                ValidatePrefabRegistryAsync();
            }
        }

        /// <summary>
        /// Validates PrefabRegistry asynchronously to avoid blocking the UI
        /// </summary>
        private static void ValidatePrefabRegistryAsync()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    ValidatePrefabRegistry();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PrefabRegistryValidator] Error during validation: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// Performs validation of the PrefabRegistry
        /// </summary>
        private static void ValidatePrefabRegistry()
        {
            var prefabRegistry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(PREFAB_REGISTRY_PATH);
            if (prefabRegistry == null) return;

            var allEntries = prefabRegistry.GetAllEntries();
            var issues = new List<string>();
            var toRemove = new List<string>();

            // Validate existing entries
            foreach (var entry in allEntries)
            {
                // Check if prefab still exists
                if (entry.Prefab == null)
                {
                    issues.Add($"Missing prefab reference for GUID: {entry.GUID} (was: {entry.PrefabName})");
                    toRemove.Add(entry.GUID);
                    continue;
                }

                // Check if prefab has SaveableBase component
                var saveableBase = entry.Prefab.GetComponent<SaveableBase>();
                if (saveableBase == null)
                {
                    issues.Add($"Prefab {entry.PrefabName} no longer has a SaveableBase component");
                    toRemove.Add(entry.GUID);
                    continue;
                }

                // Check if GUID matches
                string assetPath = AssetDatabase.GetAssetPath(entry.Prefab);
                string actualGUID = AssetDatabase.AssetPathToGUID(assetPath);
                if (entry.GUID != actualGUID)
                {
                    issues.Add($"GUID mismatch for {entry.PrefabName}: registered={entry.GUID}, actual={actualGUID}");
                    // Don't remove, just warn - might be intentional
                }
            }

            // Clean up invalid entries
            bool madeChanges = false;
            foreach (string guid in toRemove)
            {
                if (prefabRegistry.UnregisterPrefab(guid))
                {
                    madeChanges = true;
                    Debug.Log($"[PrefabRegistryValidator] Removed invalid entry: {guid}");
                }
            }

            // Find unregistered SaveableBase prefabs
            var saveablePrefabs = FindAllSaveablePrefabs();
            var registeredGUIDs = new HashSet<string>(allEntries.Select(e => e.GUID));
            var unregisteredPrefabs = saveablePrefabs.Where(p => !registeredGUIDs.Contains(p.GUID)).ToList();

            if (unregisteredPrefabs.Count > 0)
            {
                // Only show notification for a reasonable number of unregistered prefabs
                if (unregisteredPrefabs.Count <= 5)
                {
                    Debug.LogWarning($"[PrefabRegistryValidator] Found {unregisteredPrefabs.Count} unregistered SaveableBase prefabs. " +
                                   $"Use 'UCTools/Game Framework/Auto-Populate PrefabRegistry' to add them.");
                }
            }

            // Save changes if any were made
            if (madeChanges)
            {
                EditorUtility.SetDirty(prefabRegistry);
                AssetDatabase.SaveAssets();
                
                if (issues.Count > 0)
                {
                    Debug.LogWarning($"[PrefabRegistryValidator] Cleaned up {toRemove.Count} invalid entries from PrefabRegistry");
                }
            }
        }

        /// <summary>
        /// Finds all prefabs in the project that contain SaveableBase components
        /// </summary>
        private static List<SaveablePrefabInfo> FindAllSaveablePrefabs()
        {
            var saveablePrefabs = new List<SaveablePrefabInfo>();
            
            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in prefabGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab != null)
                {
                    var saveableBase = prefab.GetComponent<SaveableBase>();
                    if (saveableBase != null)
                    {
                        saveablePrefabs.Add(new SaveablePrefabInfo
                        {
                            Prefab = prefab,
                            GUID = guid,
                            Name = prefab.name,
                            TypeName = saveableBase.TypeName
                        });
                    }
                }
            }

            return saveablePrefabs;
        }

        /// <summary>
        /// Information about a saveable prefab
        /// </summary>
        private class SaveablePrefabInfo
        {
            public GameObject Prefab;
            public string GUID;
            public string Name;
            public string TypeName;
        }
    }
}
