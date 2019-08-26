// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp.Runtime;
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
        /// Tells us how many replays we need to do to be reasonably sure about whether we didn't get lucky with the bug.
        /// </summary>
        public static int NREPLAYSFORBUGREPRODUCTION { get => StaticValueNREPLAYSFORBUGREPRODUCTION; }

        /// <summary>
        /// If true, the program model considers the order of messages received by monitors as part of the partial order.
        /// Although it makes no difference to the ProgramModel itself, it does make a difference to the replay
        /// </summary>
        public static bool MonitorCommunicationIsPartOfPartialOrder = true;

        private static int StaticValueNREPLAYSFORBUGREPRODUCTION = 3;

        /// <summary>
        /// Allows the user to set how many replays are required to reproduce the bug
        /// before we conclude that the graph replay guarantees that the bug will be triggered ( regardless of the suffix )
        /// </summary>
        /// <param name="nReplays">The number of replays required. Must be positive</param>
        public static void SetRequiredReplaysForBugReproduction(int nReplays)
        {
            if (nReplays > 0)
            {
                StaticValueNREPLAYSFORBUGREPRODUCTION = nReplays;
            }
        }

        private bool StopRecordingAfterGraphCompleted;

        /// <summary>
        /// The root of the partial order
        /// </summary>
        protected ProgramStep GuideRootStep;

        private HashSet<ProgramStep> WithHeldSends;

        // Is the schedule represented by the partial order fair. ( doesn't mean our replay will be )
        private readonly bool IsScheduleFair;

        private ProgramReplayHelper ProgramReplayHelper;

        // The regular stuff
        private int ScheduledSteps;
        private readonly Configuration Configuration;
        private ProgramStep currentlyChosenStep;
        private bool HasReachedEndHard;

        /// <summary>
        /// The suffix strategy to use after we are done with the replay.
        /// </summary>
        protected readonly ISchedulingStrategy SuffixStrategy;
        private bool UseSuffixStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramGraphReplayStrategy"/> class.
        /// </summary>
        /// <param name="rootStep">The first (root) step of the program. All steps must be reachable from this to be replayed.</param>
        /// <param name="isScheduleFair">Is the schedule represented by the partial order fair</param>
        /// <param name="configuration">A configuration object</param>
        /// <param name="suffixStrategy">If the graph replay is over and we want to hit liveness violations, we may need a suffix strategy</param>
        public ProgramGraphReplayStrategy(ProgramStep rootStep, bool isScheduleFair, Configuration configuration, ISchedulingStrategy suffixStrategy = null)
            : this(rootStep, null, isScheduleFair, configuration, suffixStrategy)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramGraphReplayStrategy"/> class.
        /// </summary>
        /// <param name="rootStep">The first (root) step of the program. All steps must be reachable from this to be replayed.</param>
        /// <param name="withHeldSends">A hashset of programsteps in the partial order whose events were not enqueued</param>
        /// <param name="isScheduleFair">Is the schedule represented by the partial order fair</param>
        /// <param name="configuration">A configuration object</param>
        /// <param name="suffixStrategy">If the graph replay is over and we want to hit liveness violations, we may need a suffix strategy</param>
        public ProgramGraphReplayStrategy(ProgramStep rootStep, HashSet<ProgramStep> withHeldSends, bool isScheduleFair, Configuration configuration, ISchedulingStrategy suffixStrategy = null)
        {
            this.GuideRootStep = rootStep;
            this.WithHeldSends = withHeldSends;

            this.IsScheduleFair = isScheduleFair;
            this.ResetProgramReplayHelper(rootStep);
            this.ScheduledSteps = 0;

            this.Configuration = configuration;
            this.SuffixStrategy = suffixStrategy;

            this.HasReachedEndHard = false;

            this.StopRecordingAfterGraphCompleted = true;
        }

        /// <summary>
        /// Updates the partial order to be replayed. Don't do this mid iter.
        /// </summary>
        /// <param name="rootStep">The root of the new partial order</param>
        /// <param name="withHeldSends">The withheld sends in the partial order</param>
        public void UpdateGraphToReplay(ProgramStep rootStep, HashSet<ProgramStep> withHeldSends)
        {
            this.GuideRootStep = rootStep;
            this.WithHeldSends = withHeldSends;
        }

        /// <summary>
        /// Set the value of StopRecordingAfterGraphCompleted
        /// If true, the program model will cease to record progress when the Graph replay is complete
        /// </summary>
        /// <param name="stopRecordingAfterGraphCompleted">The value to set it to</param>
        public void SetStopRecordingAfterGraphCompleted(bool stopRecordingAfterGraphCompleted)
        {
            this.StopRecordingAfterGraphCompleted = stopRecordingAfterGraphCompleted;
        }

        /// <summary>
        /// Abandons program replay and uses Suffix Strategy for any following step
        /// </summary>
        public void SwitchToSuffixStrategy(bool stopRecording)
        {
            this.UseSuffixStrategy = true;
            if (stopRecording)
            {
                this.StopRecording();
            }
        }

        /// <inheritdoc/>
        protected override bool HashEvents => false;

        /// <inheritdoc/>
        protected override bool HashMachines => false;

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
        public override bool GetNextOperation(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            if (this.UseSuffixStrategy)
            {
                next = null;
                if (this.SuffixStrategy?.GetNext(out next, ops, current) ?? false)
                {
                    this.ScheduledSteps++;
                    return true;
                }
                else
                {
                    next = null;
                    return false;
                }
            }
            else
            {
                if (this.ProgramReplayHelper.HasReachedEnd())
                {
                    this.HasReachedEndHard = true;
                    this.SwitchToSuffixStrategy(this.StopRecordingAfterGraphCompleted);
                    return this.GetNextOperation(out next, ops, current);
                }

                // Do a sanity check on our model
                this.ProgramReplayHelper.DoSanityCheck(ops);

                Console.WriteLine("In GetNext");
                List<IAsyncOperation> enabledOps = ops.Where(o => o.IsEnabled).ToList();
                Dictionary<ProgramStep, IAsyncOperation> candidateSteps = this.ProgramReplayHelper.GetEnabledSteps(enabledOps);

                if (candidateSteps.Count > 0)
                {
                    this.ScheduledSteps++;

                    ProgramStep chosenStep = this.ChooseNextStep(candidateSteps);
                    next = candidateSteps[chosenStep];
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
        }

        /// <summary>
        /// Returns which step amongst candidate steps should be executed.
        /// Can be overriden by subclasses
        /// Default behaviour: Return first in the list.
        /// </summary>
        /// <param name="candidateSteps">A Dictionary of enabled corresponding ProgramStep -> IAsyncOperation</param>
        /// <returns>ProgramStep to replay next</returns>
        protected virtual ProgramStep ChooseNextStep(Dictionary<ProgramStep, IAsyncOperation> candidateSteps)
        {
            return candidateSteps.OrderBy(x => x.Key.TotalOrderingIndex).First().Key;
            // return candidateSteps.First().Key;
        }

        /// <inheritdoc/>
        public override bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            Console.WriteLine("In GetBool");
            if (this.UseSuffixStrategy)
            {
                this.HasReachedEndHard = true;
                next = false;
                if (this.SuffixStrategy?.GetNextBooleanChoice(maxValue, out next) ?? false)
                {
                    this.ScheduledSteps++;
                    return true;
                }
                else
                {
                    next = false;
                    return false;
                }
            }
            else
            {
                if (this.ProgramReplayHelper.HasReachedEnd())
                {
                    this.SwitchToSuffixStrategy(true);
                    return this.GetNextBooleanChoice(maxValue, out next);
                }

                ProgramStep candidateStep = null;

                candidateStep = this.ProgramReplayHelper.GetNextBooleanStep();

                if (candidateStep != null && candidateStep.BooleanChoice != null)
                {
                    this.ScheduledSteps++;

                    this.ProgramReplayHelper.RecordChoice(candidateStep, this.ScheduledSteps);
                    next = (bool)candidateStep.BooleanChoice;

                    Console.WriteLine("Out GetBool");
                    return true;
                }
                else if (candidateStep == null && ProgramReplayHelper.IsLastSchedulableStepOfMachine(this.ProgramReplayHelper.GetCurrentStep()))
                {
                    // Break arbitrarily?
                    this.ScheduledSteps++;
                    next = this.GetScheduledSteps() % 2 == 0 ? true : false;
                    return true;
                }
                else
                {
                    next = false;
                    Console.WriteLine("Fail GetBool");
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public override bool GetNextIntegerChoice(int maxValue, out int next)
        {
            Console.WriteLine("In GetInt");
            if (this.UseSuffixStrategy)
            {
                this.HasReachedEndHard = true;
                next = 0;
                if (this.SuffixStrategy?.GetNextIntegerChoice(maxValue, out next) ?? false)
                {
                    this.ScheduledSteps++;
                    return true;
                }
                else
                {
                    next = 0;
                    return false;
                }
            }
            else
            {
                if (this.ProgramReplayHelper.HasReachedEnd())
                {
                    this.SwitchToSuffixStrategy(this.StopRecordingAfterGraphCompleted);
                    return this.GetNextIntegerChoice(maxValue, out next);
                }

                ProgramStep candidateStep = this.ProgramReplayHelper.GetNextIntegerStep();
                if (candidateStep != null && candidateStep.IntChoice != null)
                {
                    this.ScheduledSteps++;
                    this.ProgramReplayHelper.RecordChoice(candidateStep, this.ScheduledSteps);
                    next = (int)candidateStep.IntChoice;

                    Console.WriteLine("Out GetInt");
                    return true;
                }
                else if (candidateStep == null && ProgramReplayHelper.IsLastSchedulableStepOfMachine(this.ProgramReplayHelper.GetCurrentStep()))
                {
                    // Break arbitrarily?
                    this.ScheduledSteps++;
                    next = 0;
                    return true;
                }
                else
                {
                    next = 0;
                    Console.WriteLine("Fail GetInt");
                    return false;
                }
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
            bool isFair = (this.SuffixStrategy != null) ? this.SuffixStrategy.IsFair() : this.IsScheduleFair;
            int stepBound = isFair ? this.Configuration.MaxFairSchedulingSteps : this.Configuration.MaxUnfairSchedulingSteps;
            bool stepBoundReached = stepBound > 0 && this.ScheduledSteps >= stepBound;

            if (this.SuffixStrategy == null)
            {
                // return this.ProgramReplayHelper.HasReachedEnd();
                return this.HasReachedEndHard || stepBoundReached;
            }
            else
            {
                return stepBoundReached;
            }
        }

        /// <inheritdoc/>
        public override bool IsFair()
        {
            return this.IsScheduleFair;
        }

        /// <inheritdoc/>
        public override void NotifySchedulingEnded(bool bugFound)
        {
            base.NotifySchedulingEnded(bugFound);
        }

        /// <inheritdoc/>
        public override bool PrepareForNextIteration()
        {
            this.ResetProgramReplayHelper(this.GuideRootStep);
            this.ScheduledSteps = 0;
            this.HasReachedEndHard = false;
            this.UseSuffixStrategy = false;

            bool basePrep = base.PrepareForNextIteration();
            bool suffixPrep = this.SuffixStrategy?.PrepareForNextIteration() ?? true;

            return basePrep && suffixPrep;
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            this.ResetProgramReplayHelper(this.GuideRootStep);
            this.ScheduledSteps = 0;
            this.HasReachedEndHard = false;
            this.UseSuffixStrategy = false;
        }

        private void ResetProgramReplayHelper(ProgramStep rootStep)
        {
            this.ProgramReplayHelper = new ProgramReplayHelper(rootStep);
        }

        /// <inheritdoc/>
        public override void RecordCreateMachine(Machine createdMachine, Machine creatorMachine)
        {
            base.RecordCreateMachine(createdMachine, creatorMachine);

            if (!this.UseSuffixStrategy)
            {
                this.ProgramReplayHelper.RecordCreateMachine(creatorMachine, createdMachine);
            }
        }

        /// <inheritdoc/>
        public override void RecordSendEvent(AsyncMachine sender, Machine targetMachine, Event e, int stepIndex, bool wasEnqueued)
        {
            base.RecordSendEvent(sender, targetMachine, e, stepIndex, wasEnqueued);
            if (!this.UseSuffixStrategy)
            {
                if (!wasEnqueued)
                {
                    this.ProgramReplayHelper.RecordSendDropped(this.ProgramReplayHelper.GetCurrentStep());
                }
            }
        }

        /// <inheritdoc/>
        public override void RecordReceiveCalled(Machine machine)
        {
            base.RecordReceiveCalled(machine);

            if (ProgramModel.MustCreateExplicitReceiveCalledStep)
            {
                ProgramStep receiveCalledStep = this.ProgramReplayHelper.GetNextNonSchedulableSteps()
                    .FirstOrDefault(s => s.PrevMachineStep == this.ProgramReplayHelper.GetCurrentStep());

                if (receiveCalledStep != null &&
                    receiveCalledStep.ProgramStepType == ProgramStepType.ExplicitReceiveCalled &&
                    this.ProgramReplayHelper.MapOldToNewMachineId(receiveCalledStep.SrcId) == machine.Id.Value)
                {
                    this.ProgramReplayHelper.RecordChoice(receiveCalledStep, this.GetScheduledSteps());
                }
                else
                {
                    throw new NotImplementedException("This is not implemented right");
                }
            }
        }

        /// <inheritdoc/>
        public override void RecordReceiveEvent(Machine machine, Event evt, int sendStepIndex, bool wasExplicitReceiveCall)
        {
            base.RecordReceiveEvent(machine, evt, sendStepIndex, wasExplicitReceiveCall);

            // Slightly messy situation - If the current choice is the receiving machine,
            // it means we're executing a scheduled ExplicitReceive.
            if (wasExplicitReceiveCall &&
                this.ProgramReplayHelper.MapOldToNewMachineId(this.ProgramReplayHelper.GetCurrentStep().SrcId) != machine.Id.Value)
            {
                ProgramStep explicitReceivedStep = this.ProgramReplayHelper.GetNextEnabledOperations()
                    .FirstOrDefault(s => this.ProgramReplayHelper.MatchingSendIndices[s.CreatorParent] == (ulong)sendStepIndex);

                if (explicitReceivedStep != null &&
                    explicitReceivedStep.ProgramStepType == ProgramStepType.ExplicitReceiveComplete &&
                    this.ProgramReplayHelper.MapOldToNewMachineId(explicitReceivedStep.SrcId) == machine.Id.Value)
                {
                    this.ProgramReplayHelper.RecordChoice(explicitReceivedStep, this.GetScheduledSteps());
                }
                else
                {
                    throw new NotImplementedException("This is not implemented right");
                }
            }
        }

        /// <inheritdoc/>
        public override bool ShouldEnqueueEvent(MachineId senderId, MachineId targetId, Event e)
        {
            ProgramStep matchingStep = this.UseSuffixStrategy ? null : this.ProgramReplayHelper.GetCurrentStep();
            bool isWithHeld = this.WithHeldSends?.Contains(matchingStep) ?? false;
            return !isWithHeld && this.ShouldEnqueueEvent(senderId, targetId, e, matchingStep);
        }

        /// <summary>
        /// Similar to <see cref="ShouldEnqueueEvent(MachineId, MachineId, Event)"/> but with the step being replayed for extra info.
        /// Intended for subclasses to make informed choices about enqueueing events
        /// </summary>
        /// <param name="senderId"><see cref="ShouldEnqueueEvent(MachineId, MachineId, Event)"/> for senderId</param>
        /// <param name="targetId"><see cref="ShouldEnqueueEvent(MachineId, MachineId, Event)"/> for targetId</param>
        /// <param name="e"><see cref="ShouldEnqueueEvent(MachineId, MachineId, Event)"/> for e</param>
        /// <param name="programStep">The program step being replayed</param>
        /// <returns>Must return true if the event must be enqueued</returns>
        public virtual bool ShouldEnqueueEvent(MachineId senderId, MachineId targetId, Event e, ProgramStep programStep)
        {
            return true;
        }
    }
}
