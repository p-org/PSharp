// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine20Test : BaseTest
    {
        class E : Event { }

        class Real1 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventPushState(typeof(E), typeof(Call))]
            class Init : MachineState { }

            void EntryInit()
            {
                this.Raise(new E());
            }

            void ExitInit() { }

            [OnEntry(nameof(EntryCall))]
            [OnExit(nameof(ExitCall))]
            class Call : MachineState { }

            void EntryCall()
            {
                this.Pop();
            }

            void ExitCall()
            {
                this.Assert(false);
            }
        }

        /// <summary>
        /// Exit function performed while explicitly popping the state.
        /// </summary>
        [Fact]
        public void TestExitAtExplicitPop()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertFailed(test, 1);
        }
    }
}
