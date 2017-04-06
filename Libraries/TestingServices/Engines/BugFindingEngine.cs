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

using Microsoft.PSharp.IO;
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
        public static BugFindingEngine Create(Configuration configuration, Action<PSharpRuntime> action)
        {
            return new BugFindingEngine(configuration, action);
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
        /// <param name="directory">Directory name</param>
        /// <param name="file">File name</param>
        public override void TryEmitTraces(string directory, string file)
        {
            // Emits the human readable trace, if it exists.
            if (!this.ReadableTrace.Equals(""))
            {
                string[] readableTraces = Directory.GetFiles(directory, file + "_*.txt").
                    Where(path => new Regex(@"^.*_[0-9]+.txt$").IsMatch(path)).ToArray();
                string readableTracePath = directory + file + "_" + readableTraces.Length + ".txt";

                base.Logger.WriteLine($"..... Writing {readableTracePath}");
                File.WriteAllText(readableTracePath, this.ReadableTrace);
            }

            // Emits the bug trace, if it exists.
            if (this.BugTrace != null)
            {
                string[] bugTraces = Directory.GetFiles(directory, file + "_*.pstrace");
                string bugTracePath = directory + file + "_" + bugTraces.Length + ".pstrace";

                using (FileStream stream = File.Open(bugTracePath, FileMode.Create))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(BugTrace));
                    base.Logger.WriteLine($"..... Writing {bugTracePath}");
                    serializer.WriteObject(stream, this.BugTrace);
                }
            }

            // Emits the reproducable trace, if it exists.
            if (!this.ReproducableTrace.Equals(""))
            {
                string[] reproTraces = Directory.GetFiles(directory, file + "_*.schedule");
                string reproTracePath = directory + file + "_" + reproTraces.Length + ".schedule";

                base.Logger.WriteLine($"..... Writing {reproTracePath}");
                File.WriteAllText(reproTracePath, this.ReproducableTrace);
            }
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        /// <returns>Report</returns>
        public override string Report()
        {
            return this.TestReport.GetText(base.Configuration, "...");
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
            string options = "";
            if (base.Configuration.SchedulingStrategy == SchedulingStrategy.Random ||
                base.Configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom ||
                base.Configuration.SchedulingStrategy == SchedulingStrategy.RandomDelayBounding ||
                base.Configuration.SchedulingStrategy == SchedulingStrategy.RandomOperationBounding ||
                base.Configuration.SchedulingStrategy == SchedulingStrategy.PrioritizedOperationBounding ||
                base.Configuration.SchedulingStrategy == SchedulingStrategy.PCT ||
                base.Configuration.SchedulingStrategy == SchedulingStrategy.FairPCT ||
                base.Configuration.SchedulingStrategy == SchedulingStrategy.MaceMC)
            {
                options = $" (seed:{base.Configuration.RandomSchedulingSeed})";
            }

            base.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} is " +
                $"using '{base.Configuration.SchedulingStrategy}' strategy{options}.");

            Task task = new Task(() =>
            {
                try
                {
                    if (base.TestInitMethod != null)
                    {
                        // Initializes the test state.
                        base.TestInitMethod.Invoke(null, new object[] { });
                    }

                    int maxIterations = base.Configuration.SchedulingIterations;
                    for (int i = 0; i < maxIterations; i++)
                    {
                        if (this.CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        // Runs a new testing iteration.
                        this.RunNextIteration(i);

                        if (!base.Configuration.PerformFullExploration && base.TestReport.NumOfFoundBugs > 0)
                        {
                            break;
                        }

                        if (base.Strategy.HasFinished())
                        {
                            break;
                        }

                        base.Strategy.ConfigureNextIteration();

                        // Increases iterations if there is a specified timeout
                        // and the default iteration given.
                        if (base.Configuration.SchedulingIterations == 1 &&
                            base.Configuration.Timeout > 0)
                        {
                            maxIterations++;
                        }
                    }

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
        /// Runs the next testing iteration.
        /// </summary>
        /// <param name="iteration">Iteration</param>
        private void RunNextIteration(int iteration)
        {
            if (this.ShouldPrintIteration(iteration + 1))
            {
                base.Logger.WriteLine($"..... Iteration #{iteration + 1}");
            }

            // Runtime used to serialize and test the program in this iteration.
            BugFindingRuntime runtime = null;

            // Logger used to intercept the program output if no custom logger
            // is installed and if verbosity is turned off.
            InMemoryLogger runtimeLogger = null;

            // Gets a handle to the standard output and error streams.
            var stdOut = Console.Out;
            var stdErr = Console.Error;

            try
            {
                // Creates a new instance of the bug-finding runtime.
                runtime = new BugFindingRuntime(base.Configuration, base.Strategy);

                // If verbosity is turned off, then intercept the program log, and also dispose
                // the standard output and error streams.
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

                if (runtime.Scheduler.BugFound)
                {
                    base.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
                }

                if (this.Configuration.EnableDataRaceDetection)
                {
                    this.EmitRaceInstrumentationTraces(runtime, iteration);
                }

                // Invokes user-provided cleanup for this iteration.
                if (base.TestIterationDisposeMethod != null)
                {
                    // Disposes the test state.
                    base.TestIterationDisposeMethod.Invoke(null, null);
                }

                // Invoke the per iteration callbacks, if any.
                foreach (var callback in base.PerIterationCallbacks)
                {
                    callback(iteration);
                }

                // TODO: Clean this up.
                base.Configuration.RaceDetectionCallback?.Invoke();
                if (base.Configuration.RaceFound)
                {
                    string message = IO.Utilities.Format("Found a race");
                    runtime.Scheduler.NotifyAssertionFailure(message, false);
                }

                // Checks for any liveness property violations. Requires
                // that the program has terminated and no safety property
                // violations have been found.
                if (!runtime.Scheduler.BugFound)
                {
                    runtime.LivenessChecker.CheckLivenessAtTermination();
                }

                this.GatherIterationStatistics(runtime);

                if (runtimeLogger != null && base.TestReport.NumOfFoundBugs > 0 &&
                    !base.Configuration.PerformFullExploration && !base.Configuration.SuppressTrace)
                {
                    this.ConstructReproducableTrace(runtime, runtimeLogger);
                }
                else if (runtimeLogger != null && base.Configuration.PrintTrace)
                {
                    this.ConstructReproducableTrace(runtime, runtimeLogger);
                }
            }
            finally
            {
                if (base.Configuration.Verbose < 2)
                {
                    // Restores the standard output and error streams.
                    Console.SetOut(stdOut);
                    Console.SetError(stdErr);
                }

                if (base.Configuration.PerformFullExploration && runtime.Scheduler.BugFound)
                {
                    base.Logger.WriteLine($"..... Iteration #{iteration + 1} " +
                        $"triggered bug #{base.TestReport.NumOfFoundBugs} " +
                        $"[task-{this.Configuration.TestingProcessId}]");
                }

                // Cleans up the runtime before the next iteration starts.
                this.CleanUpRuntime();

                runtimeLogger?.Dispose();
                runtime?.Dispose();
            }
        }

        /// <summary>
        /// Gathers the exploration strategy statistics for
        /// the latest testing iteration.
        /// </summary>
        /// <param name="runtime">BugFindingRuntime</param>
        private void GatherIterationStatistics(BugFindingRuntime runtime)
        {
            TestReport report = runtime.Scheduler.GetReport();
            report.CoverageInfo.Merge(runtime.CoverageInfo);
            this.TestReport.Merge(report);
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
        /// Constructs a reproducable trace.
        /// </summary>
        /// <param name="runtime">BugFindingRuntime</param>
        /// <param name="logger">InMemoryLogger</param>
        private void ConstructReproducableTrace(BugFindingRuntime runtime, InMemoryLogger logger)
        {
            this.ReadableTrace = logger.ToString();
            this.ReadableTrace += this.TestReport.GetText(base.Configuration, "<StrategyLog>");
            this.BugTrace = runtime.BugTrace;

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

            if (!base.Configuration.TestMethodName.Equals(""))
            {
                stringBuilder.Append("--test-method:" + 
                    base.Configuration.TestMethodName).
                    Append(Environment.NewLine);
            }

            for (int idx = 0; idx < runtime.ScheduleTrace.Count; idx++)
            {
                ScheduleStep step = runtime.ScheduleTrace[idx];
                if (step.Type == ScheduleStepType.SchedulingChoice)
                {
                    stringBuilder.Append($"{step.ScheduledMachineId.Type}" +
                        $"({step.ScheduledMachineId.Value})");
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
        /// <param name="runtime">BugFindingRuntime</param>
        /// <param name="iteration">Iteration</param>
        private void EmitRaceInstrumentationTraces(BugFindingRuntime runtime, int iteration)
        {
            string name = Path.GetFileNameWithoutExtension(this.Assembly.Location);
            name += "_" + base.Configuration.TestingProcessId;

            string directoryPath = base.GetRuntimeTracesDirectory();
            
            foreach (var kvp in runtime.MachineActionTraceMap)
            {
                Debug.WriteLine($"<RaceTracing> Machine id '{kvp.Key}'");

                foreach (var actionTrace in kvp.Value)
                {
                    if (actionTrace.Type == MachineActionType.InvocationAction)
                    {
                        Debug.WriteLine($"<RaceTracing> Action '{actionTrace.ActionName}' " +
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
                        Debug.WriteLine($"..... Writing {path}");
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
