using Microsoft.PSharp;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using System;
using Xunit;

using DHittingTestingClient;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingClientInterface.SimpleImplementation;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using System.Collections.Generic;

namespace DHittingTests
{
    public class InboxOrderDHittingTests
    {
        // The actual tests

        [Fact(Timeout = 5000)]
        public void TestTwoChainsWithNEachEventTypeIndexSignature()
        {
            int n = 4;

            Action<IMachineRuntime> testAction = (r =>
            {
                var aId = r.CreateMachine(typeof(ForwarderMachine));
                var bId = r.CreateMachine(typeof(ForwarderMachine));

                for (int i = 0; i < n; i++)
                {
                    r.SendEvent(aId, new ForwarderEvent());
                    r.SendEvent(bId, new ForwarderEvent());
                }
            });

            InboxBasedDHittingMetricReporter reporter = new InboxBasedDHittingMetricReporter(3, DHittingUtils.DHittingSignature.EventTypeIndex);

            ProgramAwareTestUtils.RunTest(testAction, new BasicProgramModelBasedStrategy(new RandomStrategy(0), false), reporter, 1, 0, true);

            // Because both machines are the same, you won't get 2n 1-tuples.
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 1, n);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 2, ProgramAwareTestUtils.Choose(n, 2));
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 3, ProgramAwareTestUtils.Choose(n, 3));
        }

        [Fact(Timeout = 5000)]
        public void TestDepthOneInvertedTreeNEachEventTypeIndexSignature()
        {
            int n = 5;
            int nIterationsRequired = ProgramAwareTestUtils.Permute(n, n);
            Action<IMachineRuntime> testAction = (r =>
            {
                var rootId = r.CreateMachine(typeof(ForwarderMachine));
                List<MachineId> machineIds = new List<MachineId>();
                for (int i = 0; i < n; i++)
                {
                    machineIds.Add(r.CreateMachine(typeof(ForwarderMachine)));
                }
                foreach (MachineId mId in machineIds)
                {
                    r.SendEvent(mId, new ForwarderEvent(rootId, new ForwarderEvent()));
                }
            });

            InboxBasedDHittingMetricReporter reporter = new InboxBasedDHittingMetricReporter(3, DHittingUtils.DHittingSignature.EventTypeIndex);

            ProgramAwareTestUtils.RunTest(testAction, new BasicProgramModelBasedStrategy(new DPORStrategy(null, -1, 0), false), reporter, nIterationsRequired, 0, true);

            ProgramAwareTestUtils.CheckDTupleCount(reporter, 1, n);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 2, ProgramAwareTestUtils.Choose(n, 2));
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 3, ProgramAwareTestUtils.Choose(n, 3));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoChainsWithNEachTreeHashSignature()
        {
            int n = 4;
            Action<IMachineRuntime> testAction = (r =>
            {
                var aId = r.CreateMachine(typeof(ForwarderMachine));
                var bId = r.CreateMachine(typeof(ForwarderMachine));

                for (int i = 0; i < n; i++)
                {
                    r.SendEvent(aId, new ForwarderEvent());
                    r.SendEvent(bId, new ForwarderEvent());
                }
            });

            InboxBasedDHittingMetricReporter reporter = new InboxBasedDHittingMetricReporter(3, DHittingUtils.DHittingSignature.TreeHash);

            ProgramAwareTestUtils.RunTest(testAction, new BasicProgramModelBasedStrategy(new RandomStrategy(0), false), reporter, 1, 0, true);

            ProgramAwareTestUtils.CheckDTupleCount(reporter, 1, 2 * n);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 2, 2 * ProgramAwareTestUtils.Choose(n, 2));
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 3, 2 * ProgramAwareTestUtils.Choose(n, 3));
        }

        [Fact(Timeout = 5000)]
        public void TestDepthOneInvertedTreeNEachTreeHashSignature()
        {
            int n = 5;
            int nIterationsRequired = ProgramAwareTestUtils.Permute(n, n);
            Action<IMachineRuntime> testAction = (r =>
            {
                var rootId = r.CreateMachine(typeof(ForwarderMachine));
                List<MachineId> machineIds = new List<MachineId>();
                for (int i = 0; i < n; i++)
                {
                    machineIds.Add(r.CreateMachine(typeof(ForwarderMachine)));
                }
                foreach (MachineId mId in machineIds)
                {
                    r.SendEvent(mId, new ForwarderEvent(rootId, new ForwarderEvent()));
                }
            });

            InboxBasedDHittingMetricReporter reporter = new InboxBasedDHittingMetricReporter(3, DHittingUtils.DHittingSignature.TreeHash);

            ProgramAwareTestUtils.RunTest(testAction, new BasicProgramModelBasedStrategy(new DPORStrategy(null, -1, 0), false), reporter, nIterationsRequired, 0, true);
            

            ProgramAwareTestUtils.CheckDTupleCount(reporter, 1, 2 * n);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 2, ProgramAwareTestUtils.Permute(n, 2));
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 3, ProgramAwareTestUtils.Permute(n, 3));
        }

    }
}
