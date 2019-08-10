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
        // Maps a MachineId in our partial order to the actual one in the run
        private readonly Dictionary<ulong, ulong> MachineIdRemap;
        private readonly HashSet<ProgramStep> DroppedSteps;

        /// <summary>
        /// Used to track what's being executed, for the boolean and int choices.
        /// </summary>
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
            this.MachineIdRemap = new Dictionary<ulong, ulong>();
            this.DroppedSteps = new HashSet<ProgramStep>();
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

            // I really hope this is right
            this.MachineIdRemap.Add(this.RootStep.SrcId, this.RootStep.SrcId);
        }

        /// <summary>
        /// Writtens the list of steps which can be scheduled, while respecting the partial order.
        /// </summary>
        /// <returns>The list of candidate steps ( or the first )</returns>
        public List<ProgramStep> GetNextSchedulableSteps()
        {
            return this.EnabledSteps.Where( step =>
                    step.ProgramStepType == ProgramStepType.SchedulableStep &&
                    this.NullOrSeen(step.PrevEnqueuedStep) && this.NullOrSeenAll(step.PrevMonitorSteps)).ToList();
        }

        /// <summary>
        /// Returns the boolean choice that was taken at this point
        /// </summary>
        /// <returns>the boolean choice that was taken at this point</returns>
        public ProgramStep GetNextBooleanStep()
        {
            // List<ProgramStep> candidateSteps = this.EnabledSteps.Where(s => s.SrcId == this.CurrentStep.SrcId && s.ProgramStepType == ProgramStepType.NonDetBoolStep).ToList();
            // return candidateSteps.Count > 0 ? candidateSteps[0] : null;
            return this.EnabledSteps.FirstOrDefault(s => s.SrcId == this.CurrentStep.SrcId && s.ProgramStepType == ProgramStepType.NonDetBoolStep);
        }

        internal Dictionary<ProgramStep, IAsyncOperation> GetEnabledSteps(List<IAsyncOperation> enabledOpsList)
        {
            Dictionary<ulong, IAsyncOperation> enabledOps = enabledOpsList.ToDictionary(x => x.SourceId);
            IEnumerable<ProgramStep> enabled = this.GetNextSchedulableSteps().Where( s => enabledOps.ContainsKey( this.MachineIdRemap[s.SrcId] ) && s.OpType == enabledOps[this.MachineIdRemap[s.SrcId]].Type).ToList();
            IEnumerable<ProgramStep> badReceives = enabled.Where(s => s.OpType == AsyncOperationType.Receive && this.MatchingSendIndices[s.CreatorParent] != enabledOps[this.MachineIdRemap[s.SrcId]].MatchingSendIndex).ToList();
            return enabled.Except(badReceives).ToDictionary( x => x, x => enabledOps[this.MachineIdRemap[x.SrcId]]);
        }

        /// <summary>
        /// Returns the integer choice that was taken at this point
        /// </summary>
        /// <returns>the integer choice that was taken at this point</returns>
        public ProgramStep GetNextIntegerStep()
        {
            return this.EnabledSteps.FirstOrDefault(s => s.SrcId == this.CurrentStep.SrcId && s.ProgramStepType == ProgramStepType.NonDetIntStep);
            // List<ProgramStep> candidateSteps = this.EnabledSteps.Where(s => s.SrcId == this.CurrentStep.SrcId && s.ProgramStepType == ProgramStepType.NonDetIntStep).ToList();
            // return candidateSteps.Count > 0 ? candidateSteps[0] : null;
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

        /// <summary>
        /// Let's the replay helper know we didn't enqueue this message.
        /// </summary>
        /// <param name="sendStep">The send step of the event which was not enqueued</param>
        public void RecordSendDropped(ProgramStep sendStep)
        {
            if (sendStep != this.CurrentStep)
            {
                throw new ArgumentException("DEBUG: Why are they not equal?");
            }

            this.DropStepsRecursive(sendStep.CreatedStep); // this.DroppedSteps.Add(sendStep);

            this.EnabledSteps.Remove(sendStep.CreatedStep);
        }

        private void DropStepsRecursive(ProgramStep step)
        {
            if (step != null)
            {
                this.DroppedSteps.Add(step);
                this.DropStepsRecursive(step.CreatedStep);
                this.DropStepsRecursive(step.NextMachineStep);
            }
        }

        // If all Prev*Step's of a step are NullOrSeen, it means it can be (or has been) scheduled.
        private bool NullOrSeen(ProgramStep step)
        {
            return step == null || this.SeenSteps.Contains(step) || this.DroppedSteps.Contains(step);
        }

        private bool NullOrSeenAll(Dictionary<Type, ProgramStep> steps)
        {
            return steps == null || steps.All( s => this.SeenSteps.Contains(s.Value) || this.DroppedSteps.Contains(s.Value) );
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

        internal void RecordCreateMachine(Machine creatorMachine, Machine createdMachine)
        {
            if ( creatorMachine != null && this.CurrentStep.SrcId != this.MachineIdRemap[creatorMachine.Id.Value])
            {
                throw new NotImplementedException("This is not implemented correctly at all");
            }

            this.MachineIdRemap.Add(this.CurrentStep.TargetId, createdMachine.Id.Value);
        }

        /// <summary>
        /// Returns the current ProgramStep being replayed
        /// </summary>
        /// <returns>The current ProgramStep being replayed</returns>
        public ProgramStep GetCurrentStep()
        {
            return this.CurrentStep;
        }

        /// <summary>
        /// Checks if step is the last Schedulable step of its machine in the graph
        /// </summary>
        /// <param name="step">The step to check for</param>
        /// <returns>true if it is the last Schedulable step of its machine in the graph</returns>
        public static bool IsLastSchedulableStepOfMachine(ProgramStep step)
        {
            if (step.NextEnqueuedStep == null)
            {
                while (step.NextMachineStep != null && step.NextMachineStep.ProgramStepType != ProgramStepType.SchedulableStep)
                {
                    step = step.NextMachineStep;
                }

                return step.NextMachineStep == null;
            }

            return false;
        }
    }
}
