using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for scene management service
    /// </summary>
    public interface ISceneService : IGameService
    {
        Task LoadSceneAsync(string sceneName);
        Task LoadSceneAdditiveAsync(string sceneName);
        Task UnloadSceneAsync(string sceneName);
        string GetCurrentSceneName();
        bool IsSceneLoaded(string sceneName);
    }
}