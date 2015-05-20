//-----------------------------------------------------------------------
// <copyright file="RandomScheduler.cs" company="Microsoft">
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

namespace Microsoft.PSharp.BugFinding
{
    /// <summary>
    /// Class representing a random delay scheduler.
    /// </summary>
    public sealed class RandomScheduler : IScheduler
    {
        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private Random Random;

        /// <summary>
        /// Number of scheduling points.
        /// </summary>
        private int NumOfSchedulingPoints;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="seed">Seed</param>
        public RandomScheduler(int seed)
        {
            this.Seed = seed;
            this.Random = new Random(seed);
            this.NumOfSchedulingPoints = 0;
        }
        
        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="machines">Machines</param>
        /// <returns>Boolean value</returns>
        bool IScheduler.TryGetNext(out Machine next, List<Machine> machines)
        {
            if (machines.Count == 0)
            {
                next = null;
                return false;
            }

            this.NumOfSchedulingPoints++;

            int id = this.Random.Next(machines.Count);
            next = machines[id];
            return true;
        }

        /// <summary>
        /// Returns true if the scheduler has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool IScheduler.HasFinished()
        {
            return false;
        }
        
        /// <summary>
        /// Returns number of scheduling points.
        /// </summary>
        /// <returns>Integer value</returns>
        int IScheduler.GetNumOfSchedulingPoints()
        {
            return this.NumOfSchedulingPoints;
        }
        
        /// <summary>
        /// Returns a textual description of the scheduler.
        /// </summary>
        /// <returns>String</returns>
        string IScheduler.GetDescription()
        {
            return "Random (seed is " + this.Seed + ")";
        }

        /// <summary>
        /// Resets the scheduler.
        /// </summary>
        void IScheduler.Reset()
        {
            this.NumOfSchedulingPoints = 0;
        }
    }
}
