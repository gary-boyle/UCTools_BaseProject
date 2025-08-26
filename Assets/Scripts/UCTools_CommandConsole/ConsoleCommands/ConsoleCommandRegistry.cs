using System;
using System.Collections.Generic;
using System.Linq;
using UCTools_CommandConsole.Enums;
using UnityEngine;

namespace UCTools_CommandConsole
{
    /// <summary>
    /// Registry system for console commands with automatic discovery and registration
    /// Handles command lifecycle and provides efficient lookup mechanisms
    /// </summary>
    public static class ConsoleCommandRegistry
    {
        private static readonly Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();
        private static bool _isInitialized = false;

        /// <summary>
        /// Reset registry state (called when domain reload is disabled)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            _commands.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Auto-discover and register all console commands
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoDiscoverCommands()
        {
            if (_isInitialized) return;

            RegisterDefaultCommands();
            DiscoverAndRegisterCommands();
            _isInitialized = true;

            Debug.Log($"Console Command Registry: Registered {_commands.Count} commands");
        }

        /// <summary>
        /// Register default built-in commands
        /// </summary>
        private static void RegisterDefaultCommands()
        {
            RegisterCommand(new HelpCommand());
            //RegisterCommand(new VarsCommand());
            RegisterCommand(new WaitCommand());
            RegisterCommand(new ExecCommand());
        }

        /// <summary>
        /// Discover and register commands via reflection
        /// </summary>
        private static void DiscoverAndRegisterCommands()
        {
            try
            {
                var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => typeof(IConsoleCommand).IsAssignableFrom(type) && 
                                  !type.IsInterface && 
                                  !type.IsAbstract &&
                                  type != typeof(ConsoleCommandBase));

                foreach (var commandType in commandTypes)
                {
                    // Skip built-in commands (already registered)
                    if (IsBuiltInCommand(commandType))
                        continue;

                    try
                    {
                        var command = (IConsoleCommand)Activator.CreateInstance(commandType);
                        RegisterCommand(command);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to instantiate command {commandType.Name}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during command discovery: {e.Message}");
            }
        }

        private static bool IsBuiltInCommand(Type commandType)
        {
            return commandType == typeof(HelpCommand) ||
                   //commandType == typeof(VarsCommand) ||
                   commandType == typeof(WaitCommand) ||
                   commandType == typeof(ExecCommand);
        }

        /// <summary>
        /// Register a command instance
        /// </summary>
        public static bool RegisterCommand(IConsoleCommand command)
        {
            if (command == null)
            {
                Debug.LogError("Cannot register null command");
                return false;
            }

            string commandName = command.CommandName.ToLower();
            
            if (_commands.ContainsKey(commandName))
            {
                Debug.LogWarning($"Command '{commandName}' is already registered");
                return false;
            }

            _commands[commandName] = command;
            return true;
        }

        /// <summary>
        /// Unregister a command by name
        /// </summary>
        public static bool UnregisterCommand(string commandName)
        {
            return _commands.Remove(commandName.ToLower());
        }

        /// <summary>
        /// Unregister all commands with specific tag
        /// </summary>
        public static int UnregisterCommandsByTag(int tag)
        {
            var commandsToRemove = _commands
                .Where(kvp => kvp.Value.Tag == tag)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var commandName in commandsToRemove)
            {
                _commands.Remove(commandName);
            }

            return commandsToRemove.Count;
        }

        /// <summary>
        /// Get command by name
        /// </summary>
        public static IConsoleCommand GetCommand(string commandName)
        {
            _commands.TryGetValue(commandName.ToLower(), out var command);
            return command;
        }

        /// <summary>
        /// Check if command is registered
        /// </summary>
        public static bool IsCommandRegistered(string commandName)
        {
            return _commands.ContainsKey(commandName.ToLower());
        }

        /// <summary>
        /// Get all registered commands
        /// </summary>
        public static IEnumerable<IConsoleCommand> GetAllCommands()
        {
            return _commands.Values;
        }

        /// <summary>
        /// Get commands by category
        /// </summary>
        public static IEnumerable<IConsoleCommand> GetCommandsByCategory(CategoryEnum category)
        {
            return _commands.Values.Where(cmd => cmd.Category == category);
        }

        /// <summary>
        /// Get command names for tab completion
        /// </summary>
        public static IEnumerable<string> GetCommandNames()
        {
            return _commands.Keys;
        }
    }

}