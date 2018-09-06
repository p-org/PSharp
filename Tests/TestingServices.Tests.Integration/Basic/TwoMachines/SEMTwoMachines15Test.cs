﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.Utilities;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMTwoMachines15Test : BaseTest
    {
        class Config : Event
        {
            public bool Value;
            public Config(bool v) : base(1, -1) { this.Value = v; }
        }

        class Real1 : Machine
        {
            bool test = false;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Monitor<M>(new Config(test));
            }
        }

        class M : Monitor
        {
            [Start]
            [OnEntry(nameof(EntryX))]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            class X : MonitorState { }

            void EntryX() { }

            void Configure()
            {
                this.Assert((this.ReceivedEvent as Config).Value == false); // passes
            }
        }

        [Fact]
        public void TestSEMTwoMachines15()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(M));
                r.CreateMachine(typeof(Real1));
            });

            base.AssertSucceeded(configuration, test);
        }
    }
}
