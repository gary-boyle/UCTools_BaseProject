using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for save/load management service
    /// </summary>
    public interface ISaveService : IGameService
    {
        Task SaveGameAsync(string saveName = null);
        Task<bool> LoadGameAsync(string saveName);
        Task<bool> LoadMostRecentSaveAsync();
        Task<string[]> GetSaveFilesAsync();
        Task<bool> DeleteSaveAsync(string saveName);
        bool HasAnySaves();
        string GetMostRecentSaveName();
    }
}