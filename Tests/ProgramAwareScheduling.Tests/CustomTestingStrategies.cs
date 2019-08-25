// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.ProgramAwareScheduling.Tests
{
    internal class TestProgramGraphReplayWithRandomTiebreakerStrategy : ProgramGraphReplayStrategy
    {
        private readonly IRandomNumberGenerator RandomGenerator;

        public TestProgramGraphReplayWithRandomTiebreakerStrategy(ProgramStep rootStep, bool isScheduleFair, Configuration configuration, ISchedulingStrategy suffixStrategy = null)
            : base(rootStep, isScheduleFair, configuration, suffixStrategy)
        {
            this.RandomGenerator = new DefaultRandomNumberGenerator(DateTime.Now.Millisecond);
        }

        protected override ProgramStep ChooseNextStep(Dictionary<ProgramStep, IAsyncOperation> candidateSteps)
        {
            int idx = this.RandomGenerator.Next(candidateSteps.Count);
            return new List<ProgramStep>(candidateSteps.Keys)[idx];
        }
    }
}
