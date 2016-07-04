//-----------------------------------------------------------------------
// <copyright file="StaticAnalysisProcess.cs">
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

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.StaticAnalysis;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# static analysis process.
    /// </summary>
    internal sealed class StaticAnalysisProcess
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# static analysis process.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>StaticAnalysisProcess</returns>
        public static StaticAnalysisProcess Create(CompilationContext context)
        {
            return new StaticAnalysisProcess(context);
        }

        /// <summary>
        /// Starts the P# static analysis process.
        /// </summary>
        public void Start()
        {
            IO.PrintLine(". Analyzing");

            // Creates and runs a P# static analysis engine.
            StaticAnalysisEngine.Create(this.CompilationContext).Run();

            // Prints error statistics and profiling results.
            AnalysisErrorReporter.PrintStats();
            AnalysisErrorReporter.ResetStats();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private StaticAnalysisProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion
    }
}
