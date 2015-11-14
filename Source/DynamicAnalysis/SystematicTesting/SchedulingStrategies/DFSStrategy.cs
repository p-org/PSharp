//-----------------------------------------------------------------------
// <copyright file="DFSStrategy.cs" company="Microsoft">
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
    /// Class representing a depth-first search scheduling strategy.
    /// </summary>
    public class DFSStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        protected AnalysisContext AnalysisContext;

        /// <summary>
        /// Stack of scheduling choices.
        /// </summary>
        private List<List<SChoice>> ScheduleStack;

        /// <summary>
        /// Stack of nondeterministic choices.
        /// </summary>
        private List<List<NondetChoice>> NondetStack;

        /// <summary>
        /// Current schedule index.
        /// </summary>
        private int SchIndex;

        /// <summary>
        /// Current nondeterministic index.
        /// </summary>
        private int NondetIndex;

        /// <summary>
        /// The number of explored scheduling steps.
        /// </summary>
        protected int SchedulingSteps;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        public DFSStrategy(AnalysisContext context)
        {
            this.AnalysisContext = context;
            this.ScheduleStack = new List<List<SChoice>>();
            this.NondetStack = new List<List<NondetChoice>>();
            this.SchIndex = 0;
            this.NondetIndex = 0;
            this.SchedulingSteps = 0;
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
            var enabledTasks = tasks.Where(task => task.IsEnabled).ToList();
            if (enabledTasks.Count == 0)
            {
                next = null;
                return false;
            }

            var availableTasks = enabledTasks.Where(
                task => !task.IsBlocked && !task.IsWaiting).ToList();
            if (availableTasks.Count == 0)
            {
                next = null;
                return false;
            }

            SChoice nextChoice = null;
            List<SChoice> scs = null;

            if (this.SchIndex < this.ScheduleStack.Count)
            {
                scs = this.ScheduleStack[this.SchIndex];
            }
            else
            {
                scs = new List<SChoice>();
                foreach (var task in availableTasks)
                {
                    scs.Add(new SChoice(task.Machine.Id.Value));
                }

                this.ScheduleStack.Add(scs);
            }

            nextChoice = scs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = null;
                return false;
            }

            if (this.SchIndex > 0)
            {
                var previousChoice = this.ScheduleStack[this.SchIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }
            
            next = availableTasks.Find(task => task.Machine.Id.Value == nextChoice.Id);
            nextChoice.IsDone = true;
            this.SchIndex++;

            if (next == null)
            {
                return false;
            }

            if (!currentTask.IsCompleted)
            {
                this.SchedulingSteps++;
            }

            return true;
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="next">Next</param>
        /// <returns>Boolean value</returns>
        public bool GetNextChoice(out bool next)
        {
            NondetChoice nextChoice = null;
            List<NondetChoice> ncs = null;

            if (this.NondetIndex < this.NondetStack.Count)
            {
                ncs = this.NondetStack[this.NondetIndex];
            }
            else
            {
                ncs = new List<NondetChoice>();
                ncs.Add(new NondetChoice(false));
                ncs.Add(new NondetChoice(true));

                this.NondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = false;
                return false;
            }

            if (this.NondetIndex > 0)
            {
                var previousChoice = this.NondetStack[this.NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            this.NondetIndex++;

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
            return this.AnalysisContext.Configuration.DepthBound;
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            if (this.GetDepthBound() == 0)
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
            return this.ScheduleStack.All(scs => scs.All(val => val.IsDone));
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            //this.PrintSchedule();
            this.SchIndex = 0;
            this.NondetIndex = 0;
            this.SchedulingSteps = 0;

            for (int idx = this.NondetStack.Count - 1; idx > 0; idx--)
            {
                if (!this.NondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = this.NondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                this.NondetStack.RemoveAt(idx);
            }

            if (this.NondetStack.Count > 0 &&
                this.NondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                this.NondetStack.Clear();
            }

            if (this.NondetStack.Count == 0)
            {
                for (int idx = this.ScheduleStack.Count - 1; idx > 0; idx--)
                {
                    if (!this.ScheduleStack[idx].All(val => val.IsDone))
                    {
                        break;
                    }

                    var previousChoice = this.ScheduleStack[idx - 1].First(val => !val.IsDone);
                    previousChoice.IsDone = true;

                    this.ScheduleStack.RemoveAt(idx);
                }
            }
            else
            {
                var previousChoice = this.ScheduleStack.Last().LastOrDefault(val => val.IsDone);
                if (previousChoice != null)
                {
                    previousChoice.IsDone = false;
                }
            }
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.ScheduleStack.Clear();
            this.NondetStack.Clear();
            this.SchIndex = 0;
            this.NondetIndex = 0;
            this.SchedulingSteps = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "DFS";
        }

        #endregion

        #region private methods

        /// <summary>
        /// Prints the schedule.
        /// </summary>
        private void PrintSchedule()
        {
            Output.PrintLine("*******************");
            Output.PrintLine("Schedule stack size: " + this.ScheduleStack.Count);
            for (int idx = 0; idx < this.ScheduleStack.Count; idx++)
            {
                Output.PrintLine("Index: " + idx);
                foreach (var sc in this.ScheduleStack[idx])
                {
                    Console.Write(sc.Id + " [" + sc.IsDone + "], ");
                }
                Output.PrintLine("");
            }

            Output.PrintLine("*******************");
            Output.PrintLine("Random stack size: " + this.NondetStack.Count);
            for (int idx = 0; idx < this.NondetStack.Count; idx++)
            {
                Output.PrintLine("Index: " + idx);
                foreach (var nc in this.NondetStack[idx])
                {
                    Console.Write(nc.Value + " [" + nc.IsDone + "], ");
                }
                Output.PrintLine("");
            }
            Output.PrintLine("*******************");
        }

        /// <summary>
        /// A scheduling choice. Contains a machine id and a boolean that is
        /// true if the choice has been previously explored.
        /// </summary>
        private class SChoice
        {
            internal int Id;
            internal bool IsDone;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="id">Id</param>
            internal SChoice(int id)
            {
                this.Id = id;
                this.IsDone = false;
            }
        }

        /// <summary>
        /// A nondeterministic choice. Contains a boolean value that
        /// corresponds to the choice and a boolean that is true if
        /// the choice has been previously explored.
        /// </summary>
        private class NondetChoice
        {
            internal bool Value;
            internal bool IsDone;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="value">Value</param>
            internal NondetChoice(bool value)
            {
                this.Value = value;
                this.IsDone = false;
            }
        }

        #endregion
    }
}
