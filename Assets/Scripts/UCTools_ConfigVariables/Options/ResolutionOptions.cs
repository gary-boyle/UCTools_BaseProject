namespace UCTools_ConfigVariables
{
    /// <summary>
    /// Predefined resolution options for dropdown selection
    /// </summary>
    public enum ResolutionOption
    {
        HD_1280x720 = 0,
        Standard_1366x768 = 1,
        FullHD_1920x1080 = 2,
        QHD_2560x1440 = 3,
        UHD_3840x2160 = 4
    }
    
    /// <summary>
    /// Extension methods for ResolutionOption enum
    /// </summary>
    public static class ResolutionOptionExtensions
    {
        private static readonly (int width, int height)[] s_resolutions = new (int, int)[]
        {
            (1280, 720),   // HD_1280x720
            (1366, 768),   // Standard_1366x768
            (1920, 1080),  // FullHD_1920x1080
            (2560, 1440),  // QHD_2560x1440
            (3840, 2160)   // UHD_3840x2160
        };
        
        private static readonly string[] s_displayNames = new string[]
        {
            "1280×720 (HD)",
            "1366×768",
            "1920×1080 (Full HD)",
            "2560×1440 (QHD)",
            "3840×2160 (4K UHD)"
        };
        
        public static (int width, int height) GetResolution(this ResolutionOption option)
        {
            int index = (int)option;
            if (index >= 0 && index < s_resolutions.Length)
                return s_resolutions[index];
            return (1920, 1080); // Default fallback
        }
        
        public static string GetDisplayName(this ResolutionOption option)
        {
            int index = (int)option;
            if (index >= 0 && index < s_displayNames.Length)
                return s_displayNames[index];
            return "1920×1080 (Full HD)"; // Default fallback
        }
        
        public static string[] GetAllDisplayNames()
        {
            return s_displayNames;
        }
    }
}