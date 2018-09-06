// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class WildCardEventTest : BaseTest
    {
        class A : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(foo))]
            [OnEventGotoState(typeof(E2), typeof(S1))]
            [DeferEvents(typeof(WildCardEvent))]
            class S0 : MachineState { }

            [OnEventDoAction(typeof(E3), nameof(bar))]
            class S1 : MachineState { }

            void foo()
            {
            }

            void bar()
            {
            }

        }

        class E1 : Event
        { }
        class E2 : Event
        { }
        class E3 : Event
        { }

        class B : Machine
        {
            [Start]
            [OnEntry(nameof(Conf))]
            class Init : MachineState { }

            void Conf()
            {
                var a = this.CreateMachine(typeof(A));
                this.Send(a, new E3());
                this.Send(a, new E1());
                this.Send(a, new E2());
            }
        }

        [Fact]
        public void TestWildCardEvent()
        {
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(B)); });
            base.AssertSucceeded(test);
        }
    }
}
