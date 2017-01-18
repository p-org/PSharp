//-----------------------------------------------------------------------
// <copyright file="ComboStrategy.cs">
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
    /// Class representing a combination of two strategies, used
    /// one after the other.
    /// </summary>
    public class ComboStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// The safety prefix depth.
        /// </summary>
        private int SafetyPrefixDepth;

        /// <summary>
        /// Strategy 1
        /// </summary>
        private ISchedulingStrategy Strategy1;

        /// <summary>
        /// Strategy 2
        /// </summary>
        private ISchedulingStrategy Strategy2;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="Strategy1">Strategy 1</param>
        /// <param name="Strategy2">Strategy 2</param>
        public ComboStrategy(Configuration configuration, ISchedulingStrategy Strategy1, ISchedulingStrategy Strategy2)
        {
            this.Configuration = configuration;
            this.SafetyPrefixDepth = this.Configuration.SafetyPrefixBound == 0 ? this.Configuration.MaxUnfairSchedulingSteps
                : this.Configuration.SafetyPrefixBound;
            this.Strategy1 = Strategy1;
            this.Strategy2 = Strategy2;
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
            if (this.Strategy1.GetExploredSteps() > this.SafetyPrefixDepth)
            {
                return this.Strategy2.TryGetNext(out next, choices, current);
            }
            else
            {
                return this.Strategy1.TryGetNext(out next, choices, current);
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
            if (this.Strategy1.GetExploredSteps() > this.SafetyPrefixDepth)
            {
                return this.Strategy2.GetNextBooleanChoice(maxValue, out next);
            }
            else
            {
                return this.Strategy1.GetNextBooleanChoice(maxValue, out next);
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
            if (this.Strategy1.GetExploredSteps() > this.SafetyPrefixDepth)
            {
                return this.Strategy2.GetNextIntegerChoice(maxValue, out next);
            }
            else
            {
                return this.Strategy1.GetNextIntegerChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            if (this.Strategy1.GetExploredSteps() > this.SafetyPrefixDepth)
            {
                return this.Strategy2.GetExploredSteps() + this.SafetyPrefixDepth;
            }
            else
            {
                return this.Strategy1.GetExploredSteps();
            }
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            return this.Strategy2.HasReachedMaxSchedulingSteps();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasFinished()
        {
            return this.Strategy2.HasFinished() && this.Strategy1.HasFinished();
        }

        /// <summary>
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return this.Strategy2.IsFair();
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            this.Strategy1.ConfigureNextIteration();
            this.Strategy2.ConfigureNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.Strategy1.Reset();
            this.Strategy2.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return string.Format("Combo[{0},{1}]", Strategy1.GetDescription(), Strategy2.GetDescription());
        }

        #endregion
    }
}
