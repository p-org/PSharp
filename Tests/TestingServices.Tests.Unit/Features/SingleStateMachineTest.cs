// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class SingleStateMachineTest : BaseTest
    {
        public SingleStateMachineTest(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event
        {
            public int counter;
            public MachineId Id;

            public E(MachineId id)
            {
                counter = 0;
                Id = id;
            }
            public E(int c, MachineId id)
            {
                counter = c;
                Id = id;
            }
        }

        class M : SingleStateMachine
        {
            int count;
            MachineId sender;

            protected override Task InitOnEntry(Event e)
            {
                count = 1;
                sender = (e as E).Id;
                return Task.CompletedTask;
            }

            protected override Task ProcessEvent(Event e)
            {
                count++;
                return Task.CompletedTask;
            }

            protected override async Task OnHaltAsync()
            {
                count++;
                await this.SendAsync(sender, new E(count, this.Id));
            }
        }

        class Harness : SingleStateMachine
        {
            protected override async Task InitOnEntry(Event e)
            {
                var m = this.CreateMachine(typeof(M), new E(this.Id));
                this.Send(m, new E(this.Id));
                this.Send(m, new Halt());
                var r = await this.Receive(typeof(E));
                this.Assert((r as E).counter == 3);
            }
            protected override Task ProcessEvent(Event e)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void TestSingleStateMachine()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Harness));
            });
            var configuration = Configuration.Create();
            configuration.SchedulingIterations = 100;

            AssertSucceeded(configuration, test);
        }
    }
}
