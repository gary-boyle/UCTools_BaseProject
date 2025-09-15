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

        /// <summary>
        /// Sets new GameSessionData and publishes change event
        /// </summary>
        void SetGameSessionData(GameSessionData gameSessionData);

        /// <summary>
        /// Updates specific game session properties
        /// </summary>
        void UpdateGameSession(string difficulty = null, string currentScene = null, float? gameTime = null);
        
        bool HasActiveSession();
        #endregion

        #region PlayerData Access
        /// <summary>
        /// Gets the current PlayerData (read-only access)
        /// </summary>
        PlayerData GetPlayerData();

        /// <summary>
        /// Sets new PlayerData and publishes change event
        /// </summary>
        void SetPlayerData(PlayerData playerData);

        /// <summary>
        /// Updates specific player properties
        /// </summary>
        void UpdatePlayer(string playerName = null, Vector3? position = null, Vector3? rotation = null);
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

        /// <summary>
        /// Resets all game data to defaults
        /// </summary>
        void ResetToDefaults();
        #endregion

        #region Validation
        /// <summary>
        /// Validates that current game data is in a consistent state
        /// </summary>
        bool ValidateGameData();
        #endregion
    }
}
