using System.Threading.Tasks;
using GameFramework.DataStructures;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Updated save service interface to work with GameSession objects
    /// </summary>
    public interface ISaveService : IGameService
    {
        bool IsInitialized { get; }
        
        Task InitializeAsync();
        void Shutdown();
        
        Task<bool> SaveGameSessionAsync(GameSession session, string saveName = null);
        Task<GameSession> LoadGameSessionAsync(string saveName);
        Task<string[]> GetSaveFilesAsync();
        Task<bool> DeleteSaveAsync(string saveName);
        bool HasAnySaves();
        string GetMostRecentSaveName();
    }
}