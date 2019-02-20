﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if NET46 || NET45
using System.ServiceModel;
using System.ServiceModel.Description;
#endif
using System.Timers;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// A P# testing process.
    /// </summary>
#if NET46 || NET45
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
#endif
    internal sealed class TestingProcess : ITestingProcess
    {
#if NET46 || NET45
        /// <summary>
        /// The notification listening service.
        /// </summary>
        private ServiceHost NotificationService;
#endif

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The testing engine associated with
        /// this testing process.
        /// </summary>
        private ITestingEngine TestingEngine;

#if NET46 || NET45
        /// <summary>
        /// The remote testing scheduler.
        /// </summary>
        private ITestingProcessScheduler TestingScheduler;
#endif

        /// <summary>
        /// Returns the test report.
        /// </summary>
        /// <returns>TestReport</returns>
        TestReport ITestingProcess.GetTestReport()
        {
            return this.TestingEngine.TestReport.Clone();
        }

        /// <summary>
        /// Stops testing.
        /// </summary>
        void ITestingProcess.Stop()
        {
            this.TestingEngine.Stop();
        }

        /// <summary>
        /// Creates a P# testing process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>TestingProcess</returns>
        internal static TestingProcess Create(Configuration configuration)
        {
            return new TestingProcess(configuration);
        }

        /// <summary>
        /// Runs the P# testing process.
        /// </summary>
        internal void Run()
        {
#if NET46 || NET45
            // Opens the remote notification listener.
            this.OpenNotificationListener();

            Timer timer = null;
            if (this.Configuration.RunAsParallelBugFindingTask)
            {
                timer = this.CreateParentStatusMonitorTimer();
                timer.Start();
            }
#endif

            this.TestingEngine.Run();

#if NET46 || NET45
            if (this.Configuration.RunAsParallelBugFindingTask)
            {
                if (this.TestingEngine.TestReport.InternalErrors.Count > 0)
                {
                    Environment.ExitCode = (int)ExitCode.InternalError;
                }
                else if (this.TestingEngine.TestReport.NumOfFoundBugs > 0)
                {
                    Environment.ExitCode = (int)ExitCode.BugFound;
                    this.NotifyBugFound();
                }

                this.SendTestReport();
            }
#endif

            if (!this.Configuration.PerformFullExploration)
            {
                if (this.TestingEngine.TestReport.NumOfFoundBugs > 0 &&
                    !this.Configuration.RunAsParallelBugFindingTask)
                {
                    Output.WriteLine($"... Task {this.Configuration.TestingProcessId} found a bug.");
                }

                if (this.TestingEngine.TestReport.NumOfFoundBugs > 0)
                {
                    this.EmitTraces();
                }
            }

#if NET46 || NET45
            // Closes the remote notification listener.
            this.CloseNotificationListener();

            if (timer != null)
            {
                timer.Stop();
            }
#endif
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private TestingProcess(Configuration configuration)
        {
            if (configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                TestingPortfolio.ConfigureStrategyForCurrentProcess(configuration);
            }

            if (configuration.RandomSchedulingSeed != null)
            {
                configuration.RandomSchedulingSeed = (int)(configuration.RandomSchedulingSeed + (673 * configuration.TestingProcessId));
            }

            configuration.EnableColoredConsoleOutput = true;

            this.Configuration = configuration;
            this.TestingEngine = TestingEngineFactory.CreateBugFindingEngine(
                this.Configuration);
        }

#if NET46 || NET45
        /// <summary>
        /// Opens the remote notification listener. If this is
        /// not a parallel testing process, then this operation
        /// does nothing.
        /// </summary>
        private void OpenNotificationListener()
        {
            if (!this.Configuration.RunAsParallelBugFindingTask)
            {
                return;
            }

            Uri address = new Uri("net.pipe://localhost/psharp/testing/process/" +
                $"{this.Configuration.TestingProcessId}/" +
                $"{this.Configuration.TestingSchedulerEndPoint}");

            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            this.NotificationService = new ServiceHost(this);
            this.NotificationService.AddServiceEndpoint(typeof(ITestingProcess), binding, address);

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
        /// Closes the remote notification listener. If this is
        /// not a parallel testing process, then this operation
        /// does nothing.
        /// </summary>
        private void CloseNotificationListener()
        {
            if (this.Configuration.RunAsParallelBugFindingTask &&
                this.NotificationService.State == CommunicationState.Opened)
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
        /// Notifies the remote testing scheduler
        /// about a discovered bug.
        /// </summary>
        private void NotifyBugFound()
        {
            Uri address = new Uri("net.pipe://localhost/psharp/testing/scheduler/" +
                $"{this.Configuration.TestingSchedulerEndPoint}");

            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            EndpointAddress endpoint = new EndpointAddress(address);

            if (this.TestingScheduler == null)
            {
                this.TestingScheduler = ChannelFactory<ITestingProcessScheduler>.
                    CreateChannel(binding, endpoint);
            }

            this.TestingScheduler.NotifyBugFound(this.Configuration.TestingProcessId);
        }

        /// <summary>
        /// Sends the test report associated with this testing process.
        /// </summary>
        private void SendTestReport()
        {
            Uri address = new Uri("net.pipe://localhost/psharp/testing/scheduler/" +
                $"{this.Configuration.TestingSchedulerEndPoint}");

            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            EndpointAddress endpoint = new EndpointAddress(address);

            if (this.TestingScheduler == null)
            {
                this.TestingScheduler = ChannelFactory<ITestingProcessScheduler>.
                    CreateChannel(binding, endpoint);
            }

            this.TestingScheduler.SetTestReport(this.TestingEngine.TestReport.Clone(),
                this.Configuration.TestingProcessId);
        }
#endif

        /// <summary>
        /// Emits the testing traces.
        /// </summary>
        private void EmitTraces()
        {
            string file = Path.GetFileNameWithoutExtension(this.Configuration.AssemblyToBeAnalyzed);
            file += "_" + this.Configuration.TestingProcessId;

            // If this is a separate (sub-)process, CodeCoverageInstrumentation.OutputDirectory may not have been set up.
            CodeCoverageInstrumentation.SetOutputDirectory(this.Configuration, makeHistory: false);

            Output.WriteLine($"... Emitting task {this.Configuration.TestingProcessId} traces:");
            this.TestingEngine.TryEmitTraces(CodeCoverageInstrumentation.OutputDirectory, file);
        }

#if NET46 || NET45
        /// <summary>
        /// Creates a timer that monitors the status of the parent process.
        /// </summary>
        /// <returns>Timer</returns>
        private Timer CreateParentStatusMonitorTimer()
        {
            Timer timer = new Timer(5000);
            timer.Elapsed += CheckParentStatus;
            timer.AutoReset = true;
            return timer;
        }

        /// <summary>
        /// Checks the status of the parent process. If the parent
        /// process exits, then this process should also exit.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void CheckParentStatus(object sender, ElapsedEventArgs e)
        {
            Process parent = Process.GetProcesses().FirstOrDefault(val
                => val.Id == this.Configuration.TestingSchedulerProcessId);
            if (parent == null || !parent.ProcessName.Equals("PSharpTester"))
            {
                Environment.Exit(1);
            }
        }
#endif
    }
}
