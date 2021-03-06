﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp;

namespace ReplicatingStorage
{
    internal class RepairTimer : Machine
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

        internal class StartTimerEvent : Event { }
        internal class CancelTimerEvent : Event { }
        internal class TimeoutEvent : Event { }

        private class TickEvent : Event { }

        MachineId Target;

        [Start]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
        class Init : MachineState { }

        void Configure()
        {
            this.Target = (this.ReceivedEvent as ConfigureEvent).Target;
            this.Raise(new StartTimerEvent());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(TickEvent), nameof(Tick))]
        [OnEventGotoState(typeof(CancelTimerEvent), typeof(Inactive))]
        [IgnoreEvents(typeof(StartTimerEvent))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.Send(this.Id, new TickEvent());
        }

        void Tick()
        {
            if (this.Random())
            {
                this.Logger.WriteLine("\n [RepairTimer] " + this.Target + " | timed out\n");
                this.Send(this.Target, new TimeoutEvent());
            }

            this.Send(this.Id, new TickEvent());
        }

        [OnEventGotoState(typeof(StartTimerEvent), typeof(Active))]
        [IgnoreEvents(typeof(CancelTimerEvent), typeof(TickEvent))]
        class Inactive : MachineState { }
    }
}
