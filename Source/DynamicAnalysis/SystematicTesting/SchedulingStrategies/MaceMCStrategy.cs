//-----------------------------------------------------------------------
// <copyright file="MaceMCStrategy.cs" company="Microsoft">
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

using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.DynamicAnalysis.Scheduling
{
    /// <summary>
    /// Class representing a depth-first search scheduling strategy
    /// that incorporates iterative deepening.
    /// </summary>
    public class MaceMCStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        protected AnalysisContext AnalysisContext;

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
        /// <param name="context">AnalysisContext</param>
        public MaceMCStrategy(AnalysisContext context)
        {
            this.AnalysisContext = context;
            this.MaxDepth = this.AnalysisContext.Configuration.DepthBound;
            this.SafetyPrefixDepth = this.AnalysisContext.Configuration.SafetyPrefixBound;
            this.BoundedDFS = new IterativeDeepeningDFSStrategy(this.AnalysisContext);
            this.Random = new RandomStrategy(this.AnalysisContext);
        }

        /// <summary>
        /// Returns the next task to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="tasks">Tasks</param>
        /// <param name="currentTask">Curent task</param>
        /// <returns>Boolean value</returns>
        public bool TryGetNext(out TaskInfo next, List<TaskInfo> tasks, TaskInfo currentTask)
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.TryGetNext(out next, tasks, currentTask);
            }
            {
                return this.BoundedDFS.TryGetNext(out next, tasks, currentTask);
            }
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        public bool GetNextChoice(out bool next)
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetNextChoice(out next);
            }
            else
            {
                return this.BoundedDFS.GetNextChoice(out next);
            }
        }

        /// <summary>
        /// Returns the explored scheduling steps.
        /// </summary>
        /// <returns>Scheduling steps</returns>
        public int GetSchedulingSteps()
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetSchedulingSteps();
            }
            else
            {
                return this.BoundedDFS.GetSchedulingSteps();
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
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            return this.Random.HasReachedDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
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
            return "MaceMC";
        }

        #endregion
    }
}
