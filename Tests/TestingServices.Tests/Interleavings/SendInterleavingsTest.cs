// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.Utilities;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class SendInterleavingsTest : BaseTest
    {
        public SendInterleavingsTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Config : Event
        {
            public MachineId Id;

            public Config(MachineId id)
                : base(-1, -1)
            {
                this.Id = id;
            }
        }

        private class Event1 : Event
        {
        }

        private class Event2 : Event
        {
        }

        private class Receiver : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(Event1), nameof(OnEvent1))]
            [OnEventDoAction(typeof(Event2), nameof(OnEvent2))]
            private class Init : MachineState
            {
            }

            private int count1 = 0;

            private void Initialize()
            {
                var s1 = this.CreateMachine(typeof(Sender1));
                this.Send(s1, new Config(this.Id));
                var s2 = this.CreateMachine(typeof(Sender2));
                this.Send(s2, new Config(this.Id));
            }

            private void OnEvent1()
            {
                this.count1++;
            }

            private void OnEvent2()
            {
                this.Assert(this.count1 != 1);
            }
        }

        private class Sender1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Config), nameof(Run))]
            private class State : MachineState
            {
            }

            private void Run()
            {
                this.Send((this.ReceivedEvent as Config).Id, new Event1());
                this.Send((this.ReceivedEvent as Config).Id, new Event1());
            }
        }

        private class Sender2 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Config), nameof(Run))]
            private class State : MachineState
            {
            }

            private void Run()
            {
                this.Send((this.ReceivedEvent as Config).Id, new Event2());
            }
        }

        [Fact]
        public void TestSendInterleavingsAssertionFailure()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.SchedulingIterations = 600;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(Receiver));
            });

            this.AssertFailed(configuration, test, 1, true);
        }
    }
}
