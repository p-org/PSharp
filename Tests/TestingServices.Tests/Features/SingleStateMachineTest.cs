﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class SingleStateMachineTest : BaseTest
    {
        public SingleStateMachineTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public int Counter;
            public MachineId Id;

            public E(MachineId id)
            {
                this.Counter = 0;
                this.Id = id;
            }

            public E(int c, MachineId id)
            {
                this.Counter = c;
                this.Id = id;
            }
        }

        private class M : SingleStateMachine
        {
            private int count;
            private MachineId sender;

            protected override Task InitOnEntry(Event e)
            {
                this.count = 1;
                this.sender = (e as E).Id;
                return Task.CompletedTask;
            }

            protected override Task ProcessEvent(Event e)
            {
                this.count++;
                return Task.CompletedTask;
            }

            protected override void OnHalt()
            {
                this.count++;
                this.Runtime.SendEvent(this.sender, new E(this.count, this.Id));
            }
        }

        private class Harness : SingleStateMachine
        {
            protected override async Task InitOnEntry(Event e)
            {
                var m = this.CreateMachine(typeof(M), new E(this.Id));
                this.Send(m, new E(this.Id));
                this.Send(m, new Halt());
                var r = await this.Receive(typeof(E));
                this.Assert((r as E).Counter == 3);
            }

            protected override Task ProcessEvent(Event e)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void TestSingleStateMachine()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(Harness));
            });
            var configuration = Configuration.Create();
            configuration.SchedulingIterations = 100;

            this.AssertSucceeded(configuration, test);
        }
    }
}
