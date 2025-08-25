using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;
using IConfigService = GameFramework.Services.Interfaces.IConfigService;
using ISaveService = GameFramework.Services.Interfaces.ISaveService;

namespace GameFramework.Services
{
    /// <summary>
    /// Save service implementation with constructor injection
    /// </summary>
    public class SaveService : ISaveService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly IConfigService _configService;
        private readonly string _saveDirectory;
        private const string SAVE_EXTENSION = ".save";
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public SaveService(IEventSystem eventSystem, IConfigService configService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
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
        
        public async Task SaveGameAsync(string saveName = null)
        {
            if (string.IsNullOrEmpty(saveName))
            {
                saveName = "AutoSave_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            }
            
            var saveData = GatherSaveData();
            var json = JsonUtility.ToJson(saveData, true);
            var filePath = _saveDirectory + saveName + SAVE_EXTENSION;
            
            await System.IO.File.WriteAllTextAsync(filePath, json);
            
            // Publish save event using injected event system
            _eventSystem.Publish<SaveGameEvent>();
            
            Debug.Log($"[SaveService] Game saved as '{saveName}'");
        }
        
        public async Task<bool> LoadGameAsync(string saveName)
        {
            var filePath = _saveDirectory + saveName + SAVE_EXTENSION;
            
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveService] Save file '{saveName}' not found");
                return false;
            }
            
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);
                
                ApplySaveData(saveData);
                
                // Publish load event using injected event system
                _eventSystem.Publish<LoadGameEvent>();
                
                Debug.Log($"[SaveService] Game loaded from '{saveName}'");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error loading save '{saveName}': {e}");
                return false;
            }
        }
        
        public async Task<bool> LoadMostRecentSaveAsync()
        {
            var mostRecent = GetMostRecentSaveName();
            if (!string.IsNullOrEmpty(mostRecent))
            {
                return await LoadGameAsync(mostRecent);
            }
            return false;
        }
        
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
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(files[i]);
                    saveNames[i] = fileName;
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
        
        private SaveData GatherSaveData()
        {
            // Implement your save data gathering logic
            return new SaveData
            {
                timestamp = DateTime.Now.ToString(),
                playerData = new PlayerData(),
                gameStateData = new GameStateData()
            };
        }
        
        private void ApplySaveData(SaveData saveData)
        {
            // Implement your save data application logic
            Debug.Log($"[SaveService] Applying save data from {saveData.timestamp}");
        }
    }
}