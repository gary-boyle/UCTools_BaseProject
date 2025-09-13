using System;

namespace GameFramework.Services.Data
{
    /// <summary>
    /// Performance data optimized for graph display
    /// </summary>
    [Serializable]
    public struct PerformanceData
    {
        public float timestamp;
        public float fps;
        public float memoryMB;
        public int drawCalls;
    
        public PerformanceData(PerformanceSnapshot snapshot)
        {
            timestamp = snapshot.timestamp;
            fps = snapshot.fps;
            memoryMB = snapshot.MemoryMB;
            drawCalls = snapshot.drawCalls;
        }
    }
    
}