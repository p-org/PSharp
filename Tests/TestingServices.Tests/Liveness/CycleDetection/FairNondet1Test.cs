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
    public class FairNondet1Test : BaseTest
    {
        public FairNondet1Test(ITestOutputHelper output)
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

        private class Loop : Event
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
            [OnEventGotoState(typeof(Done), typeof(WaitForUser))]
            [OnEventGotoState(typeof(Loop), typeof(HandleEvent))]
            private class HandleEvent : MachineState
            {
            }

            private void HandleEventOnEntry()
            {
                this.Monitor<WatchDog>(new Computing());
                if (this.FairRandom())
                {
                    this.Send(this.Id, new Done());
                }
                else
                {
                    this.Send(this.Id, new Loop());
                }
            }
        }

        private class WatchDog : Monitor
        {
            [Start]
            [Cold]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            private class CanGetUserInput : MonitorState
            {
            }

            [Hot]
            [OnEventGotoState(typeof(Waiting), typeof(CanGetUserInput))]
            [OnEventGotoState(typeof(Computing), typeof(CannotGetUserInput))]
            private class CannotGetUserInput : MonitorState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestFairNondet1()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.LivenessTemperatureThreshold = 0;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.MaxSchedulingSteps = 300;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler));
            });

            this.AssertSucceeded(configuration, test);
        }
    }
}
