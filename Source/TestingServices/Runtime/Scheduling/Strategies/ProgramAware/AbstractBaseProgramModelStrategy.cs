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
    /// Base class which implements program model construction
    /// </summary>
    public abstract class AbstractBaseProgramModelStrategy : IProgramAwareSchedulingStrategy
    {
        // Some handy constants
        protected private const ulong TESTHARNESSMACHINEID = 0;
        protected private const ulong TESTHARNESSMACHINEHASH = 199999;

        private readonly Dictionary<ulong, Tuple<Machine, Event, int>> PendingExplicitReceives;
        protected private ProgramModel ProgramModel;

        // There's a case where you call AbstractBaseProgramModelStrategy.GetNext from GetNextOperation
        private bool SafetyRecursionCheckThisHasBeenCalledOnce;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractBaseProgramModelStrategy"/> class.
        /// </summary>
        public AbstractBaseProgramModelStrategy()
        {
            this.PendingExplicitReceives = new Dictionary<ulong, Tuple<Machine, Event, int>>();
            this.ResetProgramModel();
        }

        /// <summary>
        /// If true, The ProgramStepEventInfo.HashedState field will be set for all eventInfo
        /// </summary>
        protected abstract bool HashEvents { get; }

        /// <summary>
        /// If true, The IProgramStepSignature.MachineHash field will be set for all steps
        /// </summary>
        protected abstract bool HashMachines { get; }

        /// <inheritdoc/>
        public virtual string GetDescription()
        {
            return "Abstract class which implements program model construction methods";
        }

        private void ResetProgramModel()
        {
            this.PendingExplicitReceives.Clear();
            this.ProgramModel = new ProgramModel();
        }

        /// <inheritdoc/>
        public abstract bool IsFair();

        /// <summary>
        /// Resets program model. Call if you override.
        /// </summary>
        public virtual void Reset()
        {
            // TODO
            this.ResetProgramModel();
        }

        /// <summary>
        /// Once this is called, we no longer update the program model for that iteration.
        /// </summary>
        public void StopRecording()
        {
            this.ProgramModel.StopRecording();
        }

        /// <summary>
        /// Resets program model. Call if you override.
        /// </summary>
        /// <returns>true if the reset succeeded ( which it would )</returns>
        public virtual bool PrepareForNextIteration()
        {
            // Please call even if you override
            this.ResetProgramModel();
            return true;
        }

        /// <inheritdoc/>
        public abstract int GetScheduledSteps();

        /// <inheritdoc/>
        public abstract bool HasReachedMaxSchedulingSteps();

        // Scheduling Choice(?)s

        /// <inheritdoc/>
        public abstract void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current);

        /// <inheritdoc/>
        public abstract void ForceNextBooleanChoice(int maxValue, bool next);

        /// <inheritdoc/>
        public abstract void ForceNextIntegerChoice(int maxValue, int next);

        /// <inheritdoc/>
        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            bool ret = false;
            if (!this.SafetyRecursionCheckThisHasBeenCalledOnce)
            {
                this.SafetyRecursionCheckThisHasBeenCalledOnce = true;
                ret = this.GetNextOperation(out next, ops, current);
                this.SafetyRecursionCheckThisHasBeenCalledOnce = false;
            }
            else
            {
                throw new NotImplementedException("Circular recursion detected. Please call this.GetNextOperation instead");
            }

            if (next != null && this.PendingExplicitReceives.ContainsKey(next.SourceId))
            {
                Tuple<Machine, Event, int> receiveTuple = this.PendingExplicitReceives[next.SourceId];
                if (next.Type == AsyncOperationType.Receive && (ulong)receiveTuple.Item3 == next.MatchingSendIndex)
                {
                    this.RecordReceiveEvent(receiveTuple.Item1, receiveTuple.Item2, receiveTuple.Item3, true);
                    this.PendingExplicitReceives.Remove(next.SourceId);
                }
                else
                {
                    throw new NotImplementedException("This is not implemented correctly");
                }
            }

            return ret;
        }

        /// <summary>
        /// Equivalent to <see cref="ISchedulingStrategy.GetNext"/>
        /// Forces the next asynchronous operation to be scheduled.
        /// </summary>
        /// <param name="next">The next operation to schedule.</param>
        /// <param name="ops">List of operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        public abstract bool GetNextOperation(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current);

        /// <summary>
        /// Returns the steps in order of execution
        /// </summary>
        /// <returns>RootStep</returns>
        public List<ProgramStep> GetSchedule()
        {
            return this.ProgramModel.OrderedSteps;
        }

        /// <summary>
        /// Returns a mapping of MachineId to Type
        /// </summary>
        /// <returns>a mapping of MachineId to Type</returns>
        public virtual Dictionary<ulong, Type> GetMachineIdToTypeMap()
        {
            Dictionary<ulong, Type> machineIdToType = this.ProgramModel.MachineIdToMachine.Values.ToDictionary(x => x.Id.Value, y => y.GetType());
            machineIdToType.Add(0, typeof(TestHarnessMachine));
            return machineIdToType;
        }

        /// <inheritdoc/>
        public abstract bool GetNextBooleanChoice(int maxValue, out bool next);

        /// <inheritdoc/>
        public abstract bool GetNextIntegerChoice(int maxValue, out int next);

        // The program-aware part

        /// <inheritdoc/>
        public virtual void RecordCreateMachine(Machine createdMachine, Machine creatorMachine)
        {
            ProgramStep createStep = new ProgramStep(AsyncOperationType.Create, creatorMachine?.Id.Value ?? 0, createdMachine.Id.Value, null);
            this.ProgramModel.RecordStep(createStep, this.GetScheduledSteps()); // TODO: Should i do -1?
            this.ProgramModel.RecordMachineCreation(createdMachine.Id.Value, createdMachine);
        }

        /// <inheritdoc/>
        public virtual void RecordStartMachine(Machine machine, Event initialEvent)
        {
            ProgramStepEventInfo pEventInfo = null;
            if (initialEvent != null)
            {
                pEventInfo = new ProgramStepEventInfo(initialEvent, 0, 0);
            }
            else
            {
                pEventInfo = null;
            }

            ProgramStep startStep = new ProgramStep(AsyncOperationType.Start, machine.Id.Value, machine.Id.Value, pEventInfo);
            this.ProgramModel.RecordStep(startStep, this.GetScheduledSteps());
        }

        /// <inheritdoc/>
        public virtual void RecordReceiveCalled(Machine machine)
        {
            this.ProgramModel.RecordReceiveCalled(machine);
        }

        /// <inheritdoc/>
        public void RecordExplicitReceiveEventEnabled(Machine machine, Event evt, int sendStepIndex)
        {
            ProgramStepEventInfo pEventInfo = new ProgramStepEventInfo(evt, 0, sendStepIndex);
            ProgramStep receiveStep = ProgramStep.CreateExplicitReceiveCompleteStep(machine.Id.Value, pEventInfo);
            this.PendingExplicitReceives.Add(machine.Id.Value, Tuple.Create(machine, evt, sendStepIndex));
        }

        /// <inheritdoc/>
        public virtual void RecordReceiveEvent(Machine machine, Event evt, int sendStepIndex, bool wasExplicitReceiveCall)
        {
            ProgramStepEventInfo pEventInfo = new ProgramStepEventInfo(evt, 0, sendStepIndex);

            ProgramStep receiveStep = wasExplicitReceiveCall ?
                ProgramStep.CreateExplicitReceiveCompleteStep(machine.Id.Value, pEventInfo) :
                new ProgramStep(AsyncOperationType.Receive, machine.Id.Value, machine.Id.Value, pEventInfo);

            this.ProgramModel.RecordStep(receiveStep, this.GetScheduledSteps());
        }

        /// <inheritdoc/>
        public virtual void RecordSendEvent(AsyncMachine sender, Machine targetMachine, Event e, int stepIndex, bool wasEnqueued)
        {
            ProgramStepEventInfo pEventInfo = new ProgramStepEventInfo(e, sender?.Id.Value ?? 0, stepIndex);
            if (this.HashEvents)
            {
                pEventInfo.HashedState = this.HashEvent(e);
            }

            ProgramStep sendStep = new ProgramStep(AsyncOperationType.Send, sender?.Id.Value ?? 0, targetMachine?.Id.Value ?? 0, pEventInfo);
            this.ProgramModel.RecordStep(sendStep, this.GetScheduledSteps());

            if (!wasEnqueued)
            {
                this.ProgramModel.RecordEventDropped(sendStep);
            }
        }

        private int HashEvent(Event e)
        {
            return ReflectionBasedHasher.HashObject(e);
        }

        /// <inheritdoc/>
        public void RecordNonDetBooleanChoice(bool boolChoice)
        {
            ProgramStep ndBoolStep = new ProgramStep(this.ProgramModel.ActiveStep.SrcId, boolChoice);
            this.ProgramModel.RecordStep(ndBoolStep, this.GetScheduledSteps());
        }

        /// <inheritdoc/>
        public void RecordNonDetIntegerChoice(int intChoice)
        {
            ProgramStep ndIntStep = new ProgramStep(this.ProgramModel.ActiveStep.SrcId, intChoice);
            this.ProgramModel.RecordStep(ndIntStep, this.GetScheduledSteps());
        }

        /// <inheritdoc/>
        public void RecordMonitorEvent(Type monitorType, AsyncMachine sender, Event e)
        {
            // Do Nothing
            this.ProgramModel.RecordMonitorEvent(monitorType, sender);
        }

        /// <inheritdoc/>
        public void RecordMonitorStateChange(Monitor monitor, bool isHotState)
        {
            this.ProgramModel.RecordMonitorStateChange(monitor);
        }

        /// <summary>
        /// Called at the end of a scheduling iteration.
        /// Please explicitly call base.NotifySchedulingEnded if you override.
        /// </summary>
        /// <param name="bugFound">Was bug found in this run</param>
        public virtual void NotifySchedulingEnded(bool bugFound)
        {
            this.ProgramModel.RecordSchedulingEnded(bugFound);
        }

        // Trace minimization

        /// <inheritdoc/>
        public virtual bool ShouldEnqueueEvent(MachineId senderId, MachineId targetId, Event e)
        {
            return true;
        }

        /// <summary>
        /// Returns the root of the partial order
        /// </summary>
        /// <returns>RootStep</returns>
        public ProgramModelSummary GetProgramSummary()
        {
            return this.ProgramModel.GetProgramSummary();
        }

        /// <summary>
        /// Returns the root of the partial order
        /// </summary>
        /// <returns>RootStep</returns>
        protected ProgramStep GetRootStep()
        {
            return this.ProgramModel.Rootstep;
        }

        /// <summary>
        /// Tells which bug triggered the assertion violation OR caused the monitor to go into hot state
        /// Hack for now, Only does assertion violation.
        /// </summary>
        /// <returns>The program model</returns>
        protected ProgramStep GetBugTriggeringStep()
        {
            return this.ProgramModel.BugTriggeringStep;
        }

        /// <summary>
        /// Returns the number of steps recorded by the program model.
        /// </summary>
        /// <returns>the number of steps recorded by the program model.</returns>
        protected int GetProgramModelStepsCount()
        {
            return this.ProgramModel.OrderedSteps.Count;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}
