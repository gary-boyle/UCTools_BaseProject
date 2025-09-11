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
    }
}