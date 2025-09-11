using System;

namespace GameFramework.Services.Data
{
    /// <summary>
    /// Data structure for time statistics
    /// </summary>
    public struct TimeStatistics
    {
        public float GameTime;
        public float SessionTime;
        public float LevelTime;
        public bool IsTrackingGameTime;
        public bool IsTrackingSessionTime;
        public bool IsPaused;
        public bool IsInPlayingState;
        
        public override string ToString()
        {
            return $"GameTime: {TimeSpan.FromSeconds(GameTime):hh\\:mm\\:ss}, " +
                   $"SessionTime: {TimeSpan.FromSeconds(SessionTime):hh\\:mm\\:ss}, " +
                   $"LevelTime: {TimeSpan.FromSeconds(LevelTime):hh\\:mm\\:ss}, " +
                   $"Tracking: Game={IsTrackingGameTime}, Session={IsTrackingSessionTime}, " +
                   $"Paused: {IsPaused}, PlayingState: {IsInPlayingState}";
        }
    }
}