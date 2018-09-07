// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class ReceiveEventFailTest : BaseTest
    {
        public ReceiveEventFailTest(ITestOutputHelper output)
            : base(output)
        { }

        class Config : Event
        {
            public MachineId Id;

            public Config(MachineId id)
                : base(-1, -1)
            {
                this.Id = id;
            }
        }

        class Unit : Event { }
        class Ping : Event { }
        class Pong : Event { }

        class Server : Machine
        {
            MachineId Client;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Client = this.CreateMachine(typeof(Client));
                this.Send(this.Client, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [IgnoreEvents(typeof(Pong))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.Send(this.Client, new Ping());
            }
        }

        class Client : Machine
        {
            private MachineId Server;
            private int Counter;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            class Init : MachineState { }

            void Configure()
            {
                this.Server = (this.ReceivedEvent as Config).Id;
                this.Counter = 0;
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            class Active : MachineState { }

            async Task ActiveOnEntry()
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

        [Fact]
        public void TestOneMachineReceiveEventFailure()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            var test = new Action<IPSharpRuntime>((r) => { r.CreateMachine(typeof(Server)); });
            var bugReport = "Livelock detected. 'Microsoft.PSharp.TestingServices.Tests.Unit.ReceiveEventFailTest+" +
                "Client()' is waiting for an event, but no other schedulable choices are enabled.";
            base.AssertFailed(configuration, test, bugReport, true);
        }

        [Fact]
        public void TestTwoMachinesReceiveEventFailure()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
            });
            var bugReport = "Livelock detected. 'Microsoft.PSharp.TestingServices.Tests.Unit.ReceiveEventFailTest+" +
                "Client()' and 'Microsoft.PSharp.TestingServices.Tests.Unit.ReceiveEventFailTest+Client()' " +
                "are waiting for an event, but no other schedulable choices are enabled.";
            base.AssertFailed(configuration, test, bugReport, true);
        }

        [Fact]
        public void TestThreeMachinesReceiveEventFailure()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
            });
            var bugReport = "Livelock detected. 'Microsoft.PSharp.TestingServices.Tests.Unit.ReceiveEventFailTest+" +
                "Client()', 'Microsoft.PSharp.TestingServices.Tests.Unit.ReceiveEventFailTest+Client()' " +
                "and 'Microsoft.PSharp.TestingServices.Tests.Unit.ReceiveEventFailTest+Client()' " +
                "are waiting for an event, but no other schedulable choices are enabled.";
            base.AssertFailed(configuration, test, bugReport, true);
        }
    }
}
