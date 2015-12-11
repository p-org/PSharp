//-----------------------------------------------------------------------
// <copyright file="PrioritizedOperationBoundingStrategy.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
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
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public override bool GetNextChoice(int maxValue, out bool next)
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
                    IO.PrintLine("<OperationLog> Operation '{0}' changes to lowest priority.",
                        priority);
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
            IO.Debug("<OperationDebug> Moving priority change to '{0}'.", newPriorityChangePoint);
        }

        #endregion
    }
}
