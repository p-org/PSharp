//-----------------------------------------------------------------------
// <copyright file="Trace.cs">
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.TestingServices.Exploration
{
    /// <summary>
    /// Class implementing a P# program trace. A trace is a series of
    /// transitions from some initial state to some end state.
    /// </summary>
    internal sealed class Trace : IEnumerable, IEnumerable<TraceStep>
    {
        #region fields

        /// <summary>
        /// The steps of the trace.
        /// </summary>
        private List<TraceStep> Steps;

        /// <summary>
        /// The number of steps in the trace.
        /// </summary>
        internal int Count
        {
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Index for the trace.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>TraceStep</returns>
        internal TraceStep this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal Trace()
        {
            this.Steps = new List<TraceStep>();
        }

        internal Trace(string[] traceDump)
        {
            this.Steps = new List<TraceStep>();

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
                    string[] machineId = step.TrimStart('[').TrimEnd(']').Split(',');
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
            var traceStep = TraceStep.CreateSchedulingChoice(this.Count, scheduledMachine);
            this.Push(traceStep);
        }

        /// <summary>
        /// Adds a scheduling choice.
        /// </summary>
        /// <param name="scheduledMachineType">Scheduled machine type</param>
        /// <param name="scheduledMachineId">Scheduled machine id</param>
        internal void AddSchedulingChoice(string scheduledMachineType, int scheduledMachineId)
        {
            var traceStep = TraceStep.CreateSchedulingChoice(this.Count,
                scheduledMachineType, scheduledMachineId);
            this.Push(traceStep);
        }

        /// <summary>
        /// Adds a nondeterministic choice.
        /// </summary>
        /// <param name="choice">Choice</param>
        internal void AddNondeterministicChoice(bool choice)
        {
            var traceStep = TraceStep.CreateNondeterministicChoice(this.Count, choice);
            this.Push(traceStep);
        }

        /// <summary>
        /// Adds a fair nondeterministic choice.
        /// </summary>
        /// <param name="uniqueId">Unique nondet id</param>
        /// <param name="choice">Choice</param>
        internal void AddFairNondeterministicChoice(string uniqueId, bool choice)
        {
            var traceStep = TraceStep.CreateFairNondeterministicChoice(this.Count, uniqueId, choice);
            this.Push(traceStep);
        }

        /// <summary>
        /// Returns the latest program state and removes it from the trace.
        /// </summary>
        /// <returns>TraceStep</returns>
        internal TraceStep Pop()
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
        /// Returns the latest program state without removing it.
        /// </summary>
        /// <returns>TraceStep</returns>
        internal TraceStep Peek()
        {
            TraceStep step = null;

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
        IEnumerator<TraceStep> IEnumerable<TraceStep>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        /// <param name="step">TraceStep</param>
        private void Push(TraceStep step)
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
