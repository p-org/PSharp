// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class PushStateTest : BaseTest
    {
        public PushStateTest(ITestOutputHelper output)
               : base(output)
        { }

        class A : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [OnEventPushState(typeof(E2), typeof(S1))]
            class S0 : MachineState { }

            [OnEventDoAction(typeof(E3), nameof(Bar))]
            class S1 : MachineState { }

            void Foo() { }

            void Bar()
            {
                this.Pop();
            }

        }

        class E1 : Event
        { }
        class E2 : Event
        { }
        class E3 : Event
        { }
        class E4 : Event
        { }

        class B : Machine
        {
            [Start]
            [OnEntry(nameof(Conf))]
            class Init : MachineState { }

            void Conf()
            {
                var a = this.CreateMachine(typeof(A));
                this.Send(a, new E2()); // push(S1)
                this.Send(a, new E1()); // execute foo without popping
                this.Send(a, new E3()); // can handle it because A is still in S1
            }
        }

        [Fact]
        public void TestPushStateEvent()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(B));
            });

            base.AssertSucceeded(test);
        }
    }
}
