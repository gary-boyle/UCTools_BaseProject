using UnityEngine;
using System.Collections.Generic;
using System;
//using UCTools_ConfigVariables;

namespace UCTools_CommandConsole
{
    public class Console
    {
        //[ConfigVar(Name = "config.showlastline", DefaultValue = "0", Description = "Show last logged line briefly at top of screen")]
        //static ConfigVar consoleShowLastLine;

        static List<string> s_PendingCommands = new List<string>();
        public static int s_PendingCommandsWaitForFrames = 0;
        public static bool s_PendingCommandsWaitForLoad = false;
    
        const int k_HistoryCount = 50;
        static string[] s_History = new string[k_HistoryCount];
        static int s_HistoryNextIndex = 0;
        static int s_HistoryIndex = 0;
    
        static IConsoleUI s_ConsoleUI;
        static IConsoleContext s_ConsoleContext;

        // Reset statics when domain reload is disabled
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
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
    
        public static void Init(IConsoleUI consoleUI)
        {
            Debug.Assert(s_ConsoleUI == null);

            s_ConsoleUI = consoleUI;
            s_ConsoleContext = new ConsoleContext();
            s_ConsoleUI.Init();
        
            Write("Console ready");
        }

        public static void Shutdown()
        {
            s_ConsoleUI.Shutdown();
        }

        public static void OutputString(string message)
        {
            Debug.Log("1");
            if (s_ConsoleUI != null)
                s_ConsoleUI.OutputString(message);
        }

        static string lastMsg = "";
        static double timeLastMsg;
        public static void Write(string msg)
        {
            //// Have to condition on cvar being null as this may run before cvar system is initialized
            // if (consoleShowLastLine != null && consoleShowLastLine.IntValue > 0)
            // {
            //     lastMsg = msg;
            //     //timeLastMsg = Game.frameTime;
            // }
            OutputString(msg);
        }

        public static bool IsOpen()
        {
            return s_ConsoleUI.IsOpen();
        }

        public static void SetOpen(bool open)
        {
            s_ConsoleUI.SetOpen(open);
        }

        public static void SetPrompt(string prompt)
        {
            s_ConsoleUI.SetPrompt(prompt);
        }

        public static void ConsoleUpdate()
        {
            s_ConsoleUI.ConsoleUpdate();

            while (s_PendingCommands.Count > 0)
            {
                if (s_PendingCommandsWaitForFrames > 0)
                {
                    s_PendingCommandsWaitForFrames--;
                    break;
                }
                if (s_PendingCommandsWaitForLoad)
                {
                    s_PendingCommandsWaitForLoad = false;
                }
                // Remove before executing as we may hit an 'exec' command that wants to insert commands
                var cmd = s_PendingCommands[0];
                s_PendingCommands.RemoveAt(0);
                Debug.Log("2");
                ExecuteCommand(cmd);
            }
        }

        public static void ConsoleLateUpdate()
        {
            s_ConsoleUI.ConsoleLateUpdate();
        }

        static void SkipWhite(string input, ref int pos)
        {
            while (pos < input.Length && " \t".IndexOf(input[pos]) > -1)
            {
                pos++;
            }
        }

        static string ParseQuoted(string input, ref int pos)
        {
            pos++;
            int startPos = pos;
            while (pos < input.Length)
            {
                if (input[pos] == '"' && input[pos - 1] != '\\')
                {
                    pos++;
                    return input.Substring(startPos, pos - startPos - 1);
                }
                pos++;
            }
            return input.Substring(startPos);
        }

        static string Parse(string input, ref int pos)
        {
            int startPos = pos;
            while (pos < input.Length)
            {
                if (" \t".IndexOf(input[pos]) > -1)
                {
                    return input.Substring(startPos, pos - startPos);
                }
                pos++;
            }
            return input.Substring(startPos);
        }

        static List<string> Tokenize(string input)
        {
            var pos = 0;
            var res = new List<string>();
            var c = 0;
            while (pos < input.Length && c++ < 10000)
            {
                SkipWhite(input, ref pos);
                if (pos == input.Length)
                    break;

                if (input[pos] == '"' && (pos == 0 || input[pos - 1] != '\\'))
                {
                    res.Add(ParseQuoted(input, ref pos));
                }
                else
                    res.Add(Parse(input, ref pos));
            }
            return res;
        }

