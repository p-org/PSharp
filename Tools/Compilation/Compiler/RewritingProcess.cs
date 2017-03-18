//-----------------------------------------------------------------------
// <copyright file="RewritingProcess.cs">
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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# rewriting process.
    /// </summary>
    internal sealed class RewritingProcess
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# rewriting process.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>RewritingProcess</returns>
        public static RewritingProcess Create(CompilationContext context)
        {
            return new RewritingProcess(context);
        }

        /// <summary>
        /// Starts the P# parsing process.
        /// </summary>
        public void Start()
        {
            Output.WriteLine(". Rewriting");

            try
            {
                // Creates and runs a P# rewriting engine.
                RewritingEngine.Create(this.CompilationContext).Run();
            }
            catch (RewritingException ex)
            {
                Error.ReportAndExit(ex.Message);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private RewritingProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion
    }
}
