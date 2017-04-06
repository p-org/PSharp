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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;

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

            report.AppendFormat("... Reproduced {0} bug{1}.", base.TestReport.NumOfFoundBugs,
                base.TestReport.NumOfFoundBugs == 1 ? "" : "s");
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
                try
                {
                    if (base.TestInitMethod != null)
                    {
                        // Initializes the test state.
                        base.TestInitMethod.Invoke(null, new object[] { });
                    }

                    // Creates a new instance of the bug-finding runtime.
                    var runtime = new BugFindingRuntime(base.Configuration, base.Strategy);

                    // Logger used to intercept the program output if no custom logger
                    // is installed and if verbosity is turned off.
                    InMemoryLogger runtimeLogger = null;

                    // Gets a handle to the standard output and error streams.
                    var stdOut = Console.Out;
                    var stdErr = Console.Error;

                    try
                    {
                        // If verbosity is turned off, then intercept the program log, and also redirect
                        // the standard output and error streams into the runtime logger.
                        if (base.Configuration.Verbose < 2)
                        {
                            runtimeLogger = new InMemoryLogger();
                            runtime.SetLogger(runtimeLogger);

                            var writer = new LogWriter(new DisposingLogger());
                            Console.SetOut(writer);
                            Console.SetError(writer);
                        }

                        // Runs the test inside the P# test-harness machine.
                        runtime.RunTestHarness(base.TestMethod, base.TestAction);

                        // Wait for the test to terminate.
                        runtime.Wait();
                    }
                    finally
                    {
                        if (base.Configuration.Verbose < 2)
                        {
                            // Restores the standard output and error streams.
                            Console.SetOut(stdOut);
                            Console.SetError(stdErr);
                        }

                        runtimeLogger?.Dispose();
                    }

                    if (runtime.Scheduler.BugFound)
                    {
                        base.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
                    }

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

                    // Checks for any liveness property violations. Requires
                    // that the program has terminated and no safety property
                    // violations have been found.
                    if (!runtime.Scheduler.BugFound)
                    {
                        runtime.LivenessChecker.CheckLivenessAtTermination();
                    }

                    TestReport report = runtime.Scheduler.GetReport();
                    report.CoverageInfo.Merge(runtime.CoverageInfo);
                    this.TestReport.Merge(report);

                    runtime.Dispose();
                }
                catch (TargetInvocationException ex)
                {
                    if (!(ex.InnerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }
            }, base.CancellationTokenSource.Token);

            return task;
        }

        #endregion
    }
}
