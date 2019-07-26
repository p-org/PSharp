using DHittingTestingClient;
using Microsoft.PSharp;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DHittingTests
{
    public class MessageFlowDHittingTests
    {
        /// <summary>
        /// Three machines A, B, X.
        /// * A.send(X) ; A.send(B)
        ///     - B.receive() : B.send(X)
        /// There must be no 2-tuples as the receive events are causally related.
        /// </summary>
        [Fact(Timeout = 5000)]
        public void TestCausalNotAdded()
        {
            Action<IMachineRuntime> testAction = (r =>
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
            });

            MessageFlowBasedMetricReporter reporter = new MessageFlowBasedMetricReporter(3, DHittingUtils.DHittingSignature.TreeHash);

            ProgramAwareTestUtils.RunTest(testAction, new BasicProgramModelBasedStrategy(new RandomStrategy(0), false), reporter, 10, 0, true);

            ProgramAwareTestUtils.CheckDTupleCount(reporter, 1, 4);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 2, 0);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 3, 0);
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
            Action<IMachineRuntime> testAction = (r =>
            {
                var xId = r.CreateMachine(typeof(ForwarderMachine));
                var yId = r.CreateMachine(typeof(ForwarderMachine));

                var aId = r.CreateMachine(typeof(ForwarderMachine));
                var bId = r.CreateMachine(typeof(ForwarderMachine));
                var cId = r.CreateMachine(typeof(ForwarderMachine));

                r.SendEvent(aId, new ForwarderEvent(xId, new ForwarderEvent()));
                r.SendEvent(bId, new ForwarderEvent(xId, new ForwarderEvent(yId, new ForwarderEvent())));
                r.SendEvent(cId, new ForwarderEvent(yId, new ForwarderEvent()));
            });
            
            MessageFlowBasedMetricReporter reporter = new MessageFlowBasedMetricReporter(3, DHittingUtils.DHittingSignature.TreeHash);

            ProgramAwareTestUtils.RunTest(testAction, new BasicProgramModelBasedStrategy(new DPORStrategy(null, -1, 0), false), reporter, 100, 0, true);

            ProgramAwareTestUtils.CheckDTupleCount(reporter, 1, 7);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 2, 5);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 3, 1);
        }
    }
}

