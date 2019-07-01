// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Tests.ProgramAware
{
    internal class EventWrapper
    {
        internal MachineId TargetId;
        internal Event Event;

        public EventWrapper(MachineId targetId, Event evt)
        {
            this.TargetId = targetId;
            this.Event = evt;
        }
    }

    internal class ForwarderEvent : Event
    {
        internal readonly List<EventWrapper> ForwardList;

        public ForwarderEvent()
        {
            this.ForwardList = new List<EventWrapper>() { };
        }

        public ForwarderEvent(MachineId targetId, Event e)
        {
            this.ForwardList = new List<EventWrapper>() { new EventWrapper(targetId, e) };
        }

        public ForwarderEvent(List<EventWrapper> eventList)
        {
            this.ForwardList = eventList;
        }
    }

#if DEPRECATED
    internal class PingEvent : Event
    {
    }

    internal class ReceiverMachine : Machine
    {
        private int MessageCounter;

        [Start]
        [OnEntry(nameof(InitializeFields))]
        [OnEventDoAction(typeof(ForwarderEvent), nameof(Inc))]
        [OnEventDoAction(typeof(PingEvent), nameof(Inc))]
        private class Init : MachineState
        {
        }

        private void InitializeFields()
        {
            this.MessageCounter = 0;
        }

        private void Inc()
        {
            this.MessageCounter++;
        }
    }
#endif
    internal class ForwarderMachine : Machine
    {
        private int MessageCounter;

        [Start]
        [OnEntry(nameof(InitializeCounter))]
        [OnEventDoAction(typeof(ForwarderEvent), nameof(ForwardEventOnReceive))]
        internal class Init : MachineState
        {
        }

        private void InitializeCounter()
        {
            this.MessageCounter = 0;
        }

        private void ForwardEventOnReceive()
        {
            this.MessageCounter++;
            ForwarderEvent wrappedEvent = this.ReceivedEvent as ForwarderEvent;
            foreach (EventWrapper eWrap in wrappedEvent.ForwardList)
            {
                this.Send(eWrap.TargetId, eWrap.Event);
            }
        }
    }

    internal class CreateAndSendOnPingMachine : Machine
    {
        [Start]
        [OnEventDoAction(typeof(ForwarderEvent), nameof(CreateMachineOnReceive))]
        internal class Init : MachineState
        {
        }

        private void CreateMachineOnReceive()
        {
            ForwarderEvent receivedEvent = this.ReceivedEvent as ForwarderEvent;
            MachineId createdId = this.CreateMachine(typeof(ForwarderMachine));
            foreach ( EventWrapper eWrap in receivedEvent.ForwardList )
            {
                MachineId targetId = (eWrap.TargetId == null) ? createdId : eWrap.TargetId;
                this.Send(targetId, eWrap.Event);
            }
        }
    }
}
