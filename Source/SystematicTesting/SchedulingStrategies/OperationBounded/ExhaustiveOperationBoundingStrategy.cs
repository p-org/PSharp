//-----------------------------------------------------------------------
// <copyright file="ExhaustiveOperationBoundingStrategy.cs" company="Microsoft">
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
    /// Class representing an exhaustive operation-bounding scheduling strategy.
    /// </summary>
    public class ExhaustiveOperationBoundingStrategy : OperationBoundingStrategy, ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// Cache of operation delays across iterations.
        /// </summary>
        internal List<int> OperationDelaysCache;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="opdelays">Max number of operation delays</param>
        /// <param name="delays">Max number of delays</param>
        public ExhaustiveOperationBoundingStrategy(Configuration configuration, int opdelays, int delays)
            : base(configuration, opdelays, delays)
        {
            this.OperationDelaysCache = Enumerable.Repeat(0, base.MaxOperationDelays).ToList();
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public override void ConfigureNextIteration()
        {
            base.MaxExploredSteps = Math.Max(base.MaxExploredSteps, base.ExploredSteps);
            base.ExploredSteps = 0;

            var bound = Math.Min(base.Configuration.DepthBound, base.MaxExploredSteps);
            for (var idx = 0; idx < base.MaxOperationDelays; idx++)
            {
                if (this.OperationDelaysCache[idx] < bound)
                {
                    this.OperationDelaysCache[idx] = this.OperationDelaysCache[idx] + 1;
                    break;
                }

                this.OperationDelaysCache[idx] = 0;
            }

            base.RemainingOperationDelays.Clear();
            base.RemainingOperationDelays.AddRange(this.OperationDelaysCache);
            base.RemainingOperationDelays.Sort();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public override void Reset()
        {
            this.OperationDelaysCache = Enumerable.Repeat(0, base.MaxOperationDelays).ToList();
            base.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            var text = base.MaxOperationDelays + "' operation delays, operation delays '[";
            for (int idx = 0; idx < this.OperationDelaysCache.Count; idx++)
            {
                text += this.OperationDelaysCache[idx];
                if (idx < this.OperationDelaysCache.Count - 1)
                {
                    text += ", ";
                }
            }

            text += "]'.";
            return text;
        }

        #endregion
    }
}
