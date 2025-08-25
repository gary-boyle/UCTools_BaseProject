using System;

namespace UCTools_ConfigVariables
{
    public class ConfigVarAttribute : Attribute
    {
        public string Name = null;
        public string DefaultValue = "";
        public ConfigFlags Flags = ConfigFlags.None;
        public string Description = "";
    }
}