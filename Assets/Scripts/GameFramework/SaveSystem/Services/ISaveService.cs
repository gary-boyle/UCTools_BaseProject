using GameFramework.SaveSystem.Services;
using GameFramework.Services.Interfaces;

namespace GameFramework.Services.Interfaces
{
    using System.Threading.Tasks;
    using GameFramework.SaveSystem.Data;
    using GameFramework.EventSystem.Events;

    /// <summary>
    /// Interface for the SaveService, defining its public API.
    /// </summary>
    public interface ISaveService : IGameService
    {
        /// <summary>
        /// Handles save requested events.
        /// </summary>
        /// <param name="saveEvent">The save event to process.</param>
        void OnSaveRequested(SaveRequestedEvent saveEvent);
    }
}