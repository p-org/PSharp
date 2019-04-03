// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class CycleDetectionRandomChoiceTest : BaseTest
    {
        public CycleDetectionRandomChoiceTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Configure : Event
        {
            public bool ApplyFix;

            public Configure(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        private class Message : Event
        {
        }

        private class EventHandler : Machine
        {
            private bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            private class Init : MachineState
            {
            }

            private void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
                this.Send(this.Id, new Message());
            }

            private void OnMessage()
            {
                this.Send(this.Id, new Message());
                this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                if (this.Choose())
                {
                    this.Monitor<WatchDog>(new WatchDog.NotifyDone());
                    this.Raise(new Halt());
                }
            }

            private bool Choose()
            {
                if (this.ApplyFix)
                {
                    return this.FairRandom();
                }
                else
                {
                    return this.Random();
                }
            }
        }

        private class WatchDog : Monitor
        {
            public class NotifyMessage : Event
            {
            }

            public class NotifyDone : Event
            {
            }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            [OnEventGotoState(typeof(NotifyDone), typeof(ColdState))]
            private class HotState : MonitorState
            {
            }

            [Cold]
            private class ColdState : MonitorState
            {
            }
        }

        [Fact]
        public void TestCycleDetectionRandomChoiceNoBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.RandomSchedulingSeed = 906;
            configuration.SchedulingIterations = 7;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(true));
            });

            this.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestCycleDetectionRandomChoiceBug()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.RandomSchedulingSeed = 906;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(false));
            });

            string bugReport = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            this.AssertFailed(configuration, test, bugReport, true);
        }
    }
}
