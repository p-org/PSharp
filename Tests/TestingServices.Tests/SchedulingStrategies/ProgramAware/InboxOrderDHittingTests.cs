// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware.ProgramAwareMetrics;
using Microsoft.PSharp.TestingServices.Tests.ProgramAware;
using Microsoft.PSharp.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.ProgramAware
{
    public class InboxOrderDHittingTests : BaseTest
    {
        public InboxOrderDHittingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestTwoChainsWithNEach()
        {
            int n = 4;
            ITestingEngine runtime = this.Test(r =>
            {
                var aId = r.CreateMachine(typeof(ForwarderMachine));
                var bId = r.CreateMachine(typeof(ForwarderMachine));

                for (int i = 0; i < n; i++)
                {
                    r.SendEvent(aId, new ForwarderEvent());
                    r.SendEvent(bId, new ForwarderEvent());
                }
            }, configuration: Configuration.Create().WithStrategy(SchedulingStrategy.Random).WithNumberOfIterations(1).WithWrapperStrategy(SchedulingStrategy.InboxDHittingMetric));

            InboxBasedDHittingMetricStrategy strategy = (runtime as BugFindingEngine).Strategy as InboxBasedDHittingMetricStrategy;

            ProgramAwareTestUtils.CheckDTupleCount(strategy, 1, 2 * n);
            ProgramAwareTestUtils.CheckDTupleCount(strategy, 2, 2 * ProgramAwareTestUtils.Choose(n, 2));
            ProgramAwareTestUtils.CheckDTupleCount(strategy, 3, 2 * ProgramAwareTestUtils.Choose(n, 3));
        }

        [Fact(Timeout = 5000)]
        public void TestDepthOneInvertedTreeNEach()
        {
            int n = 5;
            int nIterationsRequired = ProgramAwareTestUtils.Permute(n, n);
            ITestingEngine runtime = this.Test(r =>
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
            }, configuration: Configuration.Create().WithStrategy(SchedulingStrategy.DPOR).WithNumberOfIterations(nIterationsRequired).WithWrapperStrategy(SchedulingStrategy.InboxDHittingMetric));

            InboxBasedDHittingMetricStrategy strategy = (runtime as BugFindingEngine).Strategy as InboxBasedDHittingMetricStrategy;

            ProgramAwareTestUtils.CheckDTupleCount(strategy, 1, 2 * n);
            ProgramAwareTestUtils.CheckDTupleCount(strategy, 2, ProgramAwareTestUtils.Permute(n, 2));
            ProgramAwareTestUtils.CheckDTupleCount(strategy, 3, ProgramAwareTestUtils.Permute(n, 3));
        }
    }
}
