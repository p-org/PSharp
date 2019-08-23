﻿// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class PushStateTest : BaseTest
    {
        public PushStateTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [OnEventPushState(typeof(E2), typeof(S1))]
            private class S0 : MachineState
            {
            }

            [OnEventDoAction(typeof(E3), nameof(Bar))]
            private class S1 : MachineState
            {
            }

            private void Foo()
            {
            }

            private void Bar()
            {
                this.Pop();
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
        }

        private class B : Machine
        {
            [Start]
            [OnEntry(nameof(Conf))]
            private class Init : MachineState
            {
            }

            private void Conf()
            {
                var a = this.CreateMachine(typeof(A));
                this.Send(a, new E2()); // push(S1)
                this.Send(a, new E1()); // execute foo without popping
                this.Send(a, new E3()); // can handle it because A is still in S1
            }
        }

        [Fact(Timeout=5000)]
        public void TestPushStateEvent()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(B));
            });
        }
    }
}
