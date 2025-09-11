using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using GameFramework.ConsoleTool.Commands;
using GameFramework.ConsoleTool.Interfaces;

namespace GameFramework.ConsoleTool
{
    /// <summary>
    /// Core console system that manages command execution, history, and output.
    /// 
    /// Architecture:
    /// - Static class for global access (consider making non-static in future for better testability)
    /// - Manages command queue, history, and execution
    /// - Delegates UI operations to registered IConsoleUI implementation
    /// - Thread-safe command queuing for commands from other threads
    /// 
    /// Command Execution Flow:
    /// 1. Commands are queued via EnqueueCommand()
    /// 2. ConsoleUpdate() processes the queue each frame
    /// 3. Commands are tokenized and looked up in registry
    /// 4. Command execution results are output to console
    /// </summary>
    public static class Console
    {
        #region Constants
        private const int HISTORY_COUNT = 50;
        private const int MAX_TOKENIZE_ITERATIONS = 10000; // Prevent infinite loops
        #endregion

        #region State

        private static readonly List<string> s_PendingCommands;
        private static readonly object s_CommandLock;
        
        private static readonly string[] s_History = new string[HISTORY_COUNT];
        private static int s_HistoryNextIndex;
        private static int s_HistoryIndex;
        
        private static IConsoleUI s_ConsoleUI;
        private static IConsoleContext s_ConsoleContext;
        
        public static int s_PendingCommandsWaitForFrames;
        public static bool s_PendingCommandsWaitForLoad;
        #endregion

        #region Initialization

        /// <summary>
        /// Reset all static state when domain reload is disabled
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            lock (s_CommandLock)
            {
                s_ConsoleUI = null;
                s_ConsoleContext = null;
                s_PendingCommands.Clear();
                s_PendingCommandsWaitForFrames = 0;
                s_PendingCommandsWaitForLoad = false;
                s_HistoryNextIndex = 0;
                s_HistoryIndex = 0;
                Array.Clear(s_History, 0, s_History.Length);
            }
        }

        /// <summary>
        /// Initialize the console system with a UI implementation
        /// </summary>
        /// <param name="consoleUI">UI implementation to use for console display</param>
        public static void Init(IConsoleUI consoleUI)
        {
            if (s_ConsoleUI != null)
            {
                Debug.LogWarning("[Console] Console already initialized!");
                return;
            }

            s_ConsoleUI = consoleUI ?? throw new ArgumentNullException(nameof(consoleUI));
            s_ConsoleContext = new ConsoleContext();
            
            s_ConsoleUI.Init();
            Write("Console system ready");
        }

