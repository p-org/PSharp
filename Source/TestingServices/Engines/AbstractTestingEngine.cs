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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

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
        /// Set of callbacks to invoke at the end
        /// of each iteration.
        /// </summary>
        protected ISet<Action<int>> PerIterationCallbacks;

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        protected int PrintGuard;

        /// <summary>
        /// The installed logger.
        /// </summary>
        protected ILogger Logger;

        /// <summary>
        /// The error reporter.
        /// </summary>
        protected ErrorReporter ErrorReporter;

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
        /// Data structure containing information
        /// gathered during testing.
        /// </summary>
        public TestReport TestReport { get; set; }

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
        /// <param name="directory">Directory name</param>
        /// <param name="file">File name</param>
        public virtual void TryEmitTraces(string directory, string file)
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
            this.Configuration = configuration;
            this.PerIterationCallbacks = new HashSet<Action<int>>();

            try
            {
                this.Assembly = Assembly.LoadFrom(configuration.AssemblyToBeAnalyzed);
            }
            catch (FileNotFoundException ex)
            {
                Error.ReportAndExit(ex.Message);
            }

            this.Initialize();

            this.FindEntryPoint();
            this.TestInitMethod = FindTestMethod(typeof(TestInit));
            this.TestDisposeMethod = FindTestMethod(typeof(TestDispose));
            this.TestIterationDisposeMethod = FindTestMethod(typeof(TestIterationDispose));
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        protected AbstractTestingEngine(Configuration configuration, Assembly assembly)
        {
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
        protected AbstractTestingEngine(Configuration configuration, Action<PSharpRuntime> action)
        {
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
            this.Logger = new ConsoleLogger();
            this.ErrorReporter = new ErrorReporter(this.Configuration, this.Logger);
            this.Profiler = new Profiler();

            this.CancellationTokenSource = new CancellationTokenSource();

            this.TestReport = new TestReport(this.Configuration);

            this.PrintGuard = 1;

            if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Interactive)
            {
                this.Strategy = new InteractiveStrategy(this.Configuration, this.Logger);
                this.Configuration.SchedulingIterations = 1;
                this.Configuration.PerformFullExploration = false;
                this.Configuration.Verbose = 2;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Replay)
            {
                string[] scheduleDump;
                if (this.Configuration.ScheduleTrace.Length > 0)
                {
                    scheduleDump = this.Configuration.ScheduleTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                }
                else
                {
                    scheduleDump = File.ReadAllLines(this.Configuration.ScheduleFile);
                }

                bool isFair = false;

                foreach (var line in scheduleDump)
                {
                    if (!line.StartsWith("--"))
                    {
                        break;
                    }

                    if (line.Equals("--fair-scheduling"))
                    {
                        isFair = true;
                    }
                    else if (line.Equals("--state-caching"))
                    {
                        this.Configuration.EnableProgramStateCaching = true;
                    }
                    else if (line.StartsWith("--liveness-temperature-threshold:"))
                    {
                        this.Configuration.LivenessTemperatureThreshold =
                            Int32.Parse(line.Substring("--liveness-temperature-threshold:".Length));
                    }
                    else if (line.StartsWith("--test-method:"))
                    {
                        this.Configuration.TestMethodName =
                            line.Substring("--test-method:".Length);
                    }
                }

                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(this.Configuration, schedule, isFair);
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
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.FairPCT)
            {
                var strategy1 = new PCTStrategy(this.Configuration, this.Configuration.PrioritySwitchBound);
                var strategy2 = new RandomStrategy(this.Configuration);
                this.Strategy = new ComboStrategy(this.Configuration, strategy1, strategy2);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.MaceMC)
            {
                this.Strategy = new MaceMCStrategy(this.Configuration);
                this.Configuration.PerformFullExploration = false;
                this.Configuration.EnableProgramStateCaching = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                Error.ReportAndExit("Portfolio testing strategy in only " +
                    "available in parallel testing.");
            }
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
            }
            catch (OperationCanceledException)
            {
                if (this.CancellationTokenSource.IsCancellationRequested)
                {
                    this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} timed out.");
                }
            }
            catch (AggregateException aex)
            {
                aex.Handle((Func<Exception, bool>)((ex) =>
                {
                    Debug.WriteLine((string)ex.Message);
                    Debug.WriteLine((string)ex.StackTrace);
                    return true;
                }));

                if (aex.InnerException is FileNotFoundException)
                {
                    Error.ReportAndExit($"{aex.InnerException.Message}");
                }

                Error.ReportAndExit("Exception thrown during testing outside the context of a " +
                    "machine, possibly in a test method. Please use " +
                    "/debug /v:2 to print more information.");
            }
            finally
            {
                this.Profiler.StopMeasuringExecutionTime();

                //if (!this.Configuration.KeepTemporaryFiles &&
                //    this.Assembly != null)
                //{
                //    this.CleanTemporaryFiles();
                //}
            }
        }

        #endregion

        #region utility methods

        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        private void FindEntryPoint()
        {
            List<MethodInfo> testMethods = this.FindTestMethodsWithAttribute(typeof(Test));

            // Filter by test method name
            var filteredTestMethods = testMethods
                .FindAll(mi => string.Format("{0}.{1}", mi.DeclaringType.FullName, mi.Name)
                .EndsWith(Configuration.TestMethodName));

            if (filteredTestMethods.Count == 0)
            {
                if (testMethods.Count > 0)
                {
                    var msg = "Cannot detect a P# test method with name " + Configuration.TestMethodName +
                        ". Possible options are: " + Environment.NewLine;
                    foreach (var mi in testMethods)
                    {
                        msg += string.Format("{0}.{1}{2}", mi.DeclaringType.FullName, mi.Name, Environment.NewLine);
                    }

                    Error.ReportAndExit(msg);
                }
                else
                {
                    Error.ReportAndExit("Cannot detect a P# test method. Use the " +
                        $"attribute '[{typeof(Test).FullName}]' to declare a test method.");
                }
            }
            else if (filteredTestMethods.Count > 1)
            {
                var msg = "Only one test method to the P# program can " +
                    $"be declared with the attribute '{typeof(Test).FullName}'. " +
                    $"'{testMethods.Count}' test methods were found instead. Provide " +
                    $"/method flag to qualify the test method name you wish to use. " +
                    "Possible options are: " + Environment.NewLine;

                foreach (var mi in testMethods)
                {
                    msg += string.Format("{0}.{1}{2}", mi.DeclaringType.FullName, mi.Name, Environment.NewLine);
                }

                Error.ReportAndExit(msg);
            }

            var testMethod = filteredTestMethods[0];

            if (testMethod.ReturnType != typeof(void) ||
                testMethod.ContainsGenericParameters ||
                testMethod.IsAbstract || testMethod.IsVirtual ||
                testMethod.IsConstructor ||
                !testMethod.IsPublic || !testMethod.IsStatic ||
                testMethod.GetParameters().Length != 1 ||
                testMethod.GetParameters()[0].ParameterType != typeof(PSharpRuntime))
            {
                Error.ReportAndExit("Incorrect test method declaration. Please " +
                    "declare the test method as follows:\n" +
                    $"  [{typeof(Test).FullName}] public static void " +
                    $"void {testMethod.Name}(PSharpRuntime runtime) {{ ... }}");
            }

            this.TestMethod = testMethod;
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
                Error.ReportAndExit("Only one test method to the P# program can " +
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
                Error.ReportAndExit("Incorrect test method declaration. Please " +
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
                    this.ErrorReporter.WriteErrorLine(le.Message);
                }

                Error.ReportAndExit($"Failed to load assembly '{this.Assembly.FullName}'");
            }
            catch (Exception ex)
            {
                this.ErrorReporter.WriteErrorLine(ex.Message);
                Error.ReportAndExit($"Failed to load assembly '{this.Assembly.FullName}'");
            }

            return testMethods;
        }

        /// <summary>
        /// Returns (and creates if it does not exist) the output directory.
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

        #endregion

        #region logging

        /// <summary>
        /// Installs the specified <see cref="ILogger"/>.
        /// </summary>
        /// <param name="logger">ILogger</param>
        public void SetLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new InvalidOperationException("Cannot install a null logger.");
            }

            this.Logger.Dispose();
            this.Logger = logger;
            this.ErrorReporter.Logger = logger;
        }

        #endregion
    }
}
