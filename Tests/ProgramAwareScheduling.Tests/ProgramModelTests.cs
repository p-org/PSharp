// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.TestingClientInterface;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

using Xunit;

namespace Microsoft.PSharp.ProgramAwareScheduling.Tests
{
    public class ProgramModelTests
    {
        [Fact(Timeout = 5000)]
        public void TestCreateMachineWithEvent()
        {
            Action<IMachineRuntime> testAction = r =>
            {
                var aId = r.CreateMachine(typeof(ForwarderMachine), new ForwarderEvent());
            };

            AbstractBaseProgramModelStrategy strategy = new BasicProgramModelBasedStrategy(new RandomStrategy(0), false);
            TestingReporter reporter = new TestingReporter(strategy);

            Assert.True(SimpleTesterController.RunTest(testAction, strategy, reporter, 1, 0, true, 2), "The test encountered an unexpected error:\n" + SimpleTesterController.CaughtException);
        }
    }
}
