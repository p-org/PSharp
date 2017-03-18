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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Timers;

using Microsoft.PSharp.IO;
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
            return this.TestingEngine.TestReport.Clone();
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
            // Opens the remote notification listener.
            this.OpenNotificationListener();

            Timer timer = null;
            if (this.Configuration.RunAsParallelBugFindingTask)
            {
                timer = this.CreateParentStatusMonitorTimer();
                timer.Start();
            }
            
            this.TestingEngine.Run();

            if (this.Configuration.RunAsParallelBugFindingTask)
            {
                if (this.TestingEngine.TestReport.NumOfFoundBugs > 0)
                {
                    // A bug was found, set the exit code to a non-zero error code for general processing.
                    // See https://msdn.microsoft.com/en-gb/library/windows/desktop/ms681381(v=vs.85).aspx
                    Environment.ExitCode = 13804;

                    this.NotifyBugFound();
                }

                this.SendTestReport();
            }

            if (!this.Configuration.PerformFullExploration)
            {
                if (this.TestingEngine.TestReport.NumOfFoundBugs > 0 &&
                    !this.Configuration.RunAsParallelBugFindingTask)
                {
                    Output.WriteLine($"... Task {this.Configuration.TestingProcessId} found a bug.");
                }

                if (this.TestingEngine.TestReport.NumOfFoundBugs > 0 ||
                    this.Configuration.PrintTrace)
                {
                    this.EmitTraces();
                }
            }

            // Closes the remote notification listener.
            this.CloseNotificationListener();

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
            if (configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                TestingPortfolio.ConfigureStrategyForCurrentProcess(configuration);
            }

            if (configuration.RandomSchedulingSeed != null)
            {
                configuration.RandomSchedulingSeed = (int)(configuration.RandomSchedulingSeed + (673 * configuration.TestingProcessId));
            }

            this.Configuration = configuration;
            this.TestingEngine = TestingEngineFactory.CreateBugFindingEngine(
                this.Configuration);
        }

        #endregion

        #region private methods

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

        /// <summary>
        /// Emits the testing traces.
        /// </summary>
        private void EmitTraces()
        {
            string file = Path.GetFileNameWithoutExtension(this.Configuration.AssemblyToBeAnalyzed);
            file += "_" + this.Configuration.TestingProcessId;

            string directory = Reporter.GetOutputDirectory(this.Configuration.AssemblyToBeAnalyzed);

            Output.WriteLine($"... Emitting task {this.Configuration.TestingProcessId} traces:");
            this.TestingEngine.TryEmitTraces(directory, file);
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

        #endregion
    }
}