//-----------------------------------------------------------------------
// <copyright file="RandomDelayBoundingStrategy.cs" company="Microsoft">
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
    /// Class representing a random delay-bounding scheduling strategy.
    /// </summary>
    public class RandomDelayBoundingStrategy : DelayBoundingStrategy, ISchedulingStrategy
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="delays">Max number of delays</param>
        public RandomDelayBoundingStrategy(Configuration configuration, int delays)
            : base(configuration,delays)
        {

        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="machines">Machines</param>
        /// <param name="currentMachine">Curent machine</param>
        /// <returns>Boolean value</returns>
        public override bool TryGetNext(out MachineInfo next, List<MachineInfo> machines, MachineInfo currentMachine)
        {
            machines = machines.OrderBy(machine => machine.Machine.Id.Value).ToList();

            var currentMachineIdx = machines.IndexOf(currentMachine);
            var orderedMachines = machines.GetRange(currentMachineIdx, machines.Count - currentMachineIdx);
            if (currentMachineIdx != 0)
            {
                orderedMachines.AddRange(machines.GetRange(0, currentMachineIdx));
            }

            var availableMachines = orderedMachines.Where(
                m => m.IsEnabled && !m.IsBlocked && !m.IsWaiting).ToList();
            if (availableMachines.Count == 0)
            {
                next = null;
                return false;
            }

            int idx = 0;
            while (base.RemainingDelays.Count > 0 && base.SchedulingSteps == base.RemainingDelays[0])
            {
                idx = (idx + 1) % availableMachines.Count;
                this.RemainingDelays.RemoveAt(0);

                Output.PrintLine("<DelayLog> Inserted delay, '{0}' remaining.", base.RemainingDelays.Count);
            }

            next = availableMachines[idx];

            if (!currentMachine.IsCompleted)
            {
                base.SchedulingSteps++;
            }

            return true;
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        public override bool GetNextChoice(int maxValue, out bool next)
        {
            next = false;
            if (base.Random.Next(maxValue) == 1)
            {
                next = true;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        public override bool HasFinished()
        {
            return false;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            return "Delay-bounding (with delays '" + base.MaxDelays + "' and seed '" + base.Seed + "')";
        }

        #endregion
    }
}
