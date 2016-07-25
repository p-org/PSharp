//-----------------------------------------------------------------------
// <copyright file="CoverageInfo.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Class for storing coverage-specific data.
    /// </summary>
    [DataContract]
    public class CoverageInfo
    {
        #region fields

        /// <summary>
        /// Map from machines to states.
        /// </summary>
        [DataMember]
        internal Dictionary<string, HashSet<string>> MachinesToStates { get; }

        /// <summary>
        /// Set of (machines, states, registered events).
        /// </summary>
        [DataMember]
        internal HashSet<Tuple<string, string, string>> RegisteredEvents { get; }

        /// <summary>
        /// Set of machine transitions.
        /// </summary>
        [DataMember]
        internal HashSet<Transition> Transitions { get; }

        #endregion

        #region constructors and destructor

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