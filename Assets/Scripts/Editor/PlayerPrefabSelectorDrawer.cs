using UnityEngine;
using UnityEditor;
using GameFramework.Components.Controllers;
using GameFramework.Components.Controllers.Enum;

namespace GameFramework.Editor
{
    /// <summary>
    /// Custom PropertyDrawer for PlayerPrefabSelector to display a clean dropdown in the inspector
    /// with automatic prefab loading and validation feedback.
    /// </summary>
    [CustomPropertyDrawer(typeof(PlayerPrefabSelector))]
    public class PlayerPrefabSelectorDrawer : PropertyDrawer
    {
        #region Private Fields
        private const float LineHeight = 18f;
        private const float Spacing = 2f;
        private const float ButtonHeight = 20f;
        #endregion

        #region PropertyDrawer Implementation
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get serialized properties
            var selectedTypeProperty = property.FindPropertyRelative("_selectedPlayerType");
            var fpsProperty = property.FindPropertyRelative("_fpsPrefab");
            var thirdPersonProperty = property.FindPropertyRelative("_thirdPersonPrefab");
            var rtsProperty = property.FindPropertyRelative("_rtsPrefab");
            var isometricProperty = property.FindPropertyRelative("_isometricPrefab");

            // Calculate rects for different elements
            var dropdownRect = new Rect(position.x, position.y, position.width, LineHeight);
            var buttonRect = new Rect(position.x, position.y + LineHeight + Spacing, position.width * 0.5f, ButtonHeight);
            var statusRect = new Rect(position.x + position.width * 0.5f + 5f, position.y + LineHeight + Spacing, position.width * 0.5f - 5f, ButtonHeight);
            
            // Draw the main dropdown for player type selection
            EditorGUI.BeginChangeCheck();
            var currentType = (PlayerPrefabType)selectedTypeProperty.enumValueIndex;
            var newType = (PlayerPrefabType)EditorGUI.EnumPopup(dropdownRect, "Player Prefab Type", currentType);
            
            if (EditorGUI.EndChangeCheck())
            {
                selectedTypeProperty.enumValueIndex = (int)newType;
            }

            // Draw load prefabs button
            if (GUI.Button(buttonRect, "Auto-Assign Prefabs"))
            {
                LoadPrefabs(fpsProperty, thirdPersonProperty, rtsProperty, isometricProperty);
            }

            // Draw prefab status
            DrawPrefabStatus(statusRect, fpsProperty, thirdPersonProperty, rtsProperty, isometricProperty);

            // Draw foldout for prefab references (optional for debugging)
            var foldoutRect = new Rect(position.x, position.y + (LineHeight + Spacing) * 2 + ButtonHeight, position.width, LineHeight);
            
            if (property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, "Prefab References (Debug)", true))
            {
                EditorGUI.indentLevel++;
                
                var prefabY = foldoutRect.y + LineHeight + Spacing;
                var prefabRect = new Rect(position.x, prefabY, position.width, LineHeight);
                
                EditorGUI.PropertyField(prefabRect, fpsProperty, new GUIContent("FPS Prefab"));
                prefabRect.y += LineHeight + Spacing;
                
                EditorGUI.PropertyField(prefabRect, thirdPersonProperty, new GUIContent("Third Person Prefab"));
                prefabRect.y += LineHeight + Spacing;
                
                EditorGUI.PropertyField(prefabRect, rtsProperty, new GUIContent("RTS Prefab"));
                prefabRect.y += LineHeight + Spacing;
                
                EditorGUI.PropertyField(prefabRect, isometricProperty, new GUIContent("Isometric Prefab"));
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Base height: dropdown + button row + spacing
            float height = (LineHeight + Spacing) * 2 + ButtonHeight;

            // Add height for foldout if expanded
            if (property.isExpanded)
            {
                // Foldout header + 4 prefab fields
                height += (LineHeight + Spacing) * 5;
            }

            return height;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Automatically finds and assigns prefabs based on naming convention.
        /// </summary>
        private void LoadPrefabs(SerializedProperty fpsProperty, SerializedProperty thirdPersonProperty, 
                                SerializedProperty rtsProperty, SerializedProperty isometricProperty)
        {
            // Auto-assign prefabs by searching the project
            fpsProperty.objectReferenceValue = LoadPrefabByName("Player_FPS");
            thirdPersonProperty.objectReferenceValue = LoadPrefabByName("Player_3rdPerson");
            rtsProperty.objectReferenceValue = LoadPrefabByName("Player_RTS");
            isometricProperty.objectReferenceValue = LoadPrefabByName("Player_Isometric");

            // Mark as dirty to save changes
            if (fpsProperty.serializedObject.targetObject is MonoBehaviour target)
            {
                EditorUtility.SetDirty(target);
            }

            Debug.Log("[PlayerPrefabSelector] Prefabs auto-assigned in editor");
        }

        /// <summary>
        /// Loads a prefab by name from the project assets.
        /// </summary>
        private GameObject LoadPrefabByName(string prefabName)
        {
            // Search for the prefab in the project
            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:GameObject");
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                
                if (prefab != null && prefab.name == prefabName)
                {
                    return prefab;
                }
            }

            Debug.LogWarning($"[PlayerPrefabSelector] Could not find prefab: {prefabName}");
            return null;
        }

        /// <summary>
        /// Draws the prefab assignment status indicator.
        /// </summary>
        private void DrawPrefabStatus(Rect statusRect, SerializedProperty fpsProperty, SerializedProperty thirdPersonProperty, 
                                    SerializedProperty rtsProperty, SerializedProperty isometricProperty)
        {
            int loadedCount = 0;
            int totalCount = 4;

            if (fpsProperty.objectReferenceValue != null) loadedCount++;
            if (thirdPersonProperty.objectReferenceValue != null) loadedCount++;
            if (rtsProperty.objectReferenceValue != null) loadedCount++;
            if (isometricProperty.objectReferenceValue != null) loadedCount++;

            // Set color based on loading status
            var originalColor = GUI.color;
            if (loadedCount == totalCount)
            {
                GUI.color = Color.green;
            }
            else if (loadedCount > 0)
            {
                GUI.color = Color.yellow;
            }
            else
            {
                GUI.color = Color.red;
            }

            string statusText = $"Assigned: {loadedCount}/{totalCount}";
            GUI.Label(statusRect, statusText);
            GUI.color = originalColor;
        }
        #endregion
    }
}
