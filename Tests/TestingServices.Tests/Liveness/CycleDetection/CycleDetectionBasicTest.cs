// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class CycleDetectionBasicTest : BaseTest
    {
        public CycleDetectionBasicTest(ITestOutputHelper output)
            : base(output)
        { }

        class Configure : Event
        {
            public bool ApplyFix;

            public Configure(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        class Message : Event { }

        class EventHandler : Machine
        {
            bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
                this.Send(this.Id, new Message());
            }

            void OnMessage()
            {
                this.Send(this.Id, new Message());
                if (this.ApplyFix)
                {
                    this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                }
            }
        }

        class WatchDog : Monitor
        {
            public class NotifyMessage : Event { }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(ColdState))]
            class HotState : MonitorState { }

            [Cold]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            class ColdState : MonitorState { }
        }

        [Fact]
        public void TestCycleDetectionBasicNoBug()
        {
            var configuration = base.GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(true));
            });

            base.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestCycleDetectionBasicBug()
        {
            var configuration = base.GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(EventHandler), new Configure(false));
            });

            string bugReport = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            base.AssertFailed(configuration, test, bugReport, true);
        }
    }
}
