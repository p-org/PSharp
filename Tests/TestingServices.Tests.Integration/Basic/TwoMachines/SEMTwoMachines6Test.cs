// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.Utilities;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMTwoMachines6Test : BaseTest
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

        class PING : Machine
        {
            MachineId PongId;

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
                this.Send(PongId, new Ping(this.Id));
                this.Raise(new Success());
            }

            [OnEventGotoState(typeof(Pong), typeof(SendPing))]
            class WaitPong : MachineState { }

            class Done : MachineState { }
        }

        class PONG : Machine
        {
            int Count2 = 0;

            [Start]
            [OnEntry(nameof(EntryWaitPing))]
            [OnEventGotoState(typeof(Ping), typeof(SendPong))]
            class WaitPing : MachineState { }

            void EntryWaitPing() { }

            [OnEntry(nameof(EntrySendPong))]
            [OnEventGotoState(typeof(Success), typeof(WaitPing))]
            [OnEventDoAction(typeof(Halt), nameof(Action1))]
            class SendPong : MachineState { }

            void EntrySendPong()
            {
                Count2 = Count2 + 1;

                if (Count2 == 1)
                {
                    this.Send((this.ReceivedEvent as Ping).Id, new Pong());
                }

                if (Count2 == 2)
                {
                    this.Send((this.ReceivedEvent as Ping).Id, new Pong());
                    this.Raise(new Halt());
                    return; // important if not compiling
                }

                this.Raise(new Success());
            }

            void Action1()
            {
                this.Assert(false); // reachable
            }
        }

        /// <summary>
        ///  P# semantics test: two machines, machine is halted with "raise halt"
        /// (handled). This test is for the case when "halt" is explicitly handled
        /// - hence, it is processed as any other event.
        /// </summary>
        [Fact]
        public void TestRaisedHaltHandled()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(PING));
            });

            base.AssertFailed(configuration, test, 1);
        }
    }
}
