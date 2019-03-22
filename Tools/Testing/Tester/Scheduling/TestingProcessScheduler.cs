﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
#if NET46
using System.ServiceModel;
using System.ServiceModel.Description;
#endif

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# testing process scheduler.
    /// </summary>
#if NET46
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
#endif
    internal sealed class TestingProcessScheduler
#if NET46
        : ITestingProcessScheduler
#endif
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

#if NET46
        /// <summary>
        /// The notification listening service.
        /// </summary>
        private ServiceHost NotificationService;

        /// <summary>
        /// Map from testing process ids to testing processes.
        /// </summary>
        private Dictionary<uint, Process> TestingProcesses;
#endif

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

#if NET46
        /// <summary>
        /// The process id of the process that
        /// discovered a bug, else null.
        /// </summary>
        private uint? BugFoundByProcess;
#endif

        /// <summary>
        /// Set if ctrl-c or ctrl-break occurred.
        /// </summary>
        internal static bool ProcessCanceled;

#if NET46
        /// <summary>
        /// Set true if we have multiple parallel processes or are running code coverage.
        /// </summary>
        private bool runOutOfProcess;
#endif

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private TestingProcessScheduler(Configuration configuration)
        {
#if NET46
            this.TestingProcesses = new Dictionary<uint, Process>();
#endif
            this.TestingProcessChannels = new Dictionary<uint, ITestingProcess>();
            this.TestReports = new ConcurrentDictionary<uint, TestReport>();
            this.GlobalTestReport = new TestReport(configuration);
            this.Profiler = new Profiler();
            this.SchedulerLock = new object();
#if NET46
            this.BugFoundByProcess = null;

            // Code coverage should be run out-of-process; otherwise VSPerfMon won't shutdown correctly
            // because an instrumented process (this one) is still running.
            this.runOutOfProcess = configuration.ParallelBugFindingTasks > 1 || configuration.ReportCodeCoverage;

            if (configuration.ParallelBugFindingTasks > 1)
            {
                configuration.Verbose = 1;
                configuration.EnableDataRaceDetection = false;
            }
#endif

            configuration.EnableColoredConsoleOutput = true;

            this.Configuration = configuration;
        }

#if NET46
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
#endif

        /// <summary>
        /// Creates a new P# testing process scheduler.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>The testing process scheduler.</returns>
        internal static TestingProcessScheduler Create(Configuration configuration)
        {
            return new TestingProcessScheduler(configuration);
        }

        /// <summary>
        /// Runs the P# testing scheduler.
        /// </summary>
        internal void Run()
        {
#if NET46
            // Opens the remote notification listener.
            this.OpenNotificationListener();
#endif

            this.Profiler.StartMeasuringExecutionTime();

#if NET46
            if (runOutOfProcess)
            {
                this.CreateParallelTestingProcesses();
                this.RunParallelTestingProcesses();
            }
            else
            {
                this.CreateAndRunInMemoryTestingProcess();
            }
#else
            this.CreateAndRunInMemoryTestingProcess();
#endif

            this.Profiler.StopMeasuringExecutionTime();

#if NET46
            // Closes the remote notification listener.
            this.CloseNotificationListener();
#endif

            if (!ProcessCanceled)
            {
                // Merges and emits the test report.
                this.EmitTestReport();
            }
        }

#if NET46
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
#endif

        /// <summary>
        /// Creates and runs an in-memory testing process.
        /// </summary>
        private void CreateAndRunInMemoryTestingProcess()
        {
            TestingProcess testingProcess = TestingProcess.Create(this.Configuration);
            this.TestingProcessChannels.Add(0, testingProcess);

            Output.WriteLine($"... Created '1' testing task.");

            // Runs the testing process.
            testingProcess.Run();

            // Get and merge the test report.
            TestReport testReport = this.GetTestReport(0);
            if (testReport != null)
            {
                this.MergeTestReport(testReport, 0);
            }
        }

#if NET46
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
                this.TestingProcessChannels[processId].Stop();
            }
            catch (CommunicationException ex)
            {
                IO.Debug.WriteLine("... Unable to communicate with testing task " +
                    $"'{processId}'. Task has already terminated.");
                IO.Debug.WriteLine(ex.ToString());
            }
        }
#endif

        /// <summary>
        /// Gets the test report from the specified testing process.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>TestReport</returns>
        private TestReport GetTestReport(uint processId)
        {
            TestReport testReport = null;

#if NET46
            try
            {
#endif
                testReport = this.TestingProcessChannels[processId].GetTestReport();
#if NET46
            }
            catch (CommunicationException ex)
            {
                IO.Debug.WriteLine("... Unable to communicate with testing task " +
                    $"'{processId}'. Task has already terminated.");
                IO.Debug.WriteLine(ex.ToString());
            }
#endif

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
#if NET46
            foreach (var process in this.TestingProcesses)
            {
                if (!this.TestReports.ContainsKey(process.Key))
                {
                    Output.WriteLine($"... Task {process.Key} failed due to an internal error.");
                }
            }
#endif

            if (this.TestReports.Count == 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
                return;
            }

#if NET46
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
#endif

            Output.WriteLine(this.GlobalTestReport.GetText(this.Configuration, "..."));
            Output.WriteLine($"... Elapsed {this.Profiler.Results()} sec.");

            if (this.GlobalTestReport.InternalErrors.Count > 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
            }
            else if (this.GlobalTestReport.NumOfFoundBugs > 0)
            {
                Environment.ExitCode = (int)ExitCode.BugFound;
            }
            else
            {
                Environment.ExitCode = (int)ExitCode.Success;
            }
        }
    }
}
