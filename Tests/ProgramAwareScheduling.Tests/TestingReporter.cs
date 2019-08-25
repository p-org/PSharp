// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.PSharp.TestingClientInterface.SimpleImplementation;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.ProgramAwareScheduling.ProgramModel;
using Microsoft.PSharp.TestingServices.Runtime.Scheduling.Strategies.ProgramAware;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace Microsoft.PSharp.ProgramAwareScheduling.Tests
{
    public class TestingReporter : IMetricReporter
    {
        public readonly ISchedulingStrategy Strategy;

        public List<ProgramModelSummary> ProgramSummaries { get; private set; }

        public ProgramModelSummary ProgramSummary
            => this.ProgramSummaries.Count > 0 ?
                    this.ProgramSummaries[this.ProgramSummaries.Count - 1] :
                    null;

        public TestingReporter(ISchedulingStrategy strategy)
        {
            this.Strategy = strategy;
            this.ProgramSummaries = new List<ProgramModelSummary>();
        }

        public string GetReport()
        {
            return string.Empty;
        }

        public void RecordIteration(ISchedulingStrategy strategy, bool bugFound)
        {
            this.ProgramSummaries.Add((strategy as AbstractBaseProgramModelStrategy).GetProgramSummary());
        }
    }
}
