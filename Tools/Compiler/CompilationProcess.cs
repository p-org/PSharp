//-----------------------------------------------------------------------
// <copyright file="CompilationProcess.cs">
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
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# compilation process.
    /// </summary>
    internal sealed class CompilationProcess
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# compilation process.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>CompilationProcess</returns>
        public static CompilationProcess Create(CompilationContext context)
        {
            return new CompilationProcess(context);
        }

        /// <summary>
        /// Starts the P# compilation process.
        /// </summary>
        public void Start()
        {
            foreach (var target in this.CompilationContext.Configuration.CompilationTargets)
            {
                IO.PrintLine(". Compiling target '" + target + "'");
                this.CompilationContext.ActiveCompilationTarget = target;

                // Creates and runs a P# compilation engine.
                CompilationEngine.Create(this.CompilationContext).Run();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private CompilationProcess(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion
    }
}
