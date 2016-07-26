//-----------------------------------------------------------------------
// <copyright file="ReplayEngine.cs">
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
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# replay engine.
    /// </summary>
    internal sealed class ReplayEngine : AbstractTestingEngine
    {
        #region public API

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>ReplayEngine</returns>
        public static ReplayEngine Create(Configuration configuration)
        {
            return new ReplayEngine(configuration);
        }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>ReplayEngine</returns>
        public static ReplayEngine Create(Configuration configuration, Assembly assembly)
        {
            return new ReplayEngine(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>ReplayEngine</returns>
        public static ReplayEngine Create(Configuration configuration, Action<PSharpRuntime> action)
        {
            return new ReplayEngine(configuration, action);
        }

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        /// <returns>ITestingEngine</returns>
        public override ITestingEngine Run()
        {
            Task task = this.CreateBugReproducingTask();
            base.Execute(task);
            return this;
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        /// <returns>Report</returns>
        public override string Report()
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("... Reproduced {0} bug{1}.", base.NumOfFoundBugs,
                base.NumOfFoundBugs == 1 ? "" : "s");
            report.AppendLine();
            report.AppendFormat("... Instrumented {0} scheduling point{1}.",
                base.ExploredDepth, base.ExploredDepth == 1 ? "" : "s");
            report.AppendLine();

            report.Append($"... Elapsed {base.Profiler.Results()} sec.");

            return report.ToString();
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private ReplayEngine(Configuration configuration)
            : base(configuration)
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        private ReplayEngine(Configuration configuration, Assembly assembly)
            : base(configuration, assembly)
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        private ReplayEngine(Configuration configuration, Action<PSharpRuntime> action)
            : base(configuration, action)
        {

        }

        #endregion

        #region core methods

        /// <summary>
        /// Creates a bug-reproducing task.
        /// </summary>
        /// <returns>Task</returns>
        private Task CreateBugReproducingTask()
        {
            Task task = new Task(() =>
            {
                var runtime = new PSharpBugFindingRuntime(base.Configuration,
                    base.Strategy, base.CoverageInfo);

                StringWriter sw = null;
                if (base.Configuration.RedirectTestConsoleOutput &&
                    base.Configuration.Verbose < 2)
                {
                    sw = base.RedirectConsoleOutput();
                    base.HasRedirectedConsoleOutput = true;
                }
                
                // Start the test.
                if (base.TestAction != null)
                {
                    base.TestAction(runtime);
                }
                else
                {
                    try
                    {
                        if (base.TestInitMethod != null)
                        {
                            // Initializes the test state.
                            base.TestInitMethod.Invoke(null, new object[] { });
                        }

                        // Starts the test.
                        base.TestMethod.Invoke(null, new object[] { runtime });

                    }
                    catch (TargetInvocationException ex)
                    {
                        if (!(ex.InnerException is TaskCanceledException))
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                    }
                }

                // Wait for the test to terminate.
                runtime.Wait();

                try
                {
                    // Invokes user-provided cleanup for this iteration.
                    if (base.TestIterationDisposeMethod != null)
                    {
                        // Disposes the test state.
                        base.TestIterationDisposeMethod.Invoke(null, new object[] { });
                    }

                    // Invokes user-provided cleanup for all iterations.
                    if (base.TestDisposeMethod != null)
                    {
                        // Disposes the test state.
                        base.TestDisposeMethod.Invoke(null, new object[] { });
                    }
                }
                catch (TargetInvocationException ex)
                {
                    if (!(ex.InnerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }

                // Checks for any liveness property violations. Requires
                // that the program has terminated and no safety property
                // violations have been found.
                if (!runtime.BugFinder.BugFound)
                {
                    runtime.LivenessChecker.CheckLivenessAtTermination();
                }

                if (base.HasRedirectedConsoleOutput)
                {
                    base.ResetOutput();
                }

                base.ExploredDepth = runtime.BugFinder.ExploredSteps;

                if (runtime.BugFinder.BugFound)
                {
                    base.NumOfFoundBugs++;
                    base.BugReport = runtime.BugFinder.BugReport;
                }
                else
                {
                    base.BugReport = "";
                }
            }, base.CancellationTokenSource.Token);

            return task;
        }

        #endregion
    }
}
