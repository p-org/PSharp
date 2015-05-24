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
        private List<List<SChoice>> Stack;

        private int MaxStackIndex
        {
            get
            {
                return this.Stack.Count - 1;
            }
        }

        /// <summary>
        /// Current index.
        /// </summary>
        private int Index;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DFSSchedulingStrategy()
        {
            this.Stack = new List<List<SChoice>>();
            this.Index = 0;
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="machines">Machines</param>
        /// <returns>Boolean value</returns>
        bool ISchedulingStrategy.TryGetNext(out TaskInfo next, List<TaskInfo> tasks)
        {
            var enabledTasks = tasks.Where(task => task.IsEnabled).ToList();

            SChoice nextChoice = null;
            List<SChoice> scs = null;

            if (this.Index < this.Stack.Count)
            {
                scs = this.Stack[this.Index];
            }
            else
            {
                scs = new List<SChoice>();
                foreach (var task in enabledTasks)
                {
                    scs.Add(new SChoice(task.Machine.Id));
                }

                this.Stack.Add(scs);
            }

            nextChoice = scs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = null;
                return false;
            }

            if (this.Index > 0)
            {
                var previousChoice = this.Stack[this.Index - 1].
                    LastOrDefault(val => val.IsDone);
                previousChoice.IsDone = false;
            }
            
            next = enabledTasks.Find(task => task.Machine.Id == nextChoice.Id);
            nextChoice.IsDone = true;
            this.Index++;

            this.PrintSchedule();

            return true;
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool ISchedulingStrategy.HasFinished()
        {
            while (this.Stack.Count > 0 &&
                this.Stack[this.Stack.Count - 1].All(val => val.IsDone))
            {
                this.Stack.RemoveAt(this.Stack.Count - 1);
                if (this.Stack.Count > 0)
                {
                    var previousChoice = this.Stack[this.Stack.Count - 1].
                        FirstOrDefault(val  => !val.IsDone);
                    if (previousChoice != null)
                    {
                        previousChoice.IsDone = true;
                    }
                }
            }

            if (this.Stack.Count == 0)
            {
                return true;
            }

            return false;
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
            this.Index = 0;
        }

        /// <summary>
        /// Prints the schedule.
        /// </summary>
        private void PrintSchedule()
        {
            Console.WriteLine("Size: " + this.Stack.Count);
            for (int idx = 0; idx < this.Stack.Count; idx++)
            {
                Console.WriteLine("Index: " + idx);
                foreach (var sc in this.Stack[idx])
                {
                    Console.Write(sc.Id + " [" + sc.IsDone + "], ");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// A scheduling choice. Contains a reference to a machine id and a
        /// boolean that is true if the choice has been previously explored.
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
    }
}
