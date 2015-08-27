//-----------------------------------------------------------------------
// <copyright file="SystematicTestingProcess.cs">
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

using System.IO;

using Microsoft.CodeAnalysis;

using Microsoft.PSharp.DynamicAnalysis;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# systematic testing process.
    /// </summary>
    internal sealed class SystematicTestingProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private DynamicAnalysisConfiguration Configuration;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# systematic testing process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>ParsingProcess</returns>
        public static SystematicTestingProcess Create(DynamicAnalysisConfiguration configuration)
        {
            return new SystematicTestingProcess(configuration);
        }

        /// <summary>
        /// Starts the P# systematic testing process.
        /// </summary>
        public void Start()
        {
            Output.PrintLine(". Testing " + this.Configuration.AssemblyToBeAnalyzed);
            this.TestAssembly(this.Configuration.AssemblyToBeAnalyzed);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private SystematicTestingProcess(DynamicAnalysisConfiguration configuration)
        {
            this.Configuration = configuration;
        }
        
        /// <summary>
        /// Tests the given P# assembly.
        /// </summary>
        /// <param name="dll">Assembly</param>
        private void TestAssembly(string dll)
        {
            // Create a P# dynamic analysis context.
            var context = AnalysisContext.Create(this.Configuration, dll);

            // Creates and runs the systematic testing engine
            // to find bugs in the P# program.
            SCTEngine.Create(context).Run();
        }

        #endregion
    }
}
