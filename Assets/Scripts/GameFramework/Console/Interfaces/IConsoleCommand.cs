using GameFramework.ConsoleTool.Enums;

namespace GameFramework.ConsoleTool
{
    /// <summary>
    /// Interface for console commands that can be executed from the command line
    /// Provides metadata and execution context for modular command system
    /// </summary>
    public interface IConsoleCommand
    {
        /// <summary>
        /// The command name (without prefix)
        /// </summary>
        string CommandName { get; }
    
        /// <summary>
        /// Description shown in help
        /// </summary>
        string Description { get; }
    
        /// <summary>
        /// Command category for organization
        /// </summary>
        CategoryEnum Category { get; }
    
        /// <summary>
        /// Tag for bulk operations (removal, filtering)
        /// </summary>
        int Tag { get; }
    
        /// <summary>
        /// Execute the command with given arguments
        /// </summary>
        /// <param name="args">Command arguments</param>
        /// <param name="context">Console context for output and state access</param>
        void Execute(string[] args, IConsoleContext context);
    
        /// <summary>
        /// Get usage information for the command
        /// </summary>
        string GetUsage();
    
        /// <summary>
        /// Validate if command can execute with given arguments
        /// </summary>
        bool ValidateArgs(string[] args);
    }
}
