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
    public class MachineMonitorIntegrationTests : BaseTest
    {
        public MachineMonitorIntegrationTests(ITestOutputHelper output)
            : base(output)
        { }

        class CheckE : Event
        {
            public bool Value;

            public CheckE(bool v)
                : base(1, -1)
            {
                this.Value = v;
            }
        }

        class M1<T> : Machine
        {
            private readonly bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Monitor<T>(new CheckE(this.Test));
            }
        }

        class M2 : Machine
        {
            private readonly bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Monitor<Spec2>(new CheckE(true));
                this.Monitor<Spec2>(new CheckE(this.Test));
            }
        }

        class Spec1 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(CheckE), nameof(Check))]
            class Checking : MonitorState { }

            void Check()
            {
                this.Assert((this.ReceivedEvent as CheckE).Value == true);
            }
        }

        class Spec2 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(CheckE), nameof(Check))]
            class Checking : MonitorState { }

            void Check()
            {
                //this.Assert((this.ReceivedEvent as CheckE).Value == true); // passes
            }
        }

        class Spec3 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(CheckE), nameof(Check))]
            class Checking : MonitorState { }

            void Check()
            {
                this.Assert((this.ReceivedEvent as CheckE).Value == false);
            }
        }

        [Fact]
        public void TestMachineMonitorIntegration1()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Spec1));
                r.CreateMachine(typeof(M1<Spec1>));
            });

            base.AssertFailed(configuration, test, 1, true);
        }

        [Fact]
        public void TestMachineMonitorIntegration2()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Spec2));
                r.CreateMachine(typeof(M2));
            });

            base.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestMachineMonitorIntegration3()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Spec3));
                r.CreateMachine(typeof(M1<Spec3>));
            });

            base.AssertSucceeded(configuration, test);
        }
    }
}
