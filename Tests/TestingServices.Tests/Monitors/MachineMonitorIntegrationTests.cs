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
    public class MachineMonitorIntegrationTests : BaseTest
    {
        public MachineMonitorIntegrationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class CheckE : Event
        {
            public bool Value;

            public CheckE(bool v)
                : base(1, -1)
            {
                this.Value = v;
            }
        }

        private class M1<T> : Machine
        {
            private readonly bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Monitor<T>(new CheckE(this.Test));
            }
        }

        private class M2 : Machine
        {
            private readonly bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Monitor<Spec2>(new CheckE(true));
                this.Monitor<Spec2>(new CheckE(this.Test));
            }
        }

        private class Spec1 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(CheckE), nameof(Check))]
            private class Checking : MonitorState
            {
            }

            private void Check()
            {
                this.Assert((this.ReceivedEvent as CheckE).Value == true);
            }
        }

        private class Spec2 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(CheckE), nameof(Check))]
            private class Checking : MonitorState
            {
            }

            private void Check()
            {
                // this.Assert((this.ReceivedEvent as CheckE).Value == true); // passes
            }
        }

        private class Spec3 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(CheckE), nameof(Check))]
            private class Checking : MonitorState
            {
            }

            private void Check()
            {
                this.Assert((this.ReceivedEvent as CheckE).Value == false);
            }
        }

        [Fact]
        public void TestMachineMonitorIntegration1()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(Spec1));
                r.CreateMachine(typeof(M1<Spec1>));
            });

            this.AssertFailed(configuration, test, 1, true);
        }

        [Fact]
        public void TestMachineMonitorIntegration2()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(Spec2));
                r.CreateMachine(typeof(M2));
            });

            this.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestMachineMonitorIntegration3()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(Spec3));
                r.CreateMachine(typeof(M1<Spec3>));
            });

            this.AssertSucceeded(configuration, test);
        }
    }
}