        /// <summary>
        /// Shutdown the console system
        /// </summary>
        public static void Shutdown()
        {
            s_ConsoleUI?.Shutdown();
            s_ConsoleUI = null;
            s_ConsoleContext = null;
            
            lock (s_CommandLock)
            {
                s_PendingCommands.Clear();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Write a message to the console output
        /// </summary>
        /// <param name="message">Message to display</param>
        public static void Write(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            
            s_ConsoleUI?.OutputString(message);
        }
        
        /// <summary>
        /// Set console open/closed state
        /// </summary>
        public static void SetOpen(bool open)
        {
            s_ConsoleUI?.SetOpen(open);
        }
        
        #endregion

        #region Command Management

        /// <summary>
        /// Add a command to the execution queue with history tracking
        /// Thread-safe for commands from other threads
        /// </summary>
        /// <param name="command">Command string to execute</param>
        public static void EnqueueCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            // Add to history
            lock (s_CommandLock)
            {
                s_History[s_HistoryNextIndex % HISTORY_COUNT] = command;
                s_HistoryNextIndex++;
                s_HistoryIndex = s_HistoryNextIndex;

                // Add to pending commands
                s_PendingCommands.Add(command);
            }
        }

        /// <summary>
        /// Add a command to execution queue without adding to history
        /// Useful for programmatic commands that shouldn't appear in user history
        /// </summary>
        /// <param name="command">Command string to execute</param>
        public static void EnqueueCommandNoHistory(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            lock (s_CommandLock)
            {
                s_PendingCommands.Add(command);
            }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Process pending commands and update console UI
        /// Should be called every frame by ConsoleService
        /// </summary>
        public static void ConsoleUpdate()
        {
            // Update UI
            s_ConsoleUI?.ConsoleUpdate();

            // Process command queue
            ProcessCommandQueue();
        }

        /// <summary>
        /// Late update for console UI
        /// Should be called every frame by ConsoleService
        /// </summary>
        public static void ConsoleLateUpdate()
        {
            s_ConsoleUI?.ConsoleLateUpdate();
        }

        /// <summary>
        /// Process all pending commands in the queue
        /// </summary>
        private static void ProcessCommandQueue()
        {
            while (true)
            {
                string commandToExecute = null;
                
                lock (s_CommandLock)
                {
                    if (s_PendingCommands.Count == 0) break;
                    
                    if (s_PendingCommandsWaitForFrames > 0)
                    {
                        s_PendingCommandsWaitForFrames--;
                        break;
                    }
                    
                    if (s_PendingCommandsWaitForLoad)
                    {
                        s_PendingCommandsWaitForLoad = false;
                        break;
                    }
                    
                    // Remove command before executing to prevent issues with 'exec' commands
                    commandToExecute = s_PendingCommands[0];
                    s_PendingCommands.RemoveAt(0);
                }
                
                if (commandToExecute != null)
                {
                    ExecuteCommand(commandToExecute);
                }
            }
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Execute a single command string
        /// Tokenizes the command and attempts to find and execute it
        /// </summary>
        /// <param name="command">Raw command string from user input</param>
        private static void ExecuteCommand(string command)
        {
            try
            {
                var tokens = TokenizeCommand(command);
                if (tokens.Count == 0) return;

                // Echo the command to output
                Write($"> {command}");

                var commandName = tokens[0].ToLowerInvariant();
                var arguments = tokens.GetRange(1, tokens.Count - 1).ToArray();

                // Try to find and execute console command
                var consoleCommand = ConsoleCommandRegistry.GetCommand(commandName);
                if (consoleCommand != null)
                {
                    ExecuteConsoleCommand(consoleCommand, commandName, arguments);
                    return;
                }

                // Command not found
                Write($"Unknown command: {commandName}");
                Write("Type 'help' for available commands");
            }
            catch (Exception e)
            {
                Write($"Error processing command: {e.Message}");
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Execute a registered console command with error handling
        /// </summary>
        private static void ExecuteConsoleCommand(IConsoleCommand command, string commandName, string[] arguments)
        {
            try
            {
                if (command.ValidateArgs(arguments))
                {
                    command.Execute(arguments, s_ConsoleContext);
                }
                else
                {
                    Write($"Invalid arguments for command '{commandName}'");
                    Write($"Usage: {command.GetUsage()}");
                }
            }
            catch (Exception e)
            {
                Write($"Error executing command '{commandName}': {e.Message}");
                Debug.LogException(e);
            }
        }

        #endregion

        #region Command Parsing

        /// <summary>
        /// Tokenize a command string into individual arguments
        /// Supports quoted strings and escaped characters
        /// </summary>
        /// <param name="input">Raw command string</param>
        /// <returns>List of command tokens</returns>
        private static List<string> TokenizeCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            var tokens = new List<string>();
            var position = 0;
            var iterations = 0;

            while (position < input.Length && iterations++ < MAX_TOKENIZE_ITERATIONS)
            {
                SkipWhitespace(input, ref position);
                if (position >= input.Length) break;

                if (input[position] == '"' && (position == 0 || input[position - 1] != '\\'))
                {
                    tokens.Add(ParseQuotedString(input, ref position));
                }
                else
                {
                    tokens.Add(ParseUnquotedString(input, ref position));
                }
            }

            return tokens;
        }

        /// <summary>
        /// Skip whitespace characters in input string
        /// </summary>
        private static void SkipWhitespace(string input, ref int position)
        {
            while (position < input.Length && char.IsWhiteSpace(input[position]))
            {
                position++;
            }
        }

        /// <summary>
        /// Parse a quoted string token, handling escape sequences
        /// </summary>
        private static string ParseQuotedString(string input, ref int position)
        {
            position++; // Skip opening quote
            int startPos = position;

            while (position < input.Length)
            {
                if (input[position] == '"' && (position == 0 || input[position - 1] != '\\'))
                {
                    var result = input.Substring(startPos, position - startPos);
                    position++; // Skip closing quote
                    return result;
                }
                position++;
            }

            // Unclosed quote - return what we have
            return input.Substring(startPos);
        }

        /// <summary>
        /// Parse an unquoted string token (until whitespace)
        /// </summary>
        private static string ParseUnquotedString(string input, ref int position)
        {
            int startPos = position;

            while (position < input.Length && !char.IsWhiteSpace(input[position]))
            {
                position++;
            }

            return input.Substring(startPos, position - startPos);
        }

        #endregion

        #region History Management

        /// <summary>
        /// Get previous command from history
        /// </summary>
        /// <param name="current">Current input text (stored if at end of history)</param>
        /// <returns>Previous command or empty string if at beginning</returns>
        public static string HistoryUp(string current)
        {
            if (s_HistoryIndex == 0 || s_HistoryNextIndex - s_HistoryIndex >= HISTORY_COUNT - 1)
                return string.Empty;

            // Store current input if we're at the end of history
            if (s_HistoryIndex == s_HistoryNextIndex)
            {
                s_History[s_HistoryIndex % HISTORY_COUNT] = current ?? string.Empty;
            }

            s_HistoryIndex--;
            return s_History[s_HistoryIndex % HISTORY_COUNT] ?? string.Empty;
        }

        /// <summary>
        /// Get next command from history
        /// </summary>
        /// <returns>Next command or empty string if at end</returns>
        public static string HistoryDown()
        {
            if (s_HistoryIndex == s_HistoryNextIndex)
                return string.Empty;

            s_HistoryIndex++;
            return s_History[s_HistoryIndex % HISTORY_COUNT] ?? string.Empty;
        }

        #endregion

        #region Tab Completion

        /// <summary>
        /// Perform tab completion on a partial command
        /// </summary>
        /// <param name="prefix">Partial command to complete</param>
        /// <returns>Completed command or original if no matches</returns>
        public static string TabComplete(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return prefix;

            var matches = ConsoleCommandRegistry.GetCommandNames().Where(commandName => commandName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

            // Find matching command names

            if (matches.Count == 0) return prefix;

            // Find longest common prefix among matches
            string commonPrefix = FindLongestCommonPrefix(matches, prefix.Length);

            if (matches.Count > 1)
            {
                // Show all matches
                Write($"Matches for '{prefix}':");
                foreach (var match in matches)
                {
                    Write($"  {match}");
                }
                return commonPrefix;
            }
            else
            {
                // Single match - add space for next argument
                return commonPrefix + " ";
            }
        }

        /// <summary>
        /// Find the longest common prefix among a list of strings
        /// </summary>
        private static string FindLongestCommonPrefix(List<string> strings, int startLength)
        {
            if (strings.Count == 0) return string.Empty;
            if (strings.Count == 1) return strings[0];

            int minLength = strings[0].Length;
            
            minLength = strings.Aggregate(minLength, (current, str) => Mathf.Min(current, str.Length));

            for (int i = startLength; i < minLength; i++)
            {
                char c = char.ToLowerInvariant(strings[0][i]);
                for (int j = 1; j < strings.Count; j++)
                {
                    if (char.ToLowerInvariant(strings[j][i]) != c)
                    {
                        return strings[0].Substring(0, i);
                    }
                }
            }

            return strings[0].Substring(0, minLength);
        }

        #endregion
    }
}
