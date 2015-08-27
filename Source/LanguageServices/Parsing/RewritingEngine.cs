//-----------------------------------------------------------------------
// <copyright file="RewritingEngine.cs">
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

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// A P# rewriting engine.
    /// </summary>
    public sealed class RewritingEngine
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        #endregion

        #region public API

        /// <summary>
        /// Creates a P# rewriting engine.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>RewritingEngine</returns>
        public static RewritingEngine Create(CompilationContext context)
        {
            return new RewritingEngine(context);
        }

        /// <summary>
        /// Runs the P# rewriting engine.
        /// </summary>
        public void Run()
        {
            // Rewrite the projects for the active compilation target.
            for (int idx = 0; idx < this.CompilationContext.GetProjects().Count; idx++)
            {
                this.CompilationContext.GetProjects()[idx].Rewrite();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private RewritingEngine(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion
    }
}
