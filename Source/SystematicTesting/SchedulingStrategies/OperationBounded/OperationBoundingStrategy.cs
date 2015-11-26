//-----------------------------------------------------------------------
// <copyright file="OperationBoundingStrategy.cs" company="Microsoft">
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
    /// Class representing an operation-bounding scheduling strategy.
    /// </summary>
    public abstract class OperationBoundingStrategy : ISchedulingStrategy
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
        /// The maximum number of operation delays.
        /// </summary>
        protected int MaxOperationDelays;

        /// <summary>
        /// The maximum number of delays.
        /// </summary>
        protected int MaxDelays;

        /// <summary>
        /// The remaining operation delays.
        /// </summary>
        protected List<int> RemainingOperationDelays;

        /// <summary>
        /// The remaining delays.
        /// </summary>
        protected List<int> RemainingDelays;

        /// <summary>
        /// The prioritized operation id.
        /// </summary>
        protected int PrioritizedOperationId;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="opdelays">Max number of operation delays</param>
        /// <param name="delays">Max number of delays</param>
        public OperationBoundingStrategy(Configuration configuration, int opdelays, int delays)
        {
            this.Configuration = configuration;
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
            this.MaxOperationDelays = opdelays;
            this.MaxDelays = delays;
            this.RemainingOperationDelays = new List<int>();
            this.RemainingDelays = new List<int>();
            this.PrioritizedOperationId = 0;
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
            var availableMachines = this.GetPrioritizedMachines(machines, currentMachine);
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
                IO.PrintLine("<DelayLog> Inserted delay, '{0}' remaining.", this.RemainingDelays.Count);
            }

            next = availableMachines[idx];

            this.ExploredSteps++;

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
                IO.PrintLine("<DelayLog> Inserted delay, '{0}' remaining.", this.RemainingDelays.Count);
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
            this.RemainingOperationDelays.Clear();
            this.RemainingDelays.Clear();
            this.PrioritizedOperationId = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public abstract string GetDescription();

        #endregion

        #region private methods

        /// <summary>
        /// Returns the prioritized machines.
        /// </summary>
        /// <param name="machines">Machines</param>
        /// <param name="currentMachine">Curent machine</param>
        /// <returns>Boolean value</returns>
        private List<MachineInfo> GetPrioritizedMachines(List<MachineInfo> machines, MachineInfo currentMachine)
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
            while (this.RemainingDelays.Count > 0 && this.ExploredSteps == this.RemainingDelays[0])
            {
                idx = (idx + 1) % operations.Count;
                this.RemainingDelays.RemoveAt(0);

                IO.PrintLine("<OperationDelayLog> Priority given to operation '{0}', '{1}' delays remaining, .",
                    operations[idx], this.RemainingDelays.Count);
            }

            this.PrioritizedOperationId = operations[idx];

            foreach (var mi in availableMachines)
            {
                if ((mi.Equals(currentMachine) && mi.Machine.OperationId == this.PrioritizedOperationId) ||
                    mi.Machine.OperationId == this.PrioritizedOperationId)
                {
                    prioritizedMachines.Add(mi);
                }
            }

            return prioritizedMachines;
        }

        #endregion
    }
}
