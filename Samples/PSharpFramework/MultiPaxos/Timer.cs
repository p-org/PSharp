using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class Timer : Machine
    {
        MachineId Target;
        int TimeoutValue;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(local), typeof(Loop))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Target = (this.Payload as object[])[0] as MachineId;
            this.TimeoutValue = (int)(this.Payload as object[])[1];
            this.Raise(new local());
        }
        
        [OnEventGotoState(typeof(startTimer), typeof(TimerStarted))]
        [IgnoreEvents(typeof(cancelTimer))]
        class Loop : MachineState { }


        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(local), typeof(Loop))]
        [OnEventGotoState(typeof(cancelTimer), typeof(Loop))]
        [IgnoreEvents(typeof(startTimer))]
        class TimerStarted : MachineState { }

        void TimerStartedOnEntry()
        {
            if (this.Nondet())
            {
                this.Send(this.Target, new timeout(), this.Id);
                this.Raise(new local());
            }
        }
    }
}
