using System;
using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for time tracking service that manages game time, session time, and level time
    /// with proper pause handling and state awareness
    /// </summary>
    public interface ITimeService
    {
        bool IsInitialized { get; }
        
        // Time Properties
        float GameTime { get; }        // Time spent in PlayingState when not paused
        float SessionTime { get; }     // Total time since service started (excluding pause)
        float LevelTime { get; }       // Time spent in current level/scene
        
        TimeSpan GameTimeSpan { get; }
        TimeSpan SessionTimeSpan { get; }
        TimeSpan LevelTimeSpan { get; }
        
        // State Properties
        bool IsTrackingGameTime { get; }
        bool IsTrackingSessionTime { get; }
        
        // Service Lifecycle
        Task InitializeAsync();
        void Shutdown();
        
        // Time Methods
        string GetFormattedGameTime();
        string GetFormattedSessionTime();
        string GetFormattedLevelTime();
        void ResetAllTimers();
        void ResetLevelTimer();
        TimeStatistics GetTimeStatistics();
        
        // Events
        event Action<float> OnGameTimeChanged;
        event Action<float> OnSessionTimeChanged;
        event Action<float> OnLevelTimeChanged;
        event Action OnTimersReset;
    }
}