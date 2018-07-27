﻿//-----------------------------------------------------------------------
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
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# abstract testing engine.
    /// </summary>
    internal abstract class AbstractTestingEngine : ITestingEngine
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        internal Configuration Configuration;

        /// <summary>
        /// The P# assembly to analyze.
        /// </summary>
        internal Assembly Assembly;

        /// <summary>
        /// The assembly that provides the P# runtime to use
        /// during testing. If its null, the engine uses the
        /// default P# testing runtime.
        /// </summary>
        internal Assembly RuntimeAssembly;

        /// <summary>
        /// The P# test runtime factory method.
        /// </summary>
        internal MethodInfo TestRuntimeFactoryMethod;

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
        /// Set of callbacks to invoke at the end
        /// of each iteration.
        /// </summary>
        protected ISet<Action<int>> PerIterationCallbacks;

        /// <summary>
        /// The installed logger.
        /// </summary>
        protected IO.ILogger Logger;

        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        protected ISchedulingStrategy Strategy;

        /// <summary>
        /// Random number generator used by the scheduling strategies.
        /// </summary>
        protected IRandomNumberGenerator RandomNumberGenerator;

        /// <summary>
        /// The logger used by the scheduling strategies.
        /// </summary>
        protected SchedulingStrategyLogger SchedulingStrategyLogger;

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

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        protected int PrintGuard;

        /// <summary>
        /// Data structure containing information
        /// gathered during testing.
        /// </summary>
        public TestReport TestReport { get; set; }

        /// <summary>
        /// Interface for registering runtime operations.
        /// </summary>
        public IRegisterRuntimeOperation Reporter { get; protected set; }

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

            if (configuration.TestingRuntimeAssembly != "")
            {
                try
                {
                    this.RuntimeAssembly = Assembly.LoadFrom(configuration.TestingRuntimeAssembly);
                }
                catch (FileNotFoundException ex)
                {
                    Error.ReportAndExit(ex.Message);
                }

                this.FindRuntimeFactoryMethod();
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

            // Initializes scheduling strategy specific components.
            this.SchedulingStrategyLogger = new SchedulingStrategyLogger(this.Configuration);
            this.SetRandomNumberGenerator();

            this.TestReport = new TestReport(this.Configuration);
            this.CancellationTokenSource = new CancellationTokenSource();
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
                var scheduleDump = this.GetScheduleForReplay(out bool isFair);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(this.Configuration, schedule, isFair);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Random)
            {
                this.Strategy = new RandomStrategy(this.Configuration.MaxFairSchedulingSteps, this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.ProbabilisticRandom)
            {
                this.Strategy = new ProbabilisticRandomStrategy(this.Configuration.MaxFairSchedulingSteps,
                    this.Configuration.CoinFlipBound, this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.PCT)
            {
                this.Strategy = new PCTStrategy(this.Configuration.MaxUnfairSchedulingSteps, this.Configuration.PrioritySwitchBound,
                    this.SchedulingStrategyLogger, this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.FairPCT)
            {
                var prefixLength = this.Configuration.SafetyPrefixBound == 0 ?
                    this.Configuration.MaxUnfairSchedulingSteps : this.Configuration.SafetyPrefixBound;
                var prefixStrategy = new PCTStrategy(prefixLength, this.Configuration.PrioritySwitchBound,
                    this.SchedulingStrategyLogger, this.RandomNumberGenerator);
                var suffixStrategy = new RandomStrategy(this.Configuration.MaxFairSchedulingSteps, this.RandomNumberGenerator);
                this.Strategy = new ComboStrategy(prefixStrategy, suffixStrategy);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DFS)
            {
                this.Strategy = new DFSStrategy(this.Configuration.MaxUnfairSchedulingSteps, this.SchedulingStrategyLogger);
                this.Configuration.PerformFullExploration = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.IDDFS)
            {
                this.Strategy = new IterativeDeepeningDFSStrategy(this.Configuration.MaxUnfairSchedulingSteps,
                    this.SchedulingStrategyLogger);
                this.Configuration.PerformFullExploration = false;
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DPOR)
            {
                this.Strategy = new DPORStrategy(
                    new ContractAsserter(),
                    null,
                    -1,
                    this.Configuration.MaxUnfairSchedulingSteps);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.RDPOR)
            {
                this.Strategy = new DPORStrategy(
                    new ContractAsserter(), 
                    this.RandomNumberGenerator,
                    -1,
                    this.Configuration.MaxFairSchedulingSteps);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.DelayBounding)
            {
                this.Strategy = new ExhaustiveDelayBoundingStrategy(this.Configuration.MaxUnfairSchedulingSteps,
                    this.Configuration.DelayBound, this.SchedulingStrategyLogger, this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.RandomDelayBounding)
            {
                this.Strategy = new RandomDelayBoundingStrategy(this.Configuration.MaxUnfairSchedulingSteps,
                    this.Configuration.DelayBound, this.SchedulingStrategyLogger, this.RandomNumberGenerator);
            }
            else if (this.Configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                Error.ReportAndExit("Portfolio testing strategy is only " +
                    "available in parallel testing.");
            }

            if (this.Configuration.SchedulingStrategy != SchedulingStrategy.Replay &&
                this.Configuration.ScheduleFile.Length > 0)
            {
                var scheduleDump = this.GetScheduleForReplay(out bool isFair);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(this.Configuration, schedule, isFair, this.Strategy);
            }
        }

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

        /// <summary>
        /// Finds the testing runtime factory method, if one is provided.
        /// </summary>
        private void FindRuntimeFactoryMethod()
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            List<MethodInfo> runtimeFactoryMethods = this.FindTestMethodsWithAttribute(typeof(TestRuntimeCreate), flags, this.RuntimeAssembly);
            foreach (var x in runtimeFactoryMethods)
                Console.WriteLine(x.Name);

            if (runtimeFactoryMethods.Count == 0)
            {
                Error.ReportAndExit($"Failed to find a testing runtime factory method in the '{this.RuntimeAssembly.FullName}' assembly.");
            }
            else if (runtimeFactoryMethods.Count > 1)
            {
                Error.ReportAndExit("Only one testing runtime factory method can be declared with " +
                    $"the attribute '{typeof(TestRuntimeCreate).FullName}'. " +
                    $"'{runtimeFactoryMethods.Count}' factory methods were found instead.");
            }

            if (runtimeFactoryMethods[0].ReturnType != typeof(BugFindingRuntime) ||
                runtimeFactoryMethods[0].ContainsGenericParameters ||
                runtimeFactoryMethods[0].IsAbstract || runtimeFactoryMethods[0].IsVirtual ||
                runtimeFactoryMethods[0].IsConstructor ||
                runtimeFactoryMethods[0].IsPublic || !runtimeFactoryMethods[0].IsStatic ||
                runtimeFactoryMethods[0].GetParameters().Length != 3 ||
                runtimeFactoryMethods[0].GetParameters()[0].ParameterType != typeof(Configuration) ||
                runtimeFactoryMethods[0].GetParameters()[1].ParameterType != typeof(ISchedulingStrategy) ||
                runtimeFactoryMethods[0].GetParameters()[2].ParameterType != typeof(IRegisterRuntimeOperation))
            {
                Error.ReportAndExit("Incorrect test runtime factory method declaration. Please " +
                    "declare the method as follows:\n" +
                    $"  [{typeof(TestRuntimeCreate).FullName}] internal static BugFindingRuntime " +
                    $"{runtimeFactoryMethods[0].Name}(Configuration configuration, ISchedulingStrategy strategy, " +
                    "IRegisterRuntimeOperation reporter) {{ ... }}");
            }

            this.TestRuntimeFactoryMethod = runtimeFactoryMethods[0];
        }

        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        private void FindEntryPoint()
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            List<MethodInfo> testMethods = this.FindTestMethodsWithAttribute(typeof(Test), flags, this.Assembly);


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
                    $"{testMethod.Name}(PSharpRuntime runtime) {{ ... }}");
            }

            this.TestMethod = testMethod;
        }

        /// <summary>
        /// Finds the test method with the specified attribute.
        /// Returns null if no such method is found.
        /// </summary>
        private MethodInfo FindTestMethod(Type attribute)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            List<MethodInfo> testMethods = this.FindTestMethodsWithAttribute(attribute, flags, this.Assembly);

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
                    $"  [{attribute.FullName}] public static void " +
                    $"{testMethods[0].Name}() {{ ... }}");
            }

            return testMethods[0];
        }

        /// <summary>
        /// Finds the test methods with the specified attribute in the given assembly.
        /// Returns an empty list if no such methods are found.
        /// </summary>
        /// <param name="attribute">Type</param>
        /// <param name="bindingFlags">BindingFlags</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>MethodInfos</returns>
        private List<MethodInfo> FindTestMethodsWithAttribute(Type attribute, BindingFlags bindingFlags, Assembly assembly)
        {
            List<MethodInfo> testMethods = null;

            try
            {
                testMethods = assembly.GetTypes().SelectMany(t => t.GetMethods(bindingFlags)).
                    Where(m => m.GetCustomAttributes(attribute, false).Length > 0).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    this.ErrorReporter.WriteErrorLine(le.Message);
                }

                Error.ReportAndExit($"Failed to load assembly '{assembly.FullName}'");
            }
            catch (Exception ex)
            {
                this.ErrorReporter.WriteErrorLine(ex.Message);
                Error.ReportAndExit($"Failed to load assembly '{assembly.FullName}'");
            }

            return testMethods;
        }

        /// <summary>
        /// Returns the schedule to replay.
        /// </summary>
        /// <param name="isFair">Is strategy used during replay fair.</param>
        /// <returns>Schedule</returns>
        private string[] GetScheduleForReplay(out bool isFair)
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

            isFair = false;
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
                else if (line.Equals("--cycle-detection"))
                {
                    this.Configuration.EnableCycleDetection = true;
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

            return scheduleDump;
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
            return this.GetOutputDirectory() + Path.DirectorySeparatorChar;
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
        /// Installs the specified <see cref="IO.ILogger"/>.
        /// </summary>
        /// <param name="logger">ILogger</param>
        public void SetLogger(IO.ILogger logger)
        {
            if (logger == null)
            {
                throw new InvalidOperationException("Cannot install a null logger.");
            }

            this.Logger.Dispose();
            this.Logger = logger;
            this.ErrorReporter.Logger = logger;
        }

        /// <summary>
        /// Sets the random number generator to be used by the scheduling strategy.
        /// </summary>
        private void SetRandomNumberGenerator()
        {
            int seed = Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.RandomNumberGenerator = new DefaultRandomNumberGenerator(seed);
        }
    }
}
