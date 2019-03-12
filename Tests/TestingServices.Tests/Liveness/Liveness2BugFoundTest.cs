﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class Liveness2BugFoundTest : BaseTest
    {
        public Liveness2BugFoundTest(ITestOutputHelper output)
            : base(output)
        { }

        class Unit : Event { }
        class UserEvent : Event { }
        class Done : Event { }
        class Waiting : Event { }
        class Computing : Event { }

        class EventHandler : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(WaitForUser))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Raise(new Unit());
            }

            [OnEntry(nameof(WaitForUserOnEntry))]
            [OnEventGotoState(typeof(UserEvent), typeof(HandleEvent))]
            class WaitForUser : MachineState { }

            void WaitForUserOnEntry()
            {
                this.Monitor<WatchDog>(new Waiting());
                this.Send(this.Id, new UserEvent());
            }

            [OnEntry(nameof(HandleEventOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(HandleEvent))]
            class HandleEvent : MachineState { }

            void HandleEventOnEntry()
            {
                this.Monitor<WatchDog>(new Computing());
            }
        }

        class WatchDog : Monitor
        {
            [Start]
            [Cold]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            class CanGetUserInput : MonitorState { }

            [Hot]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            class CannotGetUserInput : MonitorState { }
        }

        [Fact]
        public void TestLiveness2BugFound()
        {
            var configuration = base.GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler));
            });

            string bugReport = "Monitor 'WatchDog' detected liveness bug in hot state " +
                "'WatchDog.CannotGetUserInput' at the end of program execution.";
            base.AssertFailed(configuration, test, bugReport, true);
        }
    }
}
