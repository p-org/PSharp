//-----------------------------------------------------------------------
// <copyright file="TestReport.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Class implementing the P# test report.
    /// </summary>
    [DataContract]
    public class TestReport
    {
        #region properties

        /// <summary>
        /// Name of the program being tested.
        /// </summary>
        [DataMember]
        public string ProgramName { get; internal set; }

        /// <summary>
        /// Information regarding code coverage.
        /// </summary>
        [DataMember]
        public CoverageInfo CoverageInfo { get; private set; }

        /// <summary>
        /// Number of explored fair schedules.
        /// </summary>
        [DataMember]
        public int NumOfExploredFairSchedules { get; internal set; }

        /// <summary>
        /// Number of explored unfair schedules.
        /// </summary>
        [DataMember]
        public int NumOfExploredUnfairSchedules { get; internal set; }

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        [DataMember]
        public int NumOfFoundBugs { get; internal set; }

        /// <summary>
        /// List of unique bug reports.
        /// </summary>
        [DataMember]
        public HashSet<string> BugReports { get; internal set; }

        /// <summary>
        /// The min explored scheduling steps in average,
        /// in fair tests.
        /// </summary>
        [DataMember]
        public int MinExploredFairSteps { get; internal set; }

        /// <summary>
        /// The max explored scheduling steps in average,
        /// in fair tests.
        /// </summary>
        [DataMember]
        public int MaxExploredFairSteps { get; internal set; }

        /// <summary>
        /// The total explored scheduling steps (across
        /// all testing iterations), in fair tests.
        /// </summary>
        [DataMember]
        public int TotalExploredFairSteps { get; internal set; }

        /// <summary>
        /// Number of times the fair max steps bound was hit,
        /// in fair tests.
        /// </summary>
        [DataMember]
        public int MaxFairStepsHitInFairTests { get; internal set; }

        /// <summary>
        /// Number of times the unfair max steps bound was hit,
        /// in fair tests.
        /// </summary>
        [DataMember]
        public int MaxUnfairStepsHitInFairTests { get; internal set; }

        /// <summary>
        /// Number of times the unfair max steps bound was hit,
        /// in unfair tests.
        /// </summary>
        [DataMember]
        public int MaxUnfairStepsHitInUnfairTests { get; internal set; }

        /// <summary>
        /// Lock for the test report.
        /// </summary>
        private object Lock;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="programName">Name of the program being tested</param>
        public TestReport(string programName)
        {
            this.ProgramName = programName;

            this.CoverageInfo = new CoverageInfo();

            this.NumOfExploredFairSchedules = 0;
            this.NumOfExploredUnfairSchedules = 0;
            this.NumOfFoundBugs = 0;
            this.BugReports = new HashSet<string>();

            this.MinExploredFairSteps = -1;
            this.MaxExploredFairSteps = -1;
            this.TotalExploredFairSteps = 0;
            this.MaxFairStepsHitInFairTests = 0;
            this.MaxUnfairStepsHitInFairTests = 0;
            this.MaxUnfairStepsHitInUnfairTests = 0;

            this.Lock = new object();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Merges the information from the specified
        /// test report.
        /// </summary>
        /// <param name="testReport">TestReport</param>
        /// <returns>True if merged successfully</returns>
        public bool Merge(TestReport testReport)
        {
            if (!this.ProgramName.Equals(testReport.ProgramName))
            {
                // Only merge test reports that have the same program name.
                return false;
            }

            lock (this.Lock)
            {
                this.CoverageInfo.Merge(testReport.CoverageInfo);

                this.NumOfFoundBugs += testReport.NumOfFoundBugs;

                this.BugReports.UnionWith(testReport.BugReports);

                this.NumOfExploredFairSchedules += testReport.NumOfExploredFairSchedules;
                this.NumOfExploredUnfairSchedules += testReport.NumOfExploredUnfairSchedules;

                if (testReport.MinExploredFairSteps >= 0 &&
                    (this.MinExploredFairSteps < 0 ||
                    this.MinExploredFairSteps > testReport.MinExploredFairSteps))
                {
                    this.MinExploredFairSteps = testReport.MinExploredFairSteps;
                }

                if (this.MaxExploredFairSteps < testReport.MaxExploredFairSteps)
                {
                    this.MaxExploredFairSteps = testReport.MaxExploredFairSteps;
                }

                this.TotalExploredFairSteps += testReport.TotalExploredFairSteps;

                this.MaxFairStepsHitInFairTests += testReport.MaxFairStepsHitInFairTests;
                this.MaxUnfairStepsHitInFairTests += testReport.MaxUnfairStepsHitInFairTests;
                this.MaxUnfairStepsHitInUnfairTests += testReport.MaxUnfairStepsHitInUnfairTests;
            }

            return true;
        }

        /// <summary>
        /// Returns the testing report as a string, given a configuration and an optional prefix.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="prefix">Prefix</param>
        /// <returns>Textrt</returns>
        public string GetText(Configuration configuration, string prefix = "")
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("{0} Testing statistics:", prefix);

            report.AppendLine();
            report.AppendFormat("{0} Found {1} bug{2}.",
                prefix.Equals("...") ? "....." : prefix, this.NumOfFoundBugs,
                this.NumOfFoundBugs == 1 ? "" : "s");

            report.AppendLine();
            report.AppendFormat("{0} Scheduling statistics:", prefix);

            int totalExploredSchedules = this.NumOfExploredFairSchedules +
                this.NumOfExploredUnfairSchedules;

            report.AppendLine();
            report.AppendFormat("{0} Explored {1} schedule{2}: {3} fair and {4} unfair.",
                prefix.Equals("...") ? "....." : prefix,
                totalExploredSchedules, totalExploredSchedules == 1 ? "" : "s",
                this.NumOfExploredFairSchedules,
                this.NumOfExploredUnfairSchedules);

            if (totalExploredSchedules > 0 &&
                this.NumOfFoundBugs > 0)
            {
                report.AppendLine();
                report.AppendFormat("{0} Found {1:F2}% buggy schedules.",
                    prefix.Equals("...") ? "....." : prefix,
                    ((double)this.NumOfFoundBugs / totalExploredSchedules) * 100);
            }

            if (this.NumOfExploredFairSchedules > 0)
            {
                if (this.TotalExploredFairSteps > 0)
                {
                    int averageExploredFairSteps = this.TotalExploredFairSteps /
                        this.NumOfExploredFairSchedules;

                    report.AppendLine();
                    report.AppendFormat("{0} Number of scheduling points in fair terminating schedules: " +
                        "{1} (min), {2} (avg), {3} (max).",
                        prefix.Equals("...") ? "....." : prefix,
                        this.MinExploredFairSteps < 0 ? 0 : this.MinExploredFairSteps,
                        averageExploredFairSteps,
                        this.MaxExploredFairSteps < 0 ? 0 : this.MaxExploredFairSteps);
                }

                if (configuration.MaxUnfairSchedulingSteps > 0 &&
                    this.MaxUnfairStepsHitInFairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat("{0} Exceeded the max-steps bound of '{1}' in {2:F2}% of the fair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxUnfairSchedulingSteps,
                        ((double)this.MaxUnfairStepsHitInFairTests /
                        (double)this.NumOfExploredFairSchedules) * 100);
                }

                if (configuration.UserExplicitlySetMaxFairSchedulingSteps &&
                    configuration.MaxFairSchedulingSteps > 0 &&
                    this.MaxFairStepsHitInFairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat("{0} Hit the max-steps bound of '{1}' in {2:F2}% of the fair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxFairSchedulingSteps,
                        ((double)this.MaxFairStepsHitInFairTests /
                        (double)this.NumOfExploredFairSchedules) * 100);
                }
            }

            if (this.NumOfExploredUnfairSchedules > 0)
            {
                if (configuration.MaxUnfairSchedulingSteps > 0 &&
                    this.MaxUnfairStepsHitInUnfairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat("{0} Hit the max-steps bound of '{1}' in {2:F2}% of the unfair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxUnfairSchedulingSteps,
                        ((double)this.MaxUnfairStepsHitInUnfairTests /
                        (double)this.NumOfExploredUnfairSchedules) * 100);
                }
            }

            return report.ToString();
        }

        /// <summary>
        /// Clones the test report.
        /// </summary>
        /// <returns>TestReport</returns>
        internal TestReport Clone()
        {
            var serializer = new DataContractSerializer(typeof(TestReport), null, int.MaxValue, false, true, null);
            using (var ms = new System.IO.MemoryStream())
            {
                lock (this.Lock)
                {
                    serializer.WriteObject(ms, this);
                    ms.Position = 0;
                    return (TestReport)serializer.ReadObject(ms);
                }
            }
        }

        #endregion
    }
}