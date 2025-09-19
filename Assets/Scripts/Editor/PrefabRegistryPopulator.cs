using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem;

namespace GameFramework.Editor
{
    /// <summary>
    /// Editor tool for automatically populating PrefabRegistry with SaveableBase prefabs.
    /// Provides menu options under "UCTools/Game Framework" for easy prefab management.
    /// </summary>
    public static class PrefabRegistryPopulator
    {
        private const string MENU_PATH = "UCTools/Game Framework/PrefabRegistry/";
        private const string PREFAB_REGISTRY_PATH = "Assets/Resources/PrefabRegistry.asset";
        
        /// <summary>
        /// Finds and registers all prefabs containing SaveableBase components
        /// </summary>
        [MenuItem(MENU_PATH + "Auto-Populate PrefabRegistry", false, 1)]
        public static void AutoPopulatePrefabRegistry()
        {
            var prefabRegistry = GetOrCreatePrefabRegistry();
            if (prefabRegistry == null)
            {
                Debug.LogError("[PrefabRegistryPopulator] Could not find or create PrefabRegistry!");
                return;
            }

            var saveablePrefabs = FindAllSaveablePrefabs();
            
            if (saveablePrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Saveable Prefabs Found", 
                    "No prefabs with SaveableBase components were found in the project.", 
                    "OK"
                );
                return;
            }

            int addedCount = 0;
            int skippedCount = 0;
            var errors = new List<string>();

            EditorUtility.DisplayProgressBar("Populating PrefabRegistry", "Processing prefabs...", 0f);

