//-----------------------------------------------------------------------
// <copyright file="TestingEngine.cs">
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

using Microsoft.PSharp.SystematicTesting.Scheduling;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.Visualization;

namespace Microsoft.PSharp.SystematicTesting
{
    /// <summary>
    /// The P# testing engine.
    /// </summary>
    public sealed class TestingEngine
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        internal Configuration Configuration;

        /// <summary>
        /// The P# assembly to analyze.
        /// </summary>
        internal Assembly Assembly;

        /// <summary>
        /// A P# test method.
        /// </summary>
        internal MethodInfo TestMethod;

        /// <summary>
        /// A P# test action.
        /// </summary>
        internal Action<PSharpRuntime> TestAction;

        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        private ISchedulingStrategy Strategy;

        /// <summary>
        /// The program visualizer.
        /// </summary>
        private IProgramVisualizer Visualizer;

        /// <summary>
        /// Explored schedules so far.
        /// </summary>
        private int ExploredSchedules;

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        private int PrintGuard;

        /// <summary>
        /// Has redirected console output.
        /// </summary>
        private bool HasRedirectedConsoleOutput;

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        public int NumOfFoundBugs { get; private set; }

        /// <summary>
        /// Explored depth of scheduling decisions.
        /// </summary>
        public int ExploredDepth { get; private set; }

        /// <summary>
        /// The latest bug report, if any.
        /// </summary>
        public string BugReport { get; private set; }

