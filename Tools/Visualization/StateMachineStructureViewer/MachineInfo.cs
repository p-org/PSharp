
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
            uniqueName = ResolutionHelper.CreateUniqueName(mdecl);
            machineDeclaration = mdecl;
            program = prog;
            baseMachine = null;
            
        }

        
        // Set of fully qualified names of the events declared ( or inherited ) in this machine
        HashSet<string> events;

        // Set of fully qualified names of the states declared ( or inherited ) in this machine 
        HashSet<string> states;

        internal void resolveBaseMachine(List<string> activeNamespaces)
        {
            string machineNamespace = machineDeclaration.Namespace.QualifiedName;
            if (machineDeclaration.BaseNameTokens.Count > 0 )
            {
                string baseMachineName = ResolutionHelper.baseTypeTokenListToIdentifier(machineDeclaration.BaseNameTokens);
                Console.WriteLine("baseMachineName=" + baseMachineName);
                baseMachine = ResolutionHelper.Instance().LookupMachine(baseMachineName, machineNamespace, activeNamespaces );
                if(baseMachine == null)
                {
                    throw new Exception(String.Format("BaseMachine {0} not found for machine {1}" , baseMachineName, this.uniqueName) );
                }
            }

        }


        internal StateInfo lookupState(string identifierText, StateGroupDeclaration stateGroupContext = null)
        {

            int x = 5;
            if (x > 4) { 
                throw new NotImplementedException("Not done");
            }
            // TODO: All of this.
            StateInfo foundState = null;
            while (stateGroupContext!=null && foundState == null) {
                foundState = ResolutionHelper.Instance().LookupState(identifierText, this.machineDeclaration, stateGroupContext);
                stateGroupContext = stateGroupContext.Group;
            }
            if (foundState == null) { 
                foundState = ResolutionHelper.Instance().LookupState(identifierText, this.machineDeclaration, null);
            }

            if ( foundState == null && baseMachine != null)
            {
                return baseMachine.lookupState(identifierText, stateGroupContext);
            }
            else
            {
                return foundState;
            }
        }
        
        internal EventInfo lookupEvent(string identifierText)
        {
            // First look up events local to this machine
            EventInfo eventInfo = ResolutionHelper.Instance().LookupEvent(identifierText, this.machineDeclaration);
            if ( eventInfo == null)
            {
                List<string> activeNamespaces = ResolutionHelper.GetActiveNamespacesFromUsingDirectives(this.program);
                eventInfo = ResolutionHelper.Instance().LookupEvent(identifierText, this.machineDeclaration.Namespace.QualifiedName, activeNamespaces);
            }
            return eventInfo;
        }
    }
}
