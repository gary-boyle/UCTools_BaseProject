using System;

namespace GameFramework.Utilities
{
    public static class TimeUtilities
    {
        /// <summary>
        /// Formats seconds into time format - overload for double precision
        /// </summary>
        public static string FormatTimeFromSeconds(double totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            
            var timeSpan = TimeSpan.FromSeconds(totalSeconds);
            
            if (timeSpan.TotalDays >= 1)
            {
                return $"{timeSpan.Days}d {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            else
            {
                int totalHours = (int)timeSpan.TotalHours;
                return $"{totalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
        }
        
        /// <summary>
        /// Formats seconds into time format - keep existing float overload for compatibility
        /// </summary>
        public static string FormatTimeFromSeconds(float totalSeconds)
        {
            return FormatTimeFromSeconds((double)totalSeconds);
        }
    }
}