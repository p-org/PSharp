// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.TestingServices.Coverage
{
    /// <summary>
    /// Class for storing coverage-specific data
    /// across multiple testing iterations. 
    /// </summary>
    [DataContract]
    public class CoverageInfo
    {
        #region fields

        /// <summary>
        /// Map from machines to states.
        /// </summary>
        [DataMember]
        public Dictionary<string, HashSet<string>> MachinesToStates { get; private set; }

        /// <summary>
        /// Set of (machines, states, registered events).
        /// </summary>
        [DataMember]
        public HashSet<Tuple<string, string, string>> RegisteredEvents { get; private set; }

        /// <summary>
        /// Set of machine transitions.
        /// </summary>
        [DataMember]
        public HashSet<Transition> Transitions { get; private set; }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public CoverageInfo()
        {
            this.MachinesToStates = new Dictionary<string, HashSet<string>>();
            this.RegisteredEvents = new HashSet<Tuple<string, string, string>>();
            this.Transitions = new HashSet<Transition>();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Adds a new transition.
        /// </summary>
        /// <param name="machineOrigin">Origin machine</param>
        /// <param name="stateOrigin">Origin state</param>
        /// <param name="edgeLabel">Edge label</param>
        /// <param name="machineTarget">Target machine</param>
        /// <param name="stateTarget">Target state</param>
        public void AddTransition(string machineOrigin, string stateOrigin, string edgeLabel,
            string machineTarget, string stateTarget)
        {
            this.AddState(machineOrigin, stateOrigin);
            this.AddState(machineTarget, stateTarget);
            this.Transitions.Add(new Transition(machineOrigin, stateOrigin,
                edgeLabel, machineTarget, stateTarget));
        }

        /// <summary>
        /// Declares a state.
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <param name="state">state name</param>
        public void DeclareMachineState(string machine, string state)
        {
            this.AddState(machine, state);
        }

        /// <summary>
        /// Declares a registered state, event pair.
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <param name="state">state name</param>
        /// <param name="eventName">Event name that the state is prepared to handle</param>
        public void DeclareStateEvent(string machine, string state, string eventName)
        {
            this.AddState(machine, state);
            this.RegisteredEvents.Add(Tuple.Create(machine, state, eventName));
        }

        /// <summary>
        /// Merges the information from the specified
        /// coverage info. This is not thread-safe.
        /// </summary>
        /// <param name="coverageInfo">CoverageInfo</param>
        public void Merge(CoverageInfo coverageInfo)
        {
            foreach (var machine in coverageInfo.MachinesToStates)
            {
                foreach (var state in machine.Value)
                {
                    this.DeclareMachineState(machine.Key, state);
                }
            }

            foreach (var tup in coverageInfo.RegisteredEvents)
            {
                this.DeclareStateEvent(tup.Item1, tup.Item2, tup.Item3);
            }

            foreach (var transition in coverageInfo.Transitions)
            {
                this.AddTransition(transition.MachineOrigin, transition.StateOrigin,
                    transition.EdgeLabel, transition.MachineTarget, transition.StateTarget);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Adds a new state.
        /// </summary>
        /// <param name="machineName">Machine name</param>
        /// <param name="stateName">State name</param>
        private void AddState(string machineName, string stateName)
        {
            if (!this.MachinesToStates.ContainsKey(machineName))
            {
                this.MachinesToStates.Add(machineName, new HashSet<string>());
            }

            this.MachinesToStates[machineName].Add(stateName);
        }

        #endregion
    }
}