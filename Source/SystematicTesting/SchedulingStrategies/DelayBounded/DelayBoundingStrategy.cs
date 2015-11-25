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
    /// Class representing a delay-bounding scheduling strategy.
    /// </summary>
    public abstract class DelayBoundingStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// The maximum number of explored steps.
        /// </summary>
        protected int MaxExploredSteps;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        protected int ExploredSteps;

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
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
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
        public virtual bool TryGetNext(out MachineInfo next, List<MachineInfo> machines, MachineInfo currentMachine)
        {
            List<MachineInfo> availableMachines;
            if (this.Configuration.BoundOperations)
            {
                availableMachines = machines;
            }
            else
            {
                machines = machines.OrderBy(machine => machine.Machine.Id.Value).ToList();

                var currentMachineIdx = machines.IndexOf(currentMachine);
                var orderedMachines = machines.GetRange(currentMachineIdx, machines.Count - currentMachineIdx);
                if (currentMachineIdx != 0)
                {
                    orderedMachines.AddRange(machines.GetRange(0, currentMachineIdx));
                }

                availableMachines = orderedMachines.Where(
                    m => m.IsEnabled && !m.IsBlocked && !m.IsWaiting).ToList();
            }

            if (availableMachines.Count == 0)
            {
                next = null;
                return false;
            }

            int idx = 0;
            while (this.RemainingDelays.Count > 0 && this.ExploredSteps == this.RemainingDelays[0])
            {
                idx = (idx + 1) % availableMachines.Count;
                this.RemainingDelays.RemoveAt(0);
                Output.PrintLine("<DelayLog> Inserted delay, '{0}' remaining.", this.RemainingDelays.Count);
            }

            next = availableMachines[idx];

            if (!currentMachine.IsCompleted)
            {
                this.ExploredSteps++;
            }

            return true;
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        public virtual bool GetNextChoice(int maxValue, out bool next)
        {
            next = false;
            if (this.RemainingDelays.Count > 0 && this.ExploredSteps == this.RemainingDelays[0])
            {
                next = true;
                this.RemainingDelays.RemoveAt(0);
                Output.PrintLine("<DelayLog> Inserted delay, '{0}' remaining.", this.RemainingDelays.Count);
            }

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            return this.ExploredSteps;
        }

        /// <summary>
        /// Returns the maximum explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetMaxExploredSteps()
        {
            return this.MaxExploredSteps;
        }

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        public int GetDepthBound()
        {
            return this.Configuration.DepthBound;
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            if (this.Configuration.DepthBound == 0)
            {
                return false;
            }

            return this.ExploredSteps == this.GetDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        public bool HasFinished()
        {
            return false;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public abstract void ConfigureNextIteration();

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public virtual void Reset()
        {
            this.ExploredSteps = 0;
            this.RemainingDelays.Clear();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public abstract string GetDescription();

        #endregion
    }
}
