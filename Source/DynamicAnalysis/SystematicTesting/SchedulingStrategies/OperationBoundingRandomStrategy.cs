//-----------------------------------------------------------------------
// <copyright file="RandomStrategy.cs" company="Microsoft">
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

namespace Microsoft.PSharp.DynamicAnalysis.Scheduling
{
    /// <summary>
    /// Class representing an operation bounding scheduling strategy.
    /// </summary>
    public class OperationBoundingRandomStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        protected AnalysisContext AnalysisContext;

        /// <summary>
        /// The machine scheduler.
        /// </summary>
        private ISchedulingStrategy MachineScheduler;

        /// <summary>
        /// The currently enabled operation id.
        /// </summary>
        private ulong EnabledOperationId;

        /// <summary>
        /// Number of max delays.
        /// </summary>
        private int Delays;

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

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="delays">Max number of delays</param>
        /// <param name="machineSchedulingStrategy">Machine scheduling strategy</param>
        public OperationBoundingRandomStrategy(AnalysisContext context, int delays,
            ISchedulingStrategy machineSchedulingStrategy)
        {
            this.AnalysisContext = context;
            this.MachineScheduler = machineSchedulingStrategy;
            this.EnabledOperationId = 0;
            this.Delays = delays;
            this.Seed = this.AnalysisContext.Configuration.RandomSchedulingSeed
                ?? DateTime.Now.Millisecond;
            this.SchedulingSteps = 0;
            this.Random = new Random(this.Seed);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="delays">Max number of delays</param>
        /// <param name="machineSchedulingStrategy">Machine scheduling strategy</param>
        /// <param name="seed">Scheduling steps</param>
        public OperationBoundingRandomStrategy(AnalysisContext context, int delays,
            ISchedulingStrategy machineSchedulingStrategy, int steps)
        {
            this.AnalysisContext = context;
            this.MachineScheduler = machineSchedulingStrategy;
            this.EnabledOperationId = 0;
            this.Delays = delays;
            this.Seed = this.AnalysisContext.Configuration.RandomSchedulingSeed
                ?? DateTime.Now.Millisecond;
            this.SchedulingSteps = steps;
            this.Random = new Random(this.Seed);
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
            // TODO: check if it returns null
            var availableOps = tasks.Select(val => val.Machine.OperationId).Distinct().ToList();
            if (availableOps.Count == 0)
            {
                next = null;
                return false;
            }

            if (!availableOps.Contains(this.EnabledOperationId))
            {
                int id = this.Random.Next(availableOps.Count);
                this.EnabledOperationId = availableOps[id];
            }
            else if (this.Delays > 0 && this.Random.Next(2) == 1)
            {
                int id = this.Random.Next(availableOps.Count);
                this.EnabledOperationId = availableOps[id];
                this.Delays--;
            }

            var allowedTasks = new List<TaskInfo>();
            foreach (var task in tasks)
            {
                ulong opId;
                if (task.Machine.TryGetNextOperationId(out opId) && opId == this.EnabledOperationId)
                {
                    allowedTasks.Add(task);
                }
            }
            
            return this.MachineScheduler.TryGetNext(out next, allowedTasks, currentTask);
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        public bool GetNextChoice(int maxValue, out bool next)
        {
            return this.MachineScheduler.GetNextChoice(maxValue, out next);
        }

        /// <summary>
        /// Returns the explored scheduling steps.
        /// </summary>
        /// <returns>Scheduling steps</returns>
        public int GetSchedulingSteps()
        {
            return this.MachineScheduler.GetSchedulingSteps();
        }

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        public int GetDepthBound()
        {
            return this.MachineScheduler.GetDepthBound();
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            return this.MachineScheduler.HasReachedDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        public bool HasFinished()
        {
            return this.MachineScheduler.HasFinished();
        }
        
        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            this.MachineScheduler.ConfigureNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.MachineScheduler.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "Operation bounding with " + this.MachineScheduler.GetDescription();
        }

        #endregion
    }
}
