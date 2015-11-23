//-----------------------------------------------------------------------
// <copyright file="IterativeDeepeningDFSStrategy.cs" company="Microsoft">
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
            this.MaxDepth = configuration.DepthBound;
            this.CurrentDepth = 1;
        }

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        public new int GetDepthBound()
        {
            return this.CurrentDepth;
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public new bool HasReachedDepthBound()
        {
            return base.SchedulingSteps == this.GetDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
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
                Output.PrintLine("....... Depth bound increased to {0}", this.CurrentDepth);
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
