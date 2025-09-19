using UnityEditor;
using UnityEngine;
using GameFramework.SaveSystem;

namespace GameFramework.Editor
{
    /// <summary>
    /// Simple post-processor that automatically generates UniqueIDs for SaveableBase objects
    /// when they are added to scenes (via drag/drop, copy/paste, etc.)
    /// </summary>
    [InitializeOnLoad]
    public static class SaveableBasePostProcessor
    {
        static SaveableBasePostProcessor()
        {
            // Subscribe to hierarchy changes
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        /// <summary>
        /// Called whenever the hierarchy changes (objects added, removed, etc.)
        /// </summary>
        private static void OnHierarchyChanged()
        {
            // Only run in edit mode, not during play
            if (Application.isPlaying) return;

            // Find all SaveableBase objects in all scenes
            var saveableObjects = Object.FindObjectsOfType<SaveableBase>();

            foreach (var saveable in saveableObjects)
            {
                // Skip if object is null or destroyed
                if (saveable == null) continue;

                // Skip if object is not in a valid scene (could be prefab asset)
                if (!saveable.gameObject.scene.IsValid()) continue;

                // Generate UniqueID if it's empty
                if (string.IsNullOrEmpty(saveable.UniqueID))
                {
                    GenerateUniqueIdForObject(saveable);
                }
            }
        }

        /// <summary>
        /// Generates a UniqueID for a SaveableBase object using SerializedObject
        /// </summary>
        private static void GenerateUniqueIdForObject(SaveableBase saveable)
        {
            try
            {
                // Generate new unique ID
                string prefix = saveable.GetType().Name.ToLower();
                string newUniqueId = $"{prefix}_{System.Guid.NewGuid():N}";

                // Set the UniqueID using SerializedObject to modify the private field
                var serializedObject = new SerializedObject(saveable);
                var uniqueIdProperty = serializedObject.FindProperty("_uniqueID");
                
                if (uniqueIdProperty != null)
                {
                    uniqueIdProperty.stringValue = newUniqueId;
                    serializedObject.ApplyModifiedProperties();
                    
                    // Mark object as dirty to ensure changes are saved
                    EditorUtility.SetDirty(saveable);
                    
                    Debug.Log($"[SaveableBasePostProcessor] Generated UniqueID for {saveable.name}: {newUniqueId}");
                }
                else
                {
                    Debug.LogWarning($"[SaveableBasePostProcessor] Could not find _uniqueID field on {saveable.name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveableBasePostProcessor] Error generating UniqueID for {saveable.name}: {ex.Message}");
            }
        }
    }
}
