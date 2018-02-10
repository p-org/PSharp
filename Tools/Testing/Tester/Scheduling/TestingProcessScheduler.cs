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
using System.ServiceModel;
using System.ServiceModel.Description;

using Microsoft.PSharp.IO;
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
        private Dictionary<uint, Process> TestingProcesses;

        /// <summary>
        /// Map from testing process ids to testing process channels.
        /// </summary>
        private Dictionary<uint, ITestingProcess> TestingProcessChannels;

        /// <summary>
        /// The test reports per process.
        /// </summary>
        private ConcurrentDictionary<uint, TestReport> TestReports;

        /// <summary>
        /// The global test report, which contains merged information
        /// from the test report of each testing process.
        /// </summary>
        private TestReport GlobalTestReport;

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
        /// discovered a bug, else null.
        /// </summary>
        private uint? BugFoundByProcess;

        /// <summary>
        /// Set if ctrl-c or ctrl-break occurred.
        /// </summary>
        internal static bool IsProcessCanceled { get; private set; }

        /// <summary>
        /// Set true if we have multiple parallel processes or are running code coverage.
        /// </summary>
        private bool runOutOfProcess;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private TestingProcessScheduler(Configuration configuration)
        {
            this.TestingProcesses = new Dictionary<uint, Process>();
            this.TestingProcessChannels = new Dictionary<uint, ITestingProcess>();
            this.TestReports = new ConcurrentDictionary<uint, TestReport>();
            this.GlobalTestReport = new TestReport(configuration);
            this.Profiler = new Profiler();
            this.SchedulerLock = new object();
            this.BugFoundByProcess = null;

            // Code coverage should be run out-of-process; otherwise VSPerfMon won't shutdown correctly
            // because an instrumented process (this one) is still running.
            this.runOutOfProcess = configuration.ParallelBugFindingTasks > 1 || configuration.ReportCodeCoverage;

            if (configuration.ParallelBugFindingTasks > 1)
            {
                configuration.Verbose = 1;
                configuration.EnableDataRaceDetection = false;
            }

            configuration.EnableColoredConsoleOutput = true;

            this.Configuration = configuration;
        }

        #endregion

        #region remote testing process methods

        /// <summary>
        /// Notifies the testing process scheduler
        /// that a bug was found.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>Boolean value</returns>
        void ITestingProcessScheduler.NotifyBugFound(uint processId)
        {
            lock (this.SchedulerLock)
            {
                if (!this.Configuration.PerformFullExploration && this.BugFoundByProcess == null)
                {
                    Output.WriteLine($"... Task {processId} found a bug.");
                    this.BugFoundByProcess = processId;

                    foreach (var testingProcess in this.TestingProcesses)
                    {
                        if (testingProcess.Key != processId)
                        {
                            this.StopTestingProcess(testingProcess.Key);

                            TestReport testReport = this.GetTestReport(testingProcess.Key);
                            if (testReport != null)
                            {
                                this.MergeTestReport(testReport, testingProcess.Key);
                            }

                            try
                            {
                                this.TestingProcesses[testingProcess.Key].Kill();
                                this.TestingProcesses[testingProcess.Key].Dispose();
                            }
                            catch (InvalidOperationException)
                            {
                                IO.Debug.WriteLine("... Unable to terminate testing task " +
                                    $"'{testingProcess.Key}'. Task has already terminated.");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the test report from the specified process.
        /// </summary>
        /// <param name="testReport">TestReport</param>
        /// <param name="processId">Unique process id</param>
        void ITestingProcessScheduler.SetTestReport(TestReport testReport, uint processId)
        {
            lock (this.SchedulerLock)
            {
                this.MergeTestReport(testReport, processId);
            }
        }

		/// <summary>
		/// Halts currently executing testing processes.
		/// </summary>
		public void Stop()
		{
			// propagate the request for cancellation using the ProcessCanceled flag.
			IsProcessCanceled = true;

			// TestingProcesses is only populated with parallel tests
			if (this.TestingProcesses.Count > 0)
			{
				foreach(var testingProcess in this.TestingProcesses)
				{
					this.StopTestingProcess(testingProcess.Key);
				}
			}
			else  // there is a single testing task
			{
				this.StopTestingProcess(0);
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
            this.OpenNotificationListener();

            this.Profiler.StartMeasuringExecutionTime();

            if (runOutOfProcess)
            {
                this.CreateParallelTestingProcesses();
                this.RunParallelTestingProcesses();
            }
            else
            {
                this.CreateAndRunInMemoryTestingProcess();
            }

            this.Profiler.StopMeasuringExecutionTime();

            // Closes the remote notification listener.
            this.CloseNotificationListener();

            // Merges and emits the test report.
            if (!IsProcessCanceled)
            {
                this.EmitTestReport();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Creates the user specified number of parallel testing processes.
        /// </summary>
        private void CreateParallelTestingProcesses()
        {
            for (uint testId = 0; testId < this.Configuration.ParallelBugFindingTasks; testId++)
            {
                this.TestingProcesses.Add(testId, TestingProcessFactory.Create(testId, this.Configuration));
                this.TestingProcessChannels.Add(testId, this.CreateTestingProcessChannel(testId));
            }

            Output.WriteLine($"... Created '{this.Configuration.ParallelBugFindingTasks}' " +
                "testing tasks.");
        }

        /// <summary>
        /// Runs the parallel testing processes.
        /// </summary>
        private void RunParallelTestingProcesses()
        {
            // Starts the testing processes.
            for (uint testId = 0; testId < this.Configuration.ParallelBugFindingTasks; testId++)
            {
                this.TestingProcesses[testId].Start();
            }

            // Waits the testing processes to exit.
            for (uint testId = 0; testId < this.Configuration.ParallelBugFindingTasks; testId++)
            {
                try
                {
                    this.TestingProcesses[testId].WaitForExit();
                }
                catch (InvalidOperationException)
                {
                    IO.Debug.WriteLine($"... Unable to wait for testing task '{testId}' to " +
                        "terminate. Task has already terminated.");
                }
            }
        }

        /// <summary>
        /// Creates and runs an in-memory testing process.
        /// </summary>
        private void CreateAndRunInMemoryTestingProcess()
        {
			TestingProcess testingProcess = TestingProcess.Create(this.Configuration);
			this.TestingProcessChannels.Add(0, testingProcess);

			Output.WriteLine($"... Created '1' testing task.");

			// Starts the testing process.
			testingProcess.Start();

			// Get and merge the test report.
			TestReport testReport = this.GetTestReport(0);
            if (testReport != null)
            {
                this.MergeTestReport(testReport, 0);
            }
        }

        /// <summary>
        /// Opens the remote notification listener. If there are
        /// less than two parallel testing processes, then this
        /// operation does nothing.
        /// </summary>
        private void OpenNotificationListener()
        {
            if (!this.runOutOfProcess)
            {
                return;
            }

            Uri address = new Uri("net.pipe://localhost/psharp/testing/scheduler/" +
                $"{this.Configuration.TestingSchedulerEndPoint}");

            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            this.NotificationService = new ServiceHost(this);
            this.NotificationService.AddServiceEndpoint(typeof(ITestingProcessScheduler), binding, address);

            ServiceDebugBehavior debug = this.NotificationService.Description.Behaviors.Find<ServiceDebugBehavior>();
            debug.IncludeExceptionDetailInFaults = true;

            try
            {
                this.NotificationService.Open();
            }
            catch (AddressAccessDeniedException)
            {
                Error.ReportAndExit("Your process does not have access " +
                    "rights to open the remote testing notification listener. " +
                    "Please run the process as administrator.");
            }
        }

        /// <summary>
        /// Closes the remote notification listener. If there are
        /// less than two parallel testing processes, then this
        /// operation does nothing.
        /// </summary>
        private void CloseNotificationListener()
        {
            if (this.runOutOfProcess && this.NotificationService.State == CommunicationState.Opened)
            {
                try
                {
                    this.NotificationService.Close();
                }
                catch (CommunicationException)
                {
                    this.NotificationService.Abort();
                    throw;
                }
                catch (TimeoutException)
                {
                    this.NotificationService.Abort();
                    throw;
                }
                catch (Exception)
                {
                    this.NotificationService.Abort();
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates and returns a new testing process communication channel.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>ITestingProcess</returns>
        private ITestingProcess CreateTestingProcessChannel(uint processId)
        {
            Uri address = new Uri("net.pipe://localhost/psharp/testing/process/" +
                $"{processId}/{this.Configuration.TestingSchedulerEndPoint}");

            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            EndpointAddress endpoint = new EndpointAddress(address);

            return ChannelFactory<ITestingProcess>.CreateChannel(binding, endpoint);
        }

        /// <summary>
        /// Stops the testing process.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        private void StopTestingProcess(uint processId)
        {
            try
            {
                // The key may not be in the dictionary yet if we cancel quickly.
                if (this.TestingProcessChannels.TryGetValue(processId, out var testingProcess))
                {
                    testingProcess.Stop();
                }
            }
            catch (CommunicationException ex)
            {
                IO.Debug.WriteLine("... Unable to communicate with testing task " +
                    $"'{processId}'. Task has already terminated.");
                IO.Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Gets the test report from the specified testing process.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>TestReport</returns>
        private TestReport GetTestReport(uint processId)
        {
            TestReport testReport = null;

            try
            {
                testReport = this.TestingProcessChannels[processId].GetTestReport();
            }
            catch (CommunicationException ex)
            {
                IO.Debug.WriteLine("... Unable to communicate with testing task " +
                    $"'{processId}'. Task has already terminated.");
                IO.Debug.WriteLine(ex.ToString());
            }

            return testReport;
        }

        /// <summary>
        /// Merges the test report from the specified process.
        /// </summary>
        /// <param name="testReport">TestReport</param>
        /// <param name="processId">Unique process id</param>
        private void MergeTestReport(TestReport testReport, uint processId)
        {
            if (this.TestReports.TryAdd(processId, testReport))
            {
                // Merges the test report into the global report.
                IO.Debug.WriteLine($"... Merging task {processId} test report.");
                this.GlobalTestReport.Merge(testReport);
            }
            else
            {
                IO.Debug.WriteLine($"... Unable to merge test report from task '{processId}'. " +
                    " Report is already merged.");
            }
        }

        /// <summary>
        /// Emits the test report.
        /// </summary>
        private void EmitTestReport()
        {
            var testReports = new List<TestReport>(this.TestReports.Values);
            foreach (var process in this.TestingProcesses)
            {
                if (!this.TestReports.ContainsKey(process.Key))
                {
                    Output.WriteLine($"... Task {process.Key} failed due to an internal error.");
                }
            }

            if (this.TestReports.Count == 0)
            {
                return;
            }

            if (this.Configuration.ReportActivityCoverage)
            {
                Output.WriteLine($"... Emitting coverage reports:");
                Reporter.EmitTestingCoverageReport(this.GlobalTestReport);
            }

            if (this.Configuration.DebugActivityCoverage)
            {
                Output.WriteLine($"... Emitting debug coverage reports:");
                foreach (var report in this.TestReports)
                {
                    Reporter.EmitTestingCoverageReport(report.Value, report.Key, isDebug: true);
                }
            }

            Output.WriteLine(this.GlobalTestReport.GetText(this.Configuration, "..."));
            Output.WriteLine($"... Elapsed {this.Profiler.Results()} sec.");
        }

        #endregion
    }
}
