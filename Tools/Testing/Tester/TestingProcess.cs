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
using System.Linq;
using System.Timers;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// A P# testing process.
    /// </summary>
    internal sealed class TestingProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The testing engine associated with
        /// this testing process.
        /// </summary>
        private ITestingEngine TestingEngine;

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
            Timer timer = this.CreateParentStatusMonitorTimer();
            timer.Start();
            
            this.TestingEngine.Run();

            timer.Stop();
        }

        /// <summary>
        /// Tries to emit the traces, if any.
        /// </summary>
        internal void TryEmitTraces()
        {
            this.TestingEngine.TryEmitTraces();
        }

        /// <summary>
        /// Reports the testing results.
        /// </summary>
        internal void Report()
        {
            this.TestingEngine.Report();
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private TestingProcess(Configuration configuration)
        {
            this.Configuration = configuration;
            this.TestingEngine = TestingEngineFactory.CreateBugFindingEngine(
                this.Configuration);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Creates a timer that monitors the status of the parent process.
        /// </summary>
        /// <returns>Timer</returns>
        private Timer CreateParentStatusMonitorTimer()
        {
            Timer timer = new Timer(2000);
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
                => val.Id == this.Configuration.ParentProcessId);
            if (parent == null || !parent.ProcessName.Equals("PSharpTester"))
            {
                Environment.Exit(1);
            }
        }

        #endregion
    }
}