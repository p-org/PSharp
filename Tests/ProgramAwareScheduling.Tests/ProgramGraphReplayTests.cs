// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;
using Xunit;

namespace Microsoft.PSharp.ProgramAwareScheduling.Tests
{
    public class ProgramGraphReplayTests
    {
        [Fact(Timeout = 5000)]
        public void ReplayProductSumBug()
        {
            Action<IMachineRuntime> testAction = r =>
            {
                var sId = r.CreateMachine(typeof(ForwarderMachine));
                var pId = r.CreateMachine(typeof(ForwarderMachine));

                // Hit bug if value ever becomes 9
                var mId = r.CreateMachine(typeof(SumProductMachine), new TargetValueEvent(9));
                r.SendEvent(mId, new AddEvent(1)); // get things off to a good start.
                for (int i = 0; i < 2; i++)
                {
                    r.SendEvent(sId, new ForwarderEvent(mId, new AddEvent(1)));
                    r.SendEvent(pId, new ForwarderEvent(mId, new ProductEvent(2)));
                }
            };

            AbstractBaseProgramModelStrategy strategy = new BasicProgramModelBasedStrategy(new DPORStrategy(), false);
            TestingReporter reporter = new TestingReporter(strategy);

            // There should be 4! possible iterations
            Assert.True(SimpleTesterController.RunTest(testAction, strategy, reporter, 48, 0, false, 0), "The test encountered an unexpected error:\n" + SimpleTesterController.CaughtException);
            Assert.True(reporter.ProgramSummary.BugTriggeringStep != null, "Program did not hit bug. Test is wrong");

            ReplayProgramGraphAndAssertBugReproduced(testAction, reporter.ProgramSummary, 1);
        }

        [Fact(Timeout = 5000)]
        public void ReplayExplicitReceive()
        {
            Action<IMachineRuntime> testAction = r =>
            {
                // Hit bug if value ever becomes 3
                var mId = r.CreateMachine(typeof(ContestingReceivesMachine), new TargetValueEvent(3));
                var sId = r.CreateMachine(typeof(ForwarderMachine));
                var pId = r.CreateMachine(typeof(ForwarderMachine));

                r.SendEvent(sId, new ForwarderEvent(mId, new AddEvent(1)));
                r.SendEvent(pId, new ForwarderEvent(mId, new ProductEvent(2)));
                r.SendEvent(mId, new BreakLoopEvent());
            };

            AbstractBaseProgramModelStrategy strategy = new BasicProgramModelBasedStrategy(new DPORStrategy(), false);
            TestingReporter reporter = new TestingReporter(strategy);

            // There should be 4! possible iterations
            Assert.True(SimpleTesterController.RunTest(testAction, strategy, reporter, 10, 0, true, 0), "The test encountered an unexpected error:\n" + SimpleTesterController.CaughtException);
            Assert.True(reporter.ProgramSummary.BugTriggeringStep != null, "Program did not hit bug. Test is wrong");

            ReplayProgramGraphAndAssertBugReproduced(testAction, reporter.ProgramSummary, 10);
        }

        private static void ReplayProgramGraphAndAssertBugReproduced(Action<IMachineRuntime> testAction, ProgramModelSummary guideSummary, int nReplays)
        {
            Configuration config = AbstractStrategyController.CreateDefaultConfiguration().WithMaxSteps(guideSummary.NumSteps);
            AbstractBaseProgramModelStrategy replayStrategy = new TestProgramGraphReplayWithRandomTiebreakerStrategy(guideSummary.PartialOrderRoot, false, config, null);
            TestingReporter replayReporter = new TestingReporter(replayStrategy);

            // Run it enough times to make sure we're good
            Assert.True(SimpleTesterController.RunTest(testAction, replayStrategy, replayReporter, nReplays, 0, true, 0), "The test encountered an unexpected error:\n" + SimpleTesterController.CaughtException);

            Assert.True(replayReporter.ProgramSummaries.Count == nReplays, "Didn't replay enough times");

            foreach (ProgramModelSummary summary in replayReporter.ProgramSummaries)
            {
                Assert.True(summary.BugTriggeringStep != null, "Replay did not reproduce bug");

                Assert.True(
                    PartialOrderManipulationUtils.StepsMatch(
                        guideSummary.PartialOrderRoot, guideSummary.BugTriggeringStep,
                        summary.PartialOrderRoot, summary.BugTriggeringStep),
                    "Replay hit bug, but the reproducing steps are different");
            }
        }
    }
}
