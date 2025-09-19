using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameFramework.SaveSystem;

namespace GameFramework.Editor
{
    /// <summary>
    /// Editor tool for validating UniqueIDs in SaveableBase objects within the current scene.
    /// Checks for empty and duplicate UniqueIDs and provides options to fix them.
    /// </summary>
    public static class SaveableValidationTool
    {
        private const string MENU_PATH = "UCTools/Game Framework/Saveable/";

        /// <summary>
        /// Validates all SaveableBase objects in the current scene for UniqueID issues
        /// </summary>
        [MenuItem(MENU_PATH + "Validate Scene UniqueIDs", false, 1)]
        public static void ValidateSceneUniqueIds()
        {
            if (!EditorApplication.isPlaying)
            {
                // Get current active scene
                var activeScene = SceneManager.GetActiveScene();
                if (!activeScene.IsValid())
                {
                    EditorUtility.DisplayDialog(
                        "No Active Scene", 
                        "No active scene found. Please open a scene to validate.", 
                        "OK"
                    );
                    return;
                }

                ValidateScene(activeScene);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Cannot Validate During Play", 
                    "UniqueID validation cannot run while the game is playing. Please stop play mode first.", 
                    "OK"
                );
            }
        }

        /// <summary>
        /// Validates all SaveableBase objects across all loaded scenes
        /// </summary>
        [MenuItem(MENU_PATH + "Validate All Loaded Scenes", false, 2)]
        public static void ValidateAllLoadedScenes()
        {
            if (!EditorApplication.isPlaying)
            {
                var loadedScenes = new List<Scene>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        loadedScenes.Add(scene);
                    }
                }

                if (loadedScenes.Count == 0)
                {
                    EditorUtility.DisplayDialog(
                        "No Loaded Scenes", 
                        "No loaded scenes found. Please load at least one scene to validate.", 
                        "OK"
                    );
                    return;
                }

