//-----------------------------------------------------------------------
// <copyright file="DFSSchedulingStrategy.cs" company="Microsoft">
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

using Microsoft.PSharp.BugFinding;

namespace Microsoft.PSharp.DynamicAnalysis
{
    /// <summary>
    /// Class representing a depth-first search scheduling strategy.
    /// </summary>
    public sealed class DFSSchedulingStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Stack of scheduling choices.
        /// </summary>
        internal List<List<SChoice>> ScheduleStack;

        /// <summary>
        /// Current index.
        /// </summary>
        private int Index;

        /// <summary>
        /// Number of scheduling points.
        /// </summary>
        private int NumOfSchedulingPoints;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DFSSchedulingStrategy()
        {
            this.ScheduleStack = new List<List<SChoice>>();
            this.Index = 0;
            this.NumOfSchedulingPoints = 0;
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="machines">Machines</param>
        /// <returns>Boolean value</returns>
        bool ISchedulingStrategy.TryGetNext(out TaskInfo next, List<TaskInfo> tasks)
        {
            SChoice nextChoice = null;
            List<SChoice> scs = null;

            if (this.Index < this.ScheduleStack.Count)
            {
                scs = this.ScheduleStack[this.Index];
            }
            else
            {
                scs = new List<SChoice>();
                foreach (var task in tasks)
                {
                    scs.Add(new SChoice(task));
                }

                this.ScheduleStack.Add(scs);
            }

            nextChoice = scs.FirstOrDefault(v => !v.IsDone);
            if (nextChoice == null)
            {
                next = null;
                return false;
            }

            if (this.Index > 0)
            {
                var previousChoice = this.ScheduleStack[this.Index - 1].
                    LastOrDefault(v => v.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Task;
            nextChoice.Done();
            this.Index++;

            //this.PrintSchedule();

            return true;
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool ISchedulingStrategy.HasFinished()
        {
            while (this.ScheduleStack.Count > 0 &&
                this.ScheduleStack[this.ScheduleStack.Count - 1].All(v => v.IsDone))
            {
                this.ScheduleStack.RemoveAt(this.ScheduleStack.Count - 1);
                if (this.ScheduleStack.Count > 0)
                {
                    var previousChoice = this.ScheduleStack[this.ScheduleStack.Count - 1].
                        FirstOrDefault(v => !v.IsDone);
                    if (previousChoice != null)
                    {
                        previousChoice.Done();
                    }
                }
            }

            if (this.ScheduleStack.Count == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns number of scheduling points.
        /// </summary>
        /// <returns>Integer value</returns>
        int ISchedulingStrategy.GetNumOfSchedulingPoints()
        {
            return this.NumOfSchedulingPoints;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        string ISchedulingStrategy.GetDescription()
        {
            return "DFS";
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        void ISchedulingStrategy.Reset()
        {
            // setup stack for next execution
            this.Index = 0;
            this.NumOfSchedulingPoints = 0;
        }

        /// <summary>
        /// Prints the schedule.
        /// </summary>
        private void PrintSchedule()
        {
            Console.WriteLine("Size: " + this.ScheduleStack.Count);
            for (int idx = 0; idx < this.ScheduleStack.Count; idx++)
            {
                Console.WriteLine("Index: " + idx);
                foreach (var sc in this.ScheduleStack[idx])
                {
                    Console.Write(sc.Task.Id + " [" + sc.IsDone + "], ");
                }
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// A scheduling choice. Contains a referce to a machine and a boolean
    /// that is true if the choice has been previously explored.
    /// </summary>
    internal class SChoice
    {
        public TaskInfo Task;
        public bool IsDone;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="task">Task</param>
        public SChoice(TaskInfo task)
        {
            this.Task = task;
            this.IsDone = false;
        }

        /// <summary>
        /// Marks the choice as done.
        /// </summary>
        public void Done()
        {
            this.IsDone = true;
        }
    }
}
