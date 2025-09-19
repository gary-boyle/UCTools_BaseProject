using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem;

namespace GameFramework.Editor
{
    /// <summary>
    /// Editor window for managing PrefabRegistry with a visual interface.
    /// Provides an easy way to view, add, remove, and validate prefab entries.
    /// </summary>
    public class PrefabRegistryWindow : EditorWindow
    {
        #region Private Fields
        private PrefabRegistry _prefabRegistry;
        private Vector2 _scrollPosition;
        private string _searchFilter = "";
        private bool _showOnlyInvalid = false;
        private bool _showOnlyUnregistered = false;
        
        // Cached data
        private PrefabEntry[] _allEntries;
        private List<SaveablePrefabInfo> _allSaveablePrefabs;
        private bool _needsRefresh = true;
        
        // UI State
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized = false;
        #endregion

        #region Unity Methods
        [MenuItem("UCTools/Game Framework/PrefabRegistry/Open PrefabRegistry Window", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabRegistryWindow>("PrefabRegistry Manager");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _needsRefresh = true;
            RefreshData();
        }

        private void OnGUI()
        {
            InitializeStyles();
            
            if (_prefabRegistry == null)
            {
                DrawNoPrefabRegistryGUI();
                return;
            }

            if (_needsRefresh)
            {
                RefreshData();
                _needsRefresh = false;
            }

            DrawHeader();
            DrawToolbar();
            DrawPrefabList();
        }

        private void OnFocus()
        {
            _needsRefresh = true;
        }
        #endregion

        #region GUI Methods
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25
            };

            _stylesInitialized = true;
        }

