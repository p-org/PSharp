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

        internal class StartTimer : Event { }
        internal class CancelTimer : Event { }
        internal class Timeout : Event { }

        private class TickEvent : Event { }

        MachineId Target;
        int counter = 0;

        [Start]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(StartTimer), typeof(Active))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
            //this.Raise(new StartTimer());
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
            if (this.Random() && counter < 5)
            {
                counter++;
                Console.WriteLine("\n [ElectionTimer] " + this.Target + " | timed out\n");
                this.Send(this.Target, new Timeout(), true);
            }

            //this.Send(this.Id, new TickEvent());
            this.Raise(new CancelTimer());
        }

        [OnEventGotoState(typeof(StartTimer), typeof(Active))]
        [IgnoreEvents(typeof(CancelTimer), typeof(TickEvent))]
        class Inactive : MachineState { }
    }
}
