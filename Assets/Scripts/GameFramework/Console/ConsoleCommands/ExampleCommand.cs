using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Utilities;
using UnityEngine;

namespace GameFramework.ConsoleTool.Commands
{ 
    /// <summary>
    /// Example console command that demonstrates how to create custom commands
    /// This command creates primitive GameObjects in the scene for testing and demonstration
    /// 
    /// Usage Examples:
    /// > spawn                    - Creates a cube at origin (0,0,0)
    /// > spawn sphere             - Creates a sphere at origin
    /// > spawn cube 5 10 0        - Creates a cube at position (5, 10, 0)
    /// > spawn cylinder 0 5 0 red - Creates a red cylinder at (0, 5, 0)
    /// 
    /// This serves as a template for creating your own console commands
    /// Copy this file and modify it to create new functionality
    /// </summary>
    public class ExampleCommand : ConsoleCommandBase
    {
        #region Command Properties
        
        /// <summary>
        /// The name users type to execute this command (case-insensitive)
        /// Keep it short and memorable - this is what users will type in the console
        /// </summary>
        public override string CommandName => "spawn";
        
        /// <summary>
        /// Brief description shown in help command
        /// Should be concise but descriptive enough to understand the command's purpose
        /// </summary>
        public override string Description => "Spawn primitive GameObjects in the scene";
        
        /// <summary>
        /// Category for organizing commands in help display
        /// Common categories: "System", "Debug", "Game", "Testing", "Development"
        /// </summary>
        public override CategoryEnum Category => CategoryEnum.Development;
        
        /// <summary>
        /// Tag for bulk operations (optional - use 0 if not needed)
        /// Useful for removing groups of related commands (e.g., all debug commands)
        /// </summary>
        public override int Tag => 1000; // Example: use 1000+ for custom/example commands
        
        #endregion

        #region Command Execution
        
        /// <summary>
        /// Main command execution method - this is called when user runs the command
        /// Parse arguments and perform the requested action
        /// Always validate input and provide helpful error messages
        /// </summary>
        /// <param name="args">Command arguments (everything after the command name)</param>
        /// <param name="context">Console context for output and system interaction</param>
        public override void Execute(string[] args, IConsoleContext context)
        {
            // Handle different argument patterns
            switch (args.Length)
            {
                case 0:
                    // No arguments - create default cube at origin
                    SpawnPrimitive(PrimitiveType.Cube, Vector3.zero, Color.white, context);
                    break;
                    
                case 1:
                    // One argument - primitive type only, spawn at origin
                    if (CommandUtilities.TryParsePrimitiveType(args[0], out PrimitiveType primitiveType, context))
                    {
                        SpawnPrimitive(primitiveType, Vector3.zero, Color.white, context);
                    }
                    break;
                    
                case 4:
                    // Four arguments - type and position (x, y, z)
                    if (CommandUtilities.TryParsePrimitiveType(args[0], out primitiveType, context) &&
                        CommandUtilities.TryParseVector3(args[1], args[2], args[3], out Vector3 position, context))
                    {
                        SpawnPrimitive(primitiveType, position, Color.white, context);
                    }
                    break;
                    
                case 5:
                    // Five arguments - type, position, and color
                    if (CommandUtilities.TryParsePrimitiveType(args[0], out primitiveType, context) &&
                        CommandUtilities.TryParseVector3(args[1], args[2], args[3], out position, context) &&
                        CommandUtilities.TryParseColor(args[4], out Color color, context))
                    {
                        SpawnPrimitive(primitiveType, position, color, context);
                    }
                    break;
                    
                default:
                    // Wrong number of arguments - show usage
                    context.WriteError("Invalid number of arguments.");
                    context.WriteLine(GetUsage());
                    break;
            }
        }
        
        #endregion

        #region Helper Methods
        
        /// <summary>
        /// The core functionality - create a primitive GameObject with specified properties
        /// This is where the actual work happens
        /// </summary>
        private void SpawnPrimitive(PrimitiveType type, Vector3 position, Color color, IConsoleContext context)
        {
            try
            {
                // Create the primitive GameObject
                GameObject primitive = GameObject.CreatePrimitive(type);
                
                // Set position
                primitive.transform.position = position;
                
                // Set color by modifying the material
                Renderer renderer = primitive.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Create new material instance to avoid affecting other objects
                    Material material = new Material(renderer.material);
                    material.color = color;
                    renderer.material = material;
                }
                
                // Give it a descriptive name
                primitive.name = $"Console_{type}_{System.DateTime.Now:HHmmss}";
                
                // Add a tag for easy identification (optional)
                //primitive.tag = "ConsoleSpawned";
                
                // Provide feedback to user
                context.WriteLine($"Created {type} '{primitive.name}' at {position} with color {color}");
            }
            catch (System.Exception e)
            {
                // Always handle exceptions gracefully
                context.WriteError($"Failed to spawn {type}: {e.Message}");
            }
        }
        
        #endregion

        #region Documentation and Validation
        
        /// <summary>
        /// Provide detailed usage information for the help system
        /// Should include all possible argument combinations with examples
        /// </summary>
        public override string GetUsage()
        {
            return "Usage: spawn [type] [x y z] [color]\n" +
                   "  spawn                    - Create cube at origin\n" +
                   "  spawn sphere             - Create sphere at origin\n" +
                   "  spawn cube 5 10 0        - Create cube at position (5, 10, 0)\n" +
                   "  spawn cylinder 0 5 0 red - Create red cylinder at (0, 5, 0)\n" +
                   "\n" +
                   "Types: cube, sphere, cylinder, capsule, plane, quad\n" +
                   "Colors: red, green, blue, yellow, purple, cyan, white, black, gray\n" +
                   "\nNote: Created objects will have physics and be tagged 'ConsoleSpawned'";
        }
        
        /// <summary>
        /// Validate arguments before execution (optional but recommended)
        /// Return false if arguments are invalid - prevents Execute() from being called
        /// </summary>
        public override bool ValidateArgs(string[] args)
        {
            // Accept 0, 1, 4, or 5 arguments
            return args.Length == 0 || args.Length == 1 || args.Length == 4 || args.Length == 5;
        }
        
        #endregion
    }
}
