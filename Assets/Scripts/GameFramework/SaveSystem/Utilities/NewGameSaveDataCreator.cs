// using System;
// using GameFramework.SaveSystem.Data;
// using UnityEngine;
//
// namespace GameFramework.SaveSystem.Utilities
// {
//     public static class NewGameSaveDataCreator
//     {
//             /// <summary>
//         /// Creates fresh SaveFileData for a new game with default values
//         /// This allows new games to go through the same loading pipeline as existing saves
//         /// </summary>
//         public static SaveFileData CreateNewGameSaveData(string playerName, string difficulty, string startingScene)
//         {
//             try
//             {
//                 // Validate and set defaults for parameters
//                 string validatedPlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
//                 string validatedDifficulty = string.IsNullOrWhiteSpace(difficulty) ? "Normal" : difficulty;
//                 string validatedStartingScene = string.IsNullOrWhiteSpace(startingScene) ? "GameLevel1" : startingScene;
//
//                 // Create fresh game session data
//                 var gameSessionSaveData = new GameSessionSaveData
//                 {
//                     uniqueID = UniqueIDGenerator.GenerateUniqueID("session"),
//                     difficulty = validatedDifficulty,
//                     currentScene = validatedStartingScene,
//                     gameTime = 0 // Start with zero game time
//                 };
//
//                 // Create fresh player data
//                 var playerSaveData = new PlayerSaveData
//                 {
//                     uniqueID = UniqueIDGenerator.GenerateUniqueID("player"),
//                     playerName = validatedPlayerName,
//                     Position = Vector3.zero, // Default starting position
//                     Rotation = Vector3.zero  // Default starting rotation
//                 };
//
//                 // Create the complete SaveFileData structure
//                 var saveFileData = new SaveFileData
//                 {
//                     GameSessionData = gameSessionSaveData,
//                     PlayerData = playerSaveData
//                 };
//
//                 Debug.Log($"[LoadService] Created new game save data - Player: {validatedPlayerName}, Difficulty: {validatedDifficulty}, Scene: {validatedStartingScene}");
//                 return saveFileData;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[LoadService] Failed to create new game save data: {ex.Message}");
//                 return null;
//             }
//         }
//     }
// }