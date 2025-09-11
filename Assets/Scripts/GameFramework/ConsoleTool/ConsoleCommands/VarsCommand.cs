// using System.Linq;
// using UCTools_CommandConsole.Enums;
// //using UCTools_ConfigVariables;
//
// namespace UCTools_CommandConsole
// {
//     /// <summary>
//     /// Variables command that displays all configuration variables and their values
//     /// Supports filtering and searching through config vars
//     /// </summary>
//     public class VarsCommand : ConsoleCommandBase
//     {
//         public override string CommandName => "vars";
//         public override string Description => "Show available configuration variables";
//         public override CategoryEnum Category => CategoryEnum.System;
//
//         public override void Execute(string[] args, IConsoleContext context)
//         {
//             if (args.Length == 0)
//             {
//                 ShowAllVars(context);
//             }
//             else if (args.Length == 1)
//             {
//                 ShowFilteredVars(args[0], context);
//             }
//             else
//             {
//                 context.WriteError(GetUsage());
//             }
//         }
//
//         public override string GetUsage()
//         {
//             return "Usage: vars [filter]\n" +
//                    "  vars          - Show all variables\n" +
//                    "  vars <filter> - Show variables containing filter text";
//         }
//
//         private void ShowAllVars(IConsoleContext context)
//         {
//             if (ConfigVar.ConfigVars.Count == 0)
//             {
//                 context.WriteLine("No configuration variables registered.");
//                 return;
//             }
//
//             context.WriteLine("Configuration Variables:");
//             
//             var sortedVars = ConfigVar.ConfigVars.Keys.ToList();
//             sortedVars.Sort();
//             
//             foreach (var varName in sortedVars)
//             {
//                 var configVar = ConfigVar.ConfigVars[varName];
//                 context.WriteLine($"  {configVar.name.PadRight(25)} = {configVar.Value}");
//             }
//         }
//
//         private void ShowFilteredVars(string filter, IConsoleContext context)
//         {
//             var matchingVars = ConfigVar.ConfigVars
//                 .Where(kvp => kvp.Key.Contains(filter.ToLower()))
//                 .OrderBy(kvp => kvp.Key);
//
//             if (!matchingVars.Any())
//             {
//                 context.WriteLine($"No variables found matching '{filter}'");
//                 return;
//             }
//
//             context.WriteLine($"Variables matching '{filter}':");
//             foreach (var kvp in matchingVars)
//             {
//                 var configVar = kvp.Value;
//                 context.WriteLine($"  {configVar.name.PadRight(25)} = {configVar.Value}");
//             }
//         }
//     }
// }