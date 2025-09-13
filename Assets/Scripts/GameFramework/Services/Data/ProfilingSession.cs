using System;
using UnityEngine;

namespace GameFramework.Services.Data
{
    /// <summary>
    /// Complete profiling session results with aggregated statistics
    /// </summary>
    [Serializable]
    public class ProfilingSession
    {
        public string sessionName;
        public DateTime startTime;
        public DateTime endTime;
        public float durationSeconds;
        public int totalFrames;
        
        // Aggregated statistics
        public PerformanceStats fpsStats;
        public PerformanceStats memoryStats;
        public PerformanceStats drawCallStats;
        public PerformanceStats batchStats;
        public PerformanceStats triangleStats;
        public PerformanceStats vertexStats;
        
        // Raw data (optional, for detailed analysis)
        public PerformanceSnapshot[] snapshots;
        
        // Session metadata
        public string deviceInfo;
        public string unityVersion;
        public string gameVersion;
        public string buildConfiguration;
        
        public ProfilingSession(string name)
        {
            sessionName = name ?? $"Session_{DateTime.Now:yyyyMMdd_HHmmss}";
            startTime = DateTime.Now;
            
            // Capture system info
            deviceInfo = $"{SystemInfo.deviceModel} - {SystemInfo.operatingSystem}";
            unityVersion = Application.unityVersion;
            gameVersion = Application.version;
            buildConfiguration = Debug.isDebugBuild ? "Debug" : "Release";
        }
    }
}