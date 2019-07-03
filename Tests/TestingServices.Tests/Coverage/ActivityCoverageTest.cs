// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.IO;
using Microsoft.PSharp.TestingServices.Coverage;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class ActivityCoverageTest : BaseTest
    {
        public ActivityCoverageTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Setup : Event
        {
            public readonly MachineId Id;

            public Setup(MachineId id)
            {
                this.Id = id;
            }
        }

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Goto<Done>();
            }

            private class Done : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateTransitionActivityCoverage()
        {
            var configuration = Configuration.Create().WithVerbosityEnabled();
            configuration.ReportActivityCoverage = true;

            ITestingEngine testingEngine = this.Test(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 100.0%
Machine: M1
***************
Machine event coverage: 100.0%
        State: Init
                State event coverage: 100.0%
                Next States: Done
        State: Done
                State event coverage: 100.0%
                Previous States: Init
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Done))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E());
            }

            private class Done : MachineState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineRaiseEventActivityCoverage()
        {
            var configuration = Configuration.Create().WithVerbosityEnabled();
            configuration.ReportActivityCoverage = true;

            ITestingEngine testingEngine = this.Test(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 100.0%
Machine: M2
***************
Machine event coverage: 100.0%
        State: Init
                State event coverage: 100.0%
                Next States: Done
        State: Done
                State event coverage: 100.0%
                Previous States: Init
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }

        private class M3A : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Done))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.CreateMachine(typeof(M3B), new Setup(this.Id));
            }

            private class Done : MachineState
            {
            }
        }

        private class M3B : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var id = (this.ReceivedEvent as Setup).Id;
                this.Send(id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineSendEventActivityCoverage()
        {
            var configuration = Configuration.Create().WithVerbosityEnabled();
            configuration.ReportActivityCoverage = true;

            ITestingEngine testingEngine = this.Test(r =>
            {
                r.CreateMachine(typeof(M3A));
            },
            configuration);

            string result;
            var activityCoverageReporter = new ActivityCoverageReporter(testingEngine.TestReport.CoverageInfo);
            using (var writer = new StringWriter())
            {
                activityCoverageReporter.WriteCoverageText(writer);
                result = RemoveNamespaceReferencesFromReport(writer.ToString());
                result = RemoveExcessiveEmptySpaceFromReport(result);
            }

            var expected = @"Total event coverage: 100.0%
Machine: M3A
***************
Machine event coverage: 100.0%
        State: Init
                State event coverage: 100.0%
                Events Received: E
                Next States: Done
        State: Done
                State event coverage: 100.0%
                Previous States: Init

Machine: M3B
***************
Machine event coverage: 100.0%
        State: Init
                State event coverage: 100.0%
                Events Sent: E
";

            expected = RemoveExcessiveEmptySpaceFromReport(expected);
            Assert.Equal(expected, result);
        }
    }
}
