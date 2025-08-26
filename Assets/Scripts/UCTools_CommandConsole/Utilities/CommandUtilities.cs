using UnityEngine;

namespace UCTools_CommandConsole.Utilities
{
    public static class CommandUtilities
    {
        /// <summary>
        /// Parse primitive type from string argument
        /// Demonstrates input validation and error handling
        /// </summary>
        public static bool TryParsePrimitiveType(string arg, out PrimitiveType primitiveType, IConsoleContext context)
        {
            // Convert to lowercase for case-insensitive matching
            string lowerArg = arg.ToLower();
            
            switch (lowerArg)
            {
                case "cube":
                case "box":
                    primitiveType = PrimitiveType.Cube;
                    return true;
                    
                case "sphere":
                case "ball":
                    primitiveType = PrimitiveType.Sphere;
                    return true;
                    
                case "cylinder":
                    primitiveType = PrimitiveType.Cylinder;
                    return true;
                    
                case "capsule":
                    primitiveType = PrimitiveType.Capsule;
                    return true;
                    
                case "plane":
                    primitiveType = PrimitiveType.Plane;
                    return true;
                    
                case "quad":
                    primitiveType = PrimitiveType.Quad;
                    return true;
                    
                default:
                    primitiveType = PrimitiveType.Cube;
                    context.WriteError($"Unknown primitive type '{arg}'. Valid types: cube, sphere, cylinder, capsule, plane, quad");
                    return false;
            }
        }
        
        /// <summary>
        /// Parse Vector3 position from three string arguments
        /// Demonstrates parsing multiple related arguments
        /// </summary>
        public static bool TryParseVector3(string xStr, string yStr, string zStr, out Vector3 position, IConsoleContext context)
        {
            position = Vector3.zero;
            
            if (float.TryParse(xStr, out float x) &&
                float.TryParse(yStr, out float y) &&
                float.TryParse(zStr, out float z))
            {
                position = new Vector3(x, y, z);
                return true;
            }
            
            context.WriteError($"Invalid position coordinates: '{xStr}', '{yStr}', '{zStr}'. Expected numbers.");
            return false;
        }
        
        /// <summary>
        /// Parse color from string argument
        /// Demonstrates parsing with predefined options and fallbacks
        /// </summary>
        public static bool TryParseColor(string colorStr, out Color color, IConsoleContext context)
        {
            string lowerColor = colorStr.ToLower();
            
            switch (lowerColor)
            {
                case "red":
                    color = Color.red;
                    return true;
                case "green":
                    color = Color.green;
                    return true;
                case "blue":
                    color = Color.blue;
                    return true;
                case "yellow":
                    color = Color.yellow;
                    return true;
                case "purple":
                case "magenta":
                    color = Color.magenta;
                    return true;
                case "cyan":
                    color = Color.cyan;
                    return true;
                case "white":
                    color = Color.white;
                    return true;
                case "black":
                    color = Color.black;
                    return true;
                case "gray":
                case "grey":
                    color = Color.gray;
                    return true;
                default:
                    color = Color.white;
                    context.WriteError($"Unknown color '{colorStr}'. Valid colors: red, green, blue, yellow, purple, cyan, white, black, gray");
                    return false;
            }
        }
    }
}