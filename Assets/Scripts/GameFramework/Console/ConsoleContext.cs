using GameFramework.ConsoleTool.Commands;

namespace GameFramework.ConsoleTool
{
    /// <summary>
    /// Implementation of console context that provides safe access to console functionality
    /// Acts as a facade to prevent commands from directly accessing console internals
    /// </summary>
    public class ConsoleContext : IConsoleContext
    {
        public void WriteLine(string message)
        {
            Console.Write(message);
        }

        public void WriteError(string message)
        {
            Console.Write($"[ERROR] {message}");
        }

        public void WriteWarning(string message)
        {
            Console.Write($"[WARNING] {message}");
        }

        public void EnqueueCommand(string command)
        {
            Console.EnqueueCommandNoHistory(command);
        }

        public void SetWaitFrames(int frames)
        {
            Console.s_PendingCommandsWaitForFrames = frames;
        }

        public void SetWaitForLoad(bool wait)
        {
            Console.s_PendingCommandsWaitForLoad = wait;
        }

        public bool IsCommandRegistered(string commandName)
        {
            return ConsoleCommandRegistry.IsCommandRegistered(commandName);
        }
    }
}