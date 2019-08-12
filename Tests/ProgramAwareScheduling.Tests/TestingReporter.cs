// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.TestingClientInterface.SimpleImplementation;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.ProgramAwareScheduling.Tests
{
    public class TestingReporter : IMetricReporter
    {
        public readonly ISchedulingStrategy Strategy;
        public ProgramModelSummary ProgramSummary;

        public bool BugFound { get; private set; }

        public TestingReporter(ISchedulingStrategy strategy)
        {
            this.Strategy = strategy;
        }

        public string GetReport()
        {
            return string.Empty;
        }

        public void RecordIteration(ISchedulingStrategy strategy, bool bugFound)
        {
            this.BugFound = bugFound;
            this.ProgramSummary = (strategy as AbstractBaseProgramModelStrategy).GetProgramSummary();
        }
    }
}
