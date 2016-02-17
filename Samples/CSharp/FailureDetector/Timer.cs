using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace FailureDetector
{
    internal class Timer : Machine
    {
        internal class Config : Event
        {
            public MachineId Target;

            public Config(MachineId target)
                : base()
            {
                this.Target = target;
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

        internal class CancelSuccess : Event { }
        internal class CancelFailure : Event { }
        internal class CancelTimer : Event { }
        internal class Timeout : Event { }
        private class Unit : Event { }

        MachineId Target;

        [Start]
        [OnEventGotoState(typeof(Unit), typeof(WaitForReq))]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as Config).Target;
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(StartTimer), typeof(WaitForCancel))]
        [OnEventGotoState(typeof(CancelTimer), typeof(WaitForReq), nameof(CancelTimerAction))]
        class WaitForReq : MachineState { }

        void CancelTimerAction()
        {
            this.Send(this.Target, new CancelFailure());
        }

        [OnEventGotoState(typeof(CancelTimer), typeof(WaitForReq), nameof(CancelTimerAction2))]
        [OnEventGotoState(typeof(Default), typeof(WaitForReq), nameof(DefaultAction))]
        [IgnoreEvents(typeof(WaitForCancel))]
        class WaitForCancel : MachineState { }

        void DefaultAction()
        {
            this.Send(this.Target, new Timeout());
        }

        void CancelTimerAction2()
        {
            if (this.Random())
            {
                this.Send(this.Target, new CancelSuccess());
            }
            else
            {
                this.Send(this.Target, new CancelFailure());
                this.Send(this.Target, new Timeout());
            }
        }
    }
}
