using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class Timer : Machine
    {
        internal class Config : Event
        {
            public MachineId Target;
            public int TimeoutValue;

            public Config(MachineId id, int value)
                : base(-1, -1)
            {
                this.Target = id;
                this.TimeoutValue = value;
            }
        }

        internal class Timeout : Event
        {
            public MachineId Timer;

            public Timeout(MachineId id)
                : base(-1, -1)
            {
                this.Timer = id;
            }
        }

        internal class StartTimer : Event { }
        internal class CancelTimer : Event { }

        MachineId Target;
        int TimeoutValue;

        [Start]
        [OnEventGotoState(typeof(local), typeof(Loop))]
        [OnEventDoAction(typeof(Timer.Config), nameof(Configure))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as Timer.Config).Target;
            this.TimeoutValue = (this.ReceivedEvent as Timer.Config).TimeoutValue;
            this.Raise(new local());
        }
        
        [OnEventGotoState(typeof(Timer.StartTimer), typeof(TimerStarted))]
        [IgnoreEvents(typeof(Timer.CancelTimer))]
        class Loop : MachineState { }


        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(local), typeof(Loop))]
        [OnEventGotoState(typeof(Timer.CancelTimer), typeof(Loop))]
        [IgnoreEvents(typeof(Timer.StartTimer))]
        class TimerStarted : MachineState { }

        void TimerStartedOnEntry()
        {
            if (this.Nondet())
            {
                this.Send(this.Target, new Timer.Timeout(this.Id));
                this.Raise(new local());
            }
        }
    }
}
