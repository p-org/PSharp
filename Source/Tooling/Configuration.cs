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
        public static string SolutionFilePath = "";

        /// <summary>
        /// The output path.
        /// </summary>
        public static string OutputFilePath = "";

        /// <summary>
        /// The name of the project to analyse.
        /// </summary>
        public static string ProjectName = "";

        /// <summary>
        /// Skip the parsing stage of the compiler.
        /// </summary>
        public static bool NoParsing = false;

        /// <summary>
        /// Skip the compilation stage of the compiler.
        /// </summary>
        public static bool NoCompilation = false;

        /// <summary>
        /// Analysis timeout.
        /// </summary>
        public static int AnalysisTimeout = 0;

        /// <summary>
        /// Verbosity level.
        /// </summary>
        public static int Verbose = 1;

        #endregion

        #region static analysis options

        /// <summary>
        /// Run the static analysis stage of the compiler.
        /// </summary>
        public static bool RunStaticAnalysis = false;

        /// <summary>
        /// Report warnings if true.
        /// </summary>
        public static bool ShowWarnings = false;

        /// <summary>
        /// Reports gives up information.
        /// </summary>
        public static bool ShowGivesUpInformation = false;

        /// <summary>
        /// Reports program statistics.
        /// </summary>
        public static bool ShowProgramStatistics = false;

        /// <summary>
        /// Reports runtime results for the whole execution.
        /// </summary>
        public static bool ShowRuntimeResults = false;

        /// <summary>
        /// Reports runtime results for the dataflow analysis.
        /// </summary>
        public static bool ShowDFARuntimeResults = false;

        /// <summary>
        /// Reports runtime results for the respects ownership analysis.
        /// </summary>
        public static bool ShowROARuntimeResults = false;

        /// <summary>
        /// Perform the state transition analysis.
        /// </summary>
        public static bool DoStateTransitionAnalysis = true;

        /// <summary>
        /// Analyse exception handling.
        /// </summary>
        public static bool AnalyzeExceptionHandling = false;

        #endregion

        #region dynamic analysis options

        /// <summary>
        /// Run the dynamic analysis of the compiler.
        /// </summary>
        public static bool RunDynamicAnalysis = false;

        /// <summary>
        /// The name of the assemblies to be analyzed for bugs.
        /// </summary>
        public static List<string> AssembliesToBeAnalyzed = new List<string>();

        /// <summary>
        /// Scheduling strategy to use with the dynamic analyzer.
        /// </summary>
        public static string SchedulingStrategy = "";

        /// <summary>
        /// Number of scheduling iterations.
        /// </summary>
        public static int SchedulingIterations = 1;

        /// <summary>
        /// Fully explore schedules.
        /// </summary>
        public static bool FullExploration = false;

        #endregion
    }
}
