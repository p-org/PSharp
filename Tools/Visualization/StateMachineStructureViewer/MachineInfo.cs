
using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.PSharpStateMachineStructureViewer
{
    internal class MachineInfo
    {
        public string uniqueName { get; }
        internal PSharpProgram program;
        internal MachineDeclaration machineDeclaration;
        internal MachineInfo baseMachine;
        
        internal MachineInfo(MachineDeclaration mdecl, PSharpProgram prog)
        {
            uniqueName = ResolutionHelper.CreateUniqueName(mdecl);
            machineDeclaration = mdecl;
            program = prog;
            baseMachine = null;
            states = null;
        }

        
        // Set of fully qualified names of the events declared ( or inherited ) in this machine
        HashSet<string> events;

        // Set of fully qualified names of the states declared ( or inherited ) in this machine 
        HashSet<string> states;

        #region api
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

        public HashSet<string> GetAllStates()
        {
            HashSet<string> states = new HashSet<string>();
            if (baseMachine != null)
            {
                var parentStates = baseMachine.GetAllStates();
                foreach (var parentMachineState in parentStates)
                {
                    states.Add(parentMachineState); // TODO : Do we add the fact that this is inherited? 
                }
            }

            foreach (string declaredState in computeStatesDeclared(false))
            {
                states.Add(declaredState);
            }

            return states;
        }

        public StateInfo LookupState(string identifierText, StateGroupDeclaration stateGroupContext = null)
        {
            return doLookupState(identifierText, stateGroupContext);
        }
        
        public EventInfo LookupEvent(string identifierText)
        {
            return doLookupEvent(identifierText, true);
        }

        #endregion

        #region private lookup logic

        private StateInfo doLookupState(string identifierText, StateGroupDeclaration stateGroupContext = null)
        {
            StateInfo foundState = null;
            while (stateGroupContext != null && foundState == null)
            {
                foundState = ResolutionHelper.Instance().LookupState(identifierText, this.machineDeclaration, stateGroupContext);
                stateGroupContext = stateGroupContext.Group;
            }
            if (foundState == null)
            {
                foundState = ResolutionHelper.Instance().LookupState(identifierText, this.machineDeclaration, null);
            }

            if (foundState == null && baseMachine != null)
            {
                return baseMachine.doLookupState(identifierText, stateGroupContext);
            }
            else
            {
                return foundState;
            }
        }

        private EventInfo doLookupEvent(string identifierText, bool checkNamespace)
        {
            // First look up events local to this machine
            EventInfo eventInfo = ResolutionHelper.Instance().LookupEvent(identifierText, this.machineDeclaration);
            // No luck? Could be from a parent event.
            if (eventInfo == null)
            {
                eventInfo = baseMachine.doLookupEvent(identifierText, false);
            }
            // Still no luck, try a global lookup if we've not been called by a base
            if ( checkNamespace && eventInfo == null)
            {
                List<string> activeNamespaces = ResolutionHelper.GetActiveNamespacesFromUsingDirectives(this.program);
                eventInfo = ResolutionHelper.Instance().LookupEvent(identifierText, this.machineDeclaration.Namespace.QualifiedName, activeNamespaces);
            }
            
            return eventInfo;
        }


        // Computes the UniqueNames of the states declared in this machine. Does not include those inherited.
        private HashSet<string> computeStatesDeclared(bool recompute=false)
        {
            if (states == null || recompute)
            {
                states = new HashSet<string>();
                foreach (StateDeclaration sdecl in machineDeclaration.StateDeclarations)
                {
                    states.Add(ResolutionHelper.CreateUniqueName(sdecl));
                }
                foreach (StateGroupDeclaration sgDecl in machineDeclaration.StateGroupDeclarations)
                {
                    computeStatesDeclaredInStateGroup(sgDecl);
                }
            }
            return states;
        }

        private HashSet<string> computeStatesDeclaredInStateGroup(StateGroupDeclaration stateGroup)
        {
            foreach (StateDeclaration sdecl in stateGroup.StateDeclarations)
            {
                states.Add(ResolutionHelper.CreateUniqueName(sdecl));
            }

            foreach (StateGroupDeclaration sgDecl in stateGroup.StateGroupDeclarations)
            {
                computeStatesDeclaredInStateGroup(sgDecl);
            }
            
            return states;
        }

        // Computes the UniqueNames of the events declared in this machine. Does not include those inherited.
        private HashSet<string> computeEventsDeclared(bool recompute = false)
        {
            if (events == null || recompute)
            {
                events = new HashSet<string>();
                foreach (EventDeclaration edecl in machineDeclaration.EventDeclarations)
                {
                    events.Add(ResolutionHelper.CreateUniqueName(edecl));
                }
            }
            return events;
        }

    }
    #endregion
    

}