        public static void ExecuteCommand(string command)
        {
            var tokens = Tokenize(command);
            if (tokens.Count < 1)
                return;

            OutputString('>' + command);
            var commandName = tokens[0].ToLower();

            // Try console command first
            var consoleCommand = ConsoleCommandRegistry.GetCommand(commandName);
            if (consoleCommand != null)
            {
                var arguments = tokens.GetRange(1, tokens.Count - 1).ToArray();
                
                try
                {
                    if (consoleCommand.ValidateArgs(arguments))
                    {
                        consoleCommand.Execute(arguments, s_ConsoleContext);
                    }
                    else
                    {
                        OutputString($"Invalid arguments for command '{commandName}'");
                        OutputString(consoleCommand.GetUsage());
                    }
                }
                catch (Exception e)
                {
                    OutputString($"Error executing command '{commandName}': {e.Message}");
                    Debug.LogException(e);
                }
                return;
            }

            //// Try config variable
            // if (ConfigVar.ConfigVars.TryGetValue(commandName, out var configVar))
            // {
            //     if (tokens.Count == 2)
            //     {
            //         configVar.Value = tokens[1];
            //     }
            //     else if (tokens.Count == 1)
            //     {
            //         OutputString($"{configVar.name} = {configVar.Value}");
            //     }
            //     else
            //     {
            //         OutputString("Too many arguments");
            //     }
            //     return;
            // }

            OutputString("Unknown command: " + tokens[0]);
        }

        public static string TabComplete(string prefix)
        {
            var matches = new List<string>();

            // Add command names
            foreach (var commandName in ConsoleCommandRegistry.GetCommandNames())
            {
                if (commandName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    matches.Add(commandName);
            }

            // // Add config vars
            // foreach (var configVar in ConfigVar.ConfigVars.Keys)
            // {
            //     if (configVar.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            //         matches.Add(configVar);
            // }

            if (matches.Count == 0)
                return prefix;

            // Find longest common prefix
            int lcp = matches[0].Length;
            for (var i = 0; i < matches.Count - 1; i++)
            {
                lcp = Mathf.Min(lcp, CommonPrefix(matches[i], matches[i + 1]));
            }
            
            prefix += matches[0].Substring(prefix.Length, lcp - prefix.Length);
            
            if (matches.Count > 1)
            {
                foreach (var match in matches)
                    Write(" " + match);
            }
            else
            {
                prefix += " ";
            }
            
            return prefix;
        }

        public static void EnqueueCommandNoHistory(string command)
        {
            Debug.Log("cmd: " + command);
            s_PendingCommands.Add(command);
        }

        public static void EnqueueCommand(string command)
        {
            s_History[s_HistoryNextIndex % k_HistoryCount] = command;
            s_HistoryNextIndex++;
            s_HistoryIndex = s_HistoryNextIndex;

            EnqueueCommandNoHistory(command);
        }

        public static string HistoryUp(string current)
        {
            if (s_HistoryIndex == 0 || s_HistoryNextIndex - s_HistoryIndex >= k_HistoryCount - 1)
                return "";

            if (s_HistoryIndex == s_HistoryNextIndex)
            {
                s_History[s_HistoryIndex % k_HistoryCount] = current;
            }

            s_HistoryIndex--;

            return s_History[s_HistoryIndex % k_HistoryCount];
        }

        public static string HistoryDown()
        {
            if (s_HistoryIndex == s_HistoryNextIndex)
                return "";

            s_HistoryIndex++;

            return s_History[s_HistoryIndex % k_HistoryCount];
        }

        // Returns length of largest common prefix of two strings
        static int CommonPrefix(string a, string b)
        {
            int minl = Mathf.Min(a.Length, b.Length);
            for (int i = 1; i <= minl; i++)
            {
                if (!a.StartsWith(b.Substring(0, i), true, null))
                    return i - 1;
            }
            return minl;
        }
    }
}
