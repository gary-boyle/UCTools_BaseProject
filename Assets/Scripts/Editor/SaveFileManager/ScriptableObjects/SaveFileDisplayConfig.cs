using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Editor.SaveFileManager.ScriptableObjects
{
     /// <summary>
     /// Configuration ScriptableObject for customizing save file display
     /// Allows adding new fields without modifying the editor code
     /// </summary>
     [CreateAssetMenu(fileName = "SaveFileDisplayConfig", menuName = "Game Framework/Save File Display Config")]
     public class SaveFileDisplayConfig : ScriptableObject
     {
         [System.Serializable]
         public class FieldDisplayConfig
         {
             public string FieldName;
             public string DisplayName;
             public bool IsReadOnly = true;
             
             public FieldDisplayConfig(string fieldName, string displayName, bool isReadOnly = true)
             {
                 FieldName = fieldName;
                 DisplayName = displayName;
                 IsReadOnly = isReadOnly;
             }
         }
         
         [Header("Display Options")]
         [Tooltip("Show all discoverable fields from SaveFileData automatically (useful for debugging and future extensibility)")]
         [SerializeField] public bool ShowDynamicFieldDiscovery = true;
         
        [Header("Runtime Objects Display")]
        [Tooltip("Maximum number of individual runtime objects to display (prevents UI overload)")]
        [SerializeField] public int MaxRuntimeObjectsDisplay = 20;
        
        [Tooltip("Show type-specific fields for runtime objects (e.g., cubeColor, cubeValue)")]
        [SerializeField] public bool ShowRuntimeObjectSpecificFields = true;
        
        [Tooltip("Show detailed transform information for runtime objects")]
        [SerializeField] public bool ShowRuntimeObjectTransforms = true;
        
        [Header("Field Display Configuration")]
        [Tooltip("Configure which fields to display. Use dot notation for nested fields (e.g., 'PlayerData.uniqueID')")]
        [SerializeField] public List<FieldDisplayConfig> DisplayFields = new List<FieldDisplayConfig>
        {
            // Basic file information
            new FieldDisplayConfig("FileName", "File Name"),
            new FieldDisplayConfig("WasAutoSaved", "Auto Save"),
            new FieldDisplayConfig("LastSaveTime", "Last Save Time"),
            
            // Player data (using nested field paths)
            new FieldDisplayConfig("PlayerData.uniqueID", "Player Unique ID"),
            new FieldDisplayConfig("PlayerData.playerName", "Player Name"),
            new FieldDisplayConfig("PlayerData.Position", "Player Position"),
            new FieldDisplayConfig("PlayerData.Rotation", "Player Rotation"),
            
            // Game session data (using nested field paths)
            new FieldDisplayConfig("GameSessionData.uniqueID", "Session Unique ID"),
            new FieldDisplayConfig("GameSessionData.difficulty", "Difficulty"),
            new FieldDisplayConfig("GameSessionData.currentScene", "Current Scene"),
            new FieldDisplayConfig("GameSessionData.gameTime", "Game Time"),
            
            // Runtime objects summary (using nested field paths)
            new FieldDisplayConfig("RuntimeObjects.Count", "Runtime Objects Count"),
        };
     }
}