using UnityEngine;
using UnityEditor;
using GameFramework.SaveSystem.Data;
using System.Linq;
using GameFramework.SaveSystem;

namespace GameFramework.Editor
{
    /// <summary>
    /// Custom editor for PrefabRegistry that provides easy management of prefab mappings.
    /// Includes tools for auto-generating GUIDs, validating entries, and organizing prefabs.
    /// </summary>
    [CustomEditor(typeof(PrefabRegistry))]
    public class PrefabRegistryEditor : UnityEditor.Editor
    {
        private SerializedProperty _prefabEntries;
        private bool _showHelp = false;
        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            _prefabEntries = serializedObject.FindProperty("_prefabEntries");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var registry = (PrefabRegistry)target;

            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab Registry", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This registry maps prefab GUIDs to prefab assets for the new save system. " +
                "Add prefabs that can be instantiated at runtime here.", 
                MessageType.Info
            );

            // Help toggle
            _showHelp = EditorGUILayout.Foldout(_showHelp, "Help & Usage", true);
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "• Add prefabs that need to be instantiated from save data\n" +
                    "• GUIDs are automatically generated from asset paths\n" +
                    "• Use 'Auto-Generate Missing GUIDs' to fill empty GUID fields\n" +
                    "• Use 'Validate GUIDs' to check for inconsistencies\n" +
                    "• Objects using SaveableBaseV2 should have their prefab registered here",
                    MessageType.None
                );
            }

            EditorGUILayout.Space();

            // Statistics
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total Prefabs: {registry.TotalPrefabs}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Valid Entries: {CountValidEntries(registry)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Auto-Generate Missing GUIDs", GUILayout.Height(30)))
            {
                registry.AutoGenerateMissingGUIDs();
                EditorUtility.SetDirty(registry);
            }
            
            if (GUILayout.Button("Validate GUIDs", GUILayout.Height(30)))
            {
                registry.ValidateGUIDs();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Cleanup Invalid Entries", GUILayout.Height(25)))
            {
                int removed = registry.ValidateAndCleanup();
                EditorUtility.SetDirty(registry);
                Debug.Log($"[PrefabRegistryEditor] Cleaned up {removed} invalid entries");
            }
            
            if (GUILayout.Button("Sort by Name", GUILayout.Height(25)))
            {
                SortEntriesByName(registry);
                EditorUtility.SetDirty(registry);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Prefab entries list
            EditorGUILayout.LabelField("Prefab Entries", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            
            for (int i = 0; i < _prefabEntries.arraySize; i++)
            {
                DrawPrefabEntry(i);
            }
            
            EditorGUILayout.EndScrollView();

            // Add new entry button
            EditorGUILayout.Space();
            if (GUILayout.Button("Add New Prefab Entry", GUILayout.Height(30)))
            {
                _prefabEntries.arraySize++;
                var newEntry = _prefabEntries.GetArrayElementAtIndex(_prefabEntries.arraySize - 1);
                newEntry.FindPropertyRelative("GUID").stringValue = "";
                newEntry.FindPropertyRelative("Prefab").objectReferenceValue = null;
                newEntry.FindPropertyRelative("PrefabName").stringValue = "";
            }

            // Bulk operations
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bulk Operations", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Find All SaveableBase Prefabs in Project"))
            {
                FindAndAddSaveablePrefabs(registry);
                EditorUtility.SetDirty(registry);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPrefabEntry(int index)
        {
            var entry = _prefabEntries.GetArrayElementAtIndex(index);
            var guidProp = entry.FindPropertyRelative("GUID");
            var prefabProp = entry.FindPropertyRelative("Prefab");
            var nameProp = entry.FindPropertyRelative("PrefabName");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // Entry header with remove button
            EditorGUILayout.LabelField($"Entry {index + 1}", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(16)))
            {
                _prefabEntries.DeleteArrayElementAtIndex(index);
                return;
            }
            
            EditorGUILayout.EndHorizontal();

            // Prefab field
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab"));
            
            if (EditorGUI.EndChangeCheck() && prefabProp.objectReferenceValue != null)
            {
                // Auto-update GUID and name when prefab is assigned
                var prefab = prefabProp.objectReferenceValue as GameObject;
                string assetPath = AssetDatabase.GetAssetPath(prefab);
                guidProp.stringValue = AssetDatabase.AssetPathToGUID(assetPath);
                nameProp.stringValue = prefab.name;
            }

            // GUID field (read-only display)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("GUID", guidProp.stringValue);
            EditorGUI.EndDisabledGroup();

            // Validation status
            bool isValid = prefabProp.objectReferenceValue != null && !string.IsNullOrEmpty(guidProp.stringValue);
            EditorGUILayout.BeginHorizontal();
            
            var statusColor = isValid ? Color.green : Color.red;
            var previousColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(isValid ? "✓ Valid" : "✗ Invalid", GUILayout.Width(60));
            GUI.color = previousColor;
            
            if (prefabProp.objectReferenceValue != null)
            {
                EditorGUILayout.LabelField($"Name: {((GameObject)prefabProp.objectReferenceValue).name}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private int CountValidEntries(PrefabRegistry registry)
        {
            var entries = registry.GetAllEntries();
            return entries.Count(e => e.Prefab != null && !string.IsNullOrEmpty(e.GUID));
        }

        private void SortEntriesByName(PrefabRegistry registry)
        {
            var entries = registry.GetAllEntries();
            var sortedEntries = entries.Where(e => e.Prefab != null)
                                    .OrderBy(e => e.Prefab.name)
                                    .ToList();

            // Clear and re-add sorted entries
            _prefabEntries.ClearArray();
            for (int i = 0; i < sortedEntries.Count; i++)
            {
                _prefabEntries.InsertArrayElementAtIndex(i);
                var entry = _prefabEntries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("GUID").stringValue = sortedEntries[i].GUID;
                entry.FindPropertyRelative("Prefab").objectReferenceValue = sortedEntries[i].Prefab;
                entry.FindPropertyRelative("PrefabName").stringValue = sortedEntries[i].PrefabName;
            }
        }

        private void FindAndAddSaveablePrefabs(PrefabRegistry registry)
        {
            // Find all prefabs in the project that have SaveableBase components
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int addedCount = 0;

            foreach (string guid in prefabGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab != null && prefab.GetComponent<GameFramework.SaveSystem.SaveableBase>() != null)
                {
                    // Check if it's already registered
                    if (!registry.IsRegistered(prefab))
                    {
                        bool success = registry.RegisterPrefab(guid, prefab);
                        if (success)
                        {
                            addedCount++;
                        }
                    }
                }
            }

            Debug.Log($"[PrefabRegistryEditor] Found and added {addedCount} SaveableBase prefabs");
        }
    }
}
