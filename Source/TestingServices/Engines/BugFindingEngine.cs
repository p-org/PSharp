// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
using Microsoft.PSharp.TestingServices.RaceDetection;
using Microsoft.PSharp.TestingServices.Runtime;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# bug-finding engine.
    /// </summary>
    internal sealed class BugFindingEngine : AbstractTestingEngine
    {
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
        internal string ReproducableTrace { get; private set; }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        public static BugFindingEngine Create(Configuration configuration, Action<IMachineRuntime> action)
        {
            return new BugFindingEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        internal static BugFindingEngine Create(Configuration configuration)
        {
            return new BugFindingEngine(configuration);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        internal static BugFindingEngine Create(Configuration configuration, Assembly assembly)
        {
            return new BugFindingEngine(configuration, assembly);
        }

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        public override ITestingEngine Run()
        {
            try
            {
                Task task = this.CreateBugFindingTask();
                this.Execute(task);
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} failed due to an internal error: {ex}");
                this.TestReport.InternalErrors.Add(ex.ToString());
            }

            return this;
        }

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        public override void TryEmitTraces(string directory, string file)
        {
            // Emits the human readable trace, if it exists.
            if (!string.IsNullOrEmpty(this.ReadableTrace))
            {
                string[] readableTraces = Directory.GetFiles(directory, file + "_*.txt").
                    Where(path => new Regex(@"^.*_[0-9]+.txt$").IsMatch(path)).ToArray();
                string readableTracePath = directory + file + "_" + readableTraces.Length + ".txt";

                this.Logger.WriteLine($"..... Writing {readableTracePath}");
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
                    this.Logger.WriteLine($"..... Writing {bugTracePath}");
                    serializer.WriteObject(stream, this.BugTrace);
                }
            }

            // Emits the reproducable trace, if it exists.
            if (!string.IsNullOrEmpty(this.ReproducableTrace))
            {
                string[] reproTraces = Directory.GetFiles(directory, file + "_*.schedule");
                string reproTracePath = directory + file + "_" + reproTraces.Length + ".schedule";

                this.Logger.WriteLine($"..... Writing {reproTracePath}");
                File.WriteAllText(reproTracePath, this.ReproducableTrace);
            }

            this.Logger.WriteLine($"... Elapsed {this.Profiler.Results()} sec.");
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        public override string Report()
        {
            return this.TestReport.GetText(this.Configuration, "...");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BugFindingEngine"/> class.
        /// </summary>
        private BugFindingEngine(Configuration configuration)
            : base(configuration)
        {
            if (this.Configuration.EnableDataRaceDetection)
            {
                // Create a reporter to monitor operations for race detection.
                this.Reporter = new RaceDetectionEngine(configuration, this.Logger, this.TestReport);
            }

            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BugFindingEngine"/> class.
        /// </summary>
        private BugFindingEngine(Configuration configuration, Assembly assembly)
            : base(configuration, assembly)
        {
            if (this.Configuration.EnableDataRaceDetection)
            {
                this.Reporter = new RaceDetectionEngine(configuration, this.Logger, this.TestReport);
            }

            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BugFindingEngine"/> class.
        /// </summary>
        private BugFindingEngine(Configuration configuration, Action<IMachineRuntime> action)
            : base(configuration, action)
        {
            if (this.Configuration.EnableDataRaceDetection)
            {
                this.Reporter = new RaceDetectionEngine(configuration, this.Logger, this.TestReport);
            }

            this.Initialize();
        }

        /// <summary>
        /// Initializes the bug-finding engine.
        /// </summary>
        private void Initialize()
        {
            this.ReadableTrace = string.Empty;
            this.ReproducableTrace = string.Empty;

            if (this.Configuration.EnableDataRaceDetection)
            {
                this.RegisterPerIterationCallBack((arg) => { this.Reporter.ClearAll(); });
            }
        }

        /// <summary>
        /// Creates a new bug-finding task.
        /// </summary>
        private Task CreateBugFindingTask()
        {
            string options = string.Empty;
            if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Random ||
                this.Configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom ||
                this.Configuration.SchedulingStrategy == SchedulingStrategy.PCT ||
                this.Configuration.SchedulingStrategy == SchedulingStrategy.FairPCT ||
                this.Configuration.SchedulingStrategy == SchedulingStrategy.RandomDelayBounding)
            {
                options = $" (seed:{this.Configuration.RandomSchedulingSeed})";
            }

            this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} is " +
                $"using '{this.Configuration.SchedulingStrategy}' strategy{options}.");

            Task task = new Task(
                () =>
                {
                    try
                    {
                        if (this.TestInitMethod != null)
                        {
                            // Initializes the test state.
                            this.TestInitMethod.Invoke(null, Array.Empty<object>());
                        }

                        int maxIterations = this.Configuration.SchedulingIterations;
                        for (int i = 0; i < maxIterations; i++)
                        {
                            if (this.CancellationTokenSource.IsCancellationRequested)
                            {
                                break;
                            }

                            // Runs a new testing iteration.
                            this.RunNextIteration(i);

                            if (!this.Configuration.PerformFullExploration && this.TestReport.NumOfFoundBugs > 0)
                            {
                                break;
                            }

                            if (!this.Strategy.PrepareForNextIteration())
                            {
                                break;
                            }

                            if (this.RandomNumberGenerator != null && this.Configuration.IncrementalSchedulingSeed)
                            {
                                // Increments the seed in the random number generator (if one is used), to
                                // capture the seed used by the scheduling strategy in the next iteration.
                                this.RandomNumberGenerator.Seed = this.RandomNumberGenerator.Seed + 1;
                            }

                            // Increases iterations if there is a specified timeout
                            // and the default iteration given.
                            if (this.Configuration.SchedulingIterations == 1 &&
                                this.Configuration.Timeout > 0)
                            {
                                maxIterations++;
                            }
                        }

                        if (this.TestDisposeMethod != null)
                        {
                            // Disposes the test state.
                            this.TestDisposeMethod.Invoke(null, Array.Empty<object>());
                        }
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (!(ex.InnerException is TaskCanceledException))
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                    }
                }, this.CancellationTokenSource.Token);

            return task;
        }

        /// <summary>
        /// Runs the next testing iteration.
        /// </summary>
        private void RunNextIteration(int iteration)
        {
            if (this.ShouldPrintIteration(iteration + 1))
            {
                this.Logger.WriteLine($"..... Iteration #{iteration + 1}");

                // Flush when logging to console.
                if (this.Logger is ConsoleLogger)
                {
                    Console.Out.Flush();
                }
            }

            // Runtime used to serialize and test the program in this iteration.
            TestingRuntime runtime = null;

            // Logger used to intercept the program output if no custom logger
            // is installed and if verbosity is turned off.
            InMemoryLogger runtimeLogger = null;

            // Gets a handle to the standard output and error streams.
            var stdOut = Console.Out;
            var stdErr = Console.Error;

            try
            {
                // Creates a new instance of the bug-finding runtime.
                if (this.TestRuntimeFactoryMethod != null)
                {
                    runtime = (TestingRuntime)this.TestRuntimeFactoryMethod.Invoke(
                        null,
                        new object[] { this.Configuration, this.Strategy, this.Reporter });
                }
                else
                {
                    runtime = new TestingRuntime(this.Configuration, this.Strategy, this.Reporter);
                }

                if (this.Configuration.EnableDataRaceDetection)
                {
                    this.Reporter.RegisterRuntime(runtime);
                }

                // If verbosity is turned off, then intercept the program log, and also dispose
                // the standard output and error streams.
                if (!this.Configuration.IsVerbose)
                {
                    runtimeLogger = new InMemoryLogger(true);
                    runtime.SetLogger(runtimeLogger);

                    var writer = new LogWriter(new DisposingLogger());
                    Console.SetOut(writer);
                    Console.SetError(writer);
                }

                // Runs the test inside the P# test-harness machine.
                runtime.RunTestHarness(this.TestMethod, this.TestAction);

                // Wait for the test to terminate.
                runtime.Wait();

                // Invokes user-provided cleanup for this iteration.
                if (this.TestIterationDisposeMethod != null)
                {
                    // Disposes the test state.
                    this.TestIterationDisposeMethod.Invoke(null, null);
                }

                // Invoke the per iteration callbacks, if any.
                foreach (var callback in this.PerIterationCallbacks)
                {
                    callback(iteration);
                }

                if (this.Configuration.RaceFound)
                {
                    runtime.Scheduler.NotifyAssertionFailure("Found a race", false);
                    foreach (var report in this.TestReport.BugReports)
                    {
                        runtime.Logger.WriteLine(report);
                    }
                }

                // Checks that no monitor is in a hot state at termination. Only
                // checked if no safety property violations have been found.
                if (!runtime.Scheduler.BugFound)
                {
                    runtime.CheckNoMonitorInHotStateAtTermination();
                }

                if (runtime.Scheduler.BugFound)
                {
                    this.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
                }

                this.GatherIterationStatistics(runtime);

                if (this.TestReport.NumOfFoundBugs > 0)
                {
                    if (runtimeLogger != null)
                    {
                        this.ReadableTrace = runtimeLogger.ToString();
                        this.ReadableTrace += this.TestReport.GetText(this.Configuration, "<StrategyLog>");
                    }

                    this.BugTrace = runtime.BugTrace;
                    this.ConstructReproducableTrace(runtime);
                }
            }
            finally
            {
                if (!this.Configuration.IsVerbose)
                {
                    // Restores the standard output and error streams.
                    Console.SetOut(stdOut);
                    Console.SetError(stdErr);
                }

                if (this.Configuration.PerformFullExploration && runtime.Scheduler.BugFound)
                {
                    this.Logger.WriteLine($"..... Iteration #{iteration + 1} " +
                        $"triggered bug #{this.TestReport.NumOfFoundBugs} " +
                        $"[task-{this.Configuration.TestingProcessId}]");
                }

                // Cleans up the runtime before the next iteration starts.
                runtimeLogger?.Dispose();
                runtime?.Dispose();
            }
        }

        /// <summary>
        /// Gathers the exploration strategy statistics for
        /// the latest testing iteration.
        /// </summary>
        private void GatherIterationStatistics(TestingRuntime runtime)
        {
            TestReport report = runtime.Scheduler.GetReport();
            report.CoverageInfo.Merge(runtime.CoverageInfo);
            this.TestReport.Merge(report);
        }

        /// <summary>
        /// Constructs a reproducable trace.
        /// </summary>
        private void ConstructReproducableTrace(TestingRuntime runtime)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (this.Strategy.IsFair())
            {
                stringBuilder.Append("--fair-scheduling").Append(Environment.NewLine);
            }

            if (this.Configuration.EnableCycleDetection)
            {
                stringBuilder.Append("--cycle-detection").Append(Environment.NewLine);
                stringBuilder.Append("--liveness-temperature-threshold:" +
                    this.Configuration.LivenessTemperatureThreshold).
                    Append(Environment.NewLine);
            }
            else
            {
                stringBuilder.Append("--liveness-temperature-threshold:" +
                    this.Configuration.LivenessTemperatureThreshold).
                    Append(Environment.NewLine);
            }

            if (!string.IsNullOrEmpty(this.Configuration.TestMethodName))
            {
                stringBuilder.Append("--test-method:" +
                    this.Configuration.TestMethodName).
                    Append(Environment.NewLine);
            }

            for (int idx = 0; idx < runtime.ScheduleTrace.Count; idx++)
            {
                ScheduleStep step = runtime.ScheduleTrace[idx];
                if (step.Type == ScheduleStepType.SchedulingChoice)
                {
                    stringBuilder.Append($"({step.ScheduledOperationId})");
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
        /// Returns true if the engine should print the current iteration.
        /// </summary>
        private bool ShouldPrintIteration(int iteration)
        {
            if (iteration > this.PrintGuard * 10)
            {
                var count = iteration.ToString().Length - 1;
                var guard = "1" + (count > 0 ? string.Concat(Enumerable.Repeat("0", count)) : string.Empty);
                this.PrintGuard = int.Parse(guard);
            }

            return iteration % this.PrintGuard == 0;
        }
    }
}
