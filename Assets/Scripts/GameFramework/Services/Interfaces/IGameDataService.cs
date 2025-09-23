using GameFramework.Components;
using GameFramework.DataStructures;
using UnityEngine;
using GameFramework.SaveSystem.Services;
using GameFramework.EventSystem.Interfaces;
using GameFramework.SaveSystem.Data;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for managing current game data (GameSessionData and PlayerData)
    /// Provides controlled access to game state with save system integration
    /// Uses EventSystem for change notifications
    /// </summary>
    public interface IGameDataService : IGameService
    {
        #region GameSessionData Access
        /// <summary>
        /// Gets the current GameSessionData (read-only access)
        /// </summary>
        GameSessionData GetGameSessionData();
        
        bool HasActiveSession();
        
        #endregion

        #region PlayerData Access
        /// <summary>
        /// Gets the current PlayerData (read-only access)
        /// </summary>
        PlayerData GetPlayerData();
        void SetPlayerData(PlayerData playerData);

        PlayerSaveData GetPendingPlayerSaveData();
        void ClearPendingPlayerData();
        void ForceDeregisterPlayerData(PlayerData playerData);
        
        #endregion

        #region Scene Resources
        /// <summary>
        /// Gets the main camera reference (read-only access)
        /// </summary>
        Camera GetMainCamera();
        
        /// <summary>
        /// Sets the main camera reference (typically called during scene initialization)
        /// </summary>
        void SetMainCamera(Camera camera);
        
        /// <summary>
        /// Checks if a main camera reference is available
        /// </summary>
        bool HasMainCamera();
        
        /// <summary>
        /// Detects and stores the main camera from the current scene
        /// </summary>
        bool DetectMainCamera();
        
        /// <summary>
        /// Sets the main camera to orthographic or perspective projection
        /// </summary>
        void SetCameraOrthographic(bool orthographic);
        #endregion

        #region Data Lifecycle

        /// <summary>
        /// Loads game data from provided data objects (used by load system)
        /// </summary>
        void LoadGameData(GameSessionData gameSessionData, PlayerSaveData playerSaveData);
        
        #endregion
    }
}
