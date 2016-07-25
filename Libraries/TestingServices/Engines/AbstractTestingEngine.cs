//-----------------------------------------------------------------------
// <copyright file="AbstractTestingEngine.cs">
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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.Visualization;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# abstract testing engine.
    /// </summary>
    internal abstract class AbstractTestingEngine : ITestingEngine
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
        /// The P# test initialization method.
        /// </summary>
        internal MethodInfo TestInitMethod;

        /// <summary>
        /// The P# test dispose method.
        /// </summary>
        internal MethodInfo TestDisposeMethod;

        /// <summary>
        /// The P# test dispose method per iteration.
        /// </summary>
        internal MethodInfo TestIterationDisposeMethod;

        /// <summary>
        /// A P# test action.
        /// </summary>
        internal Action<PSharpRuntime> TestAction;

        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        protected ISchedulingStrategy Strategy;

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        protected CoverageInfo CoverageInfo;

        /// <summary>
        /// The program visualizer.
        /// </summary>
        protected IProgramVisualizer Visualizer;

        /// <summary>
        /// Set of callbacks to invoke at the end
        /// of each iteration.
        /// </summary>
        protected ISet<Action<int>> PerIterationCallbacks;

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        protected int PrintGuard;

        /// <summary>
        /// Has redirected console output.
        /// </summary>
        protected bool HasRedirectedConsoleOutput;

        /// <summary>
        /// The profiler.
        /// </summary>
        protected Profiler Profiler;

        /// <summary>
        /// The testing task cancellation token source.
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource;

        #endregion

        #region properties

        /// <summary>
        /// The latest bug report, if any.
        /// </summary>
        public string BugReport { get; protected set; }

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        public int NumOfFoundBugs { get; protected set; }

        /// <summary>
        /// Explored depth of scheduling decisions.
        /// </summary>
        public int ExploredDepth { get; protected set; }

        #endregion

        #region public API

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        /// <returns>ITestingEngine</returns>
        public abstract ITestingEngine Run();

        /// <summary>
        /// Stops the P# testing engine.
        /// </summary>
        public void Stop()
        {
            this.CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        public virtual void TryEmitTraces()
        {
            // No-op, must be implemented in subclass.
        }

        /// <summary>
        /// Registers a callback to invoke at the end
        /// of each iteration. The callback takes as
        /// a parameter an integer representing the
        /// current iteration.
        /// </summary>
        /// <param name="callback">Callback</param>
        public void RegisterPerIterationCallBack(Action<int> callback)
        {
            this.PerIterationCallbacks.Add(callback);
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        /// <returns>Report</returns>
        public abstract string Report();

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        protected AbstractTestingEngine(Configuration configuration)
        {
            this.Profiler = new Profiler();
            this.Configuration = configuration;

            this.PerIterationCallbacks = new HashSet<Action<int>>();

            try
            {
                this.Assembly = Assembly.LoadFrom(configuration.AssemblyToBeAnalyzed);
            }
            catch (FileNotFoundException ex)
            {
                IO.Error.ReportAndExit(ex.Message);
            }

            this.FindEntryPoint();
            this.TestInitMethod = FindTestMethod(typeof(TestInit));
            this.TestDisposeMethod = FindTestMethod(typeof(TestDispose));
            this.TestIterationDisposeMethod = FindTestMethod(typeof(TestIterationDispose));
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        protected AbstractTestingEngine(Configuration configuration, Assembly assembly)
        {
            this.Profiler = new Profiler();
            this.Configuration = configuration;
            this.PerIterationCallbacks = new HashSet<Action<int>>();
            this.Assembly = assembly;
            this.FindEntryPoint();
            this.TestInitMethod = FindTestMethod(typeof(TestInit));
            this.TestDisposeMethod = FindTestMethod(typeof(TestDispose));
            this.TestIterationDisposeMethod = FindTestMethod(typeof(TestIterationDispose));
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        protected AbstractTestingEngine(Configuration configuration,
            Action<PSharpRuntime> action)
        {
            this.Profiler = new Profiler();
            this.Configuration = configuration;
            this.PerIterationCallbacks = new HashSet<Action<int>>();
            this.TestAction = action;
            this.Initialize();
        }

        /// <summary>
        /// Initialized the testing engine.
        /// </summary>
        private void Initialize()
        {
            this.CancellationTokenSource = new CancellationTokenSource();

            this.BugReport = "";
            this.NumOfFoundBugs = 0;
            this.ExploredDepth = 0;
            this.PrintGuard = 1;

            if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Interactive)
            {
                this.Strategy = new InteractiveStrategy(this.Configuration);
                this.Configuration.SchedulingIterations = 1;
                this.Configuration.PerformFullExploration = false;
                this.Configuration.Verbose = 2;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Replay)
            {
                string[] scheduleDump = File.ReadAllLines(this.Configuration.ScheduleFile);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(this.Configuration, schedule);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Random)
            {
                this.Strategy = new RandomStrategy(this.Configuration);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom)
            {
                this.Strategy = new ProbabilisticRandomStrategy(this.Configuration,
                    this.Configuration.CoinFlipBound);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DFS)
            {
                this.Strategy = new DFSStrategy(this.Configuration);
                this.Configuration.PerformFullExploration = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.IDDFS)
            {
                this.Strategy = new IterativeDeepeningDFSStrategy(this.Configuration);
                this.Configuration.PerformFullExploration = false;
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
                this.Configuration.PerformFullExploration = false;
                this.Configuration.CacheProgramState = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                IO.Error.ReportAndExit("Portfolio testing strategy in only " +
                    "available in parallel testing.");
            }

            if (this.Configuration.EnableVisualization)
            {
                this.InitializeVisualizer();
            }

            if (this.Configuration.PrintTrace)
            {
                this.Configuration.SchedulingIterations = 1;
                this.Configuration.PerformFullExploration = false;
            }

            this.HasRedirectedConsoleOutput = false;
        }

        #endregion

        #region core methods

        /// <summary>
        /// Executes the specified testing task.
        /// </summary>
        protected void Execute(Task task)
        {
            if (this.Configuration.AttachDebugger)
            {
                System.Diagnostics.Debugger.Launch();
            }

            Task visualizationTask = null;
            if (this.Configuration.EnableVisualization)
            {
                visualizationTask = this.StartVisualizing();
            }

            if (this.Configuration.EnableDataRaceDetection)
            {
                this.CreateRuntimeTracesDirectory();
            }

            if (this.Configuration.Timeout > 0)
            {
                this.CancellationTokenSource.CancelAfter(
                    this.Configuration.Timeout * 1000);
            }

            this.Profiler.StartMeasuringExecutionTime();

            try
            {
                task.Start();
                task.Wait(this.CancellationTokenSource.Token);
                
                if (this.Configuration.EnableVisualization)
                {
                    visualizationTask.Wait(this.CancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                if (this.CancellationTokenSource.IsCancellationRequested)
                {
                    if (this.Configuration.TestingProcessId >= 0)
                    {
                        IO.Error.PrintLine("... Task " +
                            $"{this.Configuration.TestingProcessId} timed out.");
                    }
                    else
                    {
                        IO.Error.PrintLine("... Timed out.");
                        IO.Error.PrintLine(this.Report());
                        IO.Error.PrintLine(". Done");
                    }
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

                if (aex.InnerException is FileNotFoundException)
                {
                    IO.Error.ReportAndExit($"{aex.InnerException.Message}");
                }

                IO.Error.ReportAndExit("Exception thrown during testing. Please use " +
                    "/debug to print more information, and contact the developer team.");
            }
            finally
            {
                this.Profiler.StopMeasuringExecutionTime();

                if (!this.Configuration.KeepTemporaryFiles &&
                    this.Assembly != null)
                {
                    this.CleanTemporaryFiles();
                }
            }
        }

        /// <summary>
        /// Starts visualizing the P# program being tested,
        /// and returns the visualization task.
        /// </summary>
        /// <returns>Task</returns>
        protected Task StartVisualizing()
        {
            Task visualizationTask = this.Visualizer.StartAsync();
            return visualizationTask;
        }

        #endregion

        #region utility methods

        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        private void FindEntryPoint()
        {
            List<MethodInfo> testMethods = this.FindTestMethodsWithAttribute(typeof(Test));

            if (testMethods.Count == 0)
            {
                IO.Error.ReportAndExit("Cannot detect a P# test method. Use the " +
                    $"attribute '[{typeof(Test).FullName}]' to declare a test method.");
            }
            else if (testMethods.Count > 1)
            {
                IO.Error.ReportAndExit("Only one test method to the P# program can " +
                    $"be declared with the attribute '{typeof(Test).FullName}'. " +
                    $"'{testMethods.Count}' test methods were found instead.");
            }

            if (testMethods[0].ReturnType != typeof(void) ||
                testMethods[0].ContainsGenericParameters ||
                testMethods[0].IsAbstract || testMethods[0].IsVirtual ||
                testMethods[0].IsConstructor ||
                !testMethods[0].IsPublic || !testMethods[0].IsStatic ||
                testMethods[0].GetParameters().Length != 1 ||
                testMethods[0].GetParameters()[0].ParameterType != typeof(PSharpRuntime))
            {
                IO.Error.ReportAndExit("Incorrect test method declaration. Please " +
                    "declare the test method as follows:\n" +
                    $"  [{typeof(Test).FullName}] public static void " +
                    $"void {testMethods[0].Name}(PSharpRuntime runtime) {{ ... }}");
            }

            this.TestMethod = testMethods[0];
        }

        /// <summary>
        /// Finds the test method with the specified attribute.
        /// Returns null if no such method is found.
        /// </summary>
        private MethodInfo FindTestMethod(Type attribute)
        {
            List<MethodInfo> testMethods = this.FindTestMethodsWithAttribute(attribute);

            if (testMethods.Count == 0)
            {
                return null;
            }
            else if (testMethods.Count > 1)
            {
                IO.Error.ReportAndExit("Only one test method to the P# program can " +
                    $"be declared with the attribute '{attribute.FullName}'. " +
                    $"'{testMethods.Count}' test methods were found instead.");
            }

            if (testMethods[0].ReturnType != typeof(void) ||
                testMethods[0].ContainsGenericParameters ||
                testMethods[0].IsAbstract || testMethods[0].IsVirtual ||
                testMethods[0].IsConstructor ||
                !testMethods[0].IsPublic || !testMethods[0].IsStatic ||
                testMethods[0].GetParameters().Length != 0)
            {
                IO.Error.ReportAndExit("Incorrect test method declaration. Please " +
                    "declare the test method as follows:\n" +
                    $"  [{attribute.FullName}] public static " +
                    $"void {testMethods[0].Name}() {{ ... }}");
            }

            return testMethods[0];
        }

        /// <summary>
        /// Finds the test methods with the specified attribute.
        /// Returns an empty list if no such methods are found.
        /// </summary>
        /// <param name="attribute">Type</param>
        /// <returns>MethodInfos</returns>
        private List<MethodInfo> FindTestMethodsWithAttribute(Type attribute)
        {
            List<MethodInfo> testMethods = null;

            try
            {
                testMethods = this.Assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod)).
                    Where(m => m.GetCustomAttributes(attribute, false).Length > 0).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    ErrorReporter.Report(le.Message);
                }

                IO.Error.ReportAndExit($"Failed to load assembly '{this.Assembly.FullName}'");
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(ex.Message);
                IO.Error.ReportAndExit($"Failed to load assembly '{this.Assembly.FullName}'");
            }

            return testMethods;
        }

        /// <summary>
        /// Initializes the visualizer.
        /// </summary>
        private void InitializeVisualizer()
        {
            var name = Path.GetFileNameWithoutExtension(this.Assembly.Location);
            var directoryPath = this.GetOutputDirectory();
            var graphFile = directoryPath + name + ".dgml";
            var coverageFile = directoryPath + name + ".coverage.txt";
            this.Visualizer = new PSharpDgmlVisualizer(graphFile, coverageFile);
            this.CoverageInfo = this.Visualizer.CoverageInfo;
        }

        /// <summary>
        /// Returns the output directory.
        /// </summary>
        /// <returns>Path</returns>
        protected string GetOutputDirectory()
        {
            string directoryPath = Path.GetDirectoryName(this.Assembly.Location) +
                Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        /// <summary>
        /// Creates the runtime traces directory.
        /// </summary>
        protected void CreateRuntimeTracesDirectory()
        {
            string directoryPath = this.GetRuntimeTracesDirectory();
            Directory.CreateDirectory(directoryPath);
        }

        /// <summary>
        /// Returns the runtime traces directory.
        /// </summary>
        /// <returns>Path</returns>
        protected string GetRuntimeTracesDirectory()
        {
            return this.GetOutputDirectory() + Path.DirectorySeparatorChar +
                "RuntimeTraces" + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Cleans the temporary files.
        /// </summary>
        protected void CleanTemporaryFiles()
        {
            string directoryPath = this.GetRuntimeTracesDirectory();
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }

        /// <summary>
        /// Redirects the console output.
        /// </summary>
        /// <returns>StringWriter</returns>
        protected StringWriter RedirectConsoleOutput()
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            return sw;
        }

        /// <summary>
        /// Resets the console output.
        /// </summary>
        protected void ResetOutput()
        {
            var sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);
            this.HasRedirectedConsoleOutput = false;
        }

        #endregion
    }
}
