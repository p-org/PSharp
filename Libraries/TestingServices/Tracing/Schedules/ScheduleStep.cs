//-----------------------------------------------------------------------
// <copyright file="ScheduleStep.cs">
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

namespace Microsoft.PSharp.TestingServices.Tracing.Schedule
{
    /// <summary>
    /// Class implementing a P# program schedule step.
    /// </summary>
    internal sealed class ScheduleStep
    {
        #region fields

        /// <summary>
        /// The unique index of this schedule step.
        /// </summary>
        internal int Index;

        /// <summary>
        /// The type of this schedule step.
        /// </summary>
        internal ScheduleStepType Type { get; private set; }

        /// <summary>
        /// The scheduled machine id. Only relevant if this is
        /// a regular schedule step.
        /// </summary>
        internal MachineId ScheduledMachineId;

        /// <summary>
        /// The non-deterministic choice id. Only relevant if
        /// this is a choice schedule step.
        /// </summary>
        internal string NondetId;

        /// <summary>
        /// The non-deterministic boolean choice value. Only relevant if
        /// this is a choice schedule step.
        /// </summary>
        internal bool? BooleanChoice;

        /// <summary>
        /// The non-deterministic integer choice value. Only relevant if
        /// this is a choice schedule step.
        /// </summary>
        internal int? IntegerChoice;

        /// <summary>
        /// Previous schedule step.
        /// </summary>
        internal ScheduleStep Previous;

        /// <summary>
        /// Next schedule step.
        /// </summary>
        internal ScheduleStep Next;

        #endregion

        #region internal methods

        /// <summary>
        /// Creates a schedule step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="scheduledMachineId">Scheduled machine id</param>
        /// <returns>ScheduleStep</returns>
        internal static ScheduleStep CreateSchedulingChoice(int index, MachineId scheduledMachineId)
        {
            var scheduleStep = new ScheduleStep();

            scheduleStep.Index = index;
            scheduleStep.Type = ScheduleStepType.SchedulingChoice;

            scheduleStep.ScheduledMachineId = scheduledMachineId;

            scheduleStep.BooleanChoice = null;
            scheduleStep.IntegerChoice = null;

            scheduleStep.Previous = null;
            scheduleStep.Next = null;
            
            return scheduleStep;
        }

        /// <summary>
        /// Creates a nondeterministic boolean choice schedule step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="choice">Choice</param>
        /// <returns>ScheduleStep</returns>
        internal static ScheduleStep CreateNondeterministicBooleanChoice(int index, bool choice)
        {
            var scheduleStep = new ScheduleStep();

            scheduleStep.Index = index;
            scheduleStep.Type = ScheduleStepType.NondeterministicChoice;

            scheduleStep.BooleanChoice = choice;
            scheduleStep.IntegerChoice = null;

            scheduleStep.Previous = null;
            scheduleStep.Next = null;
            
            return scheduleStep;
        }

        /// <summary>
        /// Creates a fair nondeterministic boolean choice schedule step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="uniqueId">Unique id</param>
        /// <param name="choice">Choice</param>
        /// <returns>ScheduleStep</returns>
        internal static ScheduleStep CreateFairNondeterministicBooleanChoice(
            int index, string uniqueId, bool choice)
        {
            var scheduleStep = new ScheduleStep();

            scheduleStep.Index = index;
            scheduleStep.Type = ScheduleStepType.FairNondeterministicChoice;

            scheduleStep.NondetId = uniqueId;
            scheduleStep.BooleanChoice = choice;
            scheduleStep.IntegerChoice = null;

            scheduleStep.Previous = null;
            scheduleStep.Next = null;
            
            return scheduleStep;
        }

        /// <summary>
        /// Creates a nondeterministic integer choice schedule step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="choice">Choice</param>
        /// <returns>ScheduleStep</returns>
        internal static ScheduleStep CreateNondeterministicIntegerChoice(int index, int choice)
        {
            var scheduleStep = new ScheduleStep();

            scheduleStep.Index = index;
            scheduleStep.Type = ScheduleStepType.NondeterministicChoice;

            scheduleStep.BooleanChoice = null;
            scheduleStep.IntegerChoice = choice;

            scheduleStep.Previous = null;
            scheduleStep.Next = null;
            
            return scheduleStep;
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

            ScheduleStep step = obj as ScheduleStep;
            if (step == null)
            {
                return false;
            }

            return this.Index == step.Index;
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
