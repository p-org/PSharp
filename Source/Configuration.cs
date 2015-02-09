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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSharp
{
    internal static class Configuration
    {
        /// <summary>
        /// The path to the solution file.
        /// </summary>
        internal static string SolutionFilePath = "";

        /// <summary>
        /// The name of the project to analyse.
        /// </summary>
        internal static string ProjectName = "";

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
        internal static bool AnalyseExceptionHandling = false;
    }
}
