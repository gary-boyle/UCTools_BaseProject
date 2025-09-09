namespace GameFramework.ConsoleTool
{
    /// <summary>
    /// Context interface providing access to console functionality
    /// Allows commands to interact with the console system safely without direct access to internal state
    /// Acts as a controlled facade that prevents commands from breaking console integrity
    /// </summary>
    public interface IConsoleContext
    {
        /// <summary>
        /// Writes a standard message to the console output
        /// Message will be displayed in the console UI and logged to Unity's console
        /// </summary>
        /// <param name="message">The message text to display</param>
        void WriteLine(string message);
        
        /// <summary>
        /// Writes an error message to the console output with error formatting
        /// Typically displayed in red or with error prefix to indicate failure
        /// </summary>
        /// <param name="message">The error message to display</param>
        void WriteError(string message);
        
        /// <summary>
        /// Writes a warning message to the console output with warning formatting
        /// Typically displayed in yellow or with warning prefix to indicate caution
        /// </summary>
        /// <param name="message">The warning message to display</param>
        void WriteWarning(string message);
        
        /// <summary>
        /// Adds a command to the execution queue without adding it to command history
        /// Useful for batch operations, scripting, and command chaining
        /// Commands are executed in FIFO order during the next console update
        /// </summary>
        /// <param name="command">The command string to queue (including arguments)</param>
        void EnqueueCommand(string command);
        
        /// <summary>
        /// Sets the number of frames to wait before processing the next queued command
        /// Useful for timing-sensitive operations and ensuring proper frame sequencing
        /// Command execution will pause until the specified number of frames have passed
        /// </summary>
        /// <param name="frames">Number of frames to wait (must be >= 0)</param>
        void SetWaitFrames(int frames);
        
        /// <summary>
        /// Sets a flag to wait for level/scene loading to complete before processing next command
        /// Essential for commands that depend on scene state or need to execute after scene transitions
        /// Command execution will pause until the current level finishes loading
        /// </summary>
        /// <param name="wait">True to wait for level load completion, false to continue normally</param>
        void SetWaitForLoad(bool wait);
        
        /// <summary>
        /// Checks if a command with the specified name is currently registered in the system
        /// Useful for conditional command execution and validation in complex command scripts
        /// Command names are case-insensitive
        /// </summary>
        /// <param name="commandName">The name of the command to check</param>
        /// <returns>True if the command is registered and available for execution</returns>
        bool IsCommandRegistered(string commandName);
    }
}
