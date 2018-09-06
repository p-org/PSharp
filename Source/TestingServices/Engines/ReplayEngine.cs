﻿// ------------------------------------------------------------------------------------------------
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
        /// <param name="configuration">Configuration</param>
        /// <returns>ReplayEngine</returns>
        public static ReplayEngine Create(Configuration configuration)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
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
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
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
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new P# replaying engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <param name="trace">Reproducable trace</param>
        /// <returns>ReplayEngine</returns>
        public static ReplayEngine Create(Configuration configuration, Action<PSharpRuntime> action, string trace)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            configuration.ScheduleTrace = trace;
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

        /// <summary>
        /// Creates a bug-reproducing task.
        /// </summary>
        /// <returns>Task</returns>
        private Task CreateBugReproducingTask()
        {
            Task task = new Task(() =>
            {
                // Runtime used to serialize and test the program.
                BugFindingRuntime runtime = null;

                // Logger used to intercept the program output if no custom logger
                // is installed and if verbosity is turned off.
                InMemoryLogger runtimeLogger = null;

                // Gets a handle to the standard output and error streams.
                var stdOut = Console.Out;
                var stdErr = Console.Error;

                try
                {
                    if (base.TestInitMethod != null)
                    {
                        // Initializes the test state.
                        base.TestInitMethod.Invoke(null, new object[] { });
                    }

                    // Creates a new instance of the bug-finding runtime.
                    if (base.TestRuntimeFactoryMethod != null)
                    {
                        runtime = (BugFindingRuntime)base.TestRuntimeFactoryMethod.Invoke(null,
                            new object[] { base.Configuration, base.Strategy, base.Reporter });
                    }
                    else
                    {
                        runtime = new BugFindingRuntime(base.Configuration, base.Strategy, base.Reporter);
                    }


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

                    this.InternalError = (base.Strategy as ReplayStrategy).ErrorText;

                    // Checks that no monitor is in a hot state at termination. Only
                    // checked if no safety property violations have been found.
                    if (!runtime.Scheduler.BugFound && this.InternalError.Length == 0)
                    {
                        runtime.AssertNoMonitorInHotStateAtTermination();
                    }

                    if (runtime.Scheduler.BugFound && this.InternalError.Length == 0)
                    {
                        base.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
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
                    if (base.Configuration.Verbose < 2)
                    {
                        // Restores the standard output and error streams.
                        Console.SetOut(stdOut);
                        Console.SetError(stdErr);
                    }

                    // Cleans up the runtime.
                    runtimeLogger?.Dispose();
                    runtime?.Dispose();
                }
            }, base.CancellationTokenSource.Token);

            return task;
        }
    }
}
