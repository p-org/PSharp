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
        private int Seed;
        private Random Random;
        private int NumOfSchedulingPoints;

        public RandomScheduler(int seed)
        {
            this.Seed = seed;
            this.Random = new Random(seed);
            this.NumOfSchedulingPoints = 0;
        }

        public Machine Next(List<Machine> machines)
        {
            if (machines.Count == 0)
            {
                return null;
            }

            this.NumOfSchedulingPoints++;

            int id = this.Random.Next(machines.Count);
            return machines[id];
        }

        public int GetNumOfSchedulingPoints()
        {
            return this.NumOfSchedulingPoints;
        }

        public bool Reset()
        {
            this.NumOfSchedulingPoints = 0;
            return true;
        }

        public string GetDescription()
        {
            return "Random (seed is " + this.Seed + ")";
        }
    }
}
