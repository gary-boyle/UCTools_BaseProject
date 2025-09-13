using System;

namespace GameFramework.Services.Data
{
    /// <summary>
    /// Aggregated statistics for a performance metric
    /// </summary>
    [Serializable]
    public struct PerformanceStats
    {
        public float min;
        public float max;
        public float average;
        public float median;
        public int sampleCount;
        
        public PerformanceStats(float min, float max, float average, float median, int sampleCount)
        {
            this.min = min;
            this.max = max;
            this.average = average;
            this.median = median;
            this.sampleCount = sampleCount;
        }
    }

}