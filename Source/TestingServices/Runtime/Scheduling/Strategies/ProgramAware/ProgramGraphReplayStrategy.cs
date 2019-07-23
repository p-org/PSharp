// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware
{
    /// <summary>
    /// Accepts a partial-order ( represented by a single root-node and the closure of nodes reachable through edges ).
    /// (Attempts to ) replay the schedule represented by it, subject to inconsistencies between the partial-order and actual program behaviour.
    /// </summary>
    public class ProgramGraphReplayStrategy : AbstractBaseProgramModelStrategy
    {
        /// <summary>
        /// The root of the partial order
        /// </summary>
        private readonly IProgramStep RootStep;

        // Is the schedule represented by the partial order fair. ( doesn't mean our replay will be )
        private readonly bool IsScheduleFair;

        private ProgramReplayHelper ProgramReplayHelper;

        // The regular stuff
        private int ScheduledSteps;

        private IProgramStep currentlyChosenStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramGraphReplayStrategy"/> class.
        /// </summary>
        /// <param name="rootStep">The first (root) step of the program. All steps must be reachable from this to be replayed.</param>
        /// <param name="isScheduleFair">Is the schedule represented by the partial order fair</param>
        public ProgramGraphReplayStrategy(IProgramStep rootStep, bool isScheduleFair)
            : base()
        {
            this.RootStep = rootStep;
            this.IsScheduleFair = isScheduleFair;
            this.ResetProgramReplayHelper(rootStep);
            this.ScheduledSteps = 0;
        }

        /// <inheritdoc/>
        protected override bool HashEvents => false;

        /// <inheritdoc/>
        public override void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            throw new System.NotImplementedException("You cannot force this replay strategy");
        }

        /// <inheritdoc/>
        public override void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new System.NotImplementedException("You cannot force this replay strategy.");
        }

        /// <inheritdoc/>
        public override void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new System.NotImplementedException("You cannot force this replay strategy");
        }

        /// <inheritdoc/>
        public override string GetDescription()
        {
            return "Replays the partial order represented by the program model";
        }

        /// <inheritdoc/>
        public override bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            Console.WriteLine("In GetNext");
            Dictionary<ulong, IAsyncOperation> enabledOps = ops.Where(o => o.IsEnabled).ToDictionary( o => o.SourceId );
            List<IProgramStep> candidateSteps = this.ProgramReplayHelper.GetEnabledSteps(enabledOps);

            if (candidateSteps.Count > 0)
            {
                this.ScheduledSteps++;

                IProgramStep chosenStep = candidateSteps.First();
                next = enabledOps[chosenStep.SrcId];
                this.ProgramReplayHelper.RecordChoice(chosenStep, this.ScheduledSteps);

                Console.WriteLine("Out GetNext");
                this.currentlyChosenStep = chosenStep;

                return true;
            }
            else
            {
                next = null;
                Console.WriteLine("Fail GetNext");
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            Console.WriteLine("In GetBool");
            IProgramStep candidateStep = this.ProgramReplayHelper.GetNextBooleanStep();
            if (candidateStep != null && candidateStep.BooleanChoice != null)
            {
                this.ScheduledSteps++;

                this.ProgramReplayHelper.RecordChoice(candidateStep, this.ScheduledSteps);
                next = (bool)candidateStep.BooleanChoice;

                Console.WriteLine("Out GetBool");
                return true;
            }
            else
            {
                next = false;
                Console.WriteLine("Fail GetBool");
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool GetNextIntegerChoice(int maxValue, out int next)
        {
            Console.WriteLine("In GetInt");
            IProgramStep candidateStep = this.ProgramReplayHelper.GetNextIntegerStep();
            if (candidateStep != null && candidateStep.BooleanChoice != null)
            {
                this.ScheduledSteps++;
                this.ProgramReplayHelper.RecordChoice(candidateStep, this.ScheduledSteps);
                next = (int)candidateStep.IntChoice;

                Console.WriteLine("Out GetInt");
                return true;
            }
            else
            {
                next = 0;
                Console.WriteLine("Fail GetInt");
                return false;
            }
        }

        /// <inheritdoc/>
        public override int GetScheduledSteps()
        {
            return this.ScheduledSteps;
        }

        /// <inheritdoc/>
        public override bool HasReachedMaxSchedulingSteps()
        {
            // return this.ScheduledSteps < this.MaxSteps;
            return this.ProgramReplayHelper.HasReachedEnd();
        }

        /// <inheritdoc/>
        public override bool IsFair()
        {
            return this.IsScheduleFair;
        }

        /// <inheritdoc/>
        public override void NotifySchedulingEnded(bool bugFound)
        {
        }

        /// <inheritdoc/>
        public override bool PrepareForNextIteration()
        {
            this.ResetProgramReplayHelper(this.RootStep);
            this.ScheduledSteps = 0;
            return true;
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            this.ResetProgramReplayHelper(this.RootStep);
            this.ScheduledSteps = 0;
        }

        private void ResetProgramReplayHelper(IProgramStep rootStep)
        {
            this.ProgramReplayHelper = new ProgramReplayHelper(rootStep);
        }

        /// <inheritdoc/>
        public override void RecordCreateMachine(Machine createdMachine, Machine creatorMachine)
        {
            base.RecordCreateMachine(createdMachine, creatorMachine);
            if ( this.currentlyChosenStep.TargetId != createdMachine.Id.Value)
            {
                Console.WriteLine("Aha. We need invariance");
            }
        }
    }
}
