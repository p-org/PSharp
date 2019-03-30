// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# replay engine.
    /// </summary>
    internal sealed class ReplayEngine : AbstractTestingEngine
    {
        /// <summary>
        /// Text describing an internal replay error.
        /// </summary>
        internal string InternalError { get; private set; }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration);
        }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration, Assembly assembly)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration, Action<PSharpRuntime> action)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration, Action<PSharpRuntime> action, string trace)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            configuration.ScheduleTrace = trace;
            return new ReplayEngine(configuration, action);
        }

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        public override ITestingEngine Run()
        {
            Task task = this.CreateBugReproducingTask();
            this.Execute(task);
            return this;
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        public override string Report()
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("... Reproduced {0} bug{1}.", this.TestReport.NumOfFoundBugs,
                this.TestReport.NumOfFoundBugs == 1 ? string.Empty : "s");
            report.AppendLine();

            report.Append($"... Elapsed {this.Profiler.Results()} sec.");

            return report.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayEngine"/> class.
        /// </summary>
        private ReplayEngine(Configuration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayEngine"/> class.
        /// </summary>
        private ReplayEngine(Configuration configuration, Assembly assembly)
            : base(configuration, assembly)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayEngine"/> class.
        /// </summary>
        private ReplayEngine(Configuration configuration, Action<PSharpRuntime> action)
            : base(configuration, action)
        {
        }

        private Task CreateBugReproducingTask()
        {
            Task task = new Task(
                () =>
                {
                    // Runtime used to serialize and test the program.
                    TestingRuntime runtime = null;

                    // Logger used to intercept the program output if no custom logger
                    // is installed and if verbosity is turned off.
                    InMemoryLogger runtimeLogger = null;

                    // Gets a handle to the standard output and error streams.
                    var stdOut = Console.Out;
                    var stdErr = Console.Error;

                    try
                    {
                        if (this.TestInitMethod != null)
                        {
                            // Initializes the test state.
                            this.TestInitMethod.Invoke(null, Array.Empty<object>());
                        }

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

                        // If verbosity is turned off, then intercept the program log, and also redirect
                        // the standard output and error streams into the runtime logger.
                        if (this.Configuration.Verbose < 2)
                        {
                            runtimeLogger = new InMemoryLogger();
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
                            this.TestIterationDisposeMethod.Invoke(null, Array.Empty<object>());
                        }

                        // Invokes user-provided cleanup for all iterations.
                        if (this.TestDisposeMethod != null)
                        {
                            // Disposes the test state.
                            this.TestDisposeMethod.Invoke(null, Array.Empty<object>());
                        }

                        this.InternalError = (this.Strategy as ReplayStrategy).ErrorText;

                        // Checks that no monitor is in a hot state at termination. Only
                        // checked if no safety property violations have been found.
                        if (!runtime.Scheduler.BugFound && this.InternalError.Length == 0)
                        {
                            runtime.CheckNoMonitorInHotStateAtTermination();
                        }

                        if (runtime.Scheduler.BugFound && this.InternalError.Length == 0)
                        {
                            this.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
                        }

                        TestReport report = runtime.Scheduler.GetReport();
                        report.CoverageInfo.Merge(runtime.CoverageInfo);
                        this.TestReport.Merge(report);
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (!(ex.InnerException is TaskCanceledException))
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                    }
                    finally
                    {
                        if (this.Configuration.Verbose < 2)
                        {
                            // Restores the standard output and error streams.
                            Console.SetOut(stdOut);
                            Console.SetError(stdErr);
                        }

                        // Cleans up the runtime.
                        runtimeLogger?.Dispose();
                        runtime?.Dispose();
                    }
                }, this.CancellationTokenSource.Token);

            return task;
        }
    }
}
