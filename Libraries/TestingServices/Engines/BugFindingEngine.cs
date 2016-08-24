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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.TestingServices.Tracing.Error;
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
        /// The readable trace, if any.
        /// </summary>
        private string ReadableTrace;

        /// <summary>
        /// The bug trace, if any.
        /// </summary>
        private BugTrace BugTrace;

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

            // Emits the human readable trace, if it exists.
            if (!this.ReadableTrace.Equals(""))
            {
                string[] readableTraces = Directory.GetFiles(directoryPath, name + "_*.txt").
                    Where(path => new Regex(@"^.*_[0-9]+.txt$").IsMatch(path)).ToArray();
                string readableTracePath = directoryPath + name + "_" + readableTraces.Length + ".txt";

                IO.Error.PrintLine($"... Writing {readableTracePath}");
                File.WriteAllText(readableTracePath, this.ReadableTrace);
            }

            // Emits the bug trace, if it exists.
            if (this.BugTrace != null)
            {
                string[] bugTraces = Directory.GetFiles(directoryPath, name + "_*.pstrace");
                string bugTracePath = directoryPath + name + "_" + bugTraces.Length + ".pstrace";

                using (FileStream stream = File.Open(bugTracePath, FileMode.Create))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(BugTrace));
                    IO.Error.PrintLine($"... Writing {bugTracePath}");
                    serializer.WriteObject(stream, this.BugTrace);
                }
            }

            // Emits the reproducable trace, if it exists.
            if (!this.ReproducableTrace.Equals(""))
            {
                string[] reproTraces = Directory.GetFiles(directoryPath, name + "_*.schedule");
                string reproTracePath = directoryPath + name + "_" + reproTraces.Length + ".schedule";

                IO.Error.PrintLine($"... Writing {reproTracePath}");
                File.WriteAllText(reproTracePath, this.ReproducableTrace);
            }
        }

        /// <summary>
        /// Tries to emit the testing coverage report, if any.
        /// </summary>
        public override void TryEmitCoverageReport()
        {
            if (base.Configuration.ReportCodeCoverage)
            {
                string name = Path.GetFileNameWithoutExtension(this.Assembly.Location);
                string directoryPath = base.GetOutputDirectory();

                var codeCoverageReporter = new CodeCoverageReporter(base.TestReport.CoverageInfo);

                string[] graphFiles = Directory.GetFiles(directoryPath, name + "_*.dgml");
                string graphFilePath = directoryPath + name + "_" + graphFiles.Length + ".dgml";

                IO.Error.PrintLine($"... Writing {graphFilePath}");
                codeCoverageReporter.EmitVisualizationGraph(graphFilePath);

                string[] coverageFiles = Directory.GetFiles(directoryPath, name + "_*.coverage.txt");
                string coverageFilePath = directoryPath + name + "_" + coverageFiles.Length + ".coverage.txt";

                IO.Error.PrintLine($"... Writing {coverageFilePath}");
                codeCoverageReporter.EmitCoverageReport(coverageFilePath);
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
        /// Initializes the bug-finding engine.
        /// </summary>
        private void Initialize()
        {
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
                        base.Strategy, base.TestReport.CoverageInfo);

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

                    // Wait for the test to terminate.
                    runtime.Wait();

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
                    
                    this.GatherIterationStatistics(runtime);

                    if (runtime.BugFinder.BugFound)
                    {
                        base.TestReport.NumOfFoundBugs++;
                        base.TestReport.BugReport = runtime.BugFinder.BugReport;

                        if (base.Configuration.PerformFullExploration)
                        {
                            IO.PrintLine($"..... Iteration #{i + 1} triggered " +
                                $"bug #{base.TestReport.NumOfFoundBugs}");
                        }
                    }
                    else
                    {
                        base.TestReport.BugReport = "";
                    }

                    if (base.Strategy.HasFinished())
                    {
                        break;
                    }

                    base.Strategy.ConfigureNextIteration();

                    if (!base.Configuration.PerformFullExploration && base.TestReport.NumOfFoundBugs > 0)
                    {
                        if (sw != null && !base.Configuration.SuppressTrace)
                        {
                            this.ReadableTrace = sw.ToString();
                            this.ReadableTrace += this.CreateReport("<StrategyLog>");
                            this.BugTrace = runtime.BugTrace;
                            this.ConstructReproducableTrace(runtime);
                        }

                        break;
                    }
                    else if (sw != null && base.Configuration.PrintTrace)
                    {
                        this.ReadableTrace = sw.ToString();
                        this.ReadableTrace += this.CreateReport("<StrategyLog>");
                        this.BugTrace = runtime.BugTrace;
                        this.ConstructReproducableTrace(runtime);
                    }

                    // Increases iterations if there is a specified timeout
                    // and the default iteration given.
                    if (base.Configuration.SchedulingIterations == 1 &&
                        base.Configuration.Timeout > 0)
                    {
                        maxIterations++;
                    }

                    runtime.Dispose();
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
        /// Gathers the exploration strategy statistics for
        /// the current iteration.
        /// </summary>
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        private void GatherIterationStatistics(PSharpBugFindingRuntime runtime)
        {
            if (base.Strategy.IsFair())
            {
                base.TestReport.NumOfExploredFairSchedules++;

                if (base.Strategy.HasReachedMaxSchedulingSteps())
                {
                    base.TestReport.MaxFairStepsHitInFairTests++;
                }

                if (runtime.BugFinder.ExploredSteps >= base.Configuration.MaxUnfairSchedulingSteps)
                {
                    base.TestReport.MaxUnfairStepsHitInFairTests++;
                }

                if (!base.Strategy.HasReachedMaxSchedulingSteps())
                {
                    base.TestReport.TotalExploredFairSteps += runtime.BugFinder.ExploredSteps;

                    if (base.TestReport.MinExploredFairSteps < 0 ||
                        base.TestReport.MinExploredFairSteps > runtime.BugFinder.ExploredSteps)
                    {
                        base.TestReport.MinExploredFairSteps = runtime.BugFinder.ExploredSteps;
                    }

                    if (base.TestReport.MaxExploredFairSteps < runtime.BugFinder.ExploredSteps)
                    {
                        base.TestReport.MaxExploredFairSteps = runtime.BugFinder.ExploredSteps;
                    }
                }
            }
            else
            {
                base.TestReport.NumOfExploredUnfairSchedules++;

                if (base.Strategy.HasReachedMaxSchedulingSteps())
                {
                    base.TestReport.MaxUnfairStepsHitInUnfairTests++;
                }
            }
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

            report.AppendFormat("{0} Found {1} bug{2}.", prefix, base.TestReport.NumOfFoundBugs,
                base.TestReport.NumOfFoundBugs == 1 ? "" : "s");

            report.AppendLine();
            report.AppendFormat("{0} Scheduling statistics:",
                prefix);

            int totalExploredSchedules = base.TestReport.NumOfExploredFairSchedules +
                base.TestReport.NumOfExploredUnfairSchedules;

            report.AppendLine();
            report.AppendFormat("{0} Explored {1} schedule{2}: {3} fair and {4} unfair.",
                prefix.Equals("...") ? "....." : prefix,
                totalExploredSchedules, totalExploredSchedules == 1 ? "" : "s",
                base.TestReport.NumOfExploredFairSchedules,
                base.TestReport.NumOfExploredUnfairSchedules);

            if (totalExploredSchedules > 0 &&
                base.TestReport.NumOfFoundBugs > 0)
            {
                report.AppendLine();
                report.AppendFormat("{0} Found {1:F2}% buggy schedules.",
                    prefix.Equals("...") ? "....." : prefix,
                    ((double)base.TestReport.NumOfFoundBugs / totalExploredSchedules) * 100);
            }
            
            if (base.TestReport.NumOfExploredFairSchedules > 0)
            {
                if (base.TestReport.TotalExploredFairSteps > 0)
                {
                    int averageExploredFairSteps = base.TestReport.TotalExploredFairSteps /
                        base.TestReport.NumOfExploredFairSchedules;

                    report.AppendLine();
                    report.AppendFormat("{0} Number of scheduling points in fair terminating schedules: " +
                        "{1} (min), {2} (avg), {3} (max).",
                        prefix.Equals("...") ? "....." : prefix,
                        base.TestReport.MinExploredFairSteps < 0 ? 0 : base.TestReport.MinExploredFairSteps,
                        averageExploredFairSteps,
                        base.TestReport.MaxExploredFairSteps < 0 ? 0 : base.TestReport.MaxExploredFairSteps);
                }

                if (base.Configuration.MaxUnfairSchedulingSteps > 0 &&
                    base.TestReport.MaxUnfairStepsHitInFairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat("{0} Exceeded the max-steps bound of '{1}' in {2:F2}% of the fair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        base.Configuration.MaxUnfairSchedulingSteps,
                        ((double)base.TestReport.MaxUnfairStepsHitInFairTests /
                        (double)base.TestReport.NumOfExploredFairSchedules) * 100);
                }

                if (base.Configuration.UserExplicitlySetMaxFairSchedulingSteps &&
                    base.Configuration.MaxFairSchedulingSteps > 0 &&
                    base.TestReport.MaxFairStepsHitInFairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat("{0} Hit the max-steps bound of '{1}' in {2:F2}% of the fair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        base.Configuration.MaxFairSchedulingSteps,
                        ((double)base.TestReport.MaxFairStepsHitInFairTests /
                        (double)base.TestReport.NumOfExploredFairSchedules) * 100);
                }
            }

            if (base.TestReport.NumOfExploredUnfairSchedules > 0)
            {
                if (base.Configuration.MaxUnfairSchedulingSteps > 0 &&
                    base.TestReport.MaxUnfairStepsHitInUnfairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat("{0} Hit the max-steps bound of '{1}' in {2:F2}% of the unfair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        base.Configuration.MaxUnfairSchedulingSteps,
                        ((double)base.TestReport.MaxUnfairStepsHitInUnfairTests /
                        (double)base.TestReport.NumOfExploredUnfairSchedules) * 100);
                }
            }
            
            if (base.Configuration.ParallelBugFindingTasks == 1)
            {
                report.AppendLine();
                report.Append($"{prefix} Elapsed {base.Profiler.Results()} sec.");
            }

            return report.ToString();
        }

        /// <summary>
        /// Constructs a reproducable trace.
        /// </summary>
        /// <param name="runtime">Runtime</param>
        private void ConstructReproducableTrace(PSharpBugFindingRuntime runtime)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (this.Strategy.IsFair())
            {
                stringBuilder.Append("--fair-scheduling").Append(Environment.NewLine);
            }

            if (base.Configuration.CacheProgramState)
            {
                stringBuilder.Append("--state-caching").Append(Environment.NewLine);
                stringBuilder.Append("--liveness-temperature-threshold:" +
                    base.Configuration.LivenessTemperatureThreshold).
                    Append(Environment.NewLine);
            }
            else
            {
                stringBuilder.Append("--liveness-temperature-threshold:" +
                    base.Configuration.LivenessTemperatureThreshold).
                    Append(Environment.NewLine);
            }

            for (int idx = 0; idx < runtime.ScheduleTrace.Count; idx++)
            {
                ScheduleStep step = runtime.ScheduleTrace[idx];
                if (step.Type == ScheduleStepType.SchedulingChoice)
                {
                    stringBuilder.Append($"{step.ScheduledMachine.Id.Type}" +
                        $"({step.ScheduledMachine.Id.Value})");
                }
                else if (step.BooleanChoice != null)
                {
                    stringBuilder.Append(step.BooleanChoice.Value);
                }
                else
                {
                    stringBuilder.Append(step.IntegerChoice.Value);
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
                //Console.WriteLine("Machine Id: " + kvp.Key);
                foreach (var actionTrace in kvp.Value)
                {
                    if (actionTrace.Type == MachineActionType.InvocationAction)
                    {
                        IO.Debug($"<RaceTracing> Action '{actionTrace.ActionName}' " +
                            $"'{actionTrace.ActionId}'");
                        //Console.WriteLine("Action: " + actionTrace.ActionName + " " + actionTrace.ActionId);
                    }
                    //if (actionTrace.Type == MachineActionType.SendAction)
                    //{
                    //    Console.WriteLine("SendEvent: " + actionTrace.ActionName + " " + actionTrace.SendId);
                    //}
                    //if (actionTrace.Type == MachineActionType.MachineCreationInfo)
                    //{
                    //    Console.WriteLine("Machine creation: " + actionTrace.ActionName + " " + actionTrace.createdMachineId);
                    //}
                    //if(actionTrace.Type == MachineActionType.TaskMachineCreation)
                    //{
                    //    Console.WriteLine("Task Machine creation: " + actionTrace.TaskId + " " + actionTrace.TaskMachineId);
                    //}
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
