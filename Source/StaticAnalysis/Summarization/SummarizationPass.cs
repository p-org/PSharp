//-----------------------------------------------------------------------
// <copyright file="SummarizationPass.cs">
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
    /// This analysis pass computes the summaries for
    /// each machine of a P# program.
    /// </summary>
    internal sealed class SummarizationPass : AnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new summarization pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>SummarizationPass</returns>
        internal static SummarizationPass Create(PSharpAnalysisContext context)
        {
            return new SummarizationPass(context);
        }

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        internal override void Run()
        {
            // Starts profiling the summarization.
            if (this.AnalysisContext.Configuration.TimeStaticAnalysis)
            {
                this.Profiler.StartMeasuringExecutionTime();
            }
            
            foreach (var machine in this.AnalysisContext.Machines)
            {
                machine.Summarize();
            }

            // Stops profiling the summarization.
            if (this.AnalysisContext.Configuration.TimeStaticAnalysis)
            {
                this.Profiler.StopMeasuringExecutionTime();
                this.PrintProfilingResults();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private SummarizationPass(PSharpAnalysisContext context)
            : base(context)
        {

        }

        #endregion

        #region profiling methods

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            IO.PrintLine("... Data-flow analysis runtime: '" +
                base.Profiler.Results() + "' seconds.");
        }

        #endregion
    }
}
