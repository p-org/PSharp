using Microsoft.PSharp.LanguageServices.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    class EventInfo
    {
        private MachineInfo machineInfo;
        private EventDeclaration eventDeclaration;
        public string uniqueName { get; }

        public EventInfo(EventDeclaration edecl, MachineInfo machineInfo)
        {
            this.eventDeclaration = edecl;
            this.machineInfo = machineInfo;
            this.uniqueName = machineInfo.uniqueName + '.' + edecl.TextUnit;
        }
    }
}
