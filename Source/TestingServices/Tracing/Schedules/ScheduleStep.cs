// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.TestingServices.StateCaching;

namespace Microsoft.PSharp.TestingServices.Tracing.Schedule
{
    /// <summary>
    /// Class implementing a P# program schedule step.
    /// </summary>
    public sealed class ScheduleStep
    {
        /// <summary>
        /// The unique index of this schedule step.
        /// </summary>
        internal int Index;

        /// <summary>
        /// The type of this schedule step.
        /// </summary>
        internal ScheduleStepType Type { get; private set; }

        /// <summary>
        /// The id of the scheduled machine. Only relevant if this is
        /// a regular schedule step.
        /// </summary>
        internal ulong ScheduledMachineId;

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

        /// <summary>
        /// Snapshot of the program state in this schedule step.
        /// </summary>
        internal State State;

        /// <summary>
        /// Creates a schedule step.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="scheduledMachineId">Scheduled machine id</param>
        /// <returns>ScheduleStep</returns>
        internal static ScheduleStep CreateSchedulingChoice(int index, ulong scheduledMachineId)
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

            return Index == step.Index;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
    }
}
