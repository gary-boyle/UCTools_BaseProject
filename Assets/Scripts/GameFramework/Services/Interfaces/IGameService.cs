
using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Base interface for all game services that can be initialized and shut down
    /// </summary>
    public interface IGameService
    {
        public bool IsInitialized { get; }
        public Task InitializeAsync();
        public void Shutdown();
    }
}