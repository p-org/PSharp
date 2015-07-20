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
    internal static class Configuration
    {
        #region core options

        /// <summary>
        /// The path to the solution file.
        /// </summary>
        internal static string SolutionFilePath;

        /// <summary>
        /// The output path.
        /// </summary>
        internal static string OutputFilePath;

        /// <summary>
        /// The name of the project to analyse.
        /// </summary>
        internal static string ProjectName;

        /// <summary>
        /// Skip the parsing stage of the compiler.
        /// </summary>
        internal static bool NoParsing;

        /// <summary>
        /// Skip the compilation stage of the compiler.
        /// </summary>
        internal static bool NoCompilation;

        /// <summary>
        /// Analysis timeout.
        /// </summary>
        internal static int AnalysisTimeout;

        /// <summary>
        /// Verbosity level.
        /// </summary>
        internal static int Verbose;

        /// <summary>
        /// Turn debugging for the specified components on.
        /// </summary>
        internal static HashSet<DebugType> Debug;

        #endregion

        #region compilation options

        /// <summary>
        /// Uses the distributed runtime.
        /// </summary>
        internal static bool CompileForDistribution;

        #endregion

        #region static analysis options

        /// <summary>
        /// Run the static analysis stage of the compiler.
        /// </summary>
        internal static bool RunStaticAnalysis;

        /// <summary>
        /// Report warnings if true.
        /// </summary>
        internal static bool ShowWarnings;

        /// <summary>
        /// Reports gives up information.
        /// </summary>
        internal static bool ShowGivesUpInformation;

        /// <summary>
        /// Reports program statistics.
        /// </summary>
        internal static bool ShowProgramStatistics;

        /// <summary>
        /// Reports runtime results for the whole execution.
        /// </summary>
        internal static bool ShowRuntimeResults;

        /// <summary>
        /// Reports runtime results for the dataflow analysis.
        /// </summary>
        internal static bool ShowDFARuntimeResults;

        /// <summary>
        /// Reports runtime results for the respects ownership analysis.
        /// </summary>
        internal static bool ShowROARuntimeResults;

        /// <summary>
        /// Perform the state transition analysis.
        /// </summary>
        internal static bool DoStateTransitionAnalysis;

        /// <summary>
        /// Analyse exception handling.
        /// </summary>
        internal static bool AnalyzeExceptionHandling;

        #endregion

        #region dynamic analysis options

        /// <summary>
        /// Run the dynamic analysis of the compiler.
        /// </summary>
        internal static bool RunDynamicAnalysis;

        /// <summary>
        /// The name of the assemblies to be analyzed for bugs.
        /// </summary>
        internal static List<string> AssembliesToBeAnalyzed;

        /// <summary>
        /// Scheduling strategy to use with the dynamic analyzer.
        /// </summary>
        internal static string SchedulingStrategy;

        /// <summary>
        /// Number of scheduling iterations.
        /// </summary>
        internal static int SchedulingIterations;

        /// <summary>
        /// Systematic tester does not stop when it finds a bug.
        /// </summary>
        internal static bool FullExploration;

        /// <summary>
        /// Depth bound. By default it is 1000.
        /// </summary>
        internal static int DepthBound;

        /// <summary>
        /// If true, then the dynamic analyzer will check if
        /// any liveness properties hold.
        /// </summary>
        internal static bool CheckLiveness;

        /// <summary>
        /// If true, then the dynamic analyzer will export the trace
        /// that leads to a found error to a file.
        /// </summary>
        internal static bool ExportTrace;

        /// <summary>
        /// If true, then the dynamic analyzer will perform state
        /// caching when checking liveness properties.
        /// </summary>
        internal static bool CacheProgramState;

        #endregion

        #region remote options

        /// <summary>
        /// Number of containers.
        /// </summary>
        internal static int NumberOfContainers;

        /// <summary>
        /// The unique container id.
        /// </summary>
        internal static int ContainerId;

        /// <summary>
        /// The path to the P# application to run in a
        /// distributed setting.
        /// </summary>
        internal static string ApplicationFilePath;

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
            Configuration.Debug = new HashSet<DebugType>();

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
            Configuration.CheckLiveness = false;
            Configuration.ExportTrace = true;
            Configuration.CacheProgramState = true;

            Configuration.NumberOfContainers = 1;
            Configuration.ContainerId = 0;
            Configuration.ApplicationFilePath = "";
        }

        #endregion
    }
}
