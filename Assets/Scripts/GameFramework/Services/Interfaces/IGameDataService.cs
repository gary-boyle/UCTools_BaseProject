using GameFramework.DataStructures;
using UnityEngine;
using GameFramework.SaveSystem.Services;
using GameFramework.EventSystem.Interfaces;

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
        
        #endregion

        #region Data Lifecycle
        /// <summary>
        /// Creates a new game session with default or specified parameters
        /// </summary>
        void StartNewGame(string playerName = "Player", string difficulty = "Normal", string startingScene = "MainMenu");

        /// <summary>
        /// Loads game data from provided data objects (used by load system)
        /// </summary>
        void LoadGameData(GameSessionData gameSessionData, PlayerData playerData);

        #endregion
    }
}
