// using System;
// using GameFramework.Utilities;
//
// namespace GameFramework.DataStructures
// {
//     /// <summary>
//     /// Represents information about a save file for display in the UI
//     /// Handles display formatting for timestamp-based saves and autosaves
//     /// Properly detects autosave files from both filename and session data
//     /// Uses TimeService-based playtime information for accurate display
//     /// </summary>
//     [Serializable]
//     public class SaveFileInfo_old
//     {
//         public string FileName;
//         public string PlayerName;
//         public string Difficulty;
//         public string CurrentScene;
//         public DateTime LastSaveTime;
//         public string FormattedPlayTime;
//         public string FormattedDate;
//         public bool IsAutoSave;
//         public int PlayerLevel;
//         public int Score;
//
//         public SaveFileInfo_old(string fileName, GameSessionData_old sessionDataOld)
//         {
//             FileName = fileName;
//             PlayerName = sessionDataOld.PlayerName;
//             Difficulty = sessionDataOld.Difficulty;
//             CurrentScene = sessionDataOld.CurrentScene;
//             LastSaveTime = sessionDataOld.LastSaveTime;
//     
//             // Use SAVED playtime information for save file display
//             FormattedPlayTime = TimeUtilities.FormatTimeFromSeconds(sessionDataOld?.SavedGameTime ?? 0f);
//             
//             // Check if this is an autosave from both filename and session data
//             IsAutoSave = sessionDataOld.WasAutoSave;
//     
//             // Format display strings
//             FormattedDate = LastSaveTime.ToString("yyyy-MM-dd HH:mm");
//         }
//     }
// }