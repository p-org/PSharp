// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class SEMOneMachine42Test : BaseTest
    {
        class Real1 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(Default), typeof(S1))]
            class Init : MachineState { }

            [OnEntry(nameof(EntryS1))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Assert(this.ReceivedEvent.GetType() == typeof(Default));
            }
        }

        /// <summary>
        /// P# semantics test: one machine: one machine, testing for
        /// "default" event.
        /// </summary>
        [Fact]
        public void TestDefaultEventHandled()
        {
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(Real1));
            });

            base.AssertSucceeded(test);
        }
    }
}
