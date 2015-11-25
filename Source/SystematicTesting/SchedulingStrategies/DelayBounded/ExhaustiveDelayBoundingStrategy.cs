//-----------------------------------------------------------------------
// <copyright file="ExhaustiveDelayBoundingStrategy.cs" company="Microsoft">
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
    /// Class representing an exhaustive delay-bounding scheduling strategy.
    /// </summary>
    public class ExhaustiveDelayBoundingStrategy : DelayBoundingStrategy, ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// Cache of delays across iterations.
        /// </summary>
        private List<int> DelaysCache;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="delays">Max number of delays</param>
        public ExhaustiveDelayBoundingStrategy(Configuration configuration, int delays)
            : base(configuration, delays)
        {
            this.DelaysCache = Enumerable.Repeat(0, base.MaxDelays).ToList();
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public override void ConfigureNextIteration()
        {
            base.MaxExploredSteps = Math.Max(base.MaxExploredSteps, base.ExploredSteps);
            base.ExploredSteps = 0;

            var bound = Math.Min(base.Configuration.DepthBound, base.MaxExploredSteps);
            for (var idx = 0; idx < base.MaxDelays; idx++)
            {
                if (this.DelaysCache[idx] < bound)
                {
                    this.DelaysCache[idx] = this.DelaysCache[idx] + 1;
                    break;
                }

                this.DelaysCache[idx] = 0;
            }

            base.RemainingDelays.Clear();
            base.RemainingDelays.AddRange(this.DelaysCache);
            base.RemainingDelays.Sort();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public override void Reset()
        {
            this.DelaysCache = Enumerable.Repeat(0, base.MaxDelays).ToList();
            base.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            var text = base.MaxDelays + "' delays, delays '[";
            for (int idx = 0; idx < this.DelaysCache.Count; idx++)
            {
                text += this.DelaysCache[idx];
                if (idx < this.DelaysCache.Count - 1)
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
