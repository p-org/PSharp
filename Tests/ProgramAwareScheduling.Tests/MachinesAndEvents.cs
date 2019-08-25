// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace Microsoft.PSharp.ProgramAwareScheduling.Tests
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
            foreach (EventWrapper eWrap in receivedEvent.ForwardList)
            {
                MachineId targetId = (eWrap.TargetId == null) ? createdId : eWrap.TargetId;
                this.Send(targetId, eWrap.Event);
            }
        }
    }

    // Replay tests
    public abstract class ValueEvent : Event
    {
        public readonly int Value;

        public ValueEvent(int val)
        {
            this.Value = val;
        }
    }

    public class AddEvent : ValueEvent
    {
        public AddEvent(int val)
            : base(val)
        {
        }
    }

    public class ProductEvent : ValueEvent
    {
        public ProductEvent(int val)
            : base(val)
        {
        }
    }

    public class TargetValueEvent : Event
    {
        public readonly int TargetValue;

        public TargetValueEvent(int targetValue) => this.TargetValue = targetValue;
    }

    public class AssertValueEvent : Event
    {
        public readonly int ExpectedSum;
        public readonly bool ShouldMatch;

        public AssertValueEvent(int expectedSum, bool shouldMatch = true)
        {
            this.ExpectedSum = expectedSum;
            this.ShouldMatch = shouldMatch;
        }
    }

    public class SumProductMachine : Machine
    {
        private int Value;
        private int? TargetValue;

        [Start]
        [OnEntry(nameof(InitializeValue))]
        [OnEventDoAction(typeof(AddEvent), nameof(OnReceiveAdd))]
        [OnEventDoAction(typeof(ProductEvent), nameof(OnReceiveProduct))]
        [OnEventDoAction(typeof(AssertValueEvent), nameof(AssertValueEventOnReceive))]
        public class Init : MachineState
        {
        }

        private void InitializeValue()
        {
            this.Value = 0;
            this.TargetValue = (this.ReceivedEvent != null && this.ReceivedEvent is TargetValueEvent) ?
                (int?)(this.ReceivedEvent as TargetValueEvent).TargetValue :
                null;
        }

        private void OnReceiveAdd()
        {
            this.Value += (this.ReceivedEvent as AddEvent).Value;

            this.Assert(this.TargetValue != this.Value, "Hit the target value of " + this.TargetValue);
        }

        private void OnReceiveProduct()
        {
            this.Value *= (this.ReceivedEvent as ProductEvent).Value;

            this.Assert(this.TargetValue != this.Value, "Hit the target value of " + this.TargetValue);
        }

        private void AssertValueEventOnReceive()
        {
            AssertValueEvent evt = this.ReceivedEvent as AssertValueEvent;
            bool misMatch = this.Value != evt.ExpectedSum;

            this.Assert(misMatch ^ evt.ShouldMatch, $"ExpectedMatching={evt.ShouldMatch}. Actual:{this.Value == evt.ExpectedSum} ({this.Value} =?= {evt.ExpectedSum})");
        }
    }

    // Receive tests

    internal class NaturallyReceivedEvent : Event
    {
    }

    internal class BreakLoopEvent : Event
    {
    }

    internal class ExplicitReceiverMachine : Machine
    {
        [Start]
        [DeferEvents(typeof(ForwarderEvent))]
        [OnEventDoAction(typeof(NaturallyReceivedEvent), nameof(DoExplicitReceive))]
        internal class Init : MachineState
        {
        }

        private async Task DoExplicitReceive()
        {
            while (true)
            {
                ForwarderEvent evt = await this.Receive(typeof(ForwarderEvent)) as ForwarderEvent;
                foreach (EventWrapper eWrap in evt.ForwardList)
                {
                    if (eWrap.Event is BreakLoopEvent)
                    {
                        return;
                    }
                    else
                    {
                        this.Send(eWrap.TargetId, eWrap.Event);
                    }
                }
            }
        }
    }

    // Receive Replay tests

    public class ContestingReceivesMachine : Machine
    {
        private int Value;
        private int? TargetValue;

        [Start]
        [OnEntry(nameof(InitializeValue))]
        public class Init : MachineState
        {
        }

        [OnEntry(nameof(DoMainLoop))]
        public class MainLoop : MachineState
        {
        }

        private void InitializeValue()
        {
            this.Value = 1;
            this.TargetValue = (this.ReceivedEvent != null && this.ReceivedEvent is TargetValueEvent) ?
                (int?)(this.ReceivedEvent as TargetValueEvent).TargetValue :
                null;
            this.Goto<MainLoop>();
        }

        private async Task DoMainLoop()
        {
            while (true)
            {
                Event evt = await this.Receive(typeof(AddEvent), typeof(ProductEvent), typeof(BreakLoopEvent));
                if (evt is BreakLoopEvent)
                {
                    return;
                }
                else if (evt is AddEvent)
                {
                    this.Value += (evt as AddEvent).Value;
                }
                else if (evt is ProductEvent)
                {
                    this.Value *= (evt as ProductEvent).Value;
                }

                this.Assert(this.TargetValue != this.Value, "Hit the target value of " + this.TargetValue);
            }
        }
    }
}