                ValidateMultipleScenes(loadedScenes);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Cannot Validate During Play", 
                    "UniqueID validation cannot run while the game is playing. Please stop play mode first.", 
                    "OK"
                );
            }
        }

        /// <summary>
        /// Shows detailed information about all SaveableBase objects in the current scene
        /// </summary>
        [MenuItem(MENU_PATH + "Show Scene Saveable Info", false, 11)]
        public static void ShowSceneSaveableInfo()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                EditorUtility.DisplayDialog("No Active Scene", "No active scene found.", "OK");
                return;
            }

            var saveables = GetSaveablesInScene(activeScene);
            
            if (saveables.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Scene Saveable Info", 
                    $"Scene '{activeScene.name}' contains no SaveableBase objects.", 
                    "OK"
                );
                return;
            }

            // Group by type
            var typeGroups = saveables.GroupBy(s => s.GetType().Name)
                                    .OrderBy(g => g.Key)
                                    .ToDictionary(g => g.Key, g => g.ToList());

            string message = $"Scene: {activeScene.name}\n" +
                           $"Total SaveableBase Objects: {saveables.Count}\n\n";

            message += "Objects by Type:\n";
            foreach (var kvp in typeGroups)
            {
                message += $"• {kvp.Key}: {kvp.Value.Count} objects\n";
            }

            message += "\nDetailed List:\n";
            foreach (var saveable in saveables.Take(20)) // Limit to first 20 for readability
            {
                string status = string.IsNullOrEmpty(saveable.UniqueID) ? "❌ NO ID" : "✅ Has ID";
                message += $"• {saveable.name} ({saveable.GetType().Name}) - {status}\n";
            }

            if (saveables.Count > 20)
            {
                message += $"... and {saveables.Count - 20} more objects\n";
            }

            EditorUtility.DisplayDialog("Scene Saveable Info", message, "OK");
        }

        #region Private Methods

        /// <summary>
        /// Validates a single scene for UniqueID issues
        /// </summary>
        private static void ValidateScene(Scene scene)
        {
            Debug.Log($"[SaveableValidationTool] Validating scene: {scene.name}");

            var saveables = GetSaveablesInScene(scene);
            var validationResult = ValidateSaveables(saveables, scene.name);

            ShowValidationResults(validationResult);
        }

        /// <summary>
        /// Validates multiple scenes for UniqueID issues
        /// </summary>
        private static void ValidateMultipleScenes(List<Scene> scenes)
        {
            Debug.Log($"[SaveableValidationTool] Validating {scenes.Count} scenes");

            var allSaveables = new List<SaveableBase>();
            var sceneNames = new List<string>();

            foreach (var scene in scenes)
            {
                allSaveables.AddRange(GetSaveablesInScene(scene));
                sceneNames.Add(scene.name);
            }

            var validationResult = ValidateSaveables(allSaveables, string.Join(", ", sceneNames));
            ShowValidationResults(validationResult);
        }

        /// <summary>
        /// Gets all SaveableBase objects in a specific scene
        /// </summary>
        private static List<SaveableBase> GetSaveablesInScene(Scene scene)
        {
            var saveables = new List<SaveableBase>();

            if (!scene.IsValid() || !scene.isLoaded)
                return saveables;

            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var saveableComponents = rootObject.GetComponentsInChildren<SaveableBase>(true);
                saveables.AddRange(saveableComponents);
            }

            return saveables;
        }

        /// <summary>
        /// Validates a list of SaveableBase objects and returns results
        /// </summary>
        private static ValidationResult ValidateSaveables(List<SaveableBase> saveables, string contextName)
        {
            var result = new ValidationResult
            {
                ContextName = contextName,
                TotalObjects = saveables.Count,
                EmptyIds = new List<SaveableBase>(),
                DuplicateGroups = new List<DuplicateGroup>()
            };

            // Check for empty IDs
            foreach (var saveable in saveables)
            {
                if (saveable == null) continue;

                if (string.IsNullOrEmpty(saveable.UniqueID))
                {
                    result.EmptyIds.Add(saveable);
                }
            }

            // Check for duplicate IDs
            var idGroups = saveables.Where(s => s != null && !string.IsNullOrEmpty(s.UniqueID))
                                   .GroupBy(s => s.UniqueID)
                                   .Where(g => g.Count() > 1);

            foreach (var group in idGroups)
            {
                result.DuplicateGroups.Add(new DuplicateGroup
                {
                    UniqueId = group.Key,
                    Objects = group.ToList()
                });
            }

            result.ValidObjects = result.TotalObjects - result.EmptyIds.Count - result.DuplicateGroups.Sum(g => g.Objects.Count);

            return result;
        }

        /// <summary>
        /// Shows validation results to the user
        /// </summary>
        private static void ShowValidationResults(ValidationResult result)
        {
            string message = $"UniqueID Validation Results\n\n";
            message += $"Context: {result.ContextName}\n";
            message += $"Total SaveableBase Objects: {result.TotalObjects}\n";
            message += $"Valid Objects: {result.ValidObjects}\n";
            message += $"Objects with Empty IDs: {result.EmptyIds.Count}\n";
            message += $"Duplicate ID Groups: {result.DuplicateGroups.Count}\n\n";

            bool hasIssues = result.EmptyIds.Count > 0 || result.DuplicateGroups.Count > 0;

            if (!hasIssues)
            {
                message += "✅ All UniqueIDs are valid! No issues found.";
                EditorUtility.DisplayDialog("Validation Complete", message, "OK");
                return;
            }

            // Show details about issues
            if (result.EmptyIds.Count > 0)
            {
                message += "❌ Objects with Empty UniqueIDs:\n";
                foreach (var obj in result.EmptyIds.Take(10))
                {
                    message += $"• {obj.name} ({obj.GetType().Name})\n";
                }
                if (result.EmptyIds.Count > 10)
                {
                    message += $"... and {result.EmptyIds.Count - 10} more\n";
                }
                message += "\n";
            }

            if (result.DuplicateGroups.Count > 0)
            {
                message += "❌ Duplicate UniqueID Groups:\n";
                foreach (var group in result.DuplicateGroups.Take(5))
                {
                    message += $"• ID '{group.UniqueId}' used by {group.Objects.Count} objects:\n";
                    foreach (var obj in group.Objects)
                    {
                        message += $"  - {obj.name} ({obj.GetType().Name})\n";
                    }
                }
                if (result.DuplicateGroups.Count > 5)
                {
                    message += $"... and {result.DuplicateGroups.Count - 5} more duplicate groups\n";
                }
            }

            // Show fix options
            bool shouldFix = EditorUtility.DisplayDialog(
                "Validation Issues Found", 
                message + "\nWould you like to automatically fix these issues?", 
                "Fix Issues", 
                "Just Report"
            );

            if (shouldFix)
            {
                FixValidationIssues(result);
            }

            // Log detailed results to console
            LogValidationResults(result);
        }

        /// <summary>
        /// Attempts to fix validation issues automatically
        /// </summary>
        private static void FixValidationIssues(ValidationResult result)
        {
            int fixedCount = 0;

            EditorUtility.DisplayProgressBar("Fixing UniqueID Issues", "Processing objects...", 0f);

            try
            {
                // Fix empty IDs
                for (int i = 0; i < result.EmptyIds.Count; i++)
                {
                    var obj = result.EmptyIds[i];
                    if (obj != null)
                    {
                        GenerateUniqueIdForObject(obj);
                        fixedCount++;
                    }

                    float progress = (float)i / (result.EmptyIds.Count + result.DuplicateGroups.Sum(g => g.Objects.Count - 1));
                    EditorUtility.DisplayProgressBar("Fixing UniqueID Issues", $"Fixed empty ID for {obj?.name}", progress);
                }

                // Fix duplicates (skip first object in each group, fix the rest)
                foreach (var group in result.DuplicateGroups)
                {
                    for (int i = 1; i < group.Objects.Count; i++) // Skip first object
                    {
                        var obj = group.Objects[i];
                        if (obj != null)
                        {
                            GenerateUniqueIdForObject(obj);
                            fixedCount++;
                        }

                        float progress = (float)(result.EmptyIds.Count + i) / 
                                       (result.EmptyIds.Count + result.DuplicateGroups.Sum(g => g.Objects.Count - 1));
                        EditorUtility.DisplayProgressBar("Fixing UniqueID Issues", $"Fixed duplicate ID for {obj?.name}", progress);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog(
                "Fix Complete", 
                $"Successfully fixed {fixedCount} UniqueID issues.\n\n" +
                "All objects should now have valid, unique IDs.", 
                "OK"
            );

            Debug.Log($"[SaveableValidationTool] Fixed {fixedCount} UniqueID issues");
        }

        /// <summary>
        /// Generates a UniqueID for a SaveableBase object using SerializedObject
        /// </summary>
        private static void GenerateUniqueIdForObject(SaveableBase saveable)
        {
            try
            {
                string prefix = saveable.GetType().Name.ToLower();
                string newUniqueId = $"{prefix}_{System.Guid.NewGuid():N}";

                var serializedObject = new SerializedObject(saveable);
                var uniqueIdProperty = serializedObject.FindProperty("_uniqueID");
                
                if (uniqueIdProperty != null)
                {
                    uniqueIdProperty.stringValue = newUniqueId;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(saveable);
                    
                    Debug.Log($"[SaveableValidationTool] Generated UniqueID for {saveable.name}: {newUniqueId}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveableValidationTool] Error generating UniqueID for {saveable.name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs detailed validation results to the console
        /// </summary>
        private static void LogValidationResults(ValidationResult result)
        {
            Debug.Log($"=== UniqueID Validation Results for {result.ContextName} ===");
            Debug.Log($"Total: {result.TotalObjects}, Valid: {result.ValidObjects}, " +
                     $"Empty: {result.EmptyIds.Count}, Duplicates: {result.DuplicateGroups.Count}");

            if (result.EmptyIds.Count > 0)
            {
                Debug.LogWarning($"Objects with empty UniqueIDs ({result.EmptyIds.Count}):");
                foreach (var obj in result.EmptyIds)
                {
                    Debug.LogWarning($"• {obj.name} ({obj.GetType().Name})", obj);
                }
            }

            if (result.DuplicateGroups.Count > 0)
            {
                Debug.LogWarning($"Duplicate UniqueID groups ({result.DuplicateGroups.Count}):");
                foreach (var group in result.DuplicateGroups)
                {
                    Debug.LogWarning($"• ID '{group.UniqueId}' used by {group.Objects.Count} objects:");
                    foreach (var obj in group.Objects)
                    {
                        Debug.LogWarning($"  - {obj.name} ({obj.GetType().Name})", obj);
                    }
                }
            }

            Debug.Log("=== End Validation Results ===");
        }

        #endregion

        #region Menu Validation

        [MenuItem(MENU_PATH + "Validate Scene UniqueIDs", true)]
        private static bool ValidateSceneUniqueIdsValidation()
        {
            return SceneManager.GetActiveScene().IsValid();
        }

        [MenuItem(MENU_PATH + "Validate All Loaded Scenes", true)]
        private static bool ValidateAllLoadedScenesValidation()
        {
            return SceneManager.sceneCount > 0;
        }

        [MenuItem(MENU_PATH + "Show Scene Saveable Info", true)]
        private static bool ShowSceneSaveableInfoValidation()
        {
            return SceneManager.GetActiveScene().IsValid();
        }

        #endregion

        #region Data Classes

        /// <summary>
        /// Results from a UniqueID validation operation
        /// </summary>
        private class ValidationResult
        {
            public string ContextName;
            public int TotalObjects;
            public int ValidObjects;
            public List<SaveableBase> EmptyIds;
            public List<DuplicateGroup> DuplicateGroups;
        }

        /// <summary>
        /// Group of objects sharing the same UniqueID
        /// </summary>
        private class DuplicateGroup
        {
            public string UniqueId;
            public List<SaveableBase> Objects;
        }

        #endregion
    }
}
