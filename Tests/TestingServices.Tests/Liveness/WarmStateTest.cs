// ------------------------------------------------------------------------------------------------
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
            class HandleEvent : MachineState { }

            void HandleEventOnEntry()
            {
                this.Monitor<WatchDog>(new Computing());
            }
        }

        class WatchDog : Monitor
        {
            [Start]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            class CanGetUserInput : MonitorState { }

            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            class CannotGetUserInput : MonitorState { }
        }

        [Fact]
        public void TestWarmState()
        {
            var configuration = base.GetConfiguration();
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler));
            });

            base.AssertSucceeded(configuration, test);
        }
    }
}
