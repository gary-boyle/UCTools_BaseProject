using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Simplified save service that works directly with GameSession objects
    /// Handles file I/O operations for game sessions
    /// </summary>
    public class SaveService : ISaveService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly string _saveDirectory;
        private const string SAVE_EXTENSION = ".gamesave";
        
        public SaveService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveDirectory = Application.persistentDataPath + "/Saves/";
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[SaveService] Initializing save system...");
            
            // Ensure save directory exists
            if (!System.IO.Directory.Exists(_saveDirectory))
            {
                System.IO.Directory.CreateDirectory(_saveDirectory);
            }
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            IsInitialized = false;
        }
        
        /// <summary>
        /// Saves a game session to file
        /// </summary>
        public async Task<bool> SaveGameSessionAsync(GameSession session, string saveName = null)
        {
            if (session == null) return false;
            
            if (string.IsNullOrEmpty(saveName))
            {
                saveName = $"{session.playerName}_AutoSave_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            }
            
            try
            {
                var json = JsonUtility.ToJson(session, true);
                var filePath = _saveDirectory + saveName + SAVE_EXTENSION;
                
                await System.IO.File.WriteAllTextAsync(filePath, json);
                
                _eventSystem.Publish(new SaveGameEvent());
                Debug.Log($"[SaveService] Game session saved as '{saveName}'");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error saving game session '{saveName}': {e}");
                return false;
            }
        }
        
        /// <summary>
        /// Loads a game session from file
        /// </summary>
        public async Task<GameSession> LoadGameSessionAsync(string saveName)
        {
            var filePath = _saveDirectory + saveName + SAVE_EXTENSION;
            
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveService] Save file '{saveName}' not found");
                return null;
            }
            
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var session = JsonUtility.FromJson<GameSession>(json);
                
                _eventSystem.Publish(new LoadGameEvent());
                Debug.Log($"[SaveService] Game session loaded from '{saveName}'");
                return session;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error loading save '{saveName}': {e}");
                return null;
            }
        }
        
        // Remaining methods stay the same but work with new file extension
        public async Task<string[]> GetSaveFilesAsync()
        {
            return await Task.Run(() =>
            {
                if (!System.IO.Directory.Exists(_saveDirectory))
                    return new string[0];
                    
                var files = System.IO.Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
                var saveNames = new string[files.Length];
                
                for (int i = 0; i < files.Length; i++)
                {
                    saveNames[i] = System.IO.Path.GetFileNameWithoutExtension(files[i]);
                }
                
                return saveNames;
            });
        }
        
        public async Task<bool> DeleteSaveAsync(string saveName)
        {
            var filePath = _saveDirectory + saveName + SAVE_EXTENSION;
            
            if (System.IO.File.Exists(filePath))
            {
                await Task.Run(() => System.IO.File.Delete(filePath));
                Debug.Log($"[SaveService] Deleted save '{saveName}'");
                return true;
            }
            
            return false;
        }
        
        public bool HasAnySaves()
        {
            if (!System.IO.Directory.Exists(_saveDirectory))
                return false;
                
            var files = System.IO.Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
            return files.Length > 0;
        }
        
        public string GetMostRecentSaveName()
        {
            if (!System.IO.Directory.Exists(_saveDirectory))
                return null;
                
            var files = System.IO.Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
            if (files.Length == 0)
                return null;
                
            var mostRecentFile = "";
            var mostRecentTime = DateTime.MinValue;
            
            foreach (var file in files)
            {
                var writeTime = System.IO.File.GetLastWriteTime(file);
                if (writeTime > mostRecentTime)
                {
                    mostRecentTime = writeTime;
                    mostRecentFile = file;
                }
            }
            
            return System.IO.Path.GetFileNameWithoutExtension(mostRecentFile);
        }
    }
}
