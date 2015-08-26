//-----------------------------------------------------------------------
// <copyright file="BugFindingConfiguration.cs">
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

namespace Microsoft.PSharp.Tooling
{
    public abstract class BugFindingConfiguration : Configuration
    {
        #region options

        /// <summary>
        /// Systematic tester does not stop when it finds a bug.
        /// </summary>
        public bool FullExploration;

        /// <summary>
        /// Depth bound. By default it is 1000.
        /// </summary>
        public int DepthBound;

        /// <summary>
        /// Safety prefix bound. By default it is 0.
        /// </summary>
        public int SafetyPrefixBound;

        /// <summary>
        /// If true, then the P# tester will try to schedule
        /// any intra-machine concurrency.
        /// </summary>
        public bool ScheduleIntraMachineConcurrency;

        /// <summary>
        /// If true, then the P# tester will check if any liveness
        /// properties hold.
        /// </summary>
        public bool CheckLiveness;

        /// <summary>
        /// If true, then the P# tester will perform state
        /// caching when checking liveness properties.
        /// </summary>
        public bool CacheProgramState;

        #endregion

        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        internal BugFindingConfiguration()
            : base()
        {
            this.FullExploration = false;
            this.DepthBound = 10000;
            this.SafetyPrefixBound = 0;
            
            this.ScheduleIntraMachineConcurrency = false;
            this.CheckLiveness = false;
            this.CacheProgramState = true;
        }

        #endregion
    }
}
