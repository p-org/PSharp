//-----------------------------------------------------------------------
// <copyright file="TestingProcess.cs">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Timers;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// A P# testing process.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal sealed class TestingProcess : ITestingProcess
    {
        #region fields

        /// <summary>
        /// The notification listening service.
        /// </summary>
        private ServiceHost NotificationService;

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The testing engine associated with
        /// this testing process.
        /// </summary>
        private ITestingEngine TestingEngine;

        /// <summary>
        /// The remote testing scheduler.
        /// </summary>
        private ITestingProcessScheduler TestingScheduler;

        #endregion

        #region remote testing process methods

        /// <summary>
        /// Returns the test report.
        /// </summary>
        /// <returns>TestReport</returns>
        TestReport ITestingProcess.GetTestReport()
        {
            if (this.Configuration.DebugCodeCoverage)
            {
                this.EmitCoverageReport(isDebug: true);
            }
            
            return this.TestingEngine.TestReport;
        }

        /// <summary>
        /// Stops testing.
        /// </summary>
        void ITestingProcess.Stop()
        {
            this.TestingEngine.Stop();
        }

        #endregion

        #region internal methods

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
        /// Starts the P# testing process.
        /// </summary>
        internal void Start()
        {
            Timer timer = null;
            if (this.Configuration.ParallelBugFindingTasks > 1)
            {
                this.OpenNotificationListener();
                timer = this.CreateParentStatusMonitorTimer();
                timer.Start();
            }
            
            this.TestingEngine.Run();
            
            if (this.Configuration.ParallelBugFindingTasks > 1)
            {
                if (this.TestingEngine.TestReport.NumOfFoundBugs > 0)
                {
                    this.NotifyBugFound();
                }
                
                this.SendTestReport();
                if (this.TestingScheduler.ShouldEmitTestReport(this.Configuration.TestingProcessId))
                {
                    this.EmitTestReport();
                }
                else if (this.Configuration.DebugCodeCoverage)
                {
                    this.EmitCoverageReport(isDebug: true);
                }

                this.CloseNotificationListener();
            }
            else
            {
                this.EmitTestReport();
            }

            if (timer != null)
            {
                timer.Stop();
            }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private TestingProcess(Configuration configuration)
        {
            if (configuration.ParallelBugFindingTasks > 1 &&
                configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                TestingPortfolio.ConfigureStrategyForCurrentProcess(configuration);
            }

            if (configuration.RandomSchedulingSeed != null && configuration.TestingProcessId >= 0)
            {
                configuration.RandomSchedulingSeed = configuration.RandomSchedulingSeed + (673 * configuration.TestingProcessId);
            }

            this.Configuration = configuration;
            this.TestingEngine = TestingEngineFactory.CreateBugFindingEngine(
                this.Configuration);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Opens the remote notification listener.
        /// </summary>
        private void OpenNotificationListener()
        {
            Uri address = new Uri("net.pipe://localhost/psharp/testing/process/" +
                this.Configuration.TestingProcessId + "/");

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
                IO.Error.ReportAndExit("Your process does not have access " +
                    "rights to open the remote testing notification listener. " +
                    "Please run the process as administrator.");
            }
        }

        /// <summary>
        /// Closes the remote notification listener.
        /// </summary>
        private void CloseNotificationListener()
        {
            if (this.NotificationService.State == CommunicationState.Opened)
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
        /// Sends the test report associated with this testing process.
        /// </summary>
        private void SendTestReport()
        {
            Uri address = new Uri("net.pipe://localhost/psharp/testing/scheduler/");

            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            EndpointAddress endpoint = new EndpointAddress(address);

            if (this.TestingScheduler == null)
            {
                this.TestingScheduler = ChannelFactory<ITestingProcessScheduler>.
                    CreateChannel(binding, endpoint);
            }

            this.TestingScheduler.SetTestData(this.TestingEngine.TestReport,
                this.Configuration.TestingProcessId);
        }

        /// <summary>
        /// Emits the test report. If the P# tester runs in parallel it also
        /// merges the reports from all testing processes.
        /// </summary>
        private void EmitTestReport()
        {
            if (this.Configuration.DebugCodeCoverage)
            {
                this.EmitCoverageReport(isDebug: true);
            }

            if (this.Configuration.ParallelBugFindingTasks > 1)
            {
                IList<TestReport> globalTestReport = this.TestingScheduler.GetGlobalTestData(
                        this.Configuration.TestingProcessId);
                foreach (var testReport in globalTestReport)
                {
                    this.TestingEngine.TestReport.Merge(testReport);
                }
            }

            IO.Error.PrintLine(this.TestingEngine.Report());

            if (this.TestingEngine.TestReport.NumOfFoundBugs > 0 ||
                this.Configuration.PrintTrace)
            {
                this.EmitTraces();
            }

            if (this.Configuration.ReportCodeCoverage)
            {
                this.EmitCoverageReport();
            }
        }

        /// <summary>
        /// Emits the testing traces.
        /// </summary>
        private void EmitTraces()
        {
            string file = Path.GetFileNameWithoutExtension(this.TestingEngine.ProgramName);
            if (this.Configuration.TestingProcessId >= 0)
            {
                file += "_" + this.Configuration.TestingProcessId;
            }
            else
            {
                file += "_0";
            }

            string directory = this.GetOutputDirectory();

            this.TestingEngine.TryEmitTraces(directory, file);
        }

        /// <summary>
        /// Emits the code coverage report.
        /// </summary>
        /// <param name="isDebug">Is a debug report</param>
        private void EmitCoverageReport(bool isDebug = false)
        {
            string file = Path.GetFileNameWithoutExtension(this.TestingEngine.ProgramName);
            if (this.Configuration.TestingProcessId >= 0)
            {
                file += "_" + this.Configuration.TestingProcessId;
            }
            else
            {
                file += "_0";
            }

            string directory = "";
            if (isDebug)
            {
                directory = this.GetOutputDirectory("CoverageDebug");
            }
            else
            {
                directory = this.GetOutputDirectory();
            }

            this.TestingEngine.TryEmitCoverageReport(directory, file);
        }

        /// <summary>
        /// Notifies the remote testing scheduler
        /// about a discovered bug.
        /// </summary>
        private void NotifyBugFound()
        {
            Uri address = new Uri("net.pipe://localhost/psharp/testing/scheduler/");

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

        /// <summary>
        /// Returns (and creates if it does not exist) the output
        /// directory with an optional suffix.
        /// </summary>
        /// <returns>Path</returns>
        private string GetOutputDirectory(string suffix = "")
        {
            string directoryPath = Path.GetDirectoryName(this.TestingEngine.ProgramName) +
                Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar;
            if (suffix.Length > 0)
            {
                directoryPath += suffix + Path.DirectorySeparatorChar;
            }

            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        #endregion
    }
}