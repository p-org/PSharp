//-----------------------------------------------------------------------
// <copyright file="DynamicAnalysisConfiguration.cs">
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

using Microsoft.PSharp.DynamicAnalysis;

namespace Microsoft.PSharp.Tooling
{
    public sealed class DynamicAnalysisConfiguration : BugFindingConfiguration
    {
        #region options

        /// <summary>
        /// The assembly to be analyzed for bugs.
        /// </summary>
        public string AssemblyToBeAnalyzed;

        /// <summary>
        /// Scheduling strategy to use with the P# tester.
        /// </summary>
        public SchedulingStrategy SchedulingStrategy;

        /// <summary>
        /// Number of scheduling iterations.
        /// </summary>
        public int SchedulingIterations;

        /// <summary>
        /// Seed for random scheduling strategies.
        /// </summary>
        internal int? RandomSchedulingSeed;

        /// <summary>
        /// Redirects the console output.
        /// </summary>
        public bool RedirectConsoleOutput;

        /// <summary>
        /// If true, then the P# tester will print the trace
        /// to a file, even if a bug is not found.
        /// </summary>
        public bool PrintTrace;

        /// <summary>
        /// If true, then the P# tester will supress the trace
        /// that leads to a found error to a file.
        /// </summary>
        public bool SuppressTrace;

        #endregion

        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public DynamicAnalysisConfiguration()
            : base()
        {
            this.AssemblyToBeAnalyzed = "";
            
            this.SchedulingStrategy = SchedulingStrategy.Random;
            this.SchedulingIterations = 1;
            this.RandomSchedulingSeed = null;

            this.RedirectConsoleOutput = true;
            this.PrintTrace = false;
            this.SuppressTrace = false;
        }

        #endregion
    }
}
