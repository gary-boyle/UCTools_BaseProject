using System;
using System.Threading.Tasks;
using GameFramework.UI;
using GameFramework.UI.Screens;
using GameFramework.UI.Popups;
using UnityEngine.UIElements;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Minimal UI Service interface with essential popup management
    /// </summary>
    public interface IUIService : IGameService
    {
        bool IsInitialized { get; }
        
        Task InitializeAsync();
        void Shutdown();
        void Update();
        
        // Screen Management
        Task ShowScreenAsync<T>() where T : UIScreen;
        Task HideScreenAsync<T>() where T : UIScreen;
        T GetScreen<T>() where T : UIScreen;
        
        T GetPopup<T>() where T : UIPopup;

        UIDocument GetUIDocument();
        
        // Essential Popup Management
        Task ShowPopupAsync<T>() where T : UIPopup;
        Task HidePopupAsync<T>() where T : UIPopup;
        Task CloseAllPopupsAsync();
        bool HasOpenPopups();
        UIPopup GetCurrentPopup();

        void RegisterPopup<T>(T popup) where T : UIPopup;
        
        // Popup State Queries
        bool IsCurrentPopup<T>() where T : UIPopup;
        Type GetCurrentPopupType();
        bool IsPopupOpen<T>() where T : UIPopup;

        int GetPopupStackPosition<T>() where T : UIPopup;
        
        // Debug
        void SetDebugPopupText(string text);
    }
}