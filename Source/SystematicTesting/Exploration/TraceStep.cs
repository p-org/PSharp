//-----------------------------------------------------------------------
// <copyright file="TraceStep.cs">
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

namespace Microsoft.PSharp.SystematicTesting.Exploration
{
    /// <summary>
    /// Class implementing a P# program trace step.
    /// </summary>
    internal sealed class TraceStep
    {
        #region fields

        /// <summary>
        /// The unique index of this trace step.
        /// </summary>
        internal int Index;

        /// <summary>
        /// The type of this trace step.
        /// </summary>
        internal TraceStepType Type { get; private set; }

        /// <summary>
        /// The scheduled machine. Only relevant if this is a scheduling
        /// trace step.
        /// </summary>
        internal AbstractMachine ScheduledMachine;

        /// <summary>
        /// The non-deterministic choice id. Only relevant if
        /// this is a choice trace step.
        /// </summary>
        internal string NondetId;

        /// <summary>
        /// The non-deterministic choice value. Only relevant if
        /// this is a choice trace step.
        /// </summary>
        internal bool Choice;

        /// <summary>
        /// Previous trace step.
        /// </summary>
        internal TraceStep Previous;

        /// <summary>
        /// Next trace step.
        /// </summary>
        internal TraceStep Next;

        #endregion

        #region internal API

        /// <summary>
        /// Creates a scheduling choice trace step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="scheduledMachine">Scheduled machine</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateSchedulingChoice(int index, AbstractMachine scheduledMachine)
        {
            var traceStep = new TraceStep();

            traceStep.Index = index;
            traceStep.Type = TraceStepType.SchedulingChoice;
            traceStep.ScheduledMachine = scheduledMachine;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        /// <summary>
        /// Creates a nondeterministic choice trace step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="choice">Choice</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateNondeterministicChoice(int index, bool choice)
        {
            var traceStep = new TraceStep();

            traceStep.Index = index;
            traceStep.Type = TraceStepType.NondeterministicChoice;
            traceStep.Choice = choice;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        /// <summary>
        /// Creates a fair nondeterministic choice trace step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="uniqueId">Unique id</param>
        /// <param name="choice">Choice</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateFairNondeterministicChoice(int index, string uniqueId, bool choice)
        {
            var traceStep = new TraceStep();

            traceStep.Index = index;
            traceStep.Type = TraceStepType.FairNondeterministicChoice;
            traceStep.NondetId = uniqueId;
            traceStep.Choice = choice;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

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

            TraceStep mid = obj as TraceStep;
            if (mid == null)
            {
                return false;
            }

            return this.Index == mid.Index;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Index.GetHashCode();
        }

        #endregion
    }
}
