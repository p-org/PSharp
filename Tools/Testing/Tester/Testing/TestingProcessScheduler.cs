//-----------------------------------------------------------------------
// <copyright file="TestingProcessScheduler.cs">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;

using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# testing process scheduler.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal sealed class TestingProcessScheduler : ITestingProcessScheduler
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The notification listening service.
        /// </summary>
        private ServiceHost NotificationService;

        /// <summary>
        /// Map from testing process ids to testing processes.
        /// </summary>
        private Dictionary<int, Process> TestingProcesses;
        
        /// <summary>
        /// The test reports per process.
        /// </summary>
        private ConcurrentDictionary<int, TestReport> TestReports;

        /// <summary>
        /// Number of terminated testing processes.
        /// </summary>
        private int NumOfTerminatedTestingProcesses;

        /// <summary>
        /// The testing profiler.
        /// </summary>
        private Profiler Profiler;

        /// <summary>
        /// The scheduler lock.
        /// </summary>
        private object SchedulerLock;

        /// <summary>
        /// The process id of the process that
        /// discovered a bug, else -1.
        /// </summary>
        private int BugFoundByProcess;

        #endregion

        #region remote testing process methods

        /// <summary>
        /// Notifies the testing process scheduler
        /// that a bug was found.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>Boolean value</returns>
        void ITestingProcessScheduler.NotifyBugFound(int processId)
        {
            lock (this.SchedulerLock)
            {
                if (this.BugFoundByProcess < 0)
                {
                    IO.PrintLine($"... Testing task '{processId}' " +
                        "found a bug.");

                    this.BugFoundByProcess = processId;
                    foreach (var testingProcess in this.TestingProcesses)
                    {
                        if (testingProcess.Key != processId)
                        {
                            if (this.Configuration.ReportCodeCoverage)
                            {
                                var testReport = this.GetTestReport(testingProcess.Key);
                                this.TestReports.TryAdd(testingProcess.Key, testReport);
                            }

                            testingProcess.Value.Kill();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the test data from the specified process.
        /// </summary>
        /// <param name="testReport">TestReport</param>
        /// <param name="processId">Unique process id</param>
        void ITestingProcessScheduler.SetTestData(TestReport testReport, int processId)
        {
            this.TestReports.TryAdd(processId, testReport);
        }

        /// <summary>
        /// Gets the global test data for the specified process.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>List of CoverageInfo</returns>
        IList<TestReport> ITestingProcessScheduler.GetGlobalTestData(int processId)
        {
            var globalTestReport = new List<TestReport>();
            globalTestReport.AddRange(this.TestReports.Where(
                val => val.Key != processId).Select(val => val.Value));
            return globalTestReport;
        }

        /// <summary>
        /// Checks if the specified process should emit the test report.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>Boolean value</returns>
        bool ITestingProcessScheduler.ShouldEmitTestReport(int processId)
        {
            lock (this.SchedulerLock)
            {
                this.NumOfTerminatedTestingProcesses++;

                if (this.BugFoundByProcess == processId)
                {
                    return true;
                }

                if (this.NumOfTerminatedTestingProcesses == this.TestingProcesses.Count)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Creates a new P# testing process scheduler.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>TestingProcessScheduler</returns>
        internal static TestingProcessScheduler Create(Configuration configuration)
        {
            return new TestingProcessScheduler(configuration);
        }

        /// <summary>
        /// Runs the P# testing scheduler.
        /// </summary>
        internal void Run()
        {
            // Opens the remote notification listener.
            // Requires administrator access.
            this.OpenNotificationListener();

            // Creates the user specified number of testing processes.
            for (int testId = 0; testId < this.Configuration.ParallelBugFindingTasks; testId++)
            {
                this.TestingProcesses.Add(testId, TestingProcessFactory.Create(testId, this.Configuration));
            }

            IO.PrintLine($"... Created '{this.Configuration.ParallelBugFindingTasks}' " +
                "parallel testing tasks.");

            this.Profiler.StartMeasuringExecutionTime();

            // Starts the testing processes.
            for (int testId = 0; testId < this.Configuration.ParallelBugFindingTasks; testId++)
            {
                this.TestingProcesses[testId].Start();
            }

            // Waits the testing processes to exit.
            for (int testId = 0; testId < this.Configuration.ParallelBugFindingTasks; testId++)
            {
                this.TestingProcesses[testId].WaitForExit();
            }

            this.Profiler.StopMeasuringExecutionTime();

            IO.PrintLine($"... Parallel testing elapsed {this.Profiler.Results()} sec.");
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private TestingProcessScheduler(Configuration configuration)
        {
            this.TestingProcesses = new Dictionary<int, Process>();
            this.TestReports = new ConcurrentDictionary<int, TestReport>();
            this.NumOfTerminatedTestingProcesses = 0;
            this.Profiler = new Profiler();
            this.SchedulerLock = new object();
            this.BugFoundByProcess = -1;

            configuration.Verbose = 1;
            configuration.PrintTrace = false;
            configuration.PerformFullExploration = false;
            configuration.EnableDataRaceDetection = false;

            this.Configuration = configuration;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Opens the remote notification listener.
        /// </summary>
        private void OpenNotificationListener()
        {
            Uri address = new Uri("http://localhost:8080/psharp/testing/scheduler/");

            WSHttpBinding binding = new WSHttpBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            this.NotificationService = new ServiceHost(this);
            this.NotificationService.AddServiceEndpoint(typeof(ITestingProcessScheduler), binding, address);

            if (this.Configuration.EnableDebugging)
            {
                ServiceDebugBehavior debug = this.NotificationService.Description.
                    Behaviors.Find<ServiceDebugBehavior>();
                debug.IncludeExceptionDetailInFaults = true;
            }
            
            try
            {
                this.NotificationService.Open();
            }
            catch (AddressAccessDeniedException)
            {
                IO.Error.ReportAndExit("Your process does not have access " +
                    "rights to open the remote testing notification listener. " +
                    "Please run the process as administrator.");
            }
        }

        /// <summary>
        /// Gets the test report from the specified testing process.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>TestReport</returns>
        private TestReport GetTestReport(int processId)
        {
            Uri address = new Uri("http://localhost:8080/psharp/testing/process/" + processId + "/");

            WSHttpBinding binding = new WSHttpBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            EndpointAddress endpoint = new EndpointAddress(address);

            var testingProcess = ChannelFactory<ITestingProcess>.
                    CreateChannel(binding, endpoint);

            return testingProcess.GetTestReport();
        }

        #endregion
    }
}