        /// <summary>
        /// The profiler.
        /// </summary>
        private Profiler Profiler;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new P# testing engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>TestingEngine</returns>
        public static TestingEngine Create(Configuration configuration, Action<PSharpRuntime> action)
        {
            return new TestingEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new P# testing engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>TestingEngine</returns>
        internal static TestingEngine Create(Configuration configuration, Assembly assembly)
        {
            return new TestingEngine(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# testing engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        /// <returns>TestingEngine</returns>
        internal static TestingEngine Create(Configuration configuration, string assemblyName)
        {
            return new TestingEngine(configuration, assemblyName);
        }

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        /// <returns>TestingEngine</returns>
        public TestingEngine Run()
        {
            this.FindBugs();
            this.Report();
            return this;
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        private TestingEngine(Configuration configuration, Action<PSharpRuntime> action)
        {
            this.Profiler = new Profiler();
            this.Configuration = configuration;
            this.TestAction = action;
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        private TestingEngine(Configuration configuration, Assembly assembly)
        {
            this.Profiler = new Profiler();
            this.Configuration = configuration;
            this.Assembly = assembly;
            this.FindEntryPoint();
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        private TestingEngine(Configuration configuration, string assemblyName)
        {
            this.Profiler = new Profiler();
            this.Configuration = configuration;

            try
            {
                this.Assembly = Assembly.LoadFrom(assemblyName);
            }
            catch (FileNotFoundException ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
            }

            this.FindEntryPoint();
            this.Initialize();
        }

        #endregion

        #region core methods

        /// <summary>
        /// Initialized the testing engine.
        /// </summary>
        private void Initialize()
        {
            this.NumOfFoundBugs = 0;
            this.BugReport = "";
            this.ExploredDepth = 0;
            this.ExploredSchedules = 0;
            this.PrintGuard = 1;

            if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Interactive)
            {
                this.Strategy = new InteractiveStrategy(this.Configuration);
                this.Configuration.SchedulingIterations = 1;
                this.Configuration.FullExploration = false;
                this.Configuration.Verbose = 2;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Random)
            {
                this.Strategy = new RandomStrategy(this.Configuration);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.RandomCoin)
            {
                this.Strategy = new RandomCoinStrategy(this.Configuration,
                    this.Configuration.CoinFlipBound);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DFS)
            {
                this.Strategy = new DFSStrategy(this.Configuration);
                this.Configuration.FullExploration = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.IDDFS)
            {
                this.Strategy = new IterativeDeepeningDFSStrategy(this.Configuration);
                this.Configuration.FullExploration = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DelayBounding)
            {
                this.Strategy = new ExhaustiveDelayBoundingStrategy(this.Configuration,
                    this.Configuration.DelayBound);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.RandomDelayBounding)
            {
                this.Strategy = new RandomDelayBoundingStrategy(this.Configuration,
                    this.Configuration.DelayBound);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.PCT)
            {
                this.Strategy = new PCTStrategy(this.Configuration, this.Configuration.PrioritySwitchBound);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.RandomOperationBounding)
            {
                this.Strategy = new RandomOperationBoundingStrategy(this.Configuration);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.PrioritizedOperationBounding)
            {
                this.Strategy = new PrioritizedOperationBoundingStrategy(this.Configuration,
                    this.Configuration.PrioritySwitchBound);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.MaceMC)
            {
                this.Strategy = new MaceMCStrategy(this.Configuration);
                this.Configuration.FullExploration = false;
                this.Configuration.CacheProgramState = false;
            }

            if (this.Configuration.EnableVisualization)
            {
                this.Visualizer = new PSharpProgramVisualizer();
            }

            this.HasRedirectedConsoleOutput = false;
        }

        /// <summary>
        /// Explores the P# program for bugs.
        /// </summary>
        private void FindBugs()
        {
            IO.PrintLine("... Using '{0}' strategy", this.Configuration.SchedulingStrategy);

            Task visualizationTask = null;
            if (this.Configuration.EnableVisualization)
            {
                visualizationTask = this.StartVisualizing();
            }

            Task task = new Task(() =>
            {
                for (int i = 0; i < this.Configuration.SchedulingIterations; i++)
                {
                    if (this.ShouldPrintIteration(i + 1))
                    {
                        IO.PrintLine("..... Iteration #{0}", i + 1);
                    }

                    var runtime = new PSharpBugFindingRuntime(this.Configuration,
                        this.Strategy, this.Visualizer);

                    StringWriter sw = null;
                    if (this.Configuration.RedirectTestConsoleOutput &&
                        this.Configuration.Verbose < 2)
                    {
                        sw = this.RedirectConsoleOutput();
                        this.HasRedirectedConsoleOutput = true;
                    }

                    // Start the test.
                    if (this.TestAction != null)
                    {
                        this.TestAction(runtime);
                    }
                    else
                    {
                        try
                        {
                            this.TestMethod.Invoke(null, new object[] { runtime });
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

                    if (this.Configuration.EnableVisualization)
                    {
                        this.Visualizer.Refresh();
                    }

                    // Checks for any liveness property violations. Requires
                    // that the program has terminated and no safety property
                    // violations have been found.
                    if (!runtime.BugFinder.BugFound)
                    {
                        runtime.LivenessChecker.CheckLivenessAtTermination();
                    }

                    if (this.HasRedirectedConsoleOutput)
                    {
                        this.ResetOutput();
                    }

                    this.ExploredSchedules++;
                    this.ExploredDepth = runtime.BugFinder.ExploredSteps;

                    if (runtime.BugFinder.BugFound)
                    {
                        this.NumOfFoundBugs++;
                        this.BugReport = runtime.BugFinder.BugReport;
                    }
                    else
                    {
                        this.BugReport = "";
                    }

                    if (this.Strategy.HasFinished())
                    {
                        break;
                    }

                    this.Strategy.ConfigureNextIteration();

                    if (!this.Configuration.FullExploration && this.NumOfFoundBugs > 0)
                    {
                        if (sw != null && !this.Configuration.SuppressTrace)
                        {
                            this.PrintTrace(sw);
                        }
                        
                        break;
                    }
                    else if (sw != null && this.Configuration.PrintTrace)
                    {
                        this.PrintTrace(sw);
                    }
                }
            });

            this.Profiler.StartMeasuringExecutionTime();
            task.Start();

            try
            {
                if (this.Configuration.Timeout > 0)
                {
                    task.Wait(this.Configuration.Timeout * 1000);
                }
                else
                {
                    task.Wait();
                }

                if (this.Configuration.EnableVisualization)
                {
                    visualizationTask.Wait();
                }
            }
            catch (AggregateException aex)
            {
                if (this.HasRedirectedConsoleOutput)
                {
                    this.ResetOutput();
                }

                aex.Handle((ex) =>
                {
                    IO.Debug(ex.Message);
                    IO.Debug(ex.StackTrace);
                    return true;
                });

                if (this.Configuration.ThrowInternalExceptions)
                {
                    throw aex;
                }

                ErrorReporter.ReportAndExit("Exception thrown during systematic testing. Please " +
                    "use /debug to print more information, and contact the developer team.");
            }
            finally
            {
                this.Profiler.StopMeasuringExecutionTime();
            }
        }

        /// <summary>
        /// Starts visualizing the P# program being tested,
        /// and returns the visualization task.
        /// </summary>
        /// <returns>Task</returns>
        private Task StartVisualizing()
        {
            Task visualizationTask = this.Visualizer.StartAsync();
            return visualizationTask;
        }

        /// <summary>
        /// Reports the testing results.
        /// </summary>
        private void Report()
        {
            IO.PrintLine("... Found {0} bug{1}.", this.NumOfFoundBugs,
                this.NumOfFoundBugs == 1 ? "" : "s");
            IO.PrintLine("... Explored {0} {1} schedule{2}.", this.ExploredSchedules,
                this.Strategy.HasFinished() ? "(all)" : "",
                this.ExploredSchedules == 1 ? "" : "s");

            if (this.ExploredSchedules > 0)
            {
                IO.PrintLine("... Found {0} % buggy schedules.",
                    (this.NumOfFoundBugs * 100 / this.ExploredSchedules));
                IO.PrintLine("... Instrumented {0} scheduling point{1} (on last iteration).",
                    this.ExploredDepth, this.ExploredDepth == 1 ? "" : "s");
            }

            if (this.Configuration.DepthBound > 0)
            {
                IO.PrintLine("... Used depth bound of {0}.", this.Configuration.DepthBound);
            }

            IO.PrintLine("... Elapsed {0} sec.", this.Profiler.Results());
        }

        /// <summary>
        /// Writes the trace in an output file.
        /// </summary>
        /// <param name="sw">StringWriter</param>
        private void PrintTrace(StringWriter sw)
        {
            var name = Path.GetFileNameWithoutExtension(this.Assembly.Location);
            var directory = Path.GetDirectoryName(this.Assembly.Location) +
                Path.DirectorySeparatorChar + "traces" + Path.DirectorySeparatorChar;

            Directory.CreateDirectory(directory);

            var traces = Directory.GetFiles(directory, name + "*.txt");
            var path = directory + name + "_" + traces.Length + ".txt";

            IO.PrintLine("... Writing {0}", path);
            File.WriteAllText(path, sw.ToString());
        }

        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        private void FindEntryPoint()
        {
            List<MethodInfo> testMethods = null;

            try
            {
                testMethods = this.Assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod)).
                    Where(m => m.GetCustomAttributes(typeof(Test), false).Length > 0).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    ErrorReporter.Report(le.Message);
                }

                ErrorReporter.ReportAndExit("Failed to load assembly '{0}'", this.Assembly.FullName);
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(ex.Message);
                ErrorReporter.ReportAndExit("Failed to load assembly '{0}'", this.Assembly.FullName);
            }

            if (testMethods.Count == 0)
            {
                ErrorReporter.ReportAndExit("Cannot detect a P# test method. " +
                    "Use the attribute [Test] to declare a test method.");
            }
            else if (testMethods.Count > 1)
            {
                ErrorReporter.ReportAndExit("Only one test method to the P# program can be declared. " +
                    "{0} test methods were found instead.", testMethods.Count);
            }

            if (testMethods[0].ReturnType != typeof(void) ||
                testMethods[0].ContainsGenericParameters ||
                testMethods[0].IsAbstract || testMethods[0].IsVirtual ||
                testMethods[0].IsConstructor ||
                !testMethods[0].IsPublic || !testMethods[0].IsStatic ||
                testMethods[0].GetParameters().Length != 1 ||
                testMethods[0].GetParameters()[0].ParameterType != typeof(PSharpRuntime))
            {
                ErrorReporter.ReportAndExit("Incorrect test method declaration. Please " +
                    "declare the test method as follows:\n" +
                    "  [Test] public static void TestCase(PSharpRuntime runtime) { ... }");
            }

            this.TestMethod = testMethods[0];
        }

        #endregion

        #region helper API

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

        /// <summary>
        /// Redirects the console output.
        /// </summary>
        /// <returns>StringWriter</returns>
        private StringWriter RedirectConsoleOutput()
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            return sw;
        }

        /// <summary>
        /// Resets the console output.
        /// </summary>
        private void ResetOutput()
        {
            var sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);
            this.HasRedirectedConsoleOutput = false;
        }

        #endregion
    }
}
