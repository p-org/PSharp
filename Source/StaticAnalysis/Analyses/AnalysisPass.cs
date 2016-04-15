//-----------------------------------------------------------------------
// <copyright file="AnalysisPass.cs">
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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Implementation of an abstract state-machine analysis pass.
    /// </summary>
    internal abstract class AnalysisPass
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        protected PSharpAnalysisContext AnalysisContext;

        /// <summary>
        /// Configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// The analysis pass profiler.
        /// </summary>
        protected Profiler Profiler;

        #endregion

        #region internal methods

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        internal abstract void Run();

        #endregion

        #region protected methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        protected AnalysisPass(PSharpAnalysisContext context, Configuration configuration)
        {
            this.Profiler = new Profiler();
            this.AnalysisContext = context;
            this.Configuration = configuration;
        }

        #endregion

        #region profiling methods

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected abstract void PrintProfilingResults();

        #endregion
    }
}
