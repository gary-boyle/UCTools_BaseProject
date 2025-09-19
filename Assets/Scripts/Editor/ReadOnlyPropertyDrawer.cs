using UnityEngine;
using UnityEditor;
using GameFramework.SaveSystem.Data;

namespace GameFramework.Editor
{
    /// <summary>
    /// Custom property drawer for ReadOnly attribute that makes fields uneditable in the Inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Store the original GUI enabled state
            bool wasEnabled = GUI.enabled;
            
            // Disable GUI interaction for this property
            GUI.enabled = false;
            
            // Draw the property normally but disabled
            EditorGUI.PropertyField(position, property, label, true);
            
            // Restore the original GUI enabled state
            GUI.enabled = wasEnabled;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Return the default height for the property
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
