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
    public class WarmStateTest : BaseTest
    {
        public WarmStateTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Unit : Event
        {
        }

        private class UserEvent : Event
        {
        }

        private class Done : Event
        {
        }

        private class Waiting : Event
        {
        }

        private class Computing : Event
        {
        }

        private class EventHandler : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(WaitForUser))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Unit());
            }

            [OnEntry(nameof(WaitForUserOnEntry))]
            [OnEventGotoState(typeof(UserEvent), typeof(HandleEvent))]
            private class WaitForUser : MachineState
            {
            }

            private void WaitForUserOnEntry()
            {
                this.Monitor<WatchDog>(new Waiting());
                this.Send(this.Id, new UserEvent());
            }

            [OnEntry(nameof(HandleEventOnEntry))]
            private class HandleEvent : MachineState
            {
            }

            private void HandleEventOnEntry()
            {
                this.Monitor<WatchDog>(new Computing());
            }
        }

        private class WatchDog : Monitor
        {
            [Start]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            private class CanGetUserInput : MonitorState
            {
            }

            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            private class CannotGetUserInput : MonitorState
            {
            }
        }

        [Fact]
        public void TestWarmState()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler));
            });

            this.AssertSucceeded(configuration, test);
        }
    }
}
