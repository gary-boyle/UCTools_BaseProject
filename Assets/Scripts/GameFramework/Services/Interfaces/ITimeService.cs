using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.Services.Data;

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

        // State Properties
        bool IsTrackingGameTime { get; }
        
        // Service Lifecycle
        Task InitializeAsync();
        void Shutdown();
        
        // Time Methods
        string GetFormattedGameTime();
        string GetFormattedSessionTime();
        TimeStatistics GetTimeStatistics();
        void UpdateSessionTimeData(GameSession session);
        string GetSavedFormattedPlayTime(GameSession session);
    }
}