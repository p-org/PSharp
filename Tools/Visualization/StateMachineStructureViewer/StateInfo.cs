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
        public string uniqueName { get; }
        internal MachineInfo machineInfo { get; }
        internal StateDeclaration stateDeclaration { get; }
        internal Dictionary<string, string> gotoTransitions;
        internal Dictionary<string, string> pushTransitions;

        public StateInfo(StateDeclaration sdecl, MachineInfo machineInfo)
        {
            this.stateDeclaration = sdecl;
            this.machineInfo = machineInfo;
            this.uniqueName = machineInfo.uniqueName + '.' + sdecl.GetFullyQualifiedName('.');
            gotoTransitions = null;
            pushTransitions = null;
        }

        public Dictionary<string, string> GetGotoTransitions()
        {
            // TODO: Inherited transitions.
            return ComputeGotoTransitions(false);
        }

        public Dictionary<string, string> GetPushTransitions()
        {
            // TODO: Inherited transitions.
            return ComputePushTransitions(false);
        }

        private Dictionary<string, string> ComputeGotoTransitions(bool recompute)
        {
            if (gotoTransitions == null || recompute)
            {
                foreach (var kvpair in stateDeclaration.GotoStateTransitions)
                {
                    EventInfo eventInfo = this.machineInfo.LookupEvent(kvpair.Key.Text);

                    string destStateName = ResolutionHelper.TokenListToStateName(kvpair.Value);
                    StateInfo destStateInfo = machineInfo.LookupState(destStateName, stateDeclaration.Group);
                    gotoTransitions.Add(eventInfo.uniqueName, destStateInfo.uniqueName);
                }
            }
            return gotoTransitions;
        }

        private Dictionary<string, string> ComputePushTransitions(bool recompute)
        {
            if (pushTransitions == null || recompute)
            {
                foreach (var kvpair in stateDeclaration.PushStateTransitions)
                {
                    EventInfo eventInfo = this.machineInfo.LookupEvent(kvpair.Key.Text);

                    string destStateName = ResolutionHelper.TokenListToStateName(kvpair.Value);
                    StateInfo destStateInfo = machineInfo.LookupState(destStateName, stateDeclaration.Group);
                    pushTransitions.Add(eventInfo.uniqueName, destStateInfo.uniqueName);
                }
            }
            return pushTransitions;
        }
    }
}