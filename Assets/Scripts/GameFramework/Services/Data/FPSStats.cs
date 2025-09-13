namespace GameFramework.Services.Data
{
    /// <summary>
    /// FPS statistics data structure
    /// </summary>
    [System.Serializable]
    public struct FPSStats
    {
        public float Current;
        public float Average;
        public float Min;
        public float Max;
        public int SampleCount;
    
        public override string ToString()
        {
            return $"FPS - Current: {Current:F1}, Avg: {Average:F1}, Min: {Min:F1}, Max: {Max:F1} ({SampleCount} samples)";
        }
    }

}