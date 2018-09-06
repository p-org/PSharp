﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class AlonBugTest : BaseTest
    {
        class E : Event { }

        class Program : Machine
        {
            int i;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventPushState(typeof(E), typeof(Call))] // Exit does not execute.
            class Init : MachineState { }

            void EntryInit()
            {
                i = 0;
                this.Raise(new E());
            }

            void ExitInit()
            {
                // This assert is unreachable.
                this.Assert(false, "Bug found.");
            }

            [OnEntry(nameof(EntryCall))]
            class Call : MachineState { }

            void EntryCall()
            {
                if (i == 3)
                {
                    this.Pop();
                }
                else
                {
                    i = i + 1;
                    this.Raise(new E()); // Call is popped.
                }
            }
        }

        [Fact]
        public void TestAlonBug()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertSucceeded(test);
        }
    }
}
