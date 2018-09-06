// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class GenericMonitorTest : BaseTest
    {
        class Program<T> : Machine
        {
            T Item;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Item = default(T);
                this.Goto<Active>();
            }

            [OnEntry(nameof(ActiveInit))]
            class Active : MachineState { }

            void ActiveInit()
            {
                this.Assert(this.Item is int);
            }
        }

        class E : Event { }
         
        class M<T> : Monitor
        {
            [Start]
            [OnEntry(nameof(Init))]
            class S1 : MonitorState { }

            class S2 : MonitorState { }

            void Init()
            {
                this.Goto<S2>();
            }
        }

        [Fact]
        public void TestGenericMonitor()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(M<int>));
                r.CreateMachine(typeof(Program<int>));
            });

            base.AssertSucceeded(test);
        }
    }
}
