using System;
using GameFramework.StateMachine.Interfaces;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Simple centralized pause service interface
    /// Single source of truth for game pause state
    /// </summary>
    public interface IPauseService : IGameService, IUpdatable
    {
        /// <summary>
        /// Is the game currently paused?
        /// </summary>
        bool IsPaused { get; }
        
        /// <summary>
        /// Pause the game
        /// </summary>
        void PauseGame(string reason = null);
        
        /// <summary>
        /// Resume the game
        /// </summary>
        void ResumeGame();
        
        /// <summary>
        /// Toggle pause state
        /// </summary>
        void TogglePause();
        
        /// <summary>
        /// Events for pause state changes
        /// </summary>
        event Action<bool> OnPauseStateChanged;
        event Action<string> OnGamePaused;
        event Action OnGameResumed;
    }
}