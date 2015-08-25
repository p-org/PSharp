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

using System.Collections.Generic;

namespace Microsoft.PSharp.Tooling
{
    public static class Configuration
    {
        #region core options

        /// <summary>
        /// The path to the solution file.
        /// </summary>
        public static string SolutionFilePath;

        /// <summary>
        /// The output path.
        /// </summary>
        public static string OutputFilePath;

        /// <summary>
        /// The name of the project to analyse.
        /// </summary>
        public static string ProjectName;

        /// <summary>
        /// Skip the parsing stage of the compiler.
        /// </summary>
        public static bool NoParsing;

        /// <summary>
        /// Skip the compilation stage of the compiler.
        /// </summary>
        public static bool NoCompilation;

        /// <summary>
        /// Analysis timeout.
        /// </summary>
        public static int AnalysisTimeout;

        /// <summary>
        /// Verbosity level.
        /// </summary>
        public static int Verbose;

        /// <summary>
        /// Redirects the console output.
        /// </summary>
        public static bool RedirectConsoleOutput;

        /// <summary>
        /// Turn logging on.
        /// </summary>
        public static bool Logging;

        /// <summary>
        /// Turn debugging for the specified components on.
        /// </summary>
        public static HashSet<DebugType> Debugging;

        #endregion

        #region compilation options

        /// <summary>
        /// Uses the distributed runtime.
        /// </summary>
        public static bool CompileForDistribution;

        #endregion

        #region static analysis options

        /// <summary>
        /// Run the static analysis stage of the compiler.
        /// </summary>
        public static bool RunStaticAnalysis;

        /// <summary>
        /// Report warnings if true.
        /// </summary>
        public static bool ShowWarnings;

        /// <summary>
        /// Reports gives up information.
        /// </summary>
        public static bool ShowGivesUpInformation;

        /// <summary>
        /// Reports program statistics.
        /// </summary>
        public static bool ShowProgramStatistics;

        /// <summary>
        /// Reports runtime results for the whole execution.
        /// </summary>
        public static bool ShowRuntimeResults;

        /// <summary>
        /// Reports runtime results for the dataflow analysis.
        /// </summary>
        public static bool ShowDFARuntimeResults;

        /// <summary>
        /// Reports runtime results for the respects ownership analysis.
        /// </summary>
        public static bool ShowROARuntimeResults;

        /// <summary>
        /// Perform the state transition analysis.
        /// </summary>
        public static bool DoStateTransitionAnalysis;

        /// <summary>
        /// Analyse exception handling.
        /// </summary>
        public static bool AnalyzeExceptionHandling;

        #endregion

        #region dynamic analysis options

        /// <summary>
        /// Run the dynamic analysis of the compiler.
        /// </summary>
        public static bool RunDynamicAnalysis;

        /// <summary>
        /// The name of the assemblies to be analyzed for bugs.
        /// </summary>
        public static List<string> AssembliesToBeAnalyzed;

        /// <summary>
        /// Scheduling strategy to use with the dynamic analyzer.
        /// </summary>
        public static string SchedulingStrategy;

        /// <summary>
        /// Number of scheduling iterations.
        /// </summary>
        public static int SchedulingIterations;

        /// <summary>
        /// Systematic tester does not stop when it finds a bug.
        /// </summary>
        public static bool FullExploration;

        /// <summary>
        /// Depth bound. By default it is 1000.
        /// </summary>
        public static int DepthBound;

        /// <summary>
        /// Safety prefix bound. By default it is 0.
        /// </summary>
        public static int SafetyPrefixBound;

        /// <summary>
        /// If true, then the dynamic analyzer will try to schedule
        /// any intra-machine concurrency.
        /// </summary>
        public static bool ScheduleIntraMachineConcurrency;

        /// <summary>
        /// If true, then the dynamic analyzer will check if
        /// any liveness properties hold.
        /// </summary>
        public static bool CheckLiveness;

        /// <summary>
        /// If true, then the dynamic analyzer will print the trace
        /// to a file, even if a bug is not found.
        /// </summary>
        public static bool PrintTrace;

        /// <summary>
        /// If true, then the dynamic analyzer will supress the trace
        /// that leads to a found error to a file.
        /// </summary>
        public static bool SuppressTrace;

        /// <summary>
        /// If true, then the dynamic analyzer will perform state
        /// caching when checking liveness properties.
        /// </summary>
        public static bool CacheProgramState;

        #endregion

        #region remote options

        /// <summary>
        /// Number of containers.
        /// </summary>
        public static int NumberOfContainers;

        /// <summary>
        /// The unique container id.
        /// </summary>
        public static int ContainerId;

        /// <summary>
        /// The path to the P# application to run in a
        /// distributed setting.
        /// </summary>
        public static string ApplicationFilePath;

        #endregion

        #region constructor

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Configuration()
        {
            Configuration.SolutionFilePath = "";
            Configuration.OutputFilePath = "";
            Configuration.ProjectName = "";
            Configuration.NoParsing = false;
            Configuration.NoCompilation = false;
            Configuration.AnalysisTimeout = 0;
            Configuration.Verbose = 1;
            Configuration.RedirectConsoleOutput = true;
            Configuration.Logging = false;
            Configuration.Debugging = new HashSet<DebugType>();

            Configuration.CompileForDistribution = false;

            Configuration.RunStaticAnalysis = false;
            Configuration.ShowWarnings = false;
            Configuration.ShowGivesUpInformation = false;
            Configuration.ShowProgramStatistics = false;
            Configuration.ShowRuntimeResults = false;
            Configuration.ShowDFARuntimeResults = false;
            Configuration.ShowROARuntimeResults = false;
            Configuration.DoStateTransitionAnalysis = true;
            Configuration.AnalyzeExceptionHandling = false;

            Configuration.RunDynamicAnalysis = false;
            Configuration.AssembliesToBeAnalyzed = new List<string>();
            Configuration.SchedulingStrategy = "";
            Configuration.SchedulingIterations = 1;
            Configuration.FullExploration = false;
            Configuration.DepthBound = 10000;
            Configuration.SafetyPrefixBound = 0;
            Configuration.ScheduleIntraMachineConcurrency = false;
            Configuration.CheckLiveness = false;
            Configuration.PrintTrace = false;
            Configuration.SuppressTrace = false;
            Configuration.CacheProgramState = true;

            Configuration.NumberOfContainers = 1;
            Configuration.ContainerId = 0;
            Configuration.ApplicationFilePath = "";
        }

        #endregion
    }
}
