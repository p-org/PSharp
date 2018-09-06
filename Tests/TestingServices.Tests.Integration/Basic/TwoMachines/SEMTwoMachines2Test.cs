﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.Utilities;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMTwoMachines2Test : BaseTest
    {
        class Ping : Event
        {
            public MachineId Id;
            public Ping(MachineId id) : base(1, -1) { this.Id = id; }
        }

        class Pong : Event
        {
            public Pong() : base(1, -1) { }
        }

        class Success : Event { }
        class PingIgnored : Event { }
        class PongHalted : Event { }

        class PING : Machine
        {
            MachineId PongId;
            int Count;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventGotoState(typeof(Success), typeof(SendPing))]
            class Init : MachineState { }

            void EntryInit()
            {
                PongId = this.CreateMachine(typeof(PONG));
                this.Raise(new Success());
            }

            [OnEntry(nameof(EntrySendPing))]
            [OnEventGotoState(typeof(Success), typeof(WaitPong))]
            class SendPing : MachineState { }

            void EntrySendPing()
            {
                Count = Count + 1;
                if (Count == 1)
                {
                    this.Send(PongId, new Ping(this.Id));
                }
                // halt PONG after one exchange
                if (Count == 2)
                {
                    //this.Send(PongId, new Halt());
                    this.Send(PongId, new PingIgnored());
                }

                this.Raise(new Success());
            }

            [OnEventGotoState(typeof(Pong), typeof(SendPing))]
            class WaitPong : MachineState { }

            class Done : MachineState { }
        }

        class PONG : Machine
        {
            [Start]
            [OnEventGotoState(typeof(Ping), typeof(SendPong))]
            [OnEventDoAction(typeof(PingIgnored), nameof(Action1))]
            class WaitPing : MachineState { }

            void Action1()
            {
                this.Assert(false); // reachable
            }

            [OnEntry(nameof(EntrySendPong))]
            [OnEventGotoState(typeof(Success), typeof(WaitPing))]
            class SendPong : MachineState { }

            void EntrySendPong()
            {
                this.Send((this.ReceivedEvent as Ping).Id, new Pong());
                this.Raise(new Success());
            }
        }

        /// <summary>
        /// Tests that an event sent to a machine after it received the
        /// "halt" event is ignored by the halted machine.
        /// </summary>
        [Fact]
        public void TestEventSentAfterSentHalt()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(PING));
            });

            base.AssertFailed(configuration, test, 1);
        }
    }
}
