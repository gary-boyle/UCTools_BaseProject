using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Events.Enums;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Service for displaying temporary notification popups in the UI
    /// Automatically subscribes to relevant game events to show contextual notifications
    /// </summary>
    public interface INotificationService : IGameService
    {
        /// <summary>
        /// Shows a notification popup with the specified message and type
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="type">Type of notification (affects styling)</param>
        /// <param name="duration">How long to display the notification in seconds</param>
        void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f);
        
        /// <summary>
        /// Shows an info notification
        /// </summary>
        void ShowInfo(string message, float duration = 3f);
        
        /// <summary>
        /// Shows a success notification
        /// </summary>
        void ShowSuccess(string message, float duration = 3f);
        
        /// <summary>
        /// Shows a warning notification
        /// </summary>
        void ShowWarning(string message, float duration = 4f);
        
        /// <summary>
        /// Shows an error notification
        /// </summary>
        void ShowError(string message, float duration = 5f);
        
        /// <summary>
        /// Hides any currently displayed notification
        /// </summary>
        void HideNotification();
    }
}