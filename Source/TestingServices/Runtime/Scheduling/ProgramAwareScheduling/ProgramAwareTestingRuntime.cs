// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    internal class ProgramAwareTestingRuntime : SystematicTestingRuntime
    {
        internal IProgramAwareSchedulingStrategy ProgramAwareStrategy;
        private readonly bool EnableEventDropping;
        private readonly HashSet<Machine> ReceivingMachines;
        // Used by the EnqueueEvent and NotifyReceivedEvent to record the Send before the Receive.

        internal ProgramAwareTestingRuntime(Configuration configuration, IProgramAwareSchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration, strategy, reporter)
        {
            this.ProgramAwareStrategy = strategy;
            this.EnableEventDropping = true;
            this.ReceivingMachines = new HashSet<Machine>();
        }

        protected override Machine CreateMachine(MachineId mid, Type type, string machineName, Machine creator, Guid opGroupId)
        {
            Machine createdMachine = base.CreateMachine(mid, type, machineName, creator, opGroupId);
            this.ProgramAwareStrategy.RecordCreateMachine(createdMachine, creator);
            return createdMachine;
        }

        protected override void NotifyMachineStart(Machine machine, Event initialEvent)
        {
            this.ProgramAwareStrategy.RecordStartMachine(machine, initialEvent);
        }

        internal override void NotifyEnteredState(Monitor monitor)
        {
            base.NotifyEnteredState(monitor);
            this.ProgramAwareStrategy.RecordMonitorStateChange(monitor, monitor.IsInHotState());
        }

        // Regular dequeue (?)
        internal override void NotifyDequeuedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            base.NotifyDequeuedEvent(machine, e, eventInfo);
            this.ProgramAwareStrategy.RecordReceiveEvent(machine, e, eventInfo?.SendStep ?? 0, false);
        }

        // Receive was explicitly called. Do we need this, or shall we just hook on to wait?
        internal override void NotifyReceiveCalled(Machine machine)
        {
            base.NotifyReceiveCalled(machine);
            // This check is needed because NotifyReceiveCalled gets called twice sometimes.
            if (!this.ReceivingMachines.Contains(machine))
            {
                this.ReceivingMachines.Add(machine);
                this.ProgramAwareStrategy.RecordReceiveCalled(machine);
            }
        }

        internal override void NotifyReceivedEventWithoutWaiting(Machine machine, Event e, EventInfo eventInfo)
        {
            base.NotifyReceivedEventWithoutWaiting(machine, e, eventInfo);
            // This check is important, because (apparently) even regular receives can do this.
            if (this.ReceivingMachines.Contains(machine))
            {
                this.ReceivingMachines.Remove(machine);
                this.ProgramAwareStrategy.RecordReceiveEvent(machine, e, eventInfo.SendStep, true);
            }

            // else Dequeue event will handle it.
        }

        // Receive called and got one finally. The send will be called soon after this.
        internal override void NotifyReceivedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            base.NotifyReceivedEvent(machine, e, eventInfo);
            this.ReceivingMachines.Remove(machine);
            this.ProgramAwareStrategy.RecordExplicitReceiveEventEnabled(machine, e, eventInfo.SendStep);
        }

        private bool? WasInnerEnqueueCalled;

        // Called for any send - Even a dropped one
        protected override EnqueueStatus EnqueueEvent(MachineId target, Event e, AsyncMachine sender, Guid opGroupId,
            SendOptions options, out Machine targetMachine, out EventInfo eventInfo)
        {
            this.WasInnerEnqueueCalled = false;
            EnqueueStatus enqueueStatus = base.EnqueueEvent(target, e, sender, opGroupId,
            options, out targetMachine, out eventInfo);

            if ( (targetMachine == null && enqueueStatus == EnqueueStatus.Dropped) == this.WasInnerEnqueueCalled)
            {
                throw new NotImplementedException("This is not done right");
            }

            this.WasInnerEnqueueCalled = null;

            if (targetMachine == null && enqueueStatus == EnqueueStatus.Dropped)
            {
                int sendStepIndex = eventInfo?.SendStep ?? this.ProgramAwareStrategy.GetScheduledSteps();
                this.ProgramAwareStrategy.RecordSendEvent(sender, targetMachine, e, sendStepIndex, true);
            }

            return enqueueStatus;
        }

        // Called by any sends where the enqueue succeeds
        // Called before the enqueue happens. Can also be a blocking receive.
        protected override EnqueueStatus EnqueueEvent(Machine targetMachine, Event e, AsyncMachine sender, Guid opGroupId,
            SendOptions options, out EventInfo eventInfo)
        {
            this.WasInnerEnqueueCalled = true;
            EnqueueStatus enqueueStatus;
            bool enqueueEvent = this.EnableEventDropping &&
                    this.ProgramAwareStrategy.ShouldEnqueueEvent(sender?.Id ?? null, targetMachine.Id, e);

            eventInfo = null;
            enqueueStatus = enqueueEvent ?
                base.EnqueueEvent(targetMachine, e, sender, opGroupId, options, out eventInfo) :
                enqueueStatus = EnqueueStatus.Dropped;

            int sendStepIndex = eventInfo?.SendStep ?? this.ProgramAwareStrategy.GetScheduledSteps();

            this.ProgramAwareStrategy.RecordSendEvent(sender, targetMachine, e, sendStepIndex, enqueueEvent);

            return enqueueStatus;
        }

        private static EventInfo CreateStandardizedEventInfo(AsyncMachine sender, Event e)
        {
            EventOriginInfo originInfo = (sender is Machine) ?
                new EventOriginInfo(sender.Id, (sender as Machine).GetType().FullName, "programaware__null") :
                originInfo = new EventOriginInfo(null, "Env", "Env"); // Message comes from outside P#.

            return new EventInfo(e, originInfo);
        }

        internal override void Monitor(Type type, AsyncMachine sender, Event e)
        {
            base.Monitor(type, sender, e);
            // Now do your thing.
            this.ProgramAwareStrategy.RecordMonitorEvent(type, sender, e);
        }

        // Non-det choices
        internal override bool GetNondeterministicBooleanChoice(AsyncMachine caller, int maxValue)
        {
            bool boolChoice = base.GetNondeterministicBooleanChoice(caller, maxValue);
            this.ProgramAwareStrategy.RecordNonDetBooleanChoice(boolChoice);
            return boolChoice;
        }

        internal override int GetNondeterministicIntegerChoice(AsyncMachine caller, int maxValue)
        {
            int intChoice = base.GetNondeterministicIntegerChoice(caller, maxValue);
            this.ProgramAwareStrategy.RecordNonDetIntegerChoice(intChoice);
            return intChoice;
        }
    }
}
