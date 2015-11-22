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

using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Class representing a delay-bounding scheduling strategy.
    /// </summary>
    public class DelayBoundingStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private Random Random;

        /// <summary>
        /// The number of explored scheduling steps.
        /// </summary>
        private int SchedulingSteps;

        /// <summary>
        /// The maximum number of scheduling steps (explored so far).
        /// </summary>
        private int MaxSchedulingSteps;

        /// <summary>
        /// The maximum number of delays.
        /// </summary>
        private int MaxDelays;

        /// <summary>
        /// The remaining delays.
        /// </summary>
        private List<int> RemainingDelays;

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
            this.SchedulingSteps = 0;
            this.MaxSchedulingSteps = 0;
            this.Random = new Random(this.Seed);
            this.MaxDelays = delays;
            this.RemainingDelays = new List<int>();
        }

        /// <summary>
        /// Returns the next task to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="tasks">Tasks</param>
        /// <param name="currentTask">Curent task</param>
        /// <returns>Boolean value</returns>
        public bool TryGetNext(out TaskInfo next, List<TaskInfo> tasks, TaskInfo currentTask)
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
        public bool GetNextChoice(int maxValue, out bool next)
        {
            next = false;
            if (this.Random.Next(maxValue) == 1)
            {
                next = true;
            }

            return true;
        }

        /// <summary>
        /// Returns the explored scheduling steps.
        /// </summary>
        /// <returns>Scheduling steps</returns>
        public int GetSchedulingSteps()
        {
            return this.SchedulingSteps;
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

            return this.SchedulingSteps == this.GetDepthBound();
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
        public void ConfigureNextIteration()
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
        public void Reset()
        {
            this.SchedulingSteps = 0;
            this.RemainingDelays.Clear();
            this.Random = new Random(this.Seed);
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "Delay-bounding (with delays '" + this.MaxDelays + "' and seed '" + this.Seed + "')";
        }

        #endregion
    }
}
