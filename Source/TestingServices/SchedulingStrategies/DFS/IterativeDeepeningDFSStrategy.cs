//-----------------------------------------------------------------------
// <copyright file="IterativeDeepeningDFSStrategy.cs">
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

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing a depth-first search scheduling strategy
    /// that incorporates iterative deepening.
    /// </summary>
    public sealed class IterativeDeepeningDFSStrategy : DFSStrategy, ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The max depth.
        /// </summary>
        private int MaxDepth;

        /// <summary>
        /// The current depth.
        /// </summary>
        private int CurrentDepth;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public IterativeDeepeningDFSStrategy(Configuration configuration)
            : base(configuration)
        {
            this.MaxDepth = base.IsFair() ? configuration.MaxFairSchedulingSteps :
                configuration.MaxUnfairSchedulingSteps;
            this.CurrentDepth = 1;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public new bool HasReachedMaxSchedulingSteps()
        {
            return base.ExploredSteps == this.CurrentDepth;
        }

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        /// <returns>False if all schedules have been explored</returns>
        public override bool PrepareForNextIteration()
        {
            bool doNext = base.PrepareForNextIteration();
            if (!doNext)
            {
                base.Reset();
                this.CurrentDepth++;
                if (this.CurrentDepth <= this.MaxDepth)
                {
                    Debug.WriteLine($"<IterativeDeepeningDFSLog> Depth bound increased to {this.CurrentDepth} (max is {this.MaxDepth}).");
                    doNext = true;
                }
            }

            return doNext;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public new string GetDescription()
        {
            return $"DFS with iterative deepening (max depth is {this.MaxDepth})";
        }

        #endregion
    }
}
