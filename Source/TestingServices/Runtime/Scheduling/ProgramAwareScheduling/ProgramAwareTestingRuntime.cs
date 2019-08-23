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
        private Tuple<Machine, Event, int> SpeculativeSendForBlockedReceive;
        // Used by the EnqueueEvent and NotifyReceivedEvent to record the Send before the Receive.
        private AsyncMachine ContextEnqueueSenderMachine;

        internal ProgramAwareTestingRuntime(Configuration configuration, IProgramAwareSchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration, strategy, reporter)
        {
            this.ProgramAwareStrategy = strategy;
            this.EnableEventDropping = true;
            this.ContextEnqueueSenderMachine = null;
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
            this.ReceivingMachines.Add(machine);
            this.ProgramAwareStrategy.RecordReceiveCalled(machine);
        }

        internal override void NotifyReceivedEventWithoutWaiting(Machine machine, Event e, EventInfo eventInfo)
        {
            base.NotifyReceivedEventWithoutWaiting(machine, e, eventInfo);
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

            this.SpeculativeSendForBlockedReceive = Tuple.Create(machine, e, eventInfo.SendStep);
            this.ProgramAwareStrategy.RecordSendEvent(this.ContextEnqueueSenderMachine, machine, e, eventInfo.SendStep, true);
            this.ProgramAwareStrategy.RecordReceiveEvent(machine, e, eventInfo.SendStep, true);
        }

        // Called for any send
        // Called before the enqueue happens. Can also be a blocking receive.
        protected override EnqueueStatus EnqueueEvent(Machine targetMachine, Event e, AsyncMachine sender, Guid opGroupId,
            SendOptions options, out EventInfo eventInfo)
        {
            EnqueueStatus enqueueStatus;
            bool enqueueEvent = this.EnableEventDropping &&
                    this.ProgramAwareStrategy.ShouldEnqueueEvent(sender?.Id ?? null, targetMachine.Id, e);

            eventInfo = null;
            {
                // Braces just for the ContextEnqueueSenderMachine stuff to be explicitly seen
                this.ContextEnqueueSenderMachine = sender;
                enqueueStatus = enqueueEvent ?
                    base.EnqueueEvent(targetMachine, e, sender, opGroupId, options, out eventInfo) :
                    enqueueStatus = EnqueueStatus.Dropped;
                this.ContextEnqueueSenderMachine = null;
            }

            int sendStepIndex = eventInfo?.SendStep ?? this.ProgramAwareStrategy.GetScheduledSteps();

            if (this.SpeculativeSendForBlockedReceive == null)
            {
                this.ProgramAwareStrategy.RecordSendEvent(sender, targetMachine, e, sendStepIndex, enqueueEvent);
            }
            else
            {
                // Just verify we fed the right stuff.
                if (this.SpeculativeSendForBlockedReceive.Item1 == targetMachine &&
                    this.SpeculativeSendForBlockedReceive.Item2 == e &&
                    this.SpeculativeSendForBlockedReceive.Item3 == sendStepIndex)
                {
                    this.SpeculativeSendForBlockedReceive = null; // Well done.
                }
                else
                {
                    throw new NotImplementedException("This is not implemented right");
                }
            }

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

#if false

        internal override bool GetFairNondeterministicBooleanChoice(AsyncMachine caller, string uniqueId)
        {
            bool boolChoice = base.GetFairNondeterministicBooleanChoice(caller, uniqueId);
            this.ProgramAwareStrategy.RecordNonDetBooleanChoice(boolChoice);
            return boolChoice;
        }
        // utils
        private static EventInfo CreateStandardizedEventInfo(AsyncMachine sender, Event evt)
        {
            EventOriginInfo originInfo;
            if (sender is Machine)
            {
                originInfo = new EventOriginInfo(sender.Id, (sender as Machine).GetType().FullName, string.Empty);
            }
            else
            {
                // Message comes from outside P#.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            return new EventInfo(evt, originInfo);
        }
#endif
    }
}
