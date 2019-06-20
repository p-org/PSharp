// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics;
using Microsoft.PSharp.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.SchedulingStrategies.ProgramAware
{
    public class TreeHashTest : BaseTest
    {
        public TreeHashTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class PingEvent : Event
        {
        }

        private class SenderMachine : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                var receiverId = this.CreateMachine(typeof(ReceiverMachine));
                this.Send(receiverId, new PingEvent());
            }
        }

        private class ReceiverMachine : Machine
        {
            [Start]
            [OnEventGotoState(typeof(PingEvent), typeof(Final))]
            private class Init : MachineState
            {
            }

            private class Final : MachineState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineIdIndependence()
        {
            ITestingEngine runtime = this.Test(r =>
            {
                r.CreateMachine(typeof(SenderMachine));
                r.CreateMachine(typeof(SenderMachine));
            }, configuration: Configuration.Create().WithStrategy(SchedulingStrategy.Random).WithNumberOfIterations(10));

            MessageFlowBasedDHittingMetricStrategy strategy = (runtime as BugFindingEngine).Strategy as MessageFlowBasedDHittingMetricStrategy;
            Console.WriteLine(strategy.GetDTupleCount(1));
            Console.WriteLine(strategy.GetDTupleCount(2));
            Console.WriteLine(strategy.GetDTupleCount(3));

            Assert.True(
                strategy.GetDTupleCount(1) == 2,
                "Number of expected 1-tuples did not match. Expected 2");

            Assert.True(
                strategy.GetDTupleCount(2) == 0,
                "Number of expected 2-tuples did not match. Expected 0");

            Assert.True(
                strategy.GetDTupleCount(3) == 0,
                "Number of expected 3-tuples did not match. Expected 0");
        }
    }
}
