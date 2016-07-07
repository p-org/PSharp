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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Tracing.Machines;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
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

        /// <summary>
        /// The readable trace, if any.
        /// </summary>
        private string ReadableTrace;

        /// <summary>
        /// The reproducable trace, if any.
        /// </summary>
        private string ReproducableTrace;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>BugFindingEngine</returns>
        public static BugFindingEngine Create(Configuration configuration,
            Action<PSharpRuntime> action)
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
        /// <returns>BugFindingEngine</returns>
        internal static BugFindingEngine Create(Configuration configuration)
        {
            return new BugFindingEngine(configuration);
        }

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        /// <returns>ITestingEngine</returns>
        public override ITestingEngine Run()
        {
            Task task = this.CreateBugFindingTask();
            this.Execute(task);
            return this;
        }

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        public override void TryEmitTraces()
        {
            string name = Path.GetFileNameWithoutExtension(this.Assembly.Location);
            string directoryPath = base.GetOutputDirectory();

            // Emits the human readable trace, if any.
            if (!this.ReadableTrace.Equals(""))
            {
                string[] readableTraces = Directory.GetFiles(directoryPath, name + "*.txt");
                string readableTracesPath = directoryPath + name + "_" + readableTraces.Length + ".txt";

                IO.PrintLine($"... Writing {readableTracesPath}");
                File.WriteAllText(readableTracesPath, this.ReadableTrace);
            }

            // Emits the reproducable trace, if any.
            if (!this.ReproducableTrace.Equals(""))
            {
                string[] reproTraces = Directory.GetFiles(directoryPath, name + "*.pstrace");
                string reproTracesPath = directoryPath + name + "_" + reproTraces.Length + ".pstrace";

                IO.PrintLine($"... Writing {reproTracesPath}");
                File.WriteAllText(reproTracesPath, this.ReproducableTrace);
            }
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        /// <returns>Report</returns>
        public override string Report()
        {
            return this.CreateReport("...");
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private BugFindingEngine(Configuration configuration)
            : base(configuration)
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
        /// <param name="action">Action</param>
        private BugFindingEngine(Configuration configuration, Action<PSharpRuntime> action)
            : base(configuration, action)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initialized the bug-finding engine.
        /// </summary>
        private void Initialize()
        {
            this.ExploredSchedules = 0;
            this.ReadableTrace = "";
            this.ReproducableTrace = "";
        }

        #endregion

        #region core methods

        /// <summary>
        /// Creates a new bug-finding task.
        /// </summary>
        /// <returns>Task</returns>
        private Task CreateBugFindingTask()
        {
            if (base.Configuration.TestingProcessId >= 0)
            {
                IO.Error.PrintLine($"... Task {this.Configuration.TestingProcessId} is " +
                    $"using '{base.Configuration.SchedulingStrategy}' strategy.");
            }
            else
            {
                IO.PrintLine($"... Using '{base.Configuration.SchedulingStrategy}' strategy.");
            }

            Task task = new Task(() =>
            {
                try
                {
                    if (base.TestInitMethod != null)
                    {
                        // Initializes the test state.
                        base.TestInitMethod.Invoke(null, new object[] { });
                    }
                }
                catch (TargetInvocationException ex)
                {
                    if (!(ex.InnerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }

                int maxIterations = base.Configuration.SchedulingIterations;
                for (int i = 0; i < maxIterations; i++)
                {
                    if (this.CancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

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

                    // Starts the test.
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

                    if (this.Configuration.EnableDataRaceDetection)
                    {
                        this.EmitRaceInstrumentationTraces(runtime, i);
                    }
                    
                    try
                    {
                        // Invokes user-provided cleanup for this iteration.
                        if (base.TestIterationDisposeMethod != null)
                        {
                            // Disposes the test state.
                            base.TestIterationDisposeMethod.Invoke(null, null);
                        }
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (!(ex.InnerException is TaskCanceledException))
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                    }

                    // Invoke the per iteration callbacks, if any.
                    foreach (var callback in base.PerIterationCallbacks)
                    {
                        callback(i);
                    }

                    // Cleans up the runtime before the next
                    // iteration starts.
                    this.CleanUpRuntime();

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
                            this.ReadableTrace = sw.ToString();
                            this.ReadableTrace += this.CreateReport("<StrategyLog>");
                            this.ConstructReproducableTrace(runtime);
                        }

                        break;
                    }
                    else if (sw != null && base.Configuration.PrintTrace)
                    {
                        this.ReadableTrace = sw.ToString();
                        this.ReadableTrace += this.CreateReport("<StrategyLog>");
                        this.ConstructReproducableTrace(runtime);
                    }

                    // Increases iterations if there is a specified timeout
                    // and the default iteration given.
                    if (base.Configuration.SchedulingIterations == 1 &&
                        base.Configuration.Timeout > 0)
                    {
                        maxIterations++;
                    }
                }
                
                try
                {
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

            }, base.CancellationTokenSource.Token);

            return task;
        }

        /// <summary>
        /// Cleans up the P# runtime. Called before the next
        /// testing iteration starts.
        /// </summary>
        private void CleanUpRuntime()
        {
            // Resets the machine id counter.
            MachineId.ResetMachineIDCounter();
        }

        #endregion

        #region utility methods

        /// <summary>
        /// Creates a new testing report with the specified prefix.
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <returns>Report</returns>
        private string CreateReport(string prefix)
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("{0} Found {1} bug{2}.", prefix, base.NumOfFoundBugs,
                base.NumOfFoundBugs == 1 ? "" : "s");
            report.AppendLine();
            report.AppendFormat("{0} Explored {1} {2} schedule{3}.", prefix,
                this.ExploredSchedules,
                base.Strategy.HasFinished() ? "(all)" : "",
                this.ExploredSchedules == 1 ? "" : "s");
            report.AppendLine();

            if (this.ExploredSchedules > 0)
            {
                report.AppendFormat("{0} Found {1}% buggy schedules.", prefix,
                    (base.NumOfFoundBugs * 100 / this.ExploredSchedules));
                report.AppendLine();
                report.AppendFormat("{0} Instrumented {1} scheduling point{2} (on last iteration).",
                    prefix, base.ExploredDepth, base.ExploredDepth == 1 ? "" : "s");
                report.AppendLine();
            }

            if (base.Configuration.DepthBound > 0)
            {
                report.Append($"{prefix} Configured to explore up to " +
                    $"'{base.Configuration.DepthBound}' max steps.");
                report.AppendLine();
            }

            report.Append($"{prefix} Elapsed {base.Profiler.Results()} sec.");

            return report.ToString();
        }

        /// <summary>
        /// Constructs a reproducable trace.
        /// </summary>
        /// <param name="runtime">Runtime</param>
        private void ConstructReproducableTrace(PSharpBugFindingRuntime runtime)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int idx = 0; idx < runtime.ScheduleTrace.Count; idx++)
            {
                ScheduleStep step = runtime.ScheduleTrace[idx];
                if (step.Type == ScheduleStepType.SchedulingChoice)
                {
                    stringBuilder.Append($"{step.ScheduledMachine.Id.Type}" +
                        $"({step.ScheduledMachine.Id.Value})");
                }
                else
                {
                    stringBuilder.Append(step.Choice);
                }

                if (idx < runtime.ScheduleTrace.Count - 1)
                {
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            this.ReproducableTrace = stringBuilder.ToString();
        }

        /// <summary>
        /// Emits race instrumentation traces.
        /// </summary>
        /// <param name="runtime">Runtime</param>
        /// <param name="iteration">Iteration</param>
        private void EmitRaceInstrumentationTraces(PSharpBugFindingRuntime runtime, int iteration)
        {
            string name = Path.GetFileNameWithoutExtension(this.Assembly.Location);
            string directoryPath = base.GetRuntimeTracesDirectory();
            
            foreach (var kvp in runtime.MachineActionTraceMap)
            {
                IO.Debug($"<RaceTracing> Machine id '{kvp.Key}'");
                foreach (var actionTrace in kvp.Value)
                {
                    if (actionTrace.Type == MachineActionType.InvocationAction)
                    {
                        IO.Debug($"<RaceTracing> Action '{actionTrace.ActionName}' " +
                            $"'{actionTrace.ActionId}'");
                    }
                }

                if (kvp.Value.Count > 0)
                {
                    string path = directoryPath + name + "_iteration_" + iteration +
                        "_machine_" + kvp.Key.GetHashCode() + ".osl";
                    using (FileStream stream = File.Open(path, FileMode.Create))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(kvp.Value.GetType());
                        serializer.WriteObject(stream, kvp.Value);
                        IO.Debug($"... Writing {path}");
                    }
                }
            }
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
