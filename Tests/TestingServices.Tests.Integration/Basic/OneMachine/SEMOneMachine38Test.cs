// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine38Test : BaseTest
    {
        class E : Event { }

        class Program : Machine
        {
            int i;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventPushState(typeof(E), typeof(Call))]
            [OnEventDoAction(typeof(Default), nameof(InitAction))]
            class Init : MachineState { }

            void EntryInit()
            {
                i = 0;
                this.Raise(new E());
            }

            void ExitInit() { }

            void InitAction()
            {
                this.Assert(false); // reachable
            }

            [OnEntry(nameof(EntryCall))]
            [OnExit(nameof(ExitCall))]
            [IgnoreEvents(typeof(E))]
            class Call : MachineState { }

            void EntryCall()
            {
                if (i == 0)
                {
                    this.Raise(new E());
                }
                else
                {
                    i = i + 1;
                }
            }

            void ExitCall() { }
        }

        /// <summary>
        /// P# semantics test: one machine: "null" handler semantics.
        /// Testing that null handler is inherited by the pushed state.
        /// </summary>
        [Fact]
        public void TestNullHandlerInheritedByPushTransition()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Program));
            });

            base.AssertFailed(test, 1);
        }
    }
}
