// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class Liveness2LoopMachineTest : BaseTest
    {
        public Liveness2LoopMachineTest(ITestOutputHelper output)
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
                this.CreateMachine(typeof(Loop));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(WaitForUserOnEntry))]
            [OnEventGotoState(typeof(UserEvent), typeof(HandleEvent))]
            class WaitForUser : MachineState { }

            void WaitForUserOnEntry()
            {
                this.Monitor<LivenessMonitor>(new Waiting());
                this.Send(this.Id, new UserEvent());
            }

            [OnEntry(nameof(HandleEventOnEntry))]
            class HandleEvent : MachineState { }

            void HandleEventOnEntry()
            {
                this.Monitor<LivenessMonitor>(new Computing());
            }
        }

        class Loop : Machine
        {
            [Start]
            [OnEntry(nameof(LoopingOnEntry))]
            [OnEventGotoState(typeof(Done), typeof(Looping))]
            class Looping : MachineState { }

            void LoopingOnEntry()
            {
                this.Send(this.Id, new Done());
            }
        }

        class LivenessMonitor : Monitor
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
        public void TestLiveness2LoopMachine()
        {
            var configuration = base.GetConfiguration();
            configuration.LivenessTemperatureThreshold = 200;
            configuration.SchedulingIterations = 1;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(EventHandler));
            });

            var bugReport = "Monitor 'LivenessMonitor' detected potential liveness bug in hot state " +
                "'LivenessMonitor.CannotGetUserInput'.";
            base.AssertFailed(configuration, test, bugReport, true);
        }
    }
}
