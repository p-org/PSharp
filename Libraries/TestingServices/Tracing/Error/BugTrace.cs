//-----------------------------------------------------------------------
// <copyright file="BugTrace.cs">
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

using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.TestingServices.Tracing.Error
{
    /// <summary>
    /// Class implementing a P# bug trace. A trace is a
    /// series of transitions from some initial state to
    /// some end state.
    /// </summary>
    [DataContract]
    internal sealed class BugTrace : IEnumerable, IEnumerable<BugTraceStep>
    {
        #region fields

        /// <summary>
        /// The steps of the bug trace.
        /// </summary>
        [DataMember]
        private List<BugTraceStep> Steps;

        /// <summary>
        /// The number of steps in the bug trace.
        /// </summary>
        internal int Count
        {
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Index for the bug trace.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>BugTraceStep</returns>
        internal BugTraceStep this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal BugTrace()
        {
            this.Steps = new List<BugTraceStep>();
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="targetMachine">Target machine</param>
        internal void AddCreateMachineStep(MachineId machine, MachineId targetMachine)
        {
            var scheduleStep = BugTraceStep.GetCreateMachineStep(this.Count,
                machine, targetMachine);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="targetMachine">Target machine</param>
        /// <param name="e">Event</param>
        internal void AddSendEventStep(MachineId machine, MachineId targetMachine, Event e)
        {
            var scheduleStep = BugTraceStep.GetSendEventStep(this.Count,
                machine, targetMachine, e);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest bug trace step and
        /// removes it from the trace.
        /// </summary>
        /// <returns>BugTraceStep</returns>
        internal BugTraceStep Pop()
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = null;
            }

            var step = this.Steps[this.Count - 1];
            this.Steps.RemoveAt(this.Count - 1);

            return step;
        }

        /// <summary>
        /// Returns the latest bug trace step
        /// without removing it.
        /// </summary>
        /// <returns>BugTraceStep</returns>
        internal BugTraceStep Peek()
        {
            BugTraceStep step = null;

            if (this.Steps.Count > 0)
            {
                step = this.Steps[this.Count - 1];
            }
            
            return step;
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator<BugTraceStep> IEnumerable<BugTraceStep>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        /// <param name="step">BugTraceStep</param>
        private void Push(BugTraceStep step)
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = step;
                step.Previous = this.Steps[this.Count - 1];
            }

            this.Steps.Add(step);
        }

        #endregion
    }
}
