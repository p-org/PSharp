using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Scheduling;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.PSharp.TestingServices.Tracing.TreeTrace;
using Microsoft.PSharp.TestingServices.Runtime;

namespace Microsoft.PSharp.TestingServices.Engines
{
    internal sealed class MinimizerEngine : AbstractTestingEngine
    {
        private bool HAX_IsTempBug;
        private EventTree minimalTree;
        private bool traceEditorReachedMinimum;

        /// <summary>
        /// Text describing an internal replay error.
        /// </summary>
        internal string InternalError { get; private set; }

        /// <summary>
        /// Creates a new P# minimizing engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>MinimizerEngine</returns>
        public static MinimizerEngine Create(Configuration configuration)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Minimize;
            return new MinimizerEngine(configuration);
        }

        /// <summary>
        /// Creates a new P# minimizing engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>MinimizerEngine</returns>
        public static MinimizerEngine Create(Configuration configuration, Assembly assembly)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Minimize;
            return new MinimizerEngine(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# minimizing engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>MinimizerEngine</returns>
        public static MinimizerEngine Create(Configuration configuration, Action<PSharpRuntime> action)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Minimize;
            return new MinimizerEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new P# minimizing engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <param name="trace">Reproducable trace</param>
        /// <returns>MinimizerEngine</returns>
        public static MinimizerEngine Create(Configuration configuration, Action<PSharpRuntime> action, string trace)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            configuration.ScheduleTrace = trace;
            return new MinimizerEngine(configuration, action);
        }

        private MinimizerEngine(Configuration configuration)
            : base(configuration)
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        private MinimizerEngine(Configuration configuration, Assembly assembly)
            : base(configuration, assembly)
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        private MinimizerEngine(Configuration configuration, Action<PSharpRuntime> action)
            : base(configuration, action)
        {

        }


        public override string Report()
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("... Reproduced {0} bug{1}.", base.TestReport.NumOfFoundBugs,
                base.TestReport.NumOfFoundBugs == 1 ? "" : "s");
            report.AppendLine();

            report.Append($"... Elapsed {base.Profiler.Results()} sec.");
            report.Append($"( t-krgov really should improve this report. It's not a bug though.)");

            return report.ToString();
        }

        public override ITestingEngine Run()
        {
            Task task = this.CreateBugMinimizingTask();
            base.Execute(task);
            return this;
        }

        private Task CreateBugMinimizingTask()
        {
            base.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} is " +
                $"using '{base.Configuration.SchedulingStrategy}' strategy.");

            Task task = new Task(() =>
            {
                traceEditorReachedMinimum = false;
                MinimizationStrategy typedStrategy = (base.Strategy as MinimizationStrategy);
                
                try
                {
                    if (base.TestInitMethod != null)
                    {
                        // Initializes the test state.
                        base.TestInitMethod.Invoke(null, new object[] { });
                    }

                    int maxIterations = base.Configuration.SchedulingIterations;

                    
                    //bool bugFoundEveryTime = true;
                    for (int i = 0; i < maxIterations; i++)
                    {
                        if (this.CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        // Runs a new testing iteration.
                        bool bugFoundThisIter = this.RunNextIteration(i);

                        //bugFoundEveryTime = bugFoundEveryTime && bugFoundThisIter;

                        // We need to replay + randomwalk till we're convinced OR till we show recovery.
                        string lrString = $"({ typedStrategy.traceEditor.currentWithHoldRangeStart},{ typedStrategy.traceEditor.currentWithHoldRangeEnd})";
                        bool minimizationIterationCanContinue = typedStrategy.PrepareForNextIteration();
                        Console.WriteLine($"Iteration {i} completed. bugFound={bugFoundThisIter}; HAX_IsTempBug={HAX_IsTempBug} ; TraceLength={typedStrategy.traceEditor.getGuideTree()?.totalOrdering.Count}; {lrString} ");


                        if (bugFoundThisIter)
                        {
                            minimalTree = typedStrategy.getBestTree();
                        }

                        if (!minimizationIterationCanContinue)
                        {
                            Console.WriteLine($"Completed run for <TODO-somedescription>; bugFound={bugFoundThisIter}");
                            
                            traceEditorReachedMinimum = true;
                            
                            break;
                            // Or, Do one more sweep
                        }
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
                finally
                {
                    
                    Logger.WriteLine($"<MinimizationEngine> ReachedMinimum:{traceEditorReachedMinimum}, " +
                        $"bestScheduleLength={minimalTree?.totalOrdering?.Count}");
                }
            }, base.CancellationTokenSource.Token);

            return task;
        }

        private bool RunNextIteration(int i)
        {
            // Runtime used to serialize and test the program.
            MinimizationRuntime runtime = null;

            // Logger used to intercept the program output if no custom logger
            // is installed and if verbosity is turned off.
            InMemoryLogger runtimeLogger = null;

            // Gets a handle to the standard output and error streams.
            var stdOut = Console.Out;
            var stdErr = Console.Error;

            bool foundBugInIter = false;
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
                    runtime = (MinimizationRuntime)base.TestRuntimeFactoryMethod.Invoke(null,
                        new object[] { base.Configuration, base.Strategy, base.Reporter });
                }
                else
                {
                    runtime = new MinimizationRuntime(base.Configuration, base.Strategy, base.Reporter);
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
                HAX_IsTempBug = false;
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

                this.InternalError = (base.Strategy as MinimizationStrategy).ErrorText;


                // Checks that no monitor is in a hot state at termination. Only
                // checked if no safety property violations have been found.
                HAX_IsTempBug = (runtime as MinimizationRuntime).HAX_Monitors[0].HAX_LivenessTemperature >= Configuration.LivenessTemperatureThreshold;

                if (!runtime.Scheduler.BugFound && this.InternalError.Length == 0)
                {
                    runtime.CheckNoMonitorInHotStateAtTermination();
                }

                if (runtime.Scheduler.BugFound && this.InternalError.Length == 0)
                {
                    base.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
                }

                (base.Strategy as MinimizationStrategy).recordResult(runtime.Scheduler.BugFound, runtime.ScheduleTrace);
                
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
                foundBugInIter = runtime.Scheduler.BugFound;
                // Cleans up the runtime.
                runtimeLogger?.Dispose();
                runtime?.Dispose();

            }
            return foundBugInIter;
        }

        #region otherstuff
        public override void TryEmitTraces(string directory, string file)
        {   
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("--is-mintrace").Append(Environment.NewLine);
            if (this.Strategy.IsFair())
            {
                stringBuilder.Append("--fair-scheduling").Append(Environment.NewLine);
            }

            if (base.Configuration.EnableCycleDetection)
            {
                stringBuilder.Append("--cycle-detection").Append(Environment.NewLine);
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
            
            stringBuilder.Append("--reached-minimum:" + traceEditorReachedMinimum).Append(Environment.NewLine);
            

            stringBuilder.Append(minimalTree.serialize());
            File.WriteAllText( directory+file+".mintrace" , stringBuilder.ToString());
        }

        #endregion
    }
}