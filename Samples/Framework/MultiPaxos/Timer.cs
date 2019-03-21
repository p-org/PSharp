// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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

        internal class TimeoutEvent : Event
        {
            public MachineId Timer;

            public TimeoutEvent(MachineId id)
                : base(-1, -1)
            {
                this.Timer = id;
            }
        }

        internal class StartTimerEvent : Event { }
        internal class CancelTimerEvent : Event { }

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

        [OnEventGotoState(typeof(Timer.StartTimerEvent), typeof(TimerStarted))]
        [IgnoreEvents(typeof(Timer.CancelTimerEvent))]
        class Loop : MachineState { }


        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(local), typeof(Loop))]
        [OnEventGotoState(typeof(Timer.CancelTimerEvent), typeof(Loop))]
        [IgnoreEvents(typeof(Timer.StartTimerEvent))]
        class TimerStarted : MachineState { }

        void TimerStartedOnEntry()
        {
            if (this.Random())
            {
                this.Send(this.Target, new Timer.TimeoutEvent(this.Id));
                this.Raise(new local());
            }
        }
    }
}
