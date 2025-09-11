using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Base class for console commands providing common functionality
    /// Handles basic validation and provides utility methods
    /// </summary>
    public abstract class ConsoleCommandBase : IConsoleCommand
    {
        public abstract string CommandName { get; }
        public abstract string Description { get; }
        public virtual CategoryEnum Category => CategoryEnum.General;
        public virtual int Tag => 0;

        public abstract void Execute(string[] args, IConsoleContext context);
        public abstract string GetUsage();
    
        public virtual bool ValidateArgs(string[] args)
        {
            return true; // Override in derived classes for specific validation
        }
    
        /// <summary>
        /// Utility method to parse integer arguments safely
        /// </summary>
        protected bool TryParseInt(string arg, out int result, IConsoleContext context, string paramName = "value")
        {
            if (int.TryParse(arg, out result))
                return true;
            
            context.WriteError($"Invalid {paramName}: '{arg}'. Expected integer value.");
            return false;
        }
    
        /// <summary>
        /// Utility method to validate argument count
        /// </summary>
        protected bool ValidateArgumentCount(string[] args, int expected, IConsoleContext context)
        {
            if (args.Length == expected)
                return true;
            
            context.WriteError($"Invalid argument count. Expected {expected}, got {args.Length}");
            context.WriteLine(GetUsage());
            return false;
        }
    
        /// <summary>
        /// Utility method to validate argument count within range
        /// </summary>
        protected bool ValidateArgumentCount(string[] args, int min, int max, IConsoleContext context)
        {
            if (args.Length >= min && args.Length <= max)
                return true;
            
            context.WriteError($"Invalid argument count. Expected {min}-{max}, got {args.Length}");
            context.WriteLine(GetUsage());
            return false;
        }
    }

}