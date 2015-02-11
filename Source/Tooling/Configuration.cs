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

namespace Microsoft.PSharp.Tooling
{
    public static class Configuration
    {
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
        public static bool SkipParsing = false;

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
        public static bool AnalyseExceptionHandling = false;
    }
}
