using System;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Single performance data point captured at a specific moment
    /// </summary>
    [Serializable]
    public struct PerformanceSnapshot
    {
        public float timestamp;
        public float fps;
        public long memoryBytes;
        public int drawCalls;
        public int batches;
        public int triangles;
        public int vertices;
        public float deltaTime;
        
        public PerformanceSnapshot(float timestamp, float fps, long memoryBytes, 
            int drawCalls, int batches, int triangles, int vertices, float deltaTime)
        {
            this.timestamp = timestamp;
            this.fps = fps;
            this.memoryBytes = memoryBytes;
            this.drawCalls = drawCalls;
            this.batches = batches;
            this.triangles = triangles;
            this.vertices = vertices;
            this.deltaTime = deltaTime;
        }
        
        /// <summary>Get memory usage in megabytes</summary>
        public float MemoryMB => memoryBytes / (1024f * 1024f);
    }
}
