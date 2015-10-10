//-----------------------------------------------------------------------
// <copyright file="LanguageServicesConfiguration.cs">
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

using Microsoft.PSharp.LanguageServices.Compilation;

namespace Microsoft.PSharp.Tooling
{
    public sealed class LanguageServicesConfiguration : Configuration
    {
        #region options

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

        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        private LanguageServicesConfiguration()
            : base()
        {
            this.CompilationTargets = new HashSet<CompilationTarget>();
            this.CompilationTargets.Add(CompilationTarget.Execution);
            this.CompilationTargets.Add(CompilationTarget.Testing);

            this.RunStaticAnalysis = false;
            this.ShowGivesUpInformation = false;
            this.ShowRuntimeResults = false;
            this.ShowDFARuntimeResults = false;
            this.ShowROARuntimeResults = false;
            this.DoStateTransitionAnalysis = true;
            this.AnalyzeExceptionHandling = false;
        }

        #endregion

        #region methods

        /// <summary>
        /// Creates a new language services configuration.
        /// </summary>
        /// <returns>LanguageServicesConfiguration</returns>
        public static LanguageServicesConfiguration Create()
        {
            return new LanguageServicesConfiguration();
        }

        #endregion
    }
}
