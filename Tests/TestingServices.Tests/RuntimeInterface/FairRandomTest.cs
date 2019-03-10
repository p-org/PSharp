// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class FairRandomTest : BaseTest
    {
        public FairRandomTest(ITestOutputHelper output)
            : base(output)
        { }

        class E1 : Event { }
        class E2 : Event { }

        class Engine
        {
            public static bool FairRandom(PSharpRuntime runtime)
            {
                return runtime.FairRandom();
            }
        }

        class UntilDone : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E2), typeof(End))]
            class Waiting : MonitorState { }

            [Cold]
            class End : MonitorState { }
        }

        class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleEvent1))]
            [OnEventDoAction(typeof(E2), nameof(HandleEvent2))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Send(this.Id, new E1());
            }

            void HandleEvent1()
            {
                if(Engine.FairRandom(this.Id.Runtime))
                {
                    this.Send(this.Id, new E2());
                }
                else
                {
                    this.Send(this.Id, new E1());
                }   
            }

            void HandleEvent2()
            {
                this.Monitor<UntilDone>(new E2());
                this.Raise(new Halt());
            }
        }

        [Fact]
        public void TestFairRandom()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(UntilDone));
                var m = r.CreateMachine(typeof(M));
                r.SendEvent(m, new E1());
            });

            base.AssertSucceeded(test);
        }
    }
}
