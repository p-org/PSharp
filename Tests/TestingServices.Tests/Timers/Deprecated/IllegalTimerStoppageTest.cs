// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Deprecated.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Deprecated
{
    public class DeprecatedIllegalTimerStoppageTest : BaseTest
    {
        public DeprecatedIllegalTimerStoppageTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class TransferTimer : Event
        {
            public TimerId Tid;

            public TransferTimer(TimerId tid)
            {
                this.Tid = tid;
            }
        }

        private class T2 : TimedMachine
        {
            private TimerId tid;
            private readonly object payload = new object();
            private MachineId m;

            [Start]
            [OnEntry(nameof(Initialize))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                this.tid = this.StartTimer(this.payload, 100, true);
                this.m = this.CreateMachine(typeof(T3), new TransferTimer(this.tid));
                this.Raise(new Halt());
            }
        }

        private class T3 : TimedMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private async Task Initialize()
            {
                TimerId tid = (this.ReceivedEvent as TransferTimer).Tid;

                // trying to stop a timer created by a different machine.
                // should throw an assertion violation
                await this.StopTimer(tid, true);
                this.Raise(new Halt());
            }
        }

        [Fact]
        public void IllegalTimerStopTest()
        {
            var config = Configuration.Create().WithNumberOfIterations(1000);
            config.MaxSchedulingSteps = 200;

            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(T2));
            });
            this.AssertFailed(test, 1, true);
        }
    }
}
