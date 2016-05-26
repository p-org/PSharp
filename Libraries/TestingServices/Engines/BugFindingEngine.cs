//-----------------------------------------------------------------------
// <copyright file="BugFindingEngine.cs">
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
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Exploration;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# bug-finding engine.
    /// </summary>
    internal sealed class BugFindingEngine : AbstractTestingEngine
    {
        #region fields

        /// <summary>
        /// Explored schedules so far.
        /// </summary>
        private int ExploredSchedules;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>BugFindingEngine</returns>
        public static BugFindingEngine Create(Configuration configuration, Action<PSharpRuntime> action)
        {
            return new BugFindingEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>BugFindingEngine</returns>
        internal static BugFindingEngine Create(Configuration configuration, Assembly assembly)
        {
            return new BugFindingEngine(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        /// <returns>BugFindingEngine</returns>
        internal static BugFindingEngine Create(Configuration configuration, string assemblyName)
        {
            return new BugFindingEngine(configuration, assemblyName);
        }

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        /// <returns>ITestingEngine</returns>
        public override ITestingEngine Run()
        {
            Task task = this.CreateBugFindingTask();
            this.Execute(task);
            this.Report();
            return this;
        }

        /// <summary>
        /// Reports the testing results.
        /// </summary>
        public override void Report()
        {
            IO.PrintLine("... Found {0} bug{1}.", base.NumOfFoundBugs,
                base.NumOfFoundBugs == 1 ? "" : "s");
            IO.PrintLine("... Explored {0} {1} schedule{2}.", this.ExploredSchedules,
                base.Strategy.HasFinished() ? "(all)" : "",
                this.ExploredSchedules == 1 ? "" : "s");

            if (this.ExploredSchedules > 0)
            {
                IO.PrintLine("... Found {0}% buggy schedules.",
                    (base.NumOfFoundBugs * 100 / this.ExploredSchedules));
                IO.PrintLine("... Instrumented {0} scheduling point{1} (on last iteration).",
                    base.ExploredDepth, base.ExploredDepth == 1 ? "" : "s");
            }

            if (base.Configuration.DepthBound > 0)
            {
                IO.PrintLine($"... Used depth bound of {base.Configuration.DepthBound}.");
            }

            IO.PrintLine($"... Elapsed {base.Profiler.Results()} sec.");
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        private BugFindingEngine(Configuration configuration, Action<PSharpRuntime> action)
            : base(configuration, action)
        {
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        private BugFindingEngine(Configuration configuration, Assembly assembly)
            : base(configuration, assembly)
        {
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        private BugFindingEngine(Configuration configuration, string assemblyName)
            : base(configuration, assemblyName)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initialized the bug-finding engine.
        /// </summary>
        private void Initialize()
        {
            this.ExploredSchedules = 0;
        }

        #endregion

        #region core methods

        /// <summary>
        /// Creates a bug-finding task.
        /// </summary>
        /// <returns>Task</returns>
        private Task CreateBugFindingTask()
        {
            IO.PrintLine($"... Using '{base.Configuration.SchedulingStrategy}' strategy");

            Task task = new Task(() =>
            {
                // Initialize the test state
                try
                {
                    if (base.TestInitMethod != null)
                        base.TestInitMethod.Invoke(null, new object[] { });
                }
                catch (TargetInvocationException ex)
                {
                    if (!(ex.InnerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }

                for (int i = 0; i < base.Configuration.SchedulingIterations; i++)
                {
                    if (this.ShouldPrintIteration(i + 1))
                    {
                        IO.PrintLine($"..... Iteration #{i + 1}");
                    }

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

                    this.ExploredSchedules++;
                    base.ExploredDepth = runtime.BugFinder.ExploredSteps;

                    if (runtime.BugFinder.BugFound)
                    {
                        base.NumOfFoundBugs++;
                        base.BugReport = runtime.BugFinder.BugReport;

                        if (base.Configuration.PerformFullExploration)
                        {
                            IO.PrintLine($"..... Iteration #{i + 1} triggered " +
                                $"bug #{base.NumOfFoundBugs}");
                        }
                    }
                    else
                    {
                        base.BugReport = "";
                    }

                    if (base.Strategy.HasFinished())
                    {
                        break;
                    }

                    base.Strategy.ConfigureNextIteration();

                    if (!base.Configuration.PerformFullExploration && base.NumOfFoundBugs > 0)
                    {
                        if (sw != null && !base.Configuration.SuppressTrace)
                        {
                            this.PrintReadableTrace(sw);
                            this.PrintReproducableTrace(runtime);
                        }

                        break;
                    }
                    else if (sw != null && base.Configuration.PrintTrace)
                    {
                        this.PrintReadableTrace(sw);
                        this.PrintReproducableTrace(runtime);
                    }
                }

                // Cleanup test state
                try
                {
                    if (base.TestCloseMethod != null)
                        base.TestCloseMethod.Invoke(null, new object[] { });

                }
                catch (TargetInvocationException ex)
                {
                    if (!(ex.InnerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }

            });

            return task;
        }

        #endregion

        #region utility methods

        /// <summary>
        /// Outputs a readable trace.
        /// </summary>
        /// <param name="sw">StringWriter</param>
        private void PrintReadableTrace(StringWriter sw)
        {
            var name = Path.GetFileNameWithoutExtension(this.Assembly.Location);
            var directoryPath = Path.GetDirectoryName(this.Assembly.Location) +
                Path.DirectorySeparatorChar + "traces" + Path.DirectorySeparatorChar;

            Directory.CreateDirectory(directoryPath);

            var traces = Directory.GetFiles(directoryPath, name + "*.txt");
            var path = directoryPath + name + "_" + traces.Length + ".txt";

            IO.PrintLine($"... Writing {path}");
            File.WriteAllText(path, sw.ToString());
        }

        /// <summary>
        /// Outputs a reproducable trace.
        /// </summary>
        /// <param name="runtime">Runtime</param>
        private void PrintReproducableTrace(PSharpBugFindingRuntime runtime)
        {
            var name = Path.GetFileNameWithoutExtension(this.Assembly.Location);
            var directoryPath = Path.GetDirectoryName(this.Assembly.Location) +
                Path.DirectorySeparatorChar + "traces" + Path.DirectorySeparatorChar;

            Directory.CreateDirectory(directoryPath);

            var traces = Directory.GetFiles(directoryPath, name + "*.pstrace");
            var path = directoryPath + name + "_" + traces.Length + ".pstrace";

            StringBuilder stringBuilder = new StringBuilder();
            for (int idx = 0; idx < runtime.ProgramTrace.Count; idx++)
            {
                TraceStep step = runtime.ProgramTrace[idx];
                if (step.Type == TraceStepType.SchedulingChoice)
                {
                    stringBuilder.Append(step.ScheduledMachine.Id);
                }
                else
                {
                    stringBuilder.Append(step.Choice);
                }

                if (idx < runtime.ProgramTrace.Count - 1)
                {
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            IO.PrintLine($"... Writing {path}");
            File.WriteAllText(path, stringBuilder.ToString());
        }

        /// <summary>
        /// Returns true if the engine should print the current iteration.
        /// </summary>
        /// <param name="iteration">Iteration</param>
        /// <returns>Boolean</returns>
        private bool ShouldPrintIteration(int iteration)
        {
            if (iteration > this.PrintGuard * 10)
            {
                var count = (iteration.ToString().Length - 1);
                var guard = "1" + (count > 0 ? String.Concat(Enumerable.Repeat("0", count)) : "");
                this.PrintGuard = int.Parse(guard);
            }

            return iteration % this.PrintGuard == 0;
        }

        #endregion
    }
}
