namespace GameFramework.DataStructures
{
    /// <summary>
    /// Playtime information structure for UI display and debugging
    /// </summary>
    public struct PlayTimeInfo
    {
        public float GameTime;
        public string FormattedGameTime;
        public bool IsTracking;
        
        public override string ToString()
        {
            return $"PlayTime[Game: {FormattedGameTime}, Tracking: {IsTracking}]";
        }
    }

}