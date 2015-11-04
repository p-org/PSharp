using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    internal class Timer : Machine
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
        internal class ResetTimer : Event { }
        internal class CancelTimer : Event { }

        private class TickEvent : Event { }
        private class LocalEvent : Event { }

        MachineId Target;
        int Counter;

        [Start]
        [OnEventGotoState(typeof(LocalEvent), typeof(Inactive))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
            this.Counter = 0;
            this.Raise(new LocalEvent());
        }

        [OnEventGotoState(typeof(StartTimer), typeof(Active))]
        [IgnoreEvents(typeof(ResetTimer), typeof(CancelTimer), typeof(TickEvent))]
        class Inactive : MachineState { }

        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(CancelTimer), typeof(Inactive))]
        [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
        [OnEventDoAction(typeof(ResetTimer), nameof(Reset))]
        [IgnoreEvents(typeof(StartTimer))]
        class Active : MachineState { }

        void TimerStartedOnEntry()
        {
            this.Send(this.Id, new TickEvent());
        }

        void Tick()
        {
            if (this.Nondet())
            {
                this.Counter++;
            }

            if (this.Counter == 4)
            {
                this.Reset();
                this.Send(this.Target, new Timeout(this.Id));
                this.Raise(new CancelTimer());
            }
            else
            {
                this.Send(this.Id, new TickEvent());
            }
        }

        void Reset()
        {
            this.Counter = 0;
        }
    }
}
