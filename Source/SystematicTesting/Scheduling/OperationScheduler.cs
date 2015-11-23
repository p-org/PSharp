//-----------------------------------------------------------------------
// <copyright file="OperationScheduler.cs" company="Microsoft">
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
    /// Class representing an operation scheduler.
    /// </summary>
    internal sealed class OperationScheduler
    {
        #region fields

        /// <summary>
        /// The P# runtime.
        /// </summary>
        private PSharpBugFindingRuntime Runtime;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private Random Random;

        /// <summary>
        /// The maximum number of scheduling steps (explored so far).
        /// </summary>
        private int MaxSchedulingSteps;

        /// <summary>
        /// The remaining delays.
        /// </summary>
        private List<int> RemainingDelays;

        /// <summary>
        /// The prioritized operation id.
        /// </summary>
        private int PrioritizedOperationId;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        internal OperationScheduler(PSharpBugFindingRuntime runtime)
        {
            this.Runtime = runtime;
            this.Seed = this.Runtime.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.MaxSchedulingSteps = 0;
            this.Random = new Random(this.Seed);
            this.RemainingDelays = new List<int>();
            this.PrioritizedOperationId = 0;
        }

        /// <summary>
        /// Returns the prioritized machines.
        /// </summary>
        /// <param name="machines">Machines</param>
        /// <param name="currentMachines">Curent machine</param>
        /// <returns>Boolean value</returns>
        public List<MachineInfo> GetPrioritizedMachines(List<MachineInfo> machines, MachineInfo currentMachine)
        {
            var prioritizedMachines = new List<MachineInfo>(); 

            machines = machines.OrderBy(task => task.Machine.OperationId).ToList();

            var currentMachineIdx = machines.IndexOf(currentMachine);
            var orderedMachines = machines.GetRange(currentMachineIdx, machines.Count - currentMachineIdx);
            if (currentMachineIdx != 0)
            {
                orderedMachines.AddRange(machines.GetRange(0, currentMachineIdx));
            }

            var availableMachines = orderedMachines.Where(
                task => task.IsEnabled && !task.IsBlocked && !task.IsWaiting).ToList();
            if (availableMachines.Count == 0)
            {
                return prioritizedMachines;
            }

            var operations = availableMachines.Select(val => val.Machine.OperationId).Distinct().ToList();

            int idx = 0;
            while (this.RemainingDelays.Count > 0 &&
                this.Runtime.BugFinder.SchedulingPoints == this.RemainingDelays[0])
            {
                idx = (idx + 1) % operations.Count;
                this.RemainingDelays.RemoveAt(0);

                Output.PrintLine("<OperationDelayLog> Priority given to operation '{0}', '{1}' delays remaining, .",
                    operations[idx], this.RemainingDelays.Count);
            }

            this.PrioritizedOperationId = operations[idx];

            foreach (var mi in availableMachines)
            {
                int opId = mi.Machine.GetNextOperationId();
                if ((mi.Equals(currentMachine) && mi.Machine.OperationId == this.PrioritizedOperationId) ||
                    opId == this.PrioritizedOperationId)
                {
                    prioritizedMachines.Add(mi);
                }
            }

            return prioritizedMachines;
        }
                
        /// <summary>
        /// Configures the next operation scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            this.PrioritizedOperationId = 0;
            this.MaxSchedulingSteps = Math.Max(this.MaxSchedulingSteps, this.Runtime.BugFinder.SchedulingPoints);

            this.RemainingDelays.Clear();
            for (int idx = 0; idx < this.Runtime.Configuration.OperationDelayBound; idx++)
            {
                this.RemainingDelays.Add(this.Random.Next(this.MaxSchedulingSteps));
            }

            this.RemainingDelays.Sort();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.PrioritizedOperationId = 0;
            this.RemainingDelays.Clear();
            this.Random = new Random(this.Seed);
        }

        #endregion
    }
}
