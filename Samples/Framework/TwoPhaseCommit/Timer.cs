// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp;

namespace TwoPhaseCommit
{
    internal class Timer : Machine
    {
        internal class Config : Event
        {
            public MachineId Target;

            public Config(MachineId id)
                : base()
            {
                this.Target = id;
            }
        }

        internal class StartTimer : Event
        {
            public int Timeout;

            public StartTimer(int timeout)
                : base()
            {
                this.Timeout = timeout;
            }
        }

        internal class Timeout : Event { }
        internal class CancelTimer : Event { }
        internal class CancelTimerSuccess : Event { }
        internal class CancelTimerFailure : Event { }
        private class Unit : Event { }

        MachineId Target;

        [Start]
        [OnEventGotoState(typeof(Unit), typeof(Loop))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as Config).Target;
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(StartTimer), typeof(TimerStarted))]
        [IgnoreEvents(typeof(CancelTimer))]
        class Loop : MachineState { }


        [OnEntry(nameof(TimerStartedOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Loop))]
        [OnEventDoAction(typeof(CancelTimer), nameof(CancelingTimer))]
        class TimerStarted : MachineState { }

        void TimerStartedOnEntry()
        {
            if (this.Random())
            {
                this.Send(this.Target, new Timer.Timeout());
                this.Raise(new Unit());
            }
        }

        void CancelingTimer()
        {
            if (this.Random())
            {
                this.Send(this.Target, new CancelTimerFailure());
                this.Send(this.Target, new Timeout());
            }
            else
            {
                this.Send(this.Target, new CancelTimerSuccess());
            }

            this.Raise(new Unit());
        }
    }
}
