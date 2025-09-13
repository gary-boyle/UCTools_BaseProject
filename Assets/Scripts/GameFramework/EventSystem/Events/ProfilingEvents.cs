using GameFramework.Services;
using GameFramework.Services.Data;

namespace GameFramework.Events
{
    /// <summary>
    /// Event published when new performance data is available from the profiling service.
    /// </summary>
    public class PerformanceDataUpdatedEvent 
    {
        #region Properties
        
        public PerformanceSnapshot Snapshot { get; set; }
        public bool HasSessionInfo { get; set; }
        public int SessionProgressPercent { get; set; }
        public bool IsSessionComplete { get; set; }
        
        #endregion

        #region Constructors
        
        public PerformanceDataUpdatedEvent(PerformanceSnapshot snapshot)
        {
            Snapshot = snapshot;
            HasSessionInfo = false;
            SessionProgressPercent = 0;
            IsSessionComplete = false;
        }
        
        public PerformanceDataUpdatedEvent(PerformanceSnapshot snapshot, int progressPercent, bool isComplete = false)
        {
            Snapshot = snapshot;
            HasSessionInfo = true;
            SessionProgressPercent = progressPercent;
            IsSessionComplete = isComplete;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Event published when a profiling session is completed.
    /// </summary>
    public class ProfilingSessionCompletedEvent
    {
        #region Properties
        
        public ProfilingSession Session { get; }
        public string FilePath { get; }
        
        #endregion

        #region Constructor
        
        public ProfilingSessionCompletedEvent(ProfilingSession session, string filePath = null)
        {
            Session = session;
            FilePath = filePath;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Event published when a profiling session starts.
    /// </summary>
    public class ProfilingSessionStartedEvent
    {
        #region Properties
        
        public string SessionName { get; }
        public bool IsFrameBased { get; }
        public int TargetFrames { get; }
        public float TargetDuration { get; }
        
        #endregion

        #region Constructor
        
        public ProfilingSessionStartedEvent(string sessionName, bool isFrameBased, int targetFrames = 0, float targetDuration = 0f)
        {
            SessionName = sessionName;
            IsFrameBased = isFrameBased;
            TargetFrames = targetFrames;
            TargetDuration = targetDuration;
        }
        
        #endregion
    }
}
