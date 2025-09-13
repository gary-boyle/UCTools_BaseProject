using System.Threading.Tasks;
using GameFramework.Services.Data;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Interfaces;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Service interface for handling frame-based performance profiling and monitoring.
    /// 
    /// Design:
    /// - Clean separation between real-time data access and session management
    /// - Simple configuration methods for service customization
    /// - Implements IUpdatable for frame-by-frame data collection
    /// 
    /// Pros:
    /// - Clear API contract for profiling operations
    /// - Minimal interface surface area
    /// - Consistent naming and organization
    /// 
    /// Cons:
    /// - Requires implementing service to handle all functionality
    /// </summary>
    public interface IProfilingService : IGameService, IUpdatable
    {
        #region Real-time Data Properties
        
        float CurrentFPS { get; }
        long CurrentMemoryUsage { get; }
        int CurrentDrawCalls { get; }
        int CurrentBatches { get; }
        int CurrentTriangles { get; }
        int CurrentVertices { get; }
        
        #endregion

        #region Session Management
        
        bool IsSessionActive { get; }
        float SessionProgress { get; }

        void StartFrameSession(int frameCount, string sessionName = null);
        void StopSession();
        
        #endregion

        #region Data Access
        
        PerformanceSnapshot GetCurrentSnapshot();
        PerformanceData[] GetHistoricalData(int sampleCount = 60);
        FPSStats GetFPSStats();
        
        #endregion

        #region Configuration
        
        void SetUpdateFrequency(float intervalSeconds);
        void ClearHistory();

        #endregion
    }
}