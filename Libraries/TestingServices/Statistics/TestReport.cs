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
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        #endregion

        #region properties

        /// <summary>
        /// Information regarding code coverage.
        /// </summary>
        [DataMember]
        public CoverageInfo CoverageInfo { get; private set; }

        /// <summary>
        /// Number of explored schedules.
        /// </summary>
        [DataMember]
        public int NumOfExploredSchedules { get; internal set; }

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        [DataMember]
        public int NumOfFoundBugs { get; internal set; }

        /// <summary>
        /// The latest bug report, if any.
        /// </summary>
        [DataMember]
        public string BugReport { get; internal set; }

        /// <summary>
        /// Explored scheduling steps in average.
        /// </summary>
        [DataMember]
        public int ExploredStepsInAverage { get; internal set; }

        /// <summary>
        /// Number of times the max steps bound was hit.
        /// </summary>
        [DataMember]
        public int MaxStepsHit { get; internal set; }

        /// <summary>
        /// The overall testing time.
        /// </summary>
        [DataMember]
        public double TestingTime { get; internal set; }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public TestReport(Configuration configuration)
        {
            this.Configuration = configuration;
            this.CoverageInfo = new CoverageInfo();
            
            this.NumOfFoundBugs = 0;
            this.BugReport = "";
            this.ExploredStepsInAverage = 0;
            this.MaxStepsHit = 0;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Merges the information from the specified
        /// test report. This is not thread-safe.
        /// </summary>
        /// <param name="testReport">TestReport</param>
        public void Merge(TestReport testReport)
        {
            this.CoverageInfo.Merge(testReport.CoverageInfo);
        }

        /// <summary>
        /// Returns a textual description of the report using the specified prefix.
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <returns>Report</returns>
        public string GetText(string prefix)
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("{0} Found {1} bug{2}.", prefix, this.NumOfFoundBugs,
                this.NumOfFoundBugs == 1 ? "" : "s");
            report.AppendLine();
            report.AppendFormat("{0} Explored {1} schedule{2}.", prefix,
                this.NumOfExploredSchedules,
                this.NumOfExploredSchedules == 1 ? "" : "s");
            report.AppendLine();

            if (this.NumOfExploredSchedules > 0)
            {
                report.AppendFormat("{0} Found {1}% buggy schedules.", prefix,
                    (this.NumOfFoundBugs * 100 / this.NumOfExploredSchedules));
                report.AppendLine();
                report.AppendFormat("{0} Instrumented {1} scheduling point{2} (on last iteration).",
                    prefix, this.ExploredStepsInAverage, this.ExploredStepsInAverage == 1 ? "" : "s");
                report.AppendLine();
            }

            if (this.Configuration.MaxSchedulingSteps > 0)
            {
                report.AppendFormat("{0} Hit max-steps bound of '{1}' in {2}% schedules.",
                    prefix, this.Configuration.MaxSchedulingSteps,
                    (this.MaxStepsHit * 100 / this.NumOfExploredSchedules));
                report.AppendLine();
            }

            report.Append($"{prefix} Elapsed {this.TestingTime} sec.");

            return report.ToString();
        }

        #endregion
    }
}