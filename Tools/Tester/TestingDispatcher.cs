//-----------------------------------------------------------------------
// <copyright file="TestingDispatcher.cs">
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

using System;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# testing dispatcher.
    /// </summary>
    internal sealed class TestingDispatcher : MarshalByRefObject
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// App domains executing the testing processes.
        /// </summary>
        private AppDomain[] TestingProcessDomains;

        /// <summary>
        /// The parallel testing processes.
        /// </summary>
        private TestingProcess[] TestingProcesses;

        /// <summary>
        /// The testing profiler.
        /// </summary>
        private Profiler Profiler;

        /// <summary>
        /// Checks if a bug was discovered.
        /// </summary>
        private bool BugFound;
        
        #endregion

        #region API

        /// <summary>
        /// Creates a new P# testing dispatcher.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>TestingDispatcher</returns>
        public static TestingDispatcher Create(Configuration configuration)
        {
            return new TestingDispatcher(configuration);
        }

        /// <summary>
        /// Invokes the P# testing dispatcher.
        /// </summary>
        public void Invoke()
        {
            IO.PrintLine(". Testing " + this.Configuration.AssemblyToBeAnalyzed);
            
            this.TestingProcessDomains = new AppDomain[this.Configuration.ParallelBugFindingTasks];
            this.TestingProcesses = new TestingProcess[this.Configuration.ParallelBugFindingTasks];

            // Creates the testing process domains.
            for (int testId = 0; testId < this.Configuration.ParallelBugFindingTasks; testId++)
            {
                this.TestingProcessDomains[testId] = AppDomain.CreateDomain($"TestingProcess-{testId}");
                this.TestingProcesses[testId] = this.TestingProcessDomains[testId].
                    CreateInstanceAndUnwrap(
                    typeof(TestingProcess).Assembly.FullName,
                    typeof(TestingProcess).FullName) as TestingProcess;
            }

            if (this.Configuration.ParallelBugFindingTasks > 1)
            {
                IO.PrintLine($"... Running '{this.Configuration.ParallelBugFindingTasks}' " +
                    "testing tasks in parallel.");
            }

            this.Profiler.StartMeasuringExecutionTime();

            // Runs the testing processes in parallel.
            Parallel.For(0, this.Configuration.ParallelBugFindingTasks, testId =>
            {
                this.TestingProcesses[testId].HandleDetectedBug += new TestingProcess.NotificationHandler(
                    ReportResultsAndTerminateTesting);
                this.TestingProcesses[testId].Configure(testId, this.Configuration);
                this.TestingProcesses[testId].Start();
            });

            this.Profiler.StopMeasuringExecutionTime();

            if (!this.BugFound)
            {
                if (this.Configuration.ParallelBugFindingTasks == 1)
                {
                    this.TestingProcesses[0].Report();
                }
                else
                {
                    IO.PrintLine($"... Elapsed {this.Profiler.Results()} sec.");
                }
            }

            // Unloads the testing process domains.
            foreach (var testingProcessDomain in this.TestingProcessDomains)
            {
                AppDomain.Unload(testingProcessDomain);
            }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private TestingDispatcher(Configuration configuration)
        {
            this.Profiler = new Profiler();
            this.BugFound = false;

            this.Configuration = configuration;
            if (this.Configuration.ParallelBugFindingTasks > 1)
            {
                this.Configuration.Verbose = 1;
                this.Configuration.PrintTrace = false;
                this.Configuration.PerformFullExploration = false;
                this.Configuration.EnableDataRaceDetection = false;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Reports the bug-finding results and terminates
        /// the testing processes.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        private void ReportResultsAndTerminateTesting(int processId)
        {
            this.BugFound = true;

            foreach (var testingProcess in this.TestingProcesses)
            {
                if (testingProcess.Id == processId)
                {
                    testingProcess.TryEmitTraces();
                    testingProcess.Report();
                }
                else
                {
                    testingProcess.Stop();
                }
            }
        }

        #endregion
    }
}
