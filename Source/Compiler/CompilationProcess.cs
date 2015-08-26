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
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# compilation process.
    /// </summary>
    internal sealed class CompilationProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private LanguageServicesConfiguration Configuration;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# compilation process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>CompilationProcess</returns>
        public static CompilationProcess Create(LanguageServicesConfiguration configuration)
        {
            return new CompilationProcess(configuration);
        }

        /// <summary>
        /// Starts the P# compilation process.
        /// </summary>
        public void Start()
        {
            Output.PrintLine(". Compiling");

            // Creates and runs a P# compilation engine.
            CompilationEngine.Create(this.Configuration).Run();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private CompilationProcess(LanguageServicesConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        #endregion
    }
}
