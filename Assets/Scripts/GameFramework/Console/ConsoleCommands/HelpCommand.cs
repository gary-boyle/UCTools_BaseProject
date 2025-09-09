using System.Linq;
using GameFramework.ConsoleTool.Enums;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Help command that displays available commands and their descriptions
    /// Supports filtering by category and detailed command information
    /// </summary>
    public class HelpCommand : ConsoleCommandBase
    {
        public override string CommandName => "help";
        public override string Description => "Show available commands";
        public override CategoryEnum Category => CategoryEnum.General;

        public override void Execute(string[] args, IConsoleContext context)
        {
            if (args.Length == 0)
            {
                ShowAllCommands(context);
            }
            else if (args.Length == 1)
            {
                ShowCommandHelp(args[0], context);
            }
            else
            {
                context.WriteError(GetUsage());
            }
        }

        public override string GetUsage()
        {
            return "Usage: help [command_name]\n" +
                   "  help          - Show all commands\n" +
                   "  help <cmd>    - Show detailed help for specific command";
        }

        private void ShowAllCommands(IConsoleContext context)
        {
            context.WriteLine("Available commands:");
            
            var commands = ConsoleCommandRegistry.GetAllCommands()
                .OrderBy(cmd => cmd.Category)
                .ThenBy(cmd => cmd.CommandName);
                
            CategoryEnum lastCategory = CategoryEnum.General;
            foreach (var cmd in commands)
            {
                if (cmd.Category != lastCategory)
                {
                    context.WriteLine($"\n--- {cmd.Category} ---");
                    lastCategory = cmd.Category;
                }
                
                context.WriteLine($"  {cmd.CommandName.PadRight(15)} - {cmd.Description}");
            }
            
            context.WriteLine("\nUse 'help <command>' for detailed information about a command.");
        }

        private void ShowCommandHelp(string commandName, IConsoleContext context)
        {
            var command = ConsoleCommandRegistry.GetCommand(commandName);
            if (command == null)
            {
                context.WriteError($"Unknown command: {commandName}");
                return;
            }

            context.WriteLine($"Command: {command.CommandName}");
            context.WriteLine($"Category: {command.Category}");
            context.WriteLine($"Description: {command.Description}");
            context.WriteLine($"\n{command.GetUsage()}");
        }
    }
}