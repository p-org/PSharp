using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    public class ConfigOptions
    {
        public bool CollapseMachines;

        public ConfigOptions()
        {
            // Set default values
            CollapseMachines = false;
        }

        public static string GetDescription()
        {
            return @"
    /CollapseMachines: 
        - If there are multiple machines, They are drawn collapsed by default
";
        }

        internal bool tryParseOption(string[] parts)
        {
            switch (parts[0])
            {
                case "CollapseMachines":
                    CollapseMachines = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}
