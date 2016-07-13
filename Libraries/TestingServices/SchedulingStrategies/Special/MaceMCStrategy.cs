//-----------------------------------------------------------------------
// <copyright file="MaceMCStrategy.cs">
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
    public class MaceMCStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// The max depth.
        /// </summary>
        private int MaxDepth;

        /// <summary>
        /// The safety prefix depth.
        /// </summary>
        private int SafetyPrefixDepth;

        /// <summary>
        /// A bounded DFS strategy with interative deepening.
        /// </summary>
        private IterativeDeepeningDFSStrategy BoundedDFS;

        /// <summary>
        /// A random strategy.
        /// </summary>
        private RandomStrategy Random;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public MaceMCStrategy(Configuration configuration)
        {
            this.Configuration = configuration;
            this.MaxDepth = this.Configuration.DepthBound;
            this.SafetyPrefixDepth = this.Configuration.SafetyPrefixBound;
            this.BoundedDFS = new IterativeDeepeningDFSStrategy(configuration);
            this.Random = new RandomStrategy(configuration);
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool TryGetNext(out MachineInfo next, IEnumerable<MachineInfo> choices, MachineInfo current)
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.TryGetNext(out next, choices, current);
            }
            {
                return this.BoundedDFS.TryGetNext(out next, choices, current);
            }
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetNextBooleanChoice(maxValue, out next);
            }
            else
            {
                return this.BoundedDFS.GetNextBooleanChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetNextIntegerChoice(maxValue, out next);
            }
            else
            {
                return this.BoundedDFS.GetNextIntegerChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetExploredSteps();
            }
            else
            {
                return this.BoundedDFS.GetExploredSteps();
            }
        }

        /// <summary>
        /// Returns the maximum explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetMaxExploredSteps()
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetMaxExploredSteps();
            }
            else
            {
                return this.BoundedDFS.GetMaxExploredSteps();
            }
        }

        /// <summary>
        /// Returns the depth bound.
        /// </summary>
        /// <returns>Depth bound</returns>
        public int GetDepthBound()
        {
            return this.MaxDepth;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            return this.Random.HasReachedDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasFinished()
        {
            return this.BoundedDFS.HasFinished();
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            this.BoundedDFS.ConfigureNextIteration();
            this.Random.ConfigureNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.BoundedDFS.Reset();
            this.Random.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "";
        }

        #endregion
    }
}
