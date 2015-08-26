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

namespace Microsoft.PSharp.Tooling
{
    public class LanguageServicesConfiguration : Configuration
    {
        #region options

        /// <summary>
        /// Compiles for testing.
        /// </summary>
        public bool CompileForTesting;

        /// <summary>
        /// Compiles for testing.
        /// </summary>
        public bool CompileForLivenessChecking;

        /// <summary>
        /// Compiles for distributed execution.
        /// </summary>
        public bool CompileForDistribution;

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
        public LanguageServicesConfiguration()
            : base()
        {
            this.CompileForTesting = false;
            this.CompileForLivenessChecking = false;
            this.CompileForDistribution = false;

            this.RunStaticAnalysis = false;
            this.ShowGivesUpInformation = false;
            this.ShowRuntimeResults = false;
            this.ShowDFARuntimeResults = false;
            this.ShowROARuntimeResults = false;
            this.DoStateTransitionAnalysis = true;
            this.AnalyzeExceptionHandling = false;
        }

        #endregion
    }
}
