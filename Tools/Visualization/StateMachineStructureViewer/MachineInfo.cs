using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    class MachineInfo
    {
        public string uniqueName { get; }
        internal PSharpProgram program;
        internal MachineDeclaration machineDeclaration;
        internal MachineInfo baseMachine;

        

        public MachineInfo(MachineDeclaration mdecl, PSharpProgram prog)
        {
            events = new HashSet<string>();
            states = new HashSet<string>();
            uniqueName = MachineResolver.CreateUniqueName(mdecl);
            machineDeclaration = mdecl;
            program = prog;
            baseMachine = null;
        }

        
        // Set of fully qualified names of the events declared ( or inherited ) in this machine
        HashSet<string> events;

        // Set of fully qualified names of the states declared ( or inherited ) in this machine 
        HashSet<string> states;

        public void resolveBaseMachine(List<string> activeNamespaces)
        {
            string machineNamespace = machineDeclaration.Namespace.QualifiedName;
            if (machineDeclaration.BaseNameTokens.Count > 0 )
            {
                string baseMachineName = MachineResolver.baseTypeTokenListToIdentifier(machineDeclaration.BaseNameTokens);
                Console.WriteLine("baseMachineName=" + baseMachineName);
                baseMachine = MachineResolver.Instance().LookupMachine(baseMachineName, machineNamespace, activeNamespaces );
                if(baseMachine == null)
                {
                    throw new Exception(String.Format("BaseMachine {0} not found for machine {1}" , baseMachineName, this.uniqueName) );
                }
            }

        }
        
    }
}
