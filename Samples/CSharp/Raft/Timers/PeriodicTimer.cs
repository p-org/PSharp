using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    internal class PeriodicTimer : Machine
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

        internal class Timeout : Event
        {
            public MachineId Timer;

            public Timeout(MachineId id)
                : base()
            {
                this.Timer = id;
            }
        }

        internal class StartTimer : Event { }
        internal class CancelTimer : Event { }

        private class TickEvent : Event { }
        private class LocalEvent : Event { }

        MachineId Target;
        int Counter;

        [Start]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Inactive))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
            this.Counter = 0;
            this.Raise(new LocalEvent());
        }

        [OnEventGotoState(typeof(StartTimer), typeof(Active))]
        [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
        class Inactive : MachineState { }

        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
        [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
        [IgnoreEvents(typeof(StartTimer))]
        class Active : MachineState { }

        void TimerStartedOnEntry()
        {
            this.Counter = 0;
            this.Send(this.Id, new TickEvent());
        }

        void Tick()
        {
            if (this.Random())
            {
                this.Counter++;
            }

            if (this.Counter == 4)
            {
                this.Send(this.Target, new Timeout(this.Id), true);
                this.Raise(new CancelTimer());
            }
            else
            {
                this.Send(this.Id, new TickEvent());
            }
        }
    }
}
