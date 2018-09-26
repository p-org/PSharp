using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp.LanguageServices.Syntax;

/*
 * Known issues:
 *  - Overriding an event handler with a different type of event handler does not throw an error
 *      - e.g. : Overriding an OnEventDoAction with OnEventGoto
 */

namespace Microsoft.PSharp.StateDiagramViewer
{
    class StateInfo
    {
        #region fields
        public readonly string uniqueName;
        internal readonly MachineInfo machineInfo;
        internal StateDeclaration stateDeclaration { get; }
        internal Dictionary<string, string> gotoTransitions;
        internal Dictionary<string, string> pushTransitions;
        internal StateInfo baseState;

        internal bool isStartState;
        #endregion

        #region initialization methods        
        public StateInfo(StateDeclaration sdecl, MachineInfo machineInfo)
        {
            this.stateDeclaration = sdecl;
            this.machineInfo = machineInfo;
            this.uniqueName = machineInfo.uniqueName + '.' + sdecl.GetFullyQualifiedName('.');
            this.gotoTransitions = null;
            this.pushTransitions = null;
            this.isStartState = sdecl.IsStart;
            baseState = null;
        }

        public void ResolveBaseState()
        {
            if (stateDeclaration.BaseStateToken != null)
            {
                string baseStateName = stateDeclaration.BaseStateToken.Text;
                baseState = this.machineInfo.LookupState(stateDeclaration.BaseStateToken.Text, stateDeclaration.Group);
                if (baseState == null)
                {
                    throw new StateDiagramViewerUnresolvedTokenException(
                        String.Format("BaseState {0} not found for state {1}", baseStateName, this.uniqueName),
                        baseStateName,
                        this.uniqueName);
                }
            }
        }
        #endregion 

        #region transition methods
        public Dictionary<string, string> GetGotoTransitions(bool includeInherited=true)
        {
            Dictionary<string, string> transitions = new Dictionary<string, string>();
            if (includeInherited && baseState != null)
            {
                foreach ( var gt in baseState.GetGotoTransitions(includeInherited)) {
                    transitions[gt.Key] = gt.Value;
                }
            }
            foreach (var gt in ComputeGotoTransitions(false))
            {
                transitions[gt.Key] = gt.Value;
            }
            return transitions;
        }

        public Dictionary<string, string> GetPushTransitions(bool includeInherited = true)
        {
            Dictionary<string, string> transitions = new Dictionary<string, string>();
            if (includeInherited && baseState != null)
            {
                foreach (var gt in baseState.GetPushTransitions(includeInherited))
                {
                    transitions[gt.Key] = gt.Value;
                }
            }
            foreach (var gt in ComputePushTransitions(false))
            {
                transitions[gt.Key] = gt.Value;
            }
            return transitions;
        }

        private Dictionary<string, string> ComputeGotoTransitions(bool recompute)
        {
            if (gotoTransitions == null || recompute)
            {
                gotoTransitions = new Dictionary<string, string>();
                foreach (var kvpair in stateDeclaration.GotoStateTransitions)
                {
                    EventInfo eventInfo = this.machineInfo.LookupEvent(kvpair.Key.Text);
                    if (eventInfo == null)
                    {
                        throw new StateDiagramViewerUnresolvedTokenException(
                            String.Format("Event:{0} not found in machine:{1}", kvpair.Key.Text, this.machineInfo.uniqueName),
                            kvpair.Key.Text,
                            this.machineInfo.uniqueName
                            );
                    }
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
                pushTransitions = new Dictionary<string, string>();
                foreach (var kvpair in stateDeclaration.PushStateTransitions)
                {
                    EventInfo eventInfo = this.machineInfo.LookupEvent(kvpair.Key.Text);
                    if (eventInfo == null)
                    {
                        throw new StateDiagramViewerUnresolvedTokenException(
                            String.Format("Event:{0} not found in machine:{1}", kvpair.Key.Text, this.machineInfo.uniqueName),
                            kvpair.Key.Text,
                            this.machineInfo.uniqueName
                            );
                    }
                    string destStateName = ResolutionHelper.TokenListToStateName(kvpair.Value);
                    StateInfo destStateInfo = machineInfo.LookupState(destStateName, stateDeclaration.Group);
                    pushTransitions.Add(eventInfo.uniqueName, destStateInfo.uniqueName);
                }
            }
            return pushTransitions;
        }
        #endregion

        #region EventHandler methods
        public HashSet<string> GetIgnoredEvents(bool includeInherited = true)
        {
            HashSet<string> ignored = new HashSet<string>();
            if(includeInherited && baseState != null) {
                foreach(var evt in baseState.GetIgnoredEvents(includeInherited))
                {
                    ignored.Add(evt);
                }
            }
            foreach (var evt in stateDeclaration.IgnoredEvents.Select(s => s.Text))
            {
                ignored.Add(evt);
            }
            return ignored;
        }
        public HashSet<string> GetDeferredEvents(bool includeInherited = true)
        {
            HashSet<string> deferred = new HashSet<string>();
            if (includeInherited && baseState != null)
            {
                foreach (var evt in baseState.GetDeferredEvents(includeInherited))
                {
                    deferred.Add(evt);
                }
            }
            foreach (var evt in stateDeclaration.DeferredEvents.Select(s => s.Text))
            {
                deferred.Add(evt);
            }
            return deferred;
        }
        public HashSet<string> GetHandledEvents(bool includeInherited = true)
        {
            HashSet<string> handled = new HashSet<string>();
            if (includeInherited && baseState != null)
            {
                foreach (var evt in baseState.GetHandledEvents(includeInherited))
                {
                    handled.Add(evt);
                }
            }
            foreach (var evt in stateDeclaration.ActionBindings.Keys.Select(s => s.Text))
            {
                handled.Add(evt);
            }
            return handled;
        }
        #endregion
    }
}