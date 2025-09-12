using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for graphics service
    /// </summary>
    public interface IGraphicsService : IGameService
    {
        /// <summary>
        /// Get current screen resolution and fullscreen state
        /// </summary>
        (int width, int height, bool fullscreen) GetCurrentResolution();

        /// <summary>
        /// Get current quality level
        /// </summary>
        int GetCurrentQualityLevel();

        /// <summary>
        /// Get current VSync state
        /// </summary>
        bool GetCurrentVSyncEnabled();

        /// <summary>
        /// Check if resolution is supported
        /// </summary>
        bool IsResolutionSupported(int width, int height);
    }
}