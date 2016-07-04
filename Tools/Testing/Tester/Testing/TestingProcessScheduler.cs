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
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;

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
        /// Map from testing process ids to testing processes.
        /// </summary>
        private Dictionary<int, Process> TestingProcesses;

        /// <summary>
        /// The notification listening service.
        /// </summary>
        private ServiceHost NotificationService;

        /// <summary>
        /// The testing profiler.
        /// </summary>
        private Profiler Profiler;

        /// <summary>
        /// The scheduler lock.
        /// </summary>
        private object SchedulerLock;

        /// <summary>
        /// Checks if a bug was discovered.
        /// </summary>
        private bool BugFound;

        #endregion

        #region remote testing process methods

        /// <summary>
        /// Notifies the testing process scheduler
        /// that a bug was found.
        /// </summary>
        /// <param name="processId">Unique process id</param>
        /// <returns>Boolean value</returns>
        bool ITestingProcessScheduler.NotifyBugFound(int processId)
        {
            bool result = false;
            lock (this.SchedulerLock)
            {
                if (!this.BugFound)
                {
                    IO.PrintLine($"... Testing task '{processId}' " +
                        "found a bug.");

                    this.BugFound = true;
                    foreach (var testingProcess in this.TestingProcesses)
                    {
                        if (testingProcess.Key == processId)
                        {
                            result = true;
                        }
                        else
                        {
                            testingProcess.Value.Kill();
                        }
                    }
                }
            }
            
            return result;
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
            this.Profiler = new Profiler();
            this.SchedulerLock = new object();
            this.BugFound = false;

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
                ErrorReporter.ReportAndExit("Your process does not have access " +
                    "rights to open the remote testing notification listener. " +
                    "Please run the process as administrator.");
            }
        }

        #endregion
    }
}
