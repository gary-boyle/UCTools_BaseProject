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
         
         [Header("Field Display Configuration")]
         [SerializeField] public List<FieldDisplayConfig> DisplayFields = new List<FieldDisplayConfig>
         {
             new FieldDisplayConfig("FileName", "File Name"),
             new FieldDisplayConfig("PlayerName", "Player Name"),
             new FieldDisplayConfig("Difficulty", "Difficulty"),
             new FieldDisplayConfig("CurrentScene", "Current Scene"),
             new FieldDisplayConfig("PlayerLevel", "Player Level"),
             new FieldDisplayConfig("Score", "Score"),
             new FieldDisplayConfig("FormattedPlayTime", "Play Time"),
             new FieldDisplayConfig("FormattedDate", "Last Save"),
             new FieldDisplayConfig("IsAutoSave", "Auto Save")
         };
     }
}