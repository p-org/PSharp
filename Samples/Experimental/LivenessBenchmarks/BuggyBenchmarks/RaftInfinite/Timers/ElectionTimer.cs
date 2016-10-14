using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace RaftInfinite
{
    internal class ElectionTimer : Machine
    {
        internal class ConfigureEvent : Event
        {
            public MachineId Target;

            public ConfigureEvent(MachineId id)
                : base()
            {
                this.Target = id;
            }
        }

        internal class StartTimer : Event { }
        internal class CancelTimer : Event { }
        internal class Timeout : Event { }

        private class TickEvent : Event { }

        MachineId Target;

        [Start]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(StartTimer), typeof(Active))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
        [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
        [IgnoreEvents(typeof(StartTimer))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.Send(this.Id, new TickEvent());
        }

        void Tick()
        {
            if (this.Random())
            {
                Console.WriteLine("\n [ElectionTimer] " + this.Target + " | timed out\n");
                this.Send(this.Target, new Timeout(), true);
            }

            this.Raise(new CancelTimer());
        }

        [OnEntry(nameof(InactiveOnEntry))]
        [OnEventGotoState(typeof(StartTimer), typeof(Active))]
        [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
        class Inactive : MachineState { }

        void InactiveOnEntry()
        {
            Send(Id, new StartTimer());
        }
    }
}
