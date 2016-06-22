//-----------------------------------------------------------------------
// <copyright file="ScheduleTrace.cs">
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

namespace Microsoft.PSharp.TestingServices.Tracing.Schedule
{
    /// <summary>
    /// Class implementing a P# program schedule trace.
    /// A trace is a series of transitions from some
    /// initial state to some end state.
    /// </summary>
    internal sealed class ScheduleTrace : IEnumerable, IEnumerable<ScheduleStep>
    {
        #region fields

        /// <summary>
        /// The steps of the schedule trace.
        /// </summary>
        private List<ScheduleStep> Steps;

        /// <summary>
        /// The number of steps in the schedule trace.
        /// </summary>
        internal int Count
        {
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Index for the schedule trace.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>ScheduleStep</returns>
        internal ScheduleStep this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal ScheduleTrace()
        {
            this.Steps = new List<ScheduleStep>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="traceDump">Trace</param>
        internal ScheduleTrace(string[] traceDump)
        {
            this.Steps = new List<ScheduleStep>();

            foreach (var step in traceDump)
            {
                if (step.Equals("True"))
                {
                    this.AddNondeterministicChoice(true);
                }
                else if (step.Equals("False"))
                {
                    this.AddNondeterministicChoice(false);
                }
                else
                {
                    string[] machineId = step.TrimEnd(')').Split('(');
                    this.AddSchedulingChoice(machineId[0], int.Parse(machineId[1]));
                }
            }
        }

        /// <summary>
        /// Adds a scheduling choice.
        /// </summary>
        /// <param name="scheduledMachine">Scheduled machine</param>
        internal void AddSchedulingChoice(AbstractMachine scheduledMachine)
        {
            var scheduleStep = ScheduleStep.CreateSchedulingChoice(this.Count, scheduledMachine);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a scheduling choice.
        /// </summary>
        /// <param name="scheduledMachineType">Scheduled machine type</param>
        /// <param name="scheduledMachineId">Scheduled machine id</param>
        internal void AddSchedulingChoice(string scheduledMachineType, int scheduledMachineId)
        {
            var scheduleStep = ScheduleStep.CreateSchedulingChoice(this.Count,
                scheduledMachineType, scheduledMachineId);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic choice.
        /// </summary>
        /// <param name="choice">Choice</param>
        internal void AddNondeterministicChoice(bool choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicChoice(this.Count, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a fair nondeterministic choice.
        /// </summary>
        /// <param name="uniqueId">Unique nondet id</param>
        /// <param name="choice">Choice</param>
        internal void AddFairNondeterministicChoice(string uniqueId, bool choice)
        {
            var scheduleStep = ScheduleStep.CreateFairNondeterministicChoice(this.Count, uniqueId, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest schedule step and removes
        /// it from the trace.
        /// </summary>
        /// <returns>ScheduleStep</returns>
        internal ScheduleStep Pop()
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
        /// Returns the latest schedule step without removing it.
        /// </summary>
        /// <returns>ScheduleStep</returns>
        internal ScheduleStep Peek()
        {
            ScheduleStep step = null;

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
        IEnumerator<ScheduleStep> IEnumerable<ScheduleStep>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        /// <param name="step">ScheduleStep</param>
        private void Push(ScheduleStep step)
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
