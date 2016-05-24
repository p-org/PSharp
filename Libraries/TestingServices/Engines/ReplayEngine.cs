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
        /// <param name="action">Action</param>
        /// <returns>ReplayEngine</returns>
        public static ReplayEngine Create(Configuration configuration, Action<PSharpRuntime> action)
        {
            return new ReplayEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>ReplayEngine</returns>
        internal static ReplayEngine Create(Configuration configuration, Assembly assembly)
        {
            return new ReplayEngine(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        /// <returns>ReplayEngine</returns>
        internal static ReplayEngine Create(Configuration configuration, string assemblyName)
        {
            return new ReplayEngine(configuration, assemblyName);
        }

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        /// <returns>ITestingEngine</returns>
        public override ITestingEngine Run()
        {
            Task task = this.CreateBugReproducingTask();
            base.Execute(task);
            this.Report();
            return this;
        }

        /// <summary>
        /// Reports the testing results.
        /// </summary>
        public override void Report()
        {
            IO.PrintLine("... Reproduced {0} bug{1}.", base.NumOfFoundBugs,
                base.NumOfFoundBugs == 1 ? "" : "s");
            IO.PrintLine("... Instrumented {0} scheduling point{1}.",
                base.ExploredDepth, base.ExploredDepth == 1 ? "" : "s");

            IO.PrintLine($"... Elapsed {base.Profiler.Results()} sec.");
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        private ReplayEngine(Configuration configuration, Action<PSharpRuntime> action)
            : base(configuration, action)
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
        /// <param name="assemblyName">Assembly name</param>
        private ReplayEngine(Configuration configuration, string assemblyName)
            : base(configuration, assemblyName)
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
                    base.Strategy, base.Visualizer);

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

                // Wait for test to terminate.
                runtime.Wait();

                if (base.Configuration.EnableVisualization)
                {
                    base.Visualizer.Refresh();
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
            });

            return task;
        }

        #endregion
    }
}