            try
            {
                for (int i = 0; i < saveablePrefabs.Count; i++)
                {
                    var prefabInfo = saveablePrefabs[i];
                    float progress = (float)i / saveablePrefabs.Count;
                    
                    EditorUtility.DisplayProgressBar(
                        "Populating PrefabRegistry", 
                        $"Processing {prefabInfo.Name} ({i + 1}/{saveablePrefabs.Count})", 
                        progress
                    );

                    try
                    {
                        if (prefabRegistry.IsRegistered(prefabInfo.GUID))
                        {
                            Debug.Log($"[PrefabRegistryPopulator] Skipping already registered prefab: {prefabInfo.Name}");
                            skippedCount++;
                        }
                        else
                        {
                            bool success = prefabRegistry.RegisterPrefab(prefabInfo.GUID, prefabInfo.Prefab);
                            if (success)
                            {
                                Debug.Log($"[PrefabRegistryPopulator] Registered prefab: {prefabInfo.Name} (GUID: {prefabInfo.GUID})");
                                addedCount++;
                            }
                            else
                            {
                                string error = $"Failed to register prefab: {prefabInfo.Name}";
                                errors.Add(error);
                                Debug.LogError($"[PrefabRegistryPopulator] {error}");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        string error = $"Error processing prefab {prefabInfo.Name}: {ex.Message}";
                        errors.Add(error);
                        Debug.LogError($"[PrefabRegistryPopulator] {error}");
                    }
                }

                // Mark the PrefabRegistry as dirty to ensure changes are saved
                EditorUtility.SetDirty(prefabRegistry);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Show results
            string message = $"PrefabRegistry population complete!\n\n" +
                           $"Added: {addedCount} prefabs\n" +
                           $"Skipped: {skippedCount} prefabs (already registered)\n" +
                           $"Errors: {errors.Count}";

            if (errors.Count > 0)
            {
                message += "\n\nErrors:\n" + string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                    message += $"\n... and {errors.Count - 5} more errors (check Console for details)";
            }

            EditorUtility.DisplayDialog("Auto-Populate Complete", message, "OK");
        }

        /// <summary>
        /// Validates the current PrefabRegistry entries
        /// </summary>
        [MenuItem(MENU_PATH + "Validate PrefabRegistry", false, 2)]
        public static void ValidatePrefabRegistry()
        {
            var prefabRegistry = GetOrCreatePrefabRegistry();
            if (prefabRegistry == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find PrefabRegistry!", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Validating PrefabRegistry", "Checking entries...", 0f);

            var allEntries = prefabRegistry.GetAllEntries();
            var issues = new List<string>();
            var validCount = 0;

            try
            {
                for (int i = 0; i < allEntries.Length; i++)
                {
                    var entry = allEntries[i];
                    float progress = (float)i / allEntries.Length;
                    
                    EditorUtility.DisplayProgressBar(
                        "Validating PrefabRegistry", 
                        $"Checking {entry.PrefabName} ({i + 1}/{allEntries.Length})", 
                        progress
                    );

                    // Check if prefab still exists
                    if (entry.Prefab == null)
                    {
                        issues.Add($"Missing prefab reference for GUID: {entry.GUID} (was: {entry.PrefabName})");
                        continue;
                    }

                    // Check if prefab has SaveableBase component
                    var saveableBase = entry.Prefab.GetComponent<SaveableBase>();
                    if (saveableBase == null)
                    {
                        issues.Add($"Prefab {entry.PrefabName} does not have a SaveableBase component");
                        continue;
                    }

                    // Check if GUID matches
                    string assetPath = AssetDatabase.GetAssetPath(entry.Prefab);
                    string actualGUID = AssetDatabase.AssetPathToGUID(assetPath);
                    if (entry.GUID != actualGUID)
                    {
                        issues.Add($"GUID mismatch for {entry.PrefabName}: registered={entry.GUID}, actual={actualGUID}");
                        continue;
                    }

                    validCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Show results
            string message = $"PrefabRegistry validation complete!\n\n" +
                           $"Total entries: {allEntries.Length}\n" +
                           $"Valid entries: {validCount}\n" +
                           $"Issues found: {issues.Count}";

            if (issues.Count > 0)
            {
                message += "\n\nIssues:\n" + string.Join("\n", issues.Take(10));
                if (issues.Count > 10)
                    message += $"\n... and {issues.Count - 10} more issues (check Console for details)";

                // Log all issues to console for detailed review
                foreach (var issue in issues)
                {
                    Debug.LogWarning($"[PrefabRegistryPopulator] Validation issue: {issue}");
                }
            }

            EditorUtility.DisplayDialog("Validation Complete", message, "OK");
        }

        /// <summary>
        /// Clears all entries from the PrefabRegistry
        /// </summary>
        [MenuItem(MENU_PATH + "Clear PrefabRegistry", false, 3)]
        public static void ClearPrefabRegistry()
        {
            var prefabRegistry = GetOrCreatePrefabRegistry();
            if (prefabRegistry == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find PrefabRegistry!", "OK");
                return;
            }

            var allEntries = prefabRegistry.GetAllEntries();
            
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear PrefabRegistry", 
                $"This will remove all {allEntries.Length} entries from the PrefabRegistry.\n\nThis action cannot be undone. Are you sure?", 
                "Clear All", 
                "Cancel"
            );

            if (!confirmed) return;

            EditorUtility.DisplayProgressBar("Clearing PrefabRegistry", "Removing entries...", 0f);

            try
            {
                int removedCount = 0;
                foreach (var entry in allEntries)
                {
                    if (prefabRegistry.UnregisterPrefab(entry.GUID))
                    {
                        removedCount++;
                    }
                    
                    float progress = (float)removedCount / allEntries.Length;
                    EditorUtility.DisplayProgressBar(
                        "Clearing PrefabRegistry", 
                        $"Removed {removedCount} entries...", 
                        progress
                    );
                }

                EditorUtility.SetDirty(prefabRegistry);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog(
                    "Clear Complete", 
                    $"Successfully removed {removedCount} entries from the PrefabRegistry.", 
                    "OK"
                );
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Shows detailed information about the current PrefabRegistry
        /// </summary>
        [MenuItem(MENU_PATH + "Show PrefabRegistry Info", false, 11)]
        public static void ShowPrefabRegistryInfo()
        {
            var prefabRegistry = GetOrCreatePrefabRegistry();
            if (prefabRegistry == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find PrefabRegistry!", "OK");
                return;
            }

            var allEntries = prefabRegistry.GetAllEntries();
            var saveablePrefabs = FindAllSaveablePrefabs();

            // Group entries by type
            var typeGroups = new Dictionary<string, int>();
            int validEntries = 0;
            int invalidEntries = 0;

            foreach (var entry in allEntries)
            {
                if (entry.Prefab != null)
                {
                    var saveableBase = entry.Prefab.GetComponent<SaveableBase>();
                    if (saveableBase != null)
                    {
                        string typeName = saveableBase.TypeName;
                        if (!typeGroups.ContainsKey(typeName))
                            typeGroups[typeName] = 0;
                        typeGroups[typeName]++;
                        validEntries++;
                    }
                    else
                    {
                        invalidEntries++;
                    }
                }
                else
                {
                    invalidEntries++;
                }
            }

            // Build info message
            string message = $"PrefabRegistry Information\n\n" +
                           $"Registry Entries: {allEntries.Length}\n" +
                           $"Valid Entries: {validEntries}\n" +
                           $"Invalid Entries: {invalidEntries}\n" +
                           $"Available Saveable Prefabs: {saveablePrefabs.Count}\n\n";

            if (typeGroups.Count > 0)
            {
                message += "Registered Types:\n";
                foreach (var kvp in typeGroups.OrderBy(x => x.Key))
                {
                    message += $"- {kvp.Key}: {kvp.Value} prefabs\n";
                }
            }

            // Show unregistered prefabs
            var registeredGUIDs = new HashSet<string>(allEntries.Select(e => e.GUID));
            var unregisteredPrefabs = saveablePrefabs.Where(p => !registeredGUIDs.Contains(p.GUID)).ToList();

            if (unregisteredPrefabs.Count > 0)
            {
                message += $"\nUnregistered Saveable Prefabs ({unregisteredPrefabs.Count}):\n";
                foreach (var prefab in unregisteredPrefabs.Take(10))
                {
                    message += $"- {prefab.Name} ({prefab.TypeName})\n";
                }
                if (unregisteredPrefabs.Count > 10)
                    message += $"... and {unregisteredPrefabs.Count - 10} more\n";
            }

            EditorUtility.DisplayDialog("PrefabRegistry Info", message, "OK");
        }

        /// <summary>
        /// Opens the PrefabRegistry asset in the Inspector
        /// </summary>
        [MenuItem(MENU_PATH + "Select PrefabRegistry", false, 12)]
        public static void SelectPrefabRegistry()
        {
            var prefabRegistry = GetOrCreatePrefabRegistry();
            if (prefabRegistry != null)
            {
                Selection.activeObject = prefabRegistry;
                EditorGUIUtility.PingObject(prefabRegistry);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Could not find PrefabRegistry!", "OK");
            }
        }

        #region Private Methods

        /// <summary>
        /// Gets the existing PrefabRegistry or creates a new one
        /// </summary>
        private static PrefabRegistry GetOrCreatePrefabRegistry()
        {
            var prefabRegistry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(PREFAB_REGISTRY_PATH);
            
            if (prefabRegistry == null)
            {
                // Check if Resources directory exists
                string resourcesDir = "Assets/Resources";
                if (!Directory.Exists(resourcesDir))
                {
                    Directory.CreateDirectory(resourcesDir);
                    AssetDatabase.Refresh();
                }

                // Create new PrefabRegistry
                prefabRegistry = ScriptableObject.CreateInstance<PrefabRegistry>();
                AssetDatabase.CreateAsset(prefabRegistry, PREFAB_REGISTRY_PATH);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"[PrefabRegistryPopulator] Created new PrefabRegistry at {PREFAB_REGISTRY_PATH}");
            }

            return prefabRegistry;
        }

        /// <summary>
        /// Finds all prefabs in the project that contain SaveableBase components
        /// </summary>
        private static List<SaveablePrefabInfo> FindAllSaveablePrefabs()
        {
            var saveablePrefabs = new List<SaveablePrefabInfo>();
            
            // Find all prefab GUIDs in the project
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
                            Path = assetPath,
                            TypeName = saveableBase.TypeName
                        });
                    }
                }
            }

