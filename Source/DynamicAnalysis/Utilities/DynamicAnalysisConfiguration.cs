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
        private DynamicAnalysisConfiguration()
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

        #region methods

        /// <summary>
        /// Creates a new dynamic analysis configuration.
        /// </summary>
        /// <returns>DynamicAnalysisConfiguration</returns>
        public static DynamicAnalysisConfiguration Create()
        {
            return new DynamicAnalysisConfiguration();
        }

        /// <summary>
        /// Updates the configuration with the scheduling strategy
        /// and returns it.
        /// </summary>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <returns>Configuration</returns>
        public Configuration WithStrategy(SchedulingStrategy strategy)
        {
            this.SchedulingStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Updates the configuration with the number of iterations
        /// and returns it.
        /// </summary>
        /// <param name="iterations">Number of iterations</param>
        /// <returns>Configuration</returns>
        public Configuration WithNumberOfIterations(int iterations)
        {
            this.SchedulingIterations = iterations;
            return this;
        }

        #endregion
    }
}
