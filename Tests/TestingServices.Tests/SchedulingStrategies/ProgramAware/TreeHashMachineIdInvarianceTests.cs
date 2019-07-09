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

namespace Microsoft.PSharp.TestingServices.Tests.ProgramAware
{
    public class TreeHashMachineIdInvarianceTests : BaseTest
    {
        private static readonly WrapperStrategyConfiguration MsgFlowDHittingWithTreeHashSigConfig =
            WrapperStrategyConfiguration.CreateDHittingStrategy(WrapperStrategyConfiguration.WrapperStrategy.MessageFlowDHitting, WrapperStrategyConfiguration.DHittingSignature.TreeHash, 3);

        public TreeHashMachineIdInvarianceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestMachineIdIndependenceTwoChains()
        {
            ITestingEngine runtime = this.Test(r =>
            {
                MachineId m0 = r.CreateMachine(typeof(CreateAndSendOnPingMachine));
                MachineId m1 = r.CreateMachine(typeof(CreateAndSendOnPingMachine));
                r.SendEvent(m0, new ForwarderEvent(null, new ForwarderEvent()));
                r.SendEvent(m1, new ForwarderEvent(null, new ForwarderEvent()));
            }, configuration: Configuration.Create().WithStrategy(SchedulingStrategy.Random).WithNumberOfIterations(10).WithWrapperStrategy(MsgFlowDHittingWithTreeHashSigConfig));

            MessageFlowBasedDHittingMetricStrategy strategy = (runtime as BugFindingEngine).Strategy as MessageFlowBasedDHittingMetricStrategy;
            Console.WriteLine(strategy.GetDTupleCount(1));
            Console.WriteLine(strategy.GetDTupleCount(2));
            Console.WriteLine(strategy.GetDTupleCount(3));

            ulong oneTuples = strategy.GetDTupleCount(1);
            Assert.True(
                oneTuples == 4,
                "Number of expected 1-tuples did not match. Expected 4 ; Received " + oneTuples);

            ulong twoTuples = strategy.GetDTupleCount(2);
            Assert.True(
                twoTuples == 0,
                "Number of expected 2-tuples did not match. Expected 0 ; Received " + twoTuples);

            ulong threeTuples = strategy.GetDTupleCount(3);
            Assert.True(
                threeTuples == 0,
                "Number of expected 3-tuples did not match. Expected 0 ; Received " + threeTuples);
        }

        [Fact(Timeout = 5000)]
        public void TestMachineIdIndependenceReceivedMessageOrder()
        {
            int nMachines = 3;
            ITestingEngine runtime = this.Test(r =>
            {
                var creatorMachineId = r.CreateMachine(typeof(CreateAndSendOnPingMachine));
                for (int i = 0; i < nMachines; i++)
                {
                    var senderId = r.CreateMachine(typeof(ForwarderMachine), new ForwarderEvent(creatorMachineId, new ForwarderEvent()));
                    r.SendEvent(senderId, new ForwarderEvent(creatorMachineId, new ForwarderEvent()));
                }
            }, configuration: Configuration.Create().WithStrategy(SchedulingStrategy.DPOR).WithNumberOfIterations(1000).WithWrapperStrategy(MsgFlowDHittingWithTreeHashSigConfig));

            MessageFlowBasedDHittingMetricStrategy strategy = (runtime as BugFindingEngine).Strategy as MessageFlowBasedDHittingMetricStrategy;

#if VERBOSE_VERSION
            ulong oneTuples = strategy.GetDTupleCount(1);
            Assert.True(
                oneTuples == 6, // 6 sends
                "Number of expected 1-tuples did not match. Expected 6 ; Received " + oneTuples);

            ulong twoTuples = strategy.GetDTupleCount(2);
            Assert.True(
                twoTuples == 9, // 3 initial sends commute. 3 dummy sends permute => 3 + 3! = 9
                "Number of expected 2-tuples did not match. Expected 9 ; Received " + twoTuples);

            ulong threeTuples = strategy.GetDTupleCount(3);
            Assert.True(
                strategy.GetDTupleCount(3) == 0,    // 3 dummy sends OR any initial send + 2 dummy sends => 3! + 3 * ( 3p2 ) = 24
                "Number of expected 3-tuples did not match. Expected 24 ; Received " + threeTuples);
#endif
            ulong oneTuples = strategy.GetDTupleCount(1);
            Assert.True(
                oneTuples == 6, // 6 sends
                "Number of expected 1-tuples did not match. Expected 6 ; Received " + oneTuples);

            ulong twoTuples = strategy.GetDTupleCount(2);
            Assert.True(
                twoTuples == 6, // 3 dummy sends permute => 3p2 = 6
                "Number of expected 2-tuples did not match. Expected 6 ; Received " + twoTuples);

            ulong threeTuples = strategy.GetDTupleCount(3);
            Assert.True(
                threeTuples == 6,    // 3 dummy sends permute => 3! = 6
                "Number of expected 3-tuples did not match. Expected 6 ; Received " + threeTuples);
        }
    }
}
