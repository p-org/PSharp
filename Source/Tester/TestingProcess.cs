//-----------------------------------------------------------------------
// <copyright file="TestingProcess.cs">
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

using Microsoft.PSharp.SystematicTesting;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# testing process.
    /// </summary>
    internal sealed class TestingProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        #endregion

        #region API

        /// <summary>
        /// Creates a P# testing process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>ParsingProcess</returns>
        public static TestingProcess Create(Configuration configuration)
        {
            return new TestingProcess(configuration);
        }

        /// <summary>
        /// Starts the P# testing process.
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
        private TestingProcess(Configuration configuration)
        {
            this.Configuration = configuration;
        }
        
        /// <summary>
        /// Tests the given P# assembly.
        /// </summary>
        /// <param name="dll">Assembly</param>
        private void TestAssembly(string dll)
        {
            // Creates and runs the P# testing engine
            // to find bugs in the P# program.
            TestingEngine.Create(this.Configuration, dll).Run();
        }

        #endregion
    }
}
