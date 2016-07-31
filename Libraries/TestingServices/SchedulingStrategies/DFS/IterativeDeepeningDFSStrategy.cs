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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing a depth-first search scheduling strategy
    /// that incorporates iterative deepening.
    /// </summary>
    public class IterativeDeepeningDFSStrategy : DFSStrategy, ISchedulingStrategy
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
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public new bool HasFinished()
        {
            return base.HasFinished() && this.CurrentDepth == this.MaxDepth;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public new void ConfigureNextIteration()
        {
            if (!base.HasFinished())
            {
                base.ConfigureNextIteration();
            }
            else
            {
                base.Reset();
                this.CurrentDepth++;
                IO.PrintLine("....... Depth bound increased to {0}", this.CurrentDepth);
            }
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public new string GetDescription()
        {
            return "DFS with iterative deepening";
        }

        #endregion
    }
}