            return saveablePrefabs;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Information about a saveable prefab
        /// </summary>
        private class SaveablePrefabInfo
        {
            public GameObject Prefab;
            public string GUID;
            public string Name;
            public string Path;
            public string TypeName;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates menu items (shows/hides menu options based on context)
        /// </summary>
        [MenuItem(MENU_PATH + "Auto-Populate PrefabRegistry", true)]
        private static bool ValidateAutoPopulate()
        {
            return !Application.isPlaying;
        }

        [MenuItem(MENU_PATH + "Validate PrefabRegistry", true)]
        private static bool ValidateValidation()
        {
            return !Application.isPlaying && GetOrCreatePrefabRegistry() != null;
        }

        [MenuItem(MENU_PATH + "Clear PrefabRegistry", true)]
        private static bool ValidateClear()
        {
            return !Application.isPlaying && GetOrCreatePrefabRegistry() != null;
        }

        [MenuItem(MENU_PATH + "Show PrefabRegistry Info", true)]
        private static bool ValidateShowInfo()
        {
            return GetOrCreatePrefabRegistry() != null;
        }

        [MenuItem(MENU_PATH + "Select PrefabRegistry", true)]
        private static bool ValidateSelect()
        {
            return GetOrCreatePrefabRegistry() != null;
        }

        #endregion
    }
}
