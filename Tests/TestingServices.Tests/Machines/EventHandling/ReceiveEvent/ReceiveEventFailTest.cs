﻿using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class ReceiveEventFailTest : BaseTest
    {
        public ReceiveEventFailTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Config : Event
        {
            public MachineId Id;

            public Config(MachineId id)
            {
                this.Id = id;
            }
        }

        private class Unit : Event
        {
        }

        private class Ping : Event
        {
        }

        private class Pong : Event
        {
        }

        private class Server : Machine
        {
            private MachineId Client;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Client = this.CreateMachine(typeof(Client));
                this.Send(this.Client, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [IgnoreEvents(typeof(Pong))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Send(this.Client, new Ping());
            }
        }

        private class Client : Machine
        {
            private MachineId Server;
            private int Counter;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void Configure()
            {
                this.Server = (this.ReceivedEvent as Config).Id;
                this.Counter = 0;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private async Task ActiveOnEntry()
            {
                while (this.Counter < 5)
                {
                    await this.Receive(typeof(Ping));
                    this.SendPong();
                }

                this.Raise(new Halt());
            }

            private void SendPong()
            {
                this.Counter++;
                this.Send(this.Server, new Pong());
            }
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Livelock detected. 'Client()' is waiting to receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachinesReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Livelock detected. 'Client()' and 'Client()' are waiting to " +
                "receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestThreeMachinesReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Livelock detected. 'Client()', 'Client()' and 'Client()' are " +
                "waiting to receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }
    }
}
