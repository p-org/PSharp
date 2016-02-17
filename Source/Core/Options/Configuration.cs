//-----------------------------------------------------------------------
// <copyright file="Configuration.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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

namespace Microsoft.PSharp.Utilities
{
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
        /// The name of the project to analyse.
        /// </summary>
        public string ProjectName;

        /// <summary>
        /// Verbosity level.
        /// </summary>
        public int Verbose;

        /// <summary>
        /// Timeout.
        /// </summary>
        public int Timeout;

        /// <summary>
        /// True if interoperation is enabled.
        /// </summary>
        public bool InteroperationEnabled;

        #endregion

        #region language service options

        /// <summary>
        /// Requested compilation targets.
        /// </summary>
        public HashSet<CompilationTarget> CompilationTargets;

        /// <summary>
        /// Run the analysis stage of the compiler.
        /// </summary>
        public bool RunStaticAnalysis;

        /// <summary>
        /// Reports gives up information.
        /// </summary>
        public bool ShowGivesUpInformation;

        /// <summary>
        /// Reports runtime results for the whole execution.
        /// </summary>
        public bool ShowRuntimeResults;

        /// <summary>
        /// Reports runtime results for the dataflow analysis.
        /// </summary>
        public bool ShowDFARuntimeResults;

        /// <summary>
        /// Reports runtime results for the respects ownership analysis.
        /// </summary>
        public bool ShowROARuntimeResults;

        /// <summary>
        /// Perform the state transition analysis.
        /// </summary>
        public bool DoStateTransitionAnalysis;

        /// <summary>
        /// Analyse exception handling.
        /// </summary>
        public bool AnalyzeExceptionHandling;

        #endregion

        #region bug finding options

        /// <summary>
        /// The assembly to be analyzed for bugs.
        /// </summary>
        public string AssemblyToBeAnalyzed;

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
        internal int? RandomSchedulingSeed;

        /// <summary>
        /// Redirects the console output.
        /// </summary>
        public bool RedirectConsoleOutput;

        /// <summary>
        /// If true, then the P# tester will print the trace
        /// to a file, even if a bug is not found.
        /// </summary>
        public bool PrintTrace;

        /// <summary>
        /// If true, then the P# tester will not output the
        /// error trace to a file.
        /// </summary>
        public bool SuppressTrace;

        /// <summary>
        /// Systematic tester does not stop when it finds a bug.
        /// </summary>
        public bool FullExploration;

        /// <summary>
        /// Depth bound. By default it is 1000.
        /// </summary>
        public int DepthBound;

        /// <summary>
        /// If true, then the P# tester will consider an execution
        /// that hits the depth bound as buggy.
        /// </summary>
        public bool ConsiderDepthBoundHitAsBug;

        /// <summary>
        /// The bug depth. By default it is 2.
        /// </summary>
        public int BugDepth;

        /// <summary>
        /// Delay bound. By default it is 2.
        /// </summary>
        public int DelayBound;

        /// <summary>
        /// Safety prefix bound. By default it is 0.
        /// </summary>
        public int SafetyPrefixBound;

        /// <summary>
        /// If true, then the P# tester will try to schedule
        /// any intra-machine concurrency.
        /// </summary>
        public bool ScheduleIntraMachineConcurrency;

        /// <summary>
        /// If true, then the P# tester will check if any liveness
        /// properties hold.
        /// </summary>
        public bool CheckLiveness;

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
        /// The path to the P# application to run in a
        /// distributed setting.
        /// </summary>
        public string ApplicationFilePath;

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

            this.Verbose = 1;
            this.Timeout = 0;

            this.InteroperationEnabled = true;

            this.CompilationTargets = new HashSet<CompilationTarget>();
            this.CompilationTargets.Add(CompilationTarget.Execution);

            this.RunStaticAnalysis = false;
            this.ShowGivesUpInformation = false;
            this.ShowRuntimeResults = false;
            this.ShowDFARuntimeResults = false;
            this.ShowROARuntimeResults = false;
            this.DoStateTransitionAnalysis = true;
            this.AnalyzeExceptionHandling = false;

            this.AssemblyToBeAnalyzed = "";

            this.SchedulingStrategy = SchedulingStrategy.Random;
            this.SchedulingIterations = 1;
            this.RandomSchedulingSeed = null;

            this.RedirectConsoleOutput = true;
            this.PrintTrace = false;
            this.SuppressTrace = false;

            this.FullExploration = false;
            this.DepthBound = 10000;
            this.ConsiderDepthBoundHitAsBug = false;
            this.BugDepth = 2;
            this.DelayBound = 2;
            this.SafetyPrefixBound = 0;

            this.ScheduleIntraMachineConcurrency = false;
            this.CheckLiveness = false;
            this.CacheProgramState = false;
            this.BoundOperations = false;
            this.DynamicEventQueuePrioritization = false;

            this.NumberOfContainers = 1;
            this.ContainerId = 0;
            this.ApplicationFilePath = "";
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
        /// <param name="level">Verbosity level</param>
        /// <returns>Configuration</returns>
        public Configuration WithDebuggingEnabled(bool value = true)
        {
            IO.Debugging = value;
            return this;
        }

        /// <summary>
        /// Updates the configuration with liveness checking enabled
        /// or disabled and returns it.
        /// </summary>
        /// <param name="value">Boolean</param>
        /// <returns>Configuration</returns>
        public Configuration WithLivenessCheckingEnabled(bool value = true)
        {
            this.CheckLiveness = value;
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
