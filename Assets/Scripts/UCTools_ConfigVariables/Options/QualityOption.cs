namespace UCTools_ConfigVariables
{
    /// <summary>
    /// Predefined quality options for dropdown selection
    /// </summary>
    public enum QualityOption
    {
        Low = 0,
        Medium = 1,
        High = 2,
        VeryHigh = 3
    }
    
    /// <summary>
    /// Extension methods for QualityOption enum
    /// </summary>
    public static class QualityOptionExtensions
    {
        private static readonly string[] s_displayNames = new string[]
        {
            "Low",     // Low = 0
            "Medium",  // Medium = 1
            "High"     // High = 2
        };
        
        private static readonly int[] s_qualityLevels = new int[]
        {
            0,  // Low
            1,  // Medium  
            2   // High
        };
        
        /// <summary>
        /// Get the Unity quality level for this option
        /// </summary>
        public static int GetQualityLevel(this QualityOption option)
        {
            int index = (int)option;
            if (index >= 0 && index < s_qualityLevels.Length)
                return s_qualityLevels[index];
            return 1; // Default to medium
        }
        
        /// <summary>
        /// Get display name for this quality option
        /// </summary>
        public static string GetDisplayName(this QualityOption option)
        {
            int index = (int)option;
            if (index >= 0 && index < s_displayNames.Length)
                return s_displayNames[index];
            return "Medium"; // Default fallback
        }
        
        /// <summary>
        /// Get all display names for dropdown choices
        /// </summary>
        public static string[] GetAllDisplayNames()
        {
            return s_displayNames;
        }
        
        /// <summary>
        /// Get the integer value of the quality option
        /// </summary>
        public static int ToInt(this QualityOption option)
        {
            return (int)option;
        }
        
        /// <summary>
        /// Convert integer back to QualityOption
        /// </summary>
        public static QualityOption FromInt(int value)
        {
            if (System.Enum.IsDefined(typeof(QualityOption), value))
                return (QualityOption)value;
            return QualityOption.Medium; // Default fallback
        }
    }
}
