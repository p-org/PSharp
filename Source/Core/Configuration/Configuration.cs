//-----------------------------------------------------------------------
// <copyright file="Configuration.cs">
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
using System.Runtime.Serialization;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# project configurations.
    /// </summary>
    [DataContract]
    [Serializable]
    public class Configuration
    {
        #region core options

        /// <summary>
        /// The path to the solution file.
        /// </summary>
        [DataMember]
        public string SolutionFilePath;

        /// <summary>
        /// The output path.
        /// </summary>
        [DataMember]
        public string OutputFilePath;

        /// <summary>
        /// The name of the project to analyze.
        /// </summary>
        [DataMember]
        public string ProjectName;

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        [DataMember]
        public int Timeout;

        #endregion

        #region language service options

        /// <summary>
        /// Requested compilation target.
        /// </summary>
        public CompilationTarget CompilationTarget;

        /// <summary>
        /// Requested optimization target.
        /// </summary>
        public OptimizationTarget OptimizationTarget;

        /// <summary>
        /// List of assembly paths to used-defined compiler passes.
        /// </summary>
        internal ISet<string> CustomCompilerPassAssemblyPaths;

        /// <summary>
        /// Enables the data flow analysis.
        /// </summary>
        public bool AnalyzeDataFlow;

        /// <summary>
        /// Enables the static data race analysis.
        /// </summary>
        public bool AnalyzeDataRaces;

        /// <summary>
        /// Perform the state transition analysis.
        /// </summary>
        public bool DoStateTransitionAnalysis;

        /// <summary>
        /// Reports the control-flow information.
        /// </summary>
        public bool ShowControlFlowInformation;

        /// <summary>
        /// Reports the data-flow information.
        /// </summary>
        public bool ShowDataFlowInformation;

        /// <summary>
        /// Reports the full data-flow information.
        /// </summary>
        public bool ShowFullDataFlowInformation;

        /// <summary>
        /// The version of CSharp to target in rewriting, if specified.
        /// </summary>
        public Version RewriteCSharpVersion;

        #endregion

        #region bug finding options

        /// <summary>
        /// The assembly to be analyzed for bugs.
        /// </summary>
        [DataMember]
        public string AssemblyToBeAnalyzed;

        /// <summary>
        /// Test method to be used.
        /// </summary>
        [DataMember]
        public string TestMethodName;

        /// <summary>
        /// Scheduling strategy to use with the P# tester.
        /// </summary>
        [DataMember]
        public SchedulingStrategy SchedulingStrategy;

        /// <summary>
        /// Reduction strategy to use with the P# tester.
        /// </summary>
        [DataMember]
        public ReductionStrategy ReductionStrategy;

        /// <summary>
        /// Number of scheduling iterations.
        /// </summary>
        [DataMember]
        public int SchedulingIterations;

        /// <summary>
        /// Seed for random scheduling strategies.
        /// </summary>
        [DataMember]
        public int? RandomSchedulingSeed;

        /// <summary>
        /// If true, the seed will increment in each
        /// testing iteration.
        /// </summary>
        [DataMember]
        public bool IncrementalSchedulingSeed;

        /// <summary>
        /// If true, the P# tester performs a full exploration,
        /// and does not stop when it finds a bug.
        /// </summary>
        [DataMember]
        public bool PerformFullExploration;

        /// <summary>
        /// The maximum scheduling steps to explore
        /// for fair schedulers.
        /// By default there is no bound.
        /// </summary>
        [DataMember]
        public int MaxFairSchedulingSteps;

        /// <summary>
        /// The maximum scheduling steps to explore
        /// for unfair schedulers.
        /// By default there is no bound.
        /// </summary>
        [DataMember]
        public int MaxUnfairSchedulingSteps;

        /// <summary>
        /// The maximum scheduling steps to explore
        /// for both fair and unfair schedulers.
        /// By default there is no bound.
        /// </summary>
        public int MaxSchedulingSteps
        {
            set
            {
                this.MaxUnfairSchedulingSteps = value;
                this.MaxFairSchedulingSteps = value;
            }
        }

        /// <summary>
        /// True if the user has explicitly set the
        /// fair scheduling steps bound.
        /// </summary>
        [DataMember]
        public bool UserExplicitlySetMaxFairSchedulingSteps;

        /// <summary>
        /// Number of parallel bug-finding tasks.
        /// By default it is 1 task.
        /// </summary>
        [DataMember]
        public uint ParallelBugFindingTasks;

        /// <summary>
        /// Runs this process as a parallel bug-finding task.
        /// </summary>
        [DataMember]
        public bool RunAsParallelBugFindingTask;

        /// <summary>
        /// The testing scheduler unique endpoint.
        /// </summary>
        [DataMember]
        public string TestingSchedulerEndPoint;

        /// <summary>
        /// The testing scheduler process id.
        /// </summary>
        [DataMember]
        public int TestingSchedulerProcessId;

        /// <summary>
        /// The unique testing process id.
        /// </summary>
        [DataMember]
        public uint TestingProcessId;

        /// <summary>
        /// If true, then the P# tester will consider an execution
        /// that hits the depth bound as buggy.
        /// </summary>
        [DataMember]
        public bool ConsiderDepthBoundHitAsBug;

        /// <summary>
        /// The priority switch bound. By default it is 2.
        /// Used by priority-based schedulers.
        /// </summary>
        [DataMember]
        public int PrioritySwitchBound;

        /// <summary>
        /// Delay bound. By default it is 2.
        /// Used by delay-bounding schedulers.
        /// </summary>
        [DataMember]
        public int DelayBound;

        /// <summary>
        /// Coin-flip bound. By default it is 2.
        /// </summary>
        [DataMember]
        public int CoinFlipBound;

        /// <summary>
        /// Safety prefix bound. By default it is 0.
        /// </summary>
        [DataMember]
        public int SafetyPrefixBound;

        /// <summary>
        /// Enables liveness checking during bug-finding.
        /// </summary>
        [DataMember]
        public bool EnableLivenessChecking;

        /// <summary>
        /// The liveness temperature threshold. If it is 0
        /// then it is disabled.
        /// </summary>
        [DataMember]
        public int LivenessTemperatureThreshold;

        /// <summary>
        /// Enables cycle-detection using state-caching
        /// for liveness checking.
        /// </summary>
        [DataMember]
        public bool EnableCycleDetection;

        /// <summary>
        /// If this option is enabled, then all <see cref="Machine.HashedState"/>
        /// and <see cref="Event.HashedState"/> state-hashing methods are used
        /// to improve the accurracy of state-caching for liveness checking.
        /// </summary>
        [DataMember]
        public bool EnableUserDefinedStateHashing;

        /// <summary>
        /// Enables (safety) monitors in the production runtime.
        /// </summary>
        [DataMember]
        public bool EnableMonitorsInProduction;

        /// <summary>
        /// Attaches the debugger during trace replay.
        /// </summary>
        [DataMember]
        public bool AttachDebugger;

        #endregion

        #region trace replay options

        /// <summary>
        /// The schedule file to be replayed.
        /// </summary>
        public string ScheduleFile;

        /// <summary>
        /// The schedule trace to be replayed.
        /// </summary>
        internal string ScheduleTrace;

        #endregion

        #region data race detection options

        /// <summary>
        /// Enables data-race detection during testing.
        /// </summary>
        [DataMember]
        public bool EnableDataRaceDetection;

        /// <summary>
        /// True if a race is found.
        /// TODO: Does not belong here.
        /// </summary>
        public bool RaceFound;

        /// <summary>
        /// Enables tracking line number information for reads and writes.
        /// </summary>
        public bool EnableReadWriteTracing = false;

        /// <summary>
        /// Enables race detector logging.
        /// </summary>
        public bool EnableRaceDetectorLogging = false;

        #endregion

        #region coverage options

        /// <summary>
        /// Enables code coverage reporting of a P# program.
        /// </summary>
        [DataMember]
        public bool ReportCodeCoverage;

        /// <summary>
        /// Enables activity coverage reporting of a P# program.
        /// </summary>
        [DataMember]
        public bool ReportActivityCoverage;

        /// <summary>
        /// Enables activity coverage debugging.
        /// </summary>
        public bool DebugActivityCoverage;

        /// <summary>
        /// Additional assembly specifications to instrument for code coverage, besides those in the
        /// dependency graph between <see cref="AssemblyToBeAnalyzed"/> and the Microsoft.PSharp DLLs.
        /// Key is filename, value is whether it is a list file (true) or a single file (false).
        /// </summary>
        public Dictionary<string, bool> AdditionalCodeCoverageAssemblies = new Dictionary<string, bool>();

        #endregion

        #region remote manager options

        /// <summary>
        /// The unique container id.
        /// </summary>
        [DataMember]
        public int ContainerId;

        /// <summary>
        /// Number of containers.
        /// </summary>
        [DataMember]
        public int NumberOfContainers;

        /// <summary>
        /// The path to the P# application to run remotely.
        /// </summary>
        [DataMember]
        public string RemoteApplicationFilePath;

        #endregion

        #region diagnostics options

        /// <summary>
        /// Verbosity level.
        /// </summary>
        [DataMember]
        public int Verbose;

        /// <summary>
        /// Shows warnings.
        /// </summary>
        [DataMember]
        public bool ShowWarnings;

        /// <summary>
        /// Enables debugging.
        /// </summary>
        [DataMember]
        public bool EnableDebugging;

        /// <summary>
        /// Enables profiling.
        /// </summary>
        [DataMember]
        public bool EnableProfiling;

        /// <summary>
        /// Keeps the temporary files.
        /// </summary>
        [DataMember]
        public bool KeepTemporaryFiles;

        #endregion

        #region tooling options

        /// <summary>
        /// Enables colored console output.
        /// </summary>
        public bool EnableColoredConsoleOutput;

        /// <summary>
        /// If true, then P# will throw any internal exceptions.
        /// </summary>
        internal bool ThrowInternalExceptions;

        /// <summary>
        /// If true, then environment exit will be disabled.
        /// </summary>
        internal bool DisableEnvironmentExit;

        #endregion

        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Configuration()
        {
            this.SolutionFilePath = "";
            this.OutputFilePath = "";
            this.ProjectName = "";
            
            this.Timeout = 0;

            this.CompilationTarget = CompilationTarget.Execution;
            this.OptimizationTarget = OptimizationTarget.Release;

            this.CustomCompilerPassAssemblyPaths = new HashSet<string>();

            this.AnalyzeDataFlow = false;
            this.AnalyzeDataRaces = false;
            this.DoStateTransitionAnalysis = false;
            this.ShowControlFlowInformation = false;
            this.ShowDataFlowInformation = false;
            this.ShowFullDataFlowInformation = false;
            this.RewriteCSharpVersion = new Version();

            this.AssemblyToBeAnalyzed = "";
            this.TestMethodName = "";

            this.SchedulingStrategy = SchedulingStrategy.Random;
            this.ReductionStrategy = ReductionStrategy.ForceSchedule;
            this.SchedulingIterations = 1;
            this.RandomSchedulingSeed = null;
            this.IncrementalSchedulingSeed = false;

            this.PerformFullExploration = false;
            this.MaxFairSchedulingSteps = 0;
            this.MaxUnfairSchedulingSteps = 0;
            this.UserExplicitlySetMaxFairSchedulingSteps = false;
            this.ParallelBugFindingTasks = 1;
            this.RunAsParallelBugFindingTask = false;
            this.TestingSchedulerEndPoint = Guid.NewGuid().ToString();
            this.TestingSchedulerProcessId = -1;
            this.TestingProcessId = 0;
            this.ConsiderDepthBoundHitAsBug = false;
            this.PrioritySwitchBound = 0;
            this.DelayBound = 0;
            this.CoinFlipBound = 0;
            this.SafetyPrefixBound = 0;

            this.EnableLivenessChecking = true;
            this.LivenessTemperatureThreshold = 0;
            this.EnableCycleDetection = false;
            this.EnableUserDefinedStateHashing = false;
            this.EnableMonitorsInProduction = false;

            this.AttachDebugger = false;

            this.ScheduleFile = "";
            this.ScheduleTrace = "";

            this.EnableDataRaceDetection = false;

            this.ReportCodeCoverage = false;
            this.ReportActivityCoverage = false;
            this.DebugActivityCoverage = false;

            this.ContainerId = 0;
            this.NumberOfContainers = 1;
            this.RemoteApplicationFilePath = "";

            this.Verbose = 1;
            this.ShowWarnings = false;
            this.EnableDebugging = false;
            this.EnableProfiling = false;
            this.KeepTemporaryFiles = false;

            this.EnableColoredConsoleOutput = false;
            this.ThrowInternalExceptions = false;
            this.DisableEnvironmentExit = true;
        }

        #endregion

        #region methods

        /// <summary>
        /// Creates a new configuration.
        /// </summary>
        /// <returns>Configuration</returns>
        public static Configuration Create()
        {
            return new Configuration();
        }

        /// <summary>
        /// Updates the configuration with verbose output enabled
        /// and returns it.
        /// </summary>
        /// <param name="level">Verbosity level</param>
        /// <returns>Configuration</returns>
        public Configuration WithVerbosityEnabled(int level)
        {
            this.Verbose = level;
            return this;
        }

        /// <summary>
        /// Updates the configuration with debugging information enabled
        /// or disabled and returns it.
        /// </summary>
        /// <param name="isEnabled">Is debugging enabled</param>
        /// <returns>Configuration</returns>
        public Configuration WithDebuggingEnabled(bool isEnabled = true)
        {
            Debug.IsEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the scheduling strategy
        /// and returns it.
        /// </summary>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <returns>Configuration</returns>
        public Configuration WithStrategy(SchedulingStrategy strategy)
        {
            this.SchedulingStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the number of iterations
        /// and returns it.
        /// </summary>
        /// <param name="iterations">Number of iterations</param>
        /// <returns>Configuration</returns>
        public Configuration WithNumberOfIterations(int iterations)
        {
            this.SchedulingIterations = iterations;
            return this;
        }

        /// <summary>
        /// Indicates whether the requested C# version is supported for for rewriting.
        /// </summary>
        /// <param name="major">The major version required</param>
        /// <param name="minor">The minor version required</param>
        /// <returns></returns>
        public bool IsRewriteCSharpVersion(int major, int minor)
        {
            // Return true if not set
            return this.RewriteCSharpVersion.Major == 0
                || this.RewriteCSharpVersion >= new Version(major, minor);
        }

        #endregion
    }
}
