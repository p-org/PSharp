using DHittingTestingClient;
using Microsoft.PSharp;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DHittingTests
{
    public class TreeHashMachineIdInvarianceTests
    { 
        [Fact(Timeout = 5000)]
        public void TestMachineIdIndependenceTwoChains()
        {
            Action<IMachineRuntime> testAction = (r =>
            {
                MachineId m0 = r.CreateMachine(typeof(CreateAndSendOnPingMachine));
                MachineId m1 = r.CreateMachine(typeof(CreateAndSendOnPingMachine));
                r.SendEvent(m0, new ForwarderEvent(null, new ForwarderEvent()));
                r.SendEvent(m1, new ForwarderEvent(null, new ForwarderEvent()));
            });
            
            MessageFlowBasedMetricReporter reporter = new MessageFlowBasedMetricReporter(3, DHittingUtils.DHittingSignature.TreeHash);

            ProgramAwareTestUtils.RunTest(testAction, new BasicProgramModelBasedStrategy(new RandomStrategy(0), false), reporter, 10, 0, true);

            ProgramAwareTestUtils.CheckDTupleCount(reporter, 1, 4);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 2, 0);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 3, 0);            
        }

        [Fact(Timeout = 5000)]
        public void TestMachineIdIndependenceReceivedMessageOrder()
        {
            int nMachines = 3;
            Action<IMachineRuntime> testAction = (r =>
            {
                var creatorMachineId = r.CreateMachine(typeof(CreateAndSendOnPingMachine));
                for (int i = 0; i < nMachines; i++)
                {
                    var senderId = r.CreateMachine(typeof(ForwarderMachine), new ForwarderEvent(creatorMachineId, new ForwarderEvent()));
                    r.SendEvent(senderId, new ForwarderEvent(creatorMachineId, new ForwarderEvent()));
                }
            });

            MessageFlowBasedMetricReporter reporter = new MessageFlowBasedMetricReporter(3, DHittingUtils.DHittingSignature.TreeHash);

            ProgramAwareTestUtils.RunTest(testAction, new BasicProgramModelBasedStrategy(new DPORStrategy(null, -1, 0), false), reporter, 1000, 0, true);

            ProgramAwareTestUtils.CheckDTupleCount(reporter, 1, 6);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 2, 6);
            ProgramAwareTestUtils.CheckDTupleCount(reporter, 3, 6);
        }
    }
}
