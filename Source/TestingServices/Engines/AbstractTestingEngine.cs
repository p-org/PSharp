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
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Exploration;
using Microsoft.PSharp.TestingServices.Scheduling;
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
        /// A P# test action.
        /// </summary>
        internal Action<PSharpRuntime> TestAction;

        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        protected ISchedulingStrategy Strategy;

        /// <summary>
        /// The program visualizer.
        /// </summary>
        protected IProgramVisualizer Visualizer;

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
        /// Reports the testing results.
        /// </summary>
        public abstract void Report();

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        protected AbstractTestingEngine(Configuration configuration, Action<PSharpRuntime> action)
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
        protected AbstractTestingEngine(Configuration configuration, Assembly assembly)
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
        protected AbstractTestingEngine(Configuration configuration, string assemblyName)
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

        /// <summary>
        /// Initialized the testing engine.
        /// </summary>
        private void Initialize()
        {
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
                string[] traceDump = File.ReadAllLines(this.Configuration.TraceFile);
                Trace trace = new Trace(traceDump);
                this.Strategy = new ReplayStrategy(this.Configuration, trace);
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

            if (this.Configuration.EnableVisualization)
            {
                this.Visualizer = new PSharpProgramVisualizer();
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
            Task visualizationTask = null;
            if (this.Configuration.EnableVisualization)
            {
                visualizationTask = this.StartVisualizing();
            }

            if (this.Configuration.AttachDebugger)
            {
                System.Diagnostics.Debugger.Launch();
            }

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

                ErrorReporter.ReportAndExit("Exception thrown during testing. Please use " +
                    "/debug to print more information, and contact the developer team.");
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

                ErrorReporter.ReportAndExit($"Failed to load assembly '{this.Assembly.FullName}'");
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(ex.Message);
                ErrorReporter.ReportAndExit($"Failed to load assembly '{this.Assembly.FullName}'");
            }

            if (testMethods.Count == 0)
            {
                ErrorReporter.ReportAndExit("Cannot detect a P# test method. " +
                    "Use the attribute [Test] to declare a test method.");
            }
            else if (testMethods.Count > 1)
            {
                ErrorReporter.ReportAndExit("Only one test method to the P# program can be declared. " +
                    $"{testMethods.Count} test methods were found instead.");
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
