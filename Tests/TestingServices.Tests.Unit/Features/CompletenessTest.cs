﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class CompletenessTest : BaseTest
    {
        class E1 : Event { }
        class E2 : Event { }

        class P : Monitor
        {
            [Cold]
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Fail))]
            [OnEventGotoState(typeof(E2), typeof(S2))]
            class S1 : MonitorState { }

            [Cold]
            [IgnoreEvents(typeof(E1), typeof(E2))]
            class S2 : MonitorState { }

            void Fail()
            {
                this.Assert(false);
            }

        }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class S : MachineState { }

            void InitOnEntry()
            {
                this.Monitor<P>(new E1());
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class S : MachineState { }

            void InitOnEntry()
            {
                this.Monitor<P>(new E2());
            }
        }

        [Fact]
        public void TestCompleteness1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(P));
                r.CreateMachine(typeof(M2));
                r.CreateMachine(typeof(M1));
            });

            var config = Configuration.Create().WithNumberOfIterations(100);

            base.AssertFailed(config, test, 1, true);
        }

        [Fact]
        public void TestCompleteness2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(P));
                r.CreateMachine(typeof(M1));
                r.CreateMachine(typeof(M2));
            });

            var config = Configuration.Create().WithNumberOfIterations(100);

            base.AssertFailed(config, test, 1, true);
        }

    }
}
