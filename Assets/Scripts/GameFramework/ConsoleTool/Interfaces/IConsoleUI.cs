namespace GameFramework.ConsoleTool.Interfaces
{
    public interface IConsoleUI
    {
        void Init();
        void Shutdown();
        void OutputString(string message);
        void SetOpen(bool open);
        void ConsoleUpdate();
        void ConsoleLateUpdate();
    }
}