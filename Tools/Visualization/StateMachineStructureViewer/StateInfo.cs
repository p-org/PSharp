using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    class StateInfo
    {
        private MachineInfo machineInfo;
        private StateDeclaration stateDeclaration;
        public string uniqueName { get; }

        public StateInfo(StateDeclaration sdecl, MachineInfo machineInfo)
        {
            this.stateDeclaration = sdecl;
            this.machineInfo = machineInfo;
            this.uniqueName = machineInfo.uniqueName + '.' + sdecl.GetFullyQualifiedName('.');
        }


    }
}
