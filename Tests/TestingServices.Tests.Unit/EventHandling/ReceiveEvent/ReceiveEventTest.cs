// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Microsoft.PSharp.Utilities;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class ReceiveEventTest : BaseTest
    {
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
            [OnEventDoAction(typeof(Pong), nameof(SendPing))]
            class Active : MachineState { }

            void ActiveOnEntry()
            {
                this.SendPing();
            }

            void SendPing()
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

        /// <summary>
        /// P# semantics test: two machines, monitor instantiation parameter.
        /// </summary>
        [Fact]
        public void TestReceiveEvent()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(Server)); });
            base.AssertSucceeded(configuration, test);
        }
    }
}
