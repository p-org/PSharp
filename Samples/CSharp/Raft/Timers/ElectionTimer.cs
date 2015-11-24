using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
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
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(Active))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
            this.Reset();
            this.Raise(new LocalEvent());
        }

        void Reset()
        {
            this.Counter = 0;
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
        [OnEventDoAction(typeof(ResetTimer), nameof(Reset))]
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
                this.Counter++;
            }

            if (this.Counter == 10)
            {
                this.Reset();
                this.Send(this.Target, new Timeout(this.Id), true);
            }

            this.Send(this.Id, new TickEvent());
        }

        [OnExit(nameof(InactiveOnExit))]
        [OnEventGotoState(typeof(StartTimer), typeof(Active))]
        [IgnoreEvents(typeof(ResetTimer), typeof(CancelTimer), typeof(TickEvent))]
        class Inactive : MachineState { }

        void InactiveOnExit()
        {
            this.Reset();
        }
    }
}
