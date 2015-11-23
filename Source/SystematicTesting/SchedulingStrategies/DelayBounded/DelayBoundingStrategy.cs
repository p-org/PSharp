//-----------------------------------------------------------------------
// <copyright file="DelayBoundingStrategy.cs" company="Microsoft">
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
    /// Class representing an abstract delay-bounding scheduling strategy.
    /// </summary>
    public abstract class DelayBoundingStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        protected int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        protected Random Random;

        /// <summary>
        /// The maximum number of explored scheduling steps.
        /// </summary>
        private int MaxSchedulingSteps;

        /// <summary>
        /// The number of explored scheduling steps.
        /// </summary>
        protected int SchedulingSteps;

        /// <summary>
        /// The maximum number of delays.
        /// </summary>
        protected int MaxDelays;

        /// <summary>
        /// The remaining delays.
        /// </summary>
        protected List<int> RemainingDelays;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="delays">Max number of delays</param>
        public DelayBoundingStrategy(Configuration configuration, int delays)
        {
            this.Configuration = configuration;
            this.Seed = this.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.MaxSchedulingSteps = 0;
            this.SchedulingSteps = 0;
            this.Random = new Random(this.Seed);
            this.MaxDelays = delays;
            this.RemainingDelays = new List<int>();
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="machines">Machines</param>
        /// <param name="currentMachine">Curent machine</param>
        /// <returns>Boolean value</returns>
        public abstract bool TryGetNext(out MachineInfo next, List<MachineInfo> machines, MachineInfo currentMachine);

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        public abstract bool GetNextChoice(int maxValue, out bool next);

        /// <summary>
        /// Returns the explored scheduling steps.
        /// </summary>
        /// <returns>Scheduling steps</returns>
        public virtual int GetSchedulingSteps()
        {
            return this.SchedulingSteps;
        }

        /// <summary>
        /// Returns the maximum explored scheduling steps.
        /// </summary>
        /// <returns>Scheduling steps</returns>
        public int GetMaxSchedulingSteps()
        {
            return this.MaxSchedulingSteps;
        }

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        public virtual int GetDepthBound()
        {
            return this.Configuration.DepthBound;
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public virtual bool HasReachedDepthBound()
        {
            if (this.Configuration.DepthBound == 0)
            {
                return false;
            }

            return this.SchedulingSteps == this.GetDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        public abstract bool HasFinished();
        
        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public virtual void ConfigureNextIteration()
        {
            this.MaxSchedulingSteps = Math.Max(this.MaxSchedulingSteps, this.SchedulingSteps);
            this.SchedulingSteps = 0;

            this.RemainingDelays.Clear();
            for (int idx = 0; idx < this.MaxDelays; idx++)
            {
                this.RemainingDelays.Add(this.Random.Next(this.MaxSchedulingSteps));
            }

            this.RemainingDelays.Sort();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public virtual void Reset()
        {
            this.SchedulingSteps = 0;
            this.RemainingDelays.Clear();
            this.Random = new Random(this.Seed);
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public abstract string GetDescription();

        #endregion
    }
}
