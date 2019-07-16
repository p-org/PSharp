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

        internal ProgramAwareTestingRuntime(Configuration configuration, IProgramAwareSchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration, strategy, reporter)
        {
            this.ProgramAwareStrategy = strategy;
        }

        protected override Machine CreateMachine(MachineId mid, Type type, string machineName, Machine creator, Guid opGroupId)
        {
            Machine createdMachine = base.CreateMachine(mid, type, machineName, creator, opGroupId);
            this.ProgramAwareStrategy.RecordCreateMachine(createdMachine, creator);
            return createdMachine;
        }

        protected override void NotifyMachineStart(Machine machine, Event initialEvent)
        {
            this.ProgramAwareStrategy.RecordStartMachine(machine, (initialEvent == null) ? null : CreateStandardizedEventInfo(null, initialEvent));
        }

        protected override EnqueueStatus EnqueueEvent(MachineId target, Event e, AsyncMachine sender, Guid opGroupId,
            SendOptions options, out Machine targetMachine, out EventInfo eventInfo)
        {
            EnqueueStatus enqueueStatus = base.EnqueueEvent(target, e, sender, opGroupId, options, out targetMachine, out eventInfo);

            // Record the Send.

            EventInfo standardizedEventInfo = null;
            if (eventInfo == null)
            {
                standardizedEventInfo = CreateStandardizedEventInfo(sender, e);
            }
            else
            {
                standardizedEventInfo = eventInfo;
            }

            this.ProgramAwareStrategy.RecordSendEvent(sender, target, standardizedEventInfo, e);

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

        internal override void NotifyDequeuedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            base.NotifyDequeuedEvent(machine, e, eventInfo);
            this.ProgramAwareStrategy.RecordReceiveEvent(machine, eventInfo);
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
