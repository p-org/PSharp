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

using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Class representing an operation-bounding scheduling strategy.
    /// </summary>
    public class OperationBoundingStrategy : DelayBoundingStrategy, ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The internal scheduler.
        /// </summary>
        private ISchedulingStrategy InternalStrategy;

        /// <summary>
        /// The currently enabled operation id.
        /// </summary>
        private int EnabledOperationId;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">Configuration</param>
        /// <param name="delays">Max number of delays</param>
        /// <param name="internalStrategy">Internal scheduling strategy</param>
        public OperationBoundingStrategy(Configuration configuration, int delays,
            ISchedulingStrategy internalStrategy)
            : base(configuration, delays)
        {
            this.InternalStrategy = internalStrategy;
            this.EnabledOperationId = 0;
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
            tasks = tasks.OrderBy(task => task.Machine.OperationId).ToList();

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

            var operations = availableTasks.Select(val => val.Machine.OperationId).Distinct().ToList();

            int idx = 0;
            while (this.RemainingDelays.Count > 0 && this.SchedulingSteps == this.RemainingDelays[0])
            {
                idx = (idx + 1) % operations.Count;
                this.RemainingDelays.RemoveAt(0);

                Output.PrintLine("....... Inserted operation delay, {0} remaining", this.RemainingDelays.Count);
            }

            this.EnabledOperationId = operations[idx];

            var allowedTasks = new List<TaskInfo>();
            foreach (var task in availableTasks)
            {
                int opId = task.Machine.GetNextOperationId();
                if ((task.Equals(currentTask) && task.Machine.OperationId == this.EnabledOperationId) ||
                    opId == this.EnabledOperationId)
                {
                    allowedTasks.Add(task);
                }
            }
            
            return this.InternalStrategy.TryGetNext(out next, allowedTasks, currentTask);
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        public override bool GetNextChoice(int maxValue, out bool next)
        {
            return this.InternalStrategy.GetNextChoice(maxValue, out next);
        }

        /// <summary>
        /// Returns the explored scheduling steps.
        /// </summary>
        /// <returns>Scheduling steps</returns>
        public override int GetSchedulingSteps()
        {
            return this.InternalStrategy.GetSchedulingSteps();
        }

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        public override int GetDepthBound()
        {
            return this.InternalStrategy.GetDepthBound();
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public override bool HasReachedDepthBound()
        {
            return this.InternalStrategy.HasReachedDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        public override bool HasFinished()
        {
            return this.InternalStrategy.HasFinished();
        }
        
        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public override void ConfigureNextIteration()
        {
            this.InternalStrategy.ConfigureNextIteration();
            base.ConfigureNextIteration();
            this.EnabledOperationId = 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public override void Reset()
        {
            this.InternalStrategy.Reset();
            base.Reset();
            this.EnabledOperationId = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            return "Operation-bounding using " + this.InternalStrategy.GetDescription();
        }

        #endregion
    }
}
