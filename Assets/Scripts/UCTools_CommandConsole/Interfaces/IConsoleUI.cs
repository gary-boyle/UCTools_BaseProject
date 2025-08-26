namespace UCTools_CommandConsole
{
    public interface IConsoleUI
    {
        void Init();
        void Shutdown();
        void OutputString(string message);
        bool IsOpen();
        void SetOpen(bool open);
        void ConsoleUpdate();
        void ConsoleLateUpdate();
        void SetPrompt(string prompt);
    }
}