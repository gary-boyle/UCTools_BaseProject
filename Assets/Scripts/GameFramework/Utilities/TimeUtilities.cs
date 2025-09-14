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
        /// Formats time from seconds to a more detailed string with days if needed
        /// </summary>
        /// <param name="seconds">Time in seconds</param>
        /// <returns>Formatted time string (D days, HH:MM:SS or HH:MM:SS)</returns>
        public static string FormatTimeFromSecondsDetailed(float seconds)
        {
            if (seconds < 0) seconds = 0;
            
            var timeSpan = TimeSpan.FromSeconds(seconds);
            
            if (timeSpan.Days > 0)
            {
                return string.Format("{0} day{1}, {2:D2}:{3:D2}:{4:D2}", 
                    timeSpan.Days,
                    timeSpan.Days == 1 ? "" : "s",
                    timeSpan.Hours, 
                    timeSpan.Minutes, 
                    timeSpan.Seconds);
            }
            
            return FormatTimeFromSeconds(seconds);
        }
        
        #endregion
        
        #region GameSession Time Utilities
        
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
        
        /// <summary>
        /// Creates a PlayTimeInfo from saved GameSession data (fallback when TimeService unavailable)
        /// </summary>
        /// <param name="session">GameSession to create info from</param>
        /// <returns>PlayTimeInfo with saved data</returns>
        public static PlayTimeInfo CreatePlayTimeInfoFromSession(GameSession session)
        {
            if (session == null)
            {
                return new PlayTimeInfo
                {
                    GameTime = 0f,
                    FormattedGameTime = "00:00:00",
                    IsTracking = false
                };
            }
            
            return new PlayTimeInfo
            {
                GameTime = session.SavedGameTime,
                FormattedGameTime = FormatTimeFromSeconds(session.SavedGameTime),
                IsTracking = false // Can't track when using saved data
            };
        }
        
        #endregion
        
        #region Time Calculations
        
        /// <summary>
        /// Calculate elapsed time between two DateTime objects in seconds
        /// </summary>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time (defaults to DateTime.Now)</param>
        /// <returns>Elapsed time in seconds</returns>
        public static float CalculateElapsedSeconds(DateTime startTime, DateTime? endTime = null)
        {
            var end = endTime ?? DateTime.Now;
            var elapsed = end - startTime;
            return (float)elapsed.TotalSeconds;
        }
        
        /// <summary>
        /// Validates that a time value is reasonable (not negative, not impossibly large)
        /// </summary>
        /// <param name="timeInSeconds">Time to validate</param>
        /// <param name="maxHours">Maximum reasonable hours (default 10000 = ~1 year)</param>
        /// <returns>True if time is reasonable</returns>
        public static bool IsReasonableTimeValue(float timeInSeconds, float maxHours = 10000f)
        {
            if (timeInSeconds < 0) return false;
            if (timeInSeconds > maxHours * 3600f) return false; // Convert hours to seconds
            return true;
        }
        
        #endregion
    }
}
