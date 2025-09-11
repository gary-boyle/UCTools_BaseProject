using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    public interface IConsoleService : IGameService
    {
        Task InitializeAsync();
        void Shutdown();
        bool IsConsoleOpen();
        void SetConsoleOpen(bool open);
    }
}