// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel
{
    /// <summary>
    /// Helps navigate the Partial order.
    /// </summary>
    public class ProgramReplayHelper
    {
        private readonly ProgramStep RootStep;

        // Iteration variables
        private readonly HashSet<ProgramStep> SeenSteps;

        // Would ideally have been a priority queue on the totalOrderingIndex field
        private readonly HashSet<ProgramStep> EnabledSteps;

        private readonly Dictionary<ProgramStep, ulong> MatchingSendIndices;

        // Used to track what's being executed, for the boolean & int choices.
        private ProgramStep CurrentStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramReplayHelper"/> class.
        /// </summary>
        /// <param name="rootStep">The root of the partial order to be replayed</param>
        public ProgramReplayHelper(ProgramStep rootStep)
        {
            this.RootStep = rootStep;

            this.SeenSteps = new HashSet<ProgramStep>();
            this.EnabledSteps = new HashSet<ProgramStep>();
            this.MatchingSendIndices = new Dictionary<ProgramStep, ulong>();

            this.ExecuteFirstStep();
        }

        private void ExecuteFirstStep()
        {
            this.SeenSteps.Add(this.RootStep);
            if (this.RootStep.CreatedStep != null)
            {
                this.EnabledSteps.Add(this.RootStep.CreatedStep);
            }

            if (this.RootStep.NextMachineStep != null)
            {
                this.EnabledSteps.Add(this.RootStep.NextMachineStep);
            }
        }

        /// <summary>
        /// Writtens the list of steps which can be scheduled, while respecting the partial order.
        /// </summary>
        /// <returns>The list of candidate steps ( or the first )</returns>
        public List<ProgramStep> GetNextSchedulableSteps()
        {
            List<ProgramStep> nextSteps = new List<ProgramStep>();

            foreach (ProgramStep step in this.EnabledSteps)
            {
                // if (this.NullOrSeen(step.PrevMachineStep) && this.NullOrSeen(step.CreatorParent)) // Will not be in enabled unless this holds true.
                if ( step.ProgramStepType == ProgramStepType.SchedulableStep &&
                    (this.NullOrSeen(step.PrevEnqueuedStep) && this.NullOrSeenAll(step.PrevMonitorSteps)) )
                {
                    nextSteps.Add(step);
                }
            }

            return nextSteps;
        }

        /// <summary>
        /// Returns the boolean choice that was taken at this point
        /// </summary>
        /// <returns>the boolean choice that was taken at this point</returns>
        public ProgramStep GetNextBooleanStep()
        {
            return this.EnabledSteps.First( s => s.SrcId == this.CurrentStep.SrcId && s.ProgramStepType == ProgramStepType.NonDetBoolStep);
        }

        internal List<ProgramStep> GetEnabledSteps(Dictionary<ulong, IAsyncOperation> enabledOps)
        {
            IEnumerable<ProgramStep> enabled = this.GetNextSchedulableSteps().Where( s => enabledOps.ContainsKey(s.SrcId) && s.OpType == enabledOps[s.SrcId].Type).ToList();
            IEnumerable<ProgramStep> badReceives = enabled.Where(s => s.OpType == AsyncOperationType.Receive && this.MatchingSendIndices[s.CreatorParent] != enabledOps[s.SrcId].MatchingSendIndex).ToList();
            return enabled.Except(badReceives).ToList();
        }

        /// <summary>
        /// Returns the integer choice that was taken at this point
        /// </summary>
        /// <returns>the integer choice that was taken at this point</returns>
        public ProgramStep GetNextIntegerStep()
        {
            return this.EnabledSteps.First(s => s.SrcId == this.CurrentStep.SrcId && s.ProgramStepType == ProgramStepType.NonDetIntStep);
        }

        /// <summary>
        /// Let us know that step was scheduled
        /// </summary>
        /// <param name="step">The step that was scheduled</param>
        /// <param name="scheduleIndex">The schedule step at which this step was scheduled. Used for MatchingSendIndex matching</param>
        public void RecordChoice(ProgramStep step, int scheduleIndex)
        {
            // Should we do a check that this was legal? Nah.
            if (!this.EnabledSteps.Contains(step))
            {
                throw new System.ArgumentException("The scheduled step was not found in the enabled step list");
            }

            this.SeenSteps.Add(step);

            // Update enabled steps
            this.EnabledSteps.Remove(step);
            if (step.CreatedStep != null)
            {
                this.EnabledSteps.Add(step.CreatedStep);
            }

            if (step.NextMachineStep != null && step.CreatedStep != step.NextMachineStep)
            {
                this.EnabledSteps.Add(step.NextMachineStep);
            }

            if ( step.OpType == AsyncOperationType.Send )
            {
                this.MatchingSendIndices[step] = (ulong)scheduleIndex;
            }

            if (step.OpType == AsyncOperationType.Receive)
            {
                this.MatchingSendIndices.Remove(step.CreatorParent);
            }

            this.CurrentStep = step;
        }

        // If all Prev*Step's of a step are NullOrSeen, it means it can be (or has been) scheduled.
        private bool NullOrSeen(ProgramStep step)
        {
            return step == null || this.SeenSteps.Contains(step);
        }

        private bool NullOrSeenAll(Dictionary<Type, ProgramStep> steps)
        {
            return steps == null || steps.All( s => this.SeenSteps.Contains(s.Value));
        }

        /// <summary>
        /// Tells us whether the whole program has been replayed.
        /// </summary>
        /// <returns>true if the whole program has been replayed</returns>
        public bool HasReachedEnd()
        {
            // Assuming the whole graph is connected, this should be fine.
            return this.EnabledSteps.Count == 0 && this.SeenSteps.Count > 0;
        }
    }
}
