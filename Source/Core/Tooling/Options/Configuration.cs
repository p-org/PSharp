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
        internal static string SolutionFilePath = "";

        /// <summary>
        /// The output path.
        /// </summary>
        internal static string OutputFilePath = "";

        /// <summary>
        /// The name of the project to analyse.
        /// </summary>
        internal static string ProjectName = "";

        /// <summary>
        /// Skip the parsing stage of the compiler.
        /// </summary>
        internal static bool NoParsing = false;

        /// <summary>
        /// Skip the compilation stage of the compiler.
        /// </summary>
        internal static bool NoCompilation = false;

        /// <summary>
        /// Analysis timeout.
        /// </summary>
        internal static int AnalysisTimeout = 0;

        /// <summary>
        /// Verbosity level.
        /// </summary>
        internal static int Verbose = 1;

        /// <summary>
        /// Turn debugging for the specified component on.
        /// </summary>
        internal static DebugType Debug = DebugType.None;

        #endregion

        #region static analysis options

        /// <summary>
        /// Run the static analysis stage of the compiler.
        /// </summary>
        internal static bool RunStaticAnalysis = false;

        /// <summary>
        /// Report warnings if true.
        /// </summary>
        internal static bool ShowWarnings = false;

        /// <summary>
        /// Reports gives up information.
        /// </summary>
        internal static bool ShowGivesUpInformation = false;

        /// <summary>
        /// Reports program statistics.
        /// </summary>
        internal static bool ShowProgramStatistics = false;

        /// <summary>
        /// Reports runtime results for the whole execution.
        /// </summary>
        internal static bool ShowRuntimeResults = false;

        /// <summary>
        /// Reports runtime results for the dataflow analysis.
        /// </summary>
        internal static bool ShowDFARuntimeResults = false;

        /// <summary>
        /// Reports runtime results for the respects ownership analysis.
        /// </summary>
        internal static bool ShowROARuntimeResults = false;

        /// <summary>
        /// Perform the state transition analysis.
        /// </summary>
        internal static bool DoStateTransitionAnalysis = true;

        /// <summary>
        /// Analyse exception handling.
        /// </summary>
        internal static bool AnalyzeExceptionHandling = false;

        #endregion

        #region dynamic analysis options

        /// <summary>
        /// Run the dynamic analysis of the compiler.
        /// </summary>
        internal static bool RunDynamicAnalysis = false;

        /// <summary>
        /// The name of the assemblies to be analyzed for bugs.
        /// </summary>
        internal static List<string> AssembliesToBeAnalyzed = new List<string>();

        /// <summary>
        /// Scheduling strategy to use with the dynamic analyzer.
        /// </summary>
        internal static string SchedulingStrategy = "";

        /// <summary>
        /// Number of scheduling iterations.
        /// </summary>
        internal static int SchedulingIterations = 1;

        /// <summary>
        /// Systematic tester does not stop when it finds a bug.
        /// </summary>
        internal static bool FullExploration = false;

        /// <summary>
        /// Depth bound.
        /// </summary>
        internal static int DepthBound = 0;

        #endregion
    }
}
