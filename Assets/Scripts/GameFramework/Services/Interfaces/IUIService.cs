using System.Threading.Tasks;
using GameFramework.UI;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for UI management service
    /// </summary>
    public interface IUIService : IGameService
    {
        Task ShowScreenAsync<T>() where T : UIScreen;
        Task HideScreenAsync<T>() where T : UIScreen;
        Task ShowPopupAsync<T>() where T : UIPopup;
        Task HidePopupAsync<T>() where T : UIPopup;
        void RegisterScreen<T>(T screen) where T : UIScreen;
        void RegisterPopup<T>(T popup) where T : UIPopup;
        T GetScreen<T>() where T : UIScreen;
        T GetPopup<T>() where T : UIPopup;
        public void SetDebugScreenText(string text);
    }
}