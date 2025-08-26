// using UCTools_ConfigVariables;
// using UCTools_Utilities;
//
// namespace UCTools_CommandConsole
// {
//     public class ConsoleManager : Singleton<ConsoleManager>
//     {
//         private IConsoleUI _consoleGUI;
//     
//         public void Initialize()
//         {
//             _consoleGUI = GetComponent<ConsoleGUI>();
//             ConfigVar.Init();
//             Console.Init(_consoleGUI);
//         }
//
//         void Update()
//         {
//             Console.ConsoleUpdate();
//         }
//
//         private void LateUpdate()
//         {
//             Console.ConsoleLateUpdate();
//         }
//     }
// }
