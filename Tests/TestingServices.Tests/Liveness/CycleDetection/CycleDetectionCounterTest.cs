// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class CycleDetectionCounterTest : BaseTest
    {
        public CycleDetectionCounterTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Configure : Event
        {
            public bool CacheCounter;
            public bool ResetCounter;

            public Configure(bool cacheCounter, bool resetCounter)
            {
                this.CacheCounter = cacheCounter;
                this.ResetCounter = resetCounter;
            }
        }

        private class Message : Event
        {
        }

        private class EventHandler : Machine
        {
            private int Counter;
            private bool CacheCounter;
            private bool ResetCounter;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            private class Init : MachineState
            {
            }

            private void OnInitEntry()
            {
                this.Counter = 0;
                this.CacheCounter = (this.ReceivedEvent as Configure).CacheCounter;
                this.ResetCounter = (this.ReceivedEvent as Configure).ResetCounter;
                this.Send(this.Id, new Message());
            }

            private void OnMessage()
            {
                this.Send(this.Id, new Message());
                this.Counter++;
                if (this.ResetCounter && this.Counter == 10)
                {
                    this.Counter = 0;
                }
            }

            protected override int HashedState
            {
                get
                {
                    if (this.CacheCounter)
                    {
                        // The counter contributes to the cached machine state.
                        // This allows the liveness checker to detect progress.
                        return this.Counter;
                    }
                    else
                    {
                        return base.HashedState;
                    }
                }
            }
        }

        private class WatchDog : Monitor
        {
            [Start]
            [Hot]
            private class HotState : MonitorState
            {
            }
        }

        [Fact]
        public void TestCycleDetectionCounterNoBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.EnableUserDefinedStateHashing = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(true, false));
            });

            this.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestCycleDetectionCounterBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.EnableUserDefinedStateHashing = true;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(false, false));
            });

            string bugReport = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            this.AssertFailed(configuration, test, bugReport, true);
        }

        [Fact]
        public void TestCycleDetectionCounterResetBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.EnableUserDefinedStateHashing = true;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(true, true));
            });

            string bugReport = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            this.AssertFailed(configuration, test, bugReport, true);
        }
    }
}
