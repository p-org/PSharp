//-----------------------------------------------------------------------
// <copyright file="AbstractMachine.cs">
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
using System.ComponentModel;
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a P# machine.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class AbstractMachine
    {
        #region fields

        /// <summary>
        /// The runtime that executes this machine.
        /// </summary>
        internal PSharpRuntime Runtime { get; private set; }

        /// <summary>
        /// The unique machine id.
        /// </summary>
        protected internal MachineId Id { get; private set; }

        /// <summary>
        /// Stores machine-related information, which can used
        /// for scheduling and testing.
        /// </summary>
        internal MachineInfo Info { get; private set; }

        /// <summary>
        /// Gets the name of the current state.
        /// </summary>
        abstract internal string CurrentStateName { get; }

        /// <summary>
        /// Is the machine executing under a synchronous call. This includes
        /// the methods CreateMachineAndExecute and SendEventAndExecute.
        /// </summary>
        internal bool IsInsideSynchronousCall;

        /// <summary>
        /// Gets the <see cref="Type"/> of the current state.
        /// </summary>
        abstract protected internal Type CurrentState { get; }

        #endregion

        #region initialize

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="mid">MachineId</param>
        /// <param name="info">MachineInfo</param>
        internal void Initialize(PSharpRuntime runtime, MachineId mid, MachineInfo info)
        {
            this.Runtime = mid.Runtime;
            this.Id = mid;
            this.Info = info;
        }

        #endregion

        #region basic machine operations

        /// <summary>
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        /// <param name="e">Event</param>
        internal abstract Task GotoStartState(Event e);

        /// <summary>
        /// Enqueues the specified <see cref="EventInfo"/>.
        /// </summary>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="runNewHandler">Run a new handler</param>
        internal abstract void Enqueue(EventInfo eventInfo, ref bool runNewHandler);

        /// <summary>
        /// Dequeues the next available <see cref="EventInfo"/> from the
        /// inbox if there is one available, else returns null.
        /// </summary>
        /// <param name="checkOnly">Only check if event can get dequeued, do not modify inbox</param>
        /// <returns>EventInfo</returns>
        internal abstract EventInfo TryDequeueEvent(bool checkOnly = false);

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        /// <param name="returnEarly">Returns after handling just one event</param>
        internal abstract Task<bool> RunEventHandler(bool returnEarly = false);

        #endregion

        #region state caching

        /// <summary>
        /// Returns the cached state of this machine.
        /// </summary>
        /// <returns>Hash value</returns>
        internal abstract int GetCachedState();

        #endregion

        #region logging routines

        /// <summary>
        /// Returns the type of the state at the specified state
        /// stack index, if there is one.
        /// </summary>
        /// <param name="index">State stack index</param>
        /// <returns>Type</returns>
        internal abstract Type GetStateTypeAtStackIndex(int index);

        /// <summary>
        /// Returns the names of the events that the machine
        /// is waiting to receive. This is not thread safe.
        /// </summary>
        /// <returns>string</returns>
        internal abstract string GetEventWaitHandlerNames();

        #endregion

        #region generic public and override methods

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            AbstractMachine m = obj as AbstractMachine;
            if (m == null ||
                this.GetType() != m.GetType())
            {
                return false;
            }

            return this.Id.Value == m.Id.Value;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.Id.Name;
        }

        #endregion

        #region Code Coverage Methods

        /// <summary>
        /// Returns the set of all states in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the machine</returns>
        internal virtual HashSet<string> GetAllStates()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all (states, registered event) pairs in the machine</returns>
        internal virtual HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            return new HashSet<Tuple<string, string>>();
        }

        /// <summary>
        /// Returns the destination state for an event (if any)
        /// (for code coverage).
        /// </summary>
        /// <returns>Destination state, or null</returns>
        internal virtual Type GetDestinationState(Type eventType)
        {
            return null;
        }

        #endregion
    }
}
