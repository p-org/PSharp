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

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// The P# project configurations.
    /// </summary>
    [Serializable]
    public class Configuration
    {
        #region core options

        /// <summary>
        /// The path to the solution file.
        /// </summary>
        public string SolutionFilePath;

        /// <summary>
        /// The output path.
        /// </summary>
        public string OutputFilePath;

        /// <summary>
        /// The name of the project to analyze.
        /// </summary>
        public string ProjectName;

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        public int Timeout;

        /// <summary>
        /// Pause on assertion failure.
        /// </summary>
        internal bool PauseOnAssertionFailure;

        /// <summary>
        /// True if interoperation is enabled.
        /// </summary>
        public bool InteroperationEnabled;

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
        /// Runs the analysis stage of the compiler.
        /// </summary>
        public bool RunStaticAnalysis;

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
        /// Perform the state transition analysis.
        /// </summary>
        public bool DoStateTransitionAnalysis;

        #endregion

        #region bug finding options

        /// <summary>
        /// The assembly to be analyzed for bugs.
        /// </summary>
        public string AssemblyToBeAnalyzed;

        /// <summary>
        /// The trace file to be replayed.
        /// </summary>
        public string TraceFile;

        /// <summary>
        /// Scheduling strategy to use with the P# tester.
        /// </summary>
        public SchedulingStrategy SchedulingStrategy;

        /// <summary>
        /// Number of scheduling iterations.
        /// </summary>
        public int SchedulingIterations;

        /// <summary>
        /// Seed for random scheduling strategies.
        /// </summary>
        public int? RandomSchedulingSeed;

        /// <summary>
        /// If true, the P# tester performs a full exploration,
        /// and does not stop when it finds a bug.
        /// </summary>
        public bool PerformFullExploration;

        /// <summary>
        /// Depth bound, in terms of maximum scheduling steps
        /// to explore. By default it is 10000.
        /// </summary>
        public int DepthBound;

        /// <summary>
        /// Number of parallel bug-finding tasks.
        /// By default it is 1 task.
        /// </summary>
        public int ParallelBugFindingTasks;

        /// <summary>
        /// The testing scheduler process id.
        /// </summary>
        internal int TestingSchedulerProcessId;

        /// <summary>
        /// The unique testing process id.
        /// </summary>
        public int TestingProcessId;

        /// <summary>
        /// If true, then the P# tester will consider an execution
        /// that hits the depth bound as buggy.
        /// </summary>
        public bool ConsiderDepthBoundHitAsBug;

        /// <summary>
        /// The priority switch bound. By default it is 2.
        /// Used by priority-based schedulers.
        /// </summary>
        public int PrioritySwitchBound;

        /// <summary>
        /// Delay bound. By default it is 2.
        /// Used by delay-bounding schedulers.
        /// </summary>
        public int DelayBound;

        /// <summary>
        /// Coin-flip bound. By default it is 2.
        /// </summary>
        public int CoinFlipBound;

        /// <summary>
        /// Safety prefix bound. By default it is 0.
        /// </summary>
        public int SafetyPrefixBound;

        /// <summary>
        /// Attaches the debugger during trace replay.
        /// </summary>
        public bool AttachDebugger;

        /// <summary>
        /// If true, then the P# tester will try to schedule
        /// any intra-machine concurrency.
        /// </summary>
        public bool ScheduleIntraMachineConcurrency;

        /// <summary>
        /// If true, then the P# tester will perform state
        /// caching when checking liveness properties.
        /// </summary>
        public bool CacheProgramState;

        /// <summary>
        /// If true, then the P# tester will try to bound
        /// the interleavings between operations.
        /// </summary>
        public bool BoundOperations;

        /// <summary>
        /// If true, the runtime can reorder events in machine
        /// queues dynamically, depending on priorities.
        /// </summary>
        public bool DynamicEventQueuePrioritization;

        #endregion

        #region data race detection options

        /// <summary>
        /// Enables data-race detection during testing.
        /// </summary>
        public bool EnableDataRaceDetection;

        #endregion

        #region visualization options

        /// <summary>
        /// Enables visualization of a P# program.
        /// </summary>
        public bool EnableVisualization;

        #endregion

        #region remote manager options

        /// <summary>
        /// The unique container id.
        /// </summary>
        public int ContainerId;

        /// <summary>
        /// Number of containers.
        /// </summary>
        public int NumberOfContainers;

        /// <summary>
        /// The path to the P# application to run remotely.
        /// </summary>
        public string RemoteApplicationFilePath;

        #endregion

        #region diagnostics options
        
        /// <summary>
        /// Verbosity level.
        /// </summary>
        public int Verbose;

        /// <summary>
        /// Enables debugging.
        /// </summary>
        public bool EnableDebugging;

        /// <summary>
        /// Enables profiling.
        /// </summary>
        public bool EnableProfiling;

        /// <summary>
        /// Keeps the temporary files.
        /// </summary>
        public bool KeepTemporaryFiles;

        /// <summary>
        /// Redirects the testing console output.
        /// </summary>
        internal bool RedirectTestConsoleOutput;

        /// <summary>
        /// If true, then the P# tester will print the trace
        /// to a file, even if a bug is not found.
        /// </summary>
        public bool PrintTrace;

        /// <summary>
        /// If true, then the P# tester will not output the
        /// error trace to a file.
        /// </summary>
        internal bool SuppressTrace;

        /// <summary>
        /// If true, then P# will throw any internal exceptions.
        /// </summary>
        internal bool ThrowInternalExceptions;

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
            
            this.PauseOnAssertionFailure = false;
            this.InteroperationEnabled = true;

            this.CompilationTarget = CompilationTarget.Execution;
            this.OptimizationTarget = OptimizationTarget.Release;

            this.CustomCompilerPassAssemblyPaths = new HashSet<string>();

            this.RunStaticAnalysis = false;
            this.ShowControlFlowInformation = false;
            this.ShowDataFlowInformation = false;
            this.ShowFullDataFlowInformation = false;
            this.DoStateTransitionAnalysis = false;

            this.AssemblyToBeAnalyzed = "";
            this.TraceFile = "";

            this.SchedulingStrategy = SchedulingStrategy.Random;
            this.SchedulingIterations = 1;
            this.RandomSchedulingSeed = null;

            this.PerformFullExploration = false;
            this.DepthBound = 10000;
            this.ParallelBugFindingTasks = 1;
            this.TestingSchedulerProcessId = -1;
            this.TestingProcessId = -1;
            this.ConsiderDepthBoundHitAsBug = false;
            this.PrioritySwitchBound = 0;
            this.DelayBound = 0;
            this.CoinFlipBound = 0;
            this.SafetyPrefixBound = 0;

            this.AttachDebugger = false;
            this.ScheduleIntraMachineConcurrency = false;
            this.CacheProgramState = false;
            this.BoundOperations = false;
            this.DynamicEventQueuePrioritization = false;
            
            this.EnableDataRaceDetection = false;

            this.EnableVisualization = false;

            this.ContainerId = 0;
            this.NumberOfContainers = 1;
            this.RemoteApplicationFilePath = "";

            this.Verbose = 1;
            this.EnableDebugging = false;
            this.EnableProfiling = false;
            this.KeepTemporaryFiles = false;
            this.RedirectTestConsoleOutput = true;
            this.PrintTrace = false;
            this.SuppressTrace = false;
            this.ThrowInternalExceptions = false;
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
            IO.Debugging = isEnabled;
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

        #endregion
    }
}
