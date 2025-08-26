using System;
using System.IO;
using UCTools_CommandConsole.Enums;

namespace UCTools_CommandConsole
{
    /// <summary>
    /// Execute command that runs console commands from a text file
    /// Supports silent execution and error handling for batch operations
    /// </summary>
    public class ExecCommand : ConsoleCommandBase
    {
        public override string CommandName => "exec";
        public override string Description => "Execute commands from file";
        public override CategoryEnum Category => CategoryEnum.Scripting;

        public override void Execute(string[] args, IConsoleContext context)
        {
            bool silent = false;
            string filename = "";

            // Parse arguments
            if (args.Length == 1)
            {
                filename = args[0];
            }
            else if (args.Length == 2 && args[0] == "-s")
            {
                silent = true;
                filename = args[1];
            }
            else
            {
                context.WriteError(GetUsage());
                return;
            }

            ExecuteFile(filename, silent, context);
        }

        public override string GetUsage()
        {
            return "Usage: exec [-s] <filename>\n" +
                   "  exec file.txt     - Execute commands from file.txt\n" +
                   "  exec -s file.txt  - Execute silently (suppress file not found errors)";
        }

        public override bool ValidateArgs(string[] args)
        {
            return args.Length == 1 || (args.Length == 2 && args[0] == "-s");
        }

        private void ExecuteFile(string filename, bool silent, IConsoleContext context)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    if (!silent)
                        context.WriteError($"File not found: {filename}");
                    return;
                }

                var lines = File.ReadAllLines(filename);
                int commandCount = 0;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                        continue;

                    context.EnqueueCommand(trimmedLine);
                    commandCount++;

                    // Prevent command overflow
                    if (commandCount > 128)
                    {
                        context.WriteError("Command overflow detected. Stopping execution to prevent system overload.");
                        break;
                    }
                }

                if (!silent)
                    context.WriteLine($"Executed {commandCount} commands from {filename}");
            }
            catch (Exception e)
            {
                if (!silent)
                    context.WriteError($"Exec failed: {e.Message}");
            }
        }
    }
}