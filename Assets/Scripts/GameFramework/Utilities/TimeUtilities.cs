using System;
using GameFramework.DataStructures;

namespace GameFramework.Utilities
{
    /// <summary>
    /// Static utility class for time formatting and common time operations
    /// Contains pure functions with no dependencies on services or state
    /// Focuses on GameTime only - no session time tracking
    /// </summary>
    public static class TimeUtilities
    {
        #region Time Formatting
        
        /// <summary>
        /// Formats time from seconds to HH:MM:SS string format
        /// </summary>
        /// <param name="seconds">Time in seconds</param>
        /// <returns>Formatted time string (HH:MM:SS)</returns>
        public static string FormatTimeFromSeconds(float seconds)
        {
            if (seconds < 0) seconds = 0; // Ensure non-negative
            
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", 
                timeSpan.Hours, 
                timeSpan.Minutes, 
                timeSpan.Seconds);
        }
        
        /// <summary>
        /// Get formatted saved playtime string for a GameSession
        /// Always uses the saved time data, not current TimeService data
        /// </summary>
        /// <param name="session">GameSession to get playtime from</param>
        /// <returns>Formatted playtime string (HH:MM:SS)</returns>
        public static string GetSavedFormattedPlayTime(GameSession session)
        {
            if (session == null) return "00:00:00";
            return FormatTimeFromSeconds(session.SavedGameTime);
        }
        
        #endregion

        
    }
}
