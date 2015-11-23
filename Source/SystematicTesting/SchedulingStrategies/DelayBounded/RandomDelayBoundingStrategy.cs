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

using Microsoft.PSharp.Scheduling;
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
        /// Returns the next task to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="tasks">Tasks</param>
        /// <param name="currentTask">Curent task</param>
        /// <returns>Boolean value</returns>
        public override bool TryGetNext(out TaskInfo next, List<TaskInfo> tasks, TaskInfo currentTask)
        {
            tasks = tasks.OrderBy(task => task.Machine.Id.Value).ToList();

            var currentTaskIdx = tasks.IndexOf(currentTask);
            var orderedTasks = tasks.GetRange(currentTaskIdx, tasks.Count - currentTaskIdx);
            if (currentTaskIdx != 0)
            {
                orderedTasks.AddRange(tasks.GetRange(0, currentTaskIdx));
            }

            var availableTasks = orderedTasks.Where(
                task => task.IsEnabled && !task.IsBlocked && !task.IsWaiting).ToList();
            if (availableTasks.Count == 0)
            {
                next = null;
                return false;
            }

            int idx = 0;
            while (this.RemainingDelays.Count > 0 && this.SchedulingSteps == this.RemainingDelays[0])
            {
                idx = (idx + 1) % availableTasks.Count;
                this.RemainingDelays.RemoveAt(0);

                Output.PrintLine("....... Inserted delay, {0} remaining", this.RemainingDelays.Count);
            }

            next = availableTasks[idx];

            if (!currentTask.IsCompleted)
            {
                this.SchedulingSteps++;
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
            if (this.Random.Next(maxValue) == 1)
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
            return "Delay-bounding (with delays '" + this.MaxDelays + "' and seed '" + this.Seed + "')";
        }

        #endregion
    }
}