        private void DrawNoPrefabRegistryGUI()
        {
            EditorGUILayout.Space(50);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            
            EditorGUILayout.LabelField("No PrefabRegistry Found", _headerStyle);
            EditorGUILayout.Space(20);
            
            EditorGUILayout.HelpBox(
                "A PrefabRegistry asset is required for the save/load system to work properly. " +
                "Click the button below to create one.",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Create PrefabRegistry", _buttonStyle))
            {
                PrefabRegistryPopulator.SelectPrefabRegistry();
                _needsRefresh = true;
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PrefabRegistry Manager", _headerStyle);
            EditorGUILayout.Space(10);
            
            // Registry info
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField($"Registry Entries: {_allEntries?.Length ?? 0}", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField($"Available Prefabs: {_allSaveablePrefabs?.Count ?? 0}", EditorStyles.boldLabel, GUILayout.Width(140));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Select Registry Asset", GUILayout.Width(140)))
            {
                Selection.activeObject = _prefabRegistry;
                EditorGUIUtility.PingObject(_prefabRegistry);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal("toolbar");
            
            // Action buttons
            if (GUILayout.Button("Auto-Populate", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                PrefabRegistryPopulator.AutoPopulatePrefabRegistry();
                _needsRefresh = true;
            }
            
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                PrefabRegistryPopulator.ValidatePrefabRegistry();
                _needsRefresh = true;
            }
            
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                PrefabRegistryPopulator.ClearPrefabRegistry();
                _needsRefresh = true;
            }
            
            GUILayout.FlexibleSpace();
            
            // Filter options
            _showOnlyInvalid = GUILayout.Toggle(_showOnlyInvalid, "Show Only Invalid", EditorStyles.toolbarButton);
            _showOnlyUnregistered = GUILayout.Toggle(_showOnlyUnregistered, "Show Only Unregistered", EditorStyles.toolbarButton);
            
            GUILayout.Space(10);
            
            // Search field
            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchFilter = GUILayout.TextField(_searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
            
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPrefabList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_showOnlyUnregistered)
            {
                DrawUnregisteredPrefabs();
            }
            else
            {
                DrawRegisteredPrefabs();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawRegisteredPrefabs()
        {
            if (_allEntries == null || _allEntries.Length == 0)
            {
                EditorGUILayout.HelpBox("No prefabs registered in PrefabRegistry.", MessageType.Info);
                return;
            }

            var filteredEntries = FilterEntries(_allEntries);
            
            EditorGUILayout.LabelField($"Registered Prefabs ({filteredEntries.Count}/{_allEntries.Length})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            foreach (var entry in filteredEntries)
            {
                DrawRegisteredPrefabEntry(entry);
            }
        }

        private void DrawUnregisteredPrefabs()
        {
            if (_allSaveablePrefabs == null)
            {
                EditorGUILayout.HelpBox("Loading unregistered prefabs...", MessageType.Info);
                return;
            }

            var registeredGUIDs = new HashSet<string>(_allEntries?.Select(e => e.GUID) ?? Enumerable.Empty<string>());
            var unregisteredPrefabs = _allSaveablePrefabs.Where(p => !registeredGUIDs.Contains(p.GUID)).ToList();
            
            if (unregisteredPrefabs.Count == 0)
            {
                EditorGUILayout.HelpBox("All SaveableBase prefabs are registered!", MessageType.Info);
                return;
            }

            var filteredPrefabs = FilterUnregisteredPrefabs(unregisteredPrefabs);
            
            EditorGUILayout.LabelField($"Unregistered Prefabs ({filteredPrefabs.Count}/{unregisteredPrefabs.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            foreach (var prefab in filteredPrefabs)
            {
                DrawUnregisteredPrefabEntry(prefab);
            }
        }

        private void DrawRegisteredPrefabEntry(PrefabEntry entry)
        {
            bool isValid = entry.Prefab != null && entry.Prefab.GetComponent<SaveableBase>() != null;
            
            EditorGUILayout.BeginHorizontal("box");
            
            // Icon and prefab field
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(entry.Prefab, typeof(GameObject), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            
            // Info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(entry.PrefabName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"GUID: {entry.GUID}", EditorStyles.miniLabel);
            
            if (!isValid)
            {
                EditorGUILayout.LabelField("⚠ INVALID", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.red } });
            }
            else if (entry.Prefab != null)
            {
                var saveableBase = entry.Prefab.GetComponent<SaveableBase>();
                if (saveableBase != null)
                {
                    EditorGUILayout.LabelField($"Type: {saveableBase.TypeName}", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            // Actions
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            
            if (GUILayout.Button("Select", GUILayout.Height(20)))
            {
                if (entry.Prefab != null)
                {
                    Selection.activeObject = entry.Prefab;
                    EditorGUIUtility.PingObject(entry.Prefab);
                }
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove", GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Remove Prefab", 
                    $"Remove '{entry.PrefabName}' from PrefabRegistry?", "Remove", "Cancel"))
                {
                    _prefabRegistry.UnregisterPrefab(entry.GUID);
                    EditorUtility.SetDirty(_prefabRegistry);
                    _needsRefresh = true;
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawUnregisteredPrefabEntry(SaveablePrefabInfo prefab)
        {
            EditorGUILayout.BeginHorizontal("box");
            
            // Icon and prefab field
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(prefab.Prefab, typeof(GameObject), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            
            // Info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(prefab.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Type: {prefab.TypeName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"GUID: {prefab.GUID}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            // Actions
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            
            if (GUILayout.Button("Select", GUILayout.Height(20)))
            {
                Selection.activeObject = prefab.Prefab;
                EditorGUIUtility.PingObject(prefab.Prefab);
            }
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Register", GUILayout.Height(20)))
            {
                if (_prefabRegistry.RegisterPrefab(prefab.GUID, prefab.Prefab))
                {
                    EditorUtility.SetDirty(_prefabRegistry);
                    _needsRefresh = true;
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Helper Methods
        private void RefreshData()
        {
            const string PREFAB_REGISTRY_PATH = "Assets/Resources/PrefabRegistry.asset";
            _prefabRegistry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(PREFAB_REGISTRY_PATH);
            
            if (_prefabRegistry != null)
            {
                _allEntries = _prefabRegistry.GetAllEntries();
            }
            
            _allSaveablePrefabs = FindAllSaveablePrefabs();
        }

        private List<PrefabEntry> FilterEntries(PrefabEntry[] entries)
        {
            var filtered = entries.AsEnumerable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filtered = filtered.Where(e => 
                    e.PrefabName.ToLower().Contains(_searchFilter.ToLower()) ||
                    e.GUID.ToLower().Contains(_searchFilter.ToLower()));
            }
            
            // Apply invalid filter
            if (_showOnlyInvalid)
            {
                filtered = filtered.Where(e => 
                    e.Prefab == null || 
                    e.Prefab.GetComponent<SaveableBase>() == null);
            }
            
            return filtered.ToList();
        }

        private List<SaveablePrefabInfo> FilterUnregisteredPrefabs(List<SaveablePrefabInfo> prefabs)
        {
            var filtered = prefabs.AsEnumerable();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filtered = filtered.Where(p => 
                    p.Name.ToLower().Contains(_searchFilter.ToLower()) ||
                    p.TypeName.ToLower().Contains(_searchFilter.ToLower()) ||
                    p.GUID.ToLower().Contains(_searchFilter.ToLower()));
            }
            
            return filtered.ToList();
        }

        private List<SaveablePrefabInfo> FindAllSaveablePrefabs()
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
        #endregion

        #region Helper Classes
        private class SaveablePrefabInfo
        {
            public GameObject Prefab;
            public string GUID;
            public string Name;
            public string TypeName;
        }
        #endregion
    }
}
