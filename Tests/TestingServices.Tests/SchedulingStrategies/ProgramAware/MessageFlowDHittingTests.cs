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
    public class MessageFlowDHittingTests : BaseTest
    {
        private static readonly WrapperStrategyConfiguration MsgFlowDHittingWithTreeHashSigConfig =
            WrapperStrategyConfiguration.CreateDHittingStrategy(WrapperStrategyConfiguration.WrapperStrategy.MessageFlowDHitting, WrapperStrategyConfiguration.DHittingSignature.TreeHash);

        public MessageFlowDHittingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        /// <summary>
        /// Three machines A, B, X.
        /// * A.send(X) ; A.send(B)
        ///     - B.receive() : B.send(X)
        /// There must be no 2-tuples as the receive events are causally related.
        /// </summary>
        [Fact(Timeout = 5000)]
        public void TestCausalNotAdded()
        {
            ITestingEngine runtime = this.Test(r =>
            {
                var aId = r.CreateMachine(typeof(ForwarderMachine));
                var bId = r.CreateMachine(typeof(ForwarderMachine));
                var xId = r.CreateMachine(typeof(ForwarderMachine));

                r.SendEvent(
                    aId, new ForwarderEvent(new List<EventWrapper>()
                        {
                            new EventWrapper(xId, new ForwarderEvent()),
                            new EventWrapper(bId, new ForwarderEvent(xId, new ForwarderEvent()))
                        }));
            }, configuration: Configuration.Create().WithStrategy(SchedulingStrategy.Random).WithNumberOfIterations(10).WithWrapperStrategy(MsgFlowDHittingWithTreeHashSigConfig));

            MessageFlowBasedDHittingMetricStrategy strategy = (runtime as BugFindingEngine).Strategy as MessageFlowBasedDHittingMetricStrategy;

            ProgramAwareTestUtils.CheckDTupleCount(strategy, 1, 4);
            ProgramAwareTestUtils.CheckDTupleCount(strategy, 2, 0);
            ProgramAwareTestUtils.CheckDTupleCount(strategy, 3, 0);
        }

        /// <summary>
        /// If (A,B) and (B,C) are in the 2-tuple relation, (A,C) must also be.
        /// 5 machines A,B,C , X,Y.
        ///     * A.send(X)
        ///         - X.receive()
        ///     * B.send(X)
        ///         - X.receive() ; X.send(Y)
        ///     * C.send(Y)
        ///
        ///     We want to ensure that (A.send(X), C.send(Y)) is in the relation.
        /// </summary>
        [Fact(Timeout = 5000)]
        public void TestTransitivityFolding()
        {
            ITestingEngine runtime = this.Test(r =>
            {
                var xId = r.CreateMachine(typeof(ForwarderMachine));
                var yId = r.CreateMachine(typeof(ForwarderMachine));

                var aId = r.CreateMachine(typeof(ForwarderMachine));
                var bId = r.CreateMachine(typeof(ForwarderMachine));
                var cId = r.CreateMachine(typeof(ForwarderMachine));

                r.SendEvent(aId, new ForwarderEvent(xId, new ForwarderEvent()));
                r.SendEvent(bId, new ForwarderEvent(xId, new ForwarderEvent(yId, new ForwarderEvent())));
                r.SendEvent(cId, new ForwarderEvent(yId, new ForwarderEvent()));
            }, configuration: Configuration.Create().WithStrategy(SchedulingStrategy.DPOR).WithNumberOfIterations(100).WithWrapperStrategy(MsgFlowDHittingWithTreeHashSigConfig));

            MessageFlowBasedDHittingMetricStrategy strategy = (runtime as BugFindingEngine).Strategy as MessageFlowBasedDHittingMetricStrategy;

            ProgramAwareTestUtils.CheckDTupleCount(strategy, 1, 7);
            ProgramAwareTestUtils.CheckDTupleCount(strategy, 2, 5);
            ProgramAwareTestUtils.CheckDTupleCount(strategy, 3, 1);
        }
    }
}
