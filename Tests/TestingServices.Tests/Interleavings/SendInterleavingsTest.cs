﻿// ------------------------------------------------------------------------------------------------
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
        { }

        class Config : Event
        {
            public MachineId Id;
            public Config(MachineId id) : base(-1, -1) { this.Id = id; }
        }

        class Event1 : Event { }
        class Event2 : Event { }

        class Receiver : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(Event1), nameof(OnEvent1))]
            [OnEventDoAction(typeof(Event2), nameof(OnEvent2))]
            class Init : MachineState { }

            int count1 = 0;
            void Initialize()
            {
                var s1 = CreateMachine(typeof(Sender1));
                this.Send(s1, new Config(this.Id));
                var s2 = CreateMachine(typeof(Sender2));
                this.Send(s2, new Config(this.Id));
            }

            void OnEvent1()
            {
                count1++;
            }
            void OnEvent2()
            {
                Assert(count1 != 1);
            }
        }

        class Sender1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Config), nameof(Run))]
            class State : MachineState { }

            void Run()
            {
                Send((this.ReceivedEvent as Config).Id, new Event1());
                Send((this.ReceivedEvent as Config).Id, new Event1());
            }
        }

        class Sender2 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Config), nameof(Run))]
            class State : MachineState { }

            void Run()
            {
                Send((this.ReceivedEvent as Config).Id, new Event2());
            }
        }

        [Fact]
        public void TestSendInterleavingsAssertionFailure()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.SchedulingIterations = 600;

            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Receiver));
            });

            base.AssertFailed(configuration, test, 1, true);
        }
    }
}
