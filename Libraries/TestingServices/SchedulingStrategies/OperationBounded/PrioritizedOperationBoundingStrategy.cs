//-----------------------------------------------------------------------
// <copyright file="PrioritizedOperationBoundingStrategy.cs">
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

using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing a prioritized operation-bounding scheduling strategy.
    /// </summary>
    public class PrioritizedOperationBoundingStrategy : OperationBoundingStrategy, ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The bug depth.
        /// </summary>
        private int BugDepth;

        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private SortedSet<int> PriorityChangePoints;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="depth">Bug depth</param>
        public PrioritizedOperationBoundingStrategy(Configuration configuration, int depth)
            : base(configuration)
        {
            this.BugDepth = depth;
            this.PriorityChangePoints = new SortedSet<int>();
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public override bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            next = false;

            if (base.Random.Next(maxValue) == 0)
            {
                next = true;
            }

            if (this.PriorityChangePoints.Contains(this.ExploredSteps))
            {
                this.MovePriorityChangePointForward();
            }

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <param name="interval">interval</param>
        /// <returns>Boolean</returns>
        public override bool GetNextBooleanChoice(int maxValue, out bool next, int interval)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public override bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = this.Random.Next(maxValue);

            if (this.PriorityChangePoints.Contains(this.ExploredSteps))
            {
                this.MovePriorityChangePointForward();
            }

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public override void ConfigureNextIteration()
        {
            base.ConfigureNextIteration();
            
            this.PriorityChangePoints.Clear();
            for (int idx = 0; idx < this.BugDepth - 1; idx++)
            {
                this.PriorityChangePoints.Add(base.Random.Next(base.MaxExploredSteps));
            }
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public override void Reset()
        {
            this.PriorityChangePoints.Clear();
            base.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            var text = this.BugDepth + "' bug depth, priority change points '[";

            int idx = 0;
            foreach (var points in this.PriorityChangePoints)
            {
                text += points;
                if (idx < this.PriorityChangePoints.Count - 1)
                {
                    text += ", ";
                }

                idx++;
            }

            text += "]'.";
            return text;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Returns the next operation to schedule.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>OperationId</returns>
        protected override int GetNextOperation(List<MachineInfo> choices, MachineInfo current)
        {
            var operationIds = choices.Select(val => val.Machine.OperationId).Distinct();
            if (this.PriorityChangePoints.Contains(this.ExploredSteps))
            {
                if (operationIds.Count() == 1)
                {
                    this.MovePriorityChangePointForward();
                }
                else
                {
                    var priority = this.GetHighestPriorityEnabledOperationId(choices);
                    base.Operations.Remove(priority);
                    base.Operations.Add(priority);
                    Debug.WriteLine("<OperationLog> Operation '{0}' changes to lowest priority.", priority);
                }
            }
            
            return this.GetHighestPriorityEnabledOperationId(choices);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the highest-priority enabled operation id.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <returns>OperationId</returns>
        private int GetHighestPriorityEnabledOperationId(IEnumerable<MachineInfo> choices)
        {
            var prioritizedOperation = -1;
            foreach (var op in base.Operations)
            {
                if (choices.Any(m => m.Machine.OperationId == op))
                {
                    prioritizedOperation = op;
                    break;
                }
            }

            return prioritizedOperation;
        }

        /// <summary>
        /// Moves the current priority change point forward. This is a useful
        /// optimization when a priority change point is assigned in either a
        /// sequential execution or a nondeterministic choice.
        /// </summary>
        private void MovePriorityChangePointForward()
        {
            this.PriorityChangePoints.Remove(this.ExploredSteps);
            var newPriorityChangePoint = this.ExploredSteps + 1;
            while (this.PriorityChangePoints.Contains(newPriorityChangePoint))
            {
                newPriorityChangePoint++;
            }

            this.PriorityChangePoints.Add(newPriorityChangePoint);
            Debug.WriteLine("<OperationDebug> Moving priority change to '{0}'.", newPriorityChangePoint);
        }

        #endregion
    }
}
