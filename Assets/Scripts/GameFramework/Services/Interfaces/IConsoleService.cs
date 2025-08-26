using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    public interface IConsoleService
    {
        Task InitializeAsync();
        void Shutdown();
        bool IsConsoleOpen();
        void SetConsoleOpen(bool open);
        void ExecuteCommand(string command);
        void WriteLine(string message);
    }
}