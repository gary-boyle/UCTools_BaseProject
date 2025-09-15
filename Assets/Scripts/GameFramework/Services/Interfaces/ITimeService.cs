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

        // State Properties
        bool IsTrackingGameTime { get; }
        
        // Service Lifecycle
        Task InitializeAsync();
        void Shutdown();
        
        // Time Methods
        string GetFormattedGameTime();
        TimeStatistics GetTimeStatistics();
    }
}