//-----------------------------------------------------------------------
// <copyright file="DFSScheduler.cs" company="Microsoft">
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
using System.Diagnostics;
using System.Linq;

namespace Microsoft.PSharp.BugFinding
{
    /// <summary>
    /// Class representing a depth-first search scheduler.
    /// </summary>
    public sealed class DFSScheduler : IScheduler
    {
        /// <summary>
        /// Stack of scheduling choices.
        /// </summary>
        internal List<List<SChoice>> ScheduleStack;

        private int Index;

        private bool DelayBounding = false;
        private int MaxBoundCount = -1;
        private int BoundCount;

        private Random DeterministicRandom;
        private int DeterministicRandSeed;

        private int NumOfSchedulingPoints;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="deterministicRandSeed">Seed</param>
        public DFSScheduler(int deterministicRandSeed)
        {
            this.ScheduleStack = new List<List<SChoice>>();

            this.Index = 0;

            this.DeterministicRandSeed = deterministicRandSeed;
            this.DeterministicRandom = new Random(deterministicRandSeed);

            this.DelayBounding = false;
            this.BoundCount = 0;

            this.NumOfSchedulingPoints = 0;
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="machines">Machines</param>
        /// <returns>Boolean value</returns>
        bool IScheduler.TryGetNext(out Machine next, List<Machine> machines)
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
                foreach (var machine in machines)
                {
                    scs.Add(new SChoice(machine.Id));
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

            next = machines.Find(val => val.Id == nextChoice.Id);
            nextChoice.Done();
            this.Index++;

            this.PrintSchedule();

            return true;
        }

        /// <summary>
        /// Returns true if the scheduler has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool IScheduler.HasFinished()
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
        int IScheduler.GetNumOfSchedulingPoints()
        {
            return this.NumOfSchedulingPoints;
        }

        /// <summary>
        /// Returns a textual description of the scheduler.
        /// </summary>
        /// <returns>String</returns>
        string IScheduler.GetDescription()
        {
            return "DFS" + (this.MaxBoundCount >= 0 ? (this.DelayBounding ? " DB of " : " PB of ") +
                this.MaxBoundCount : "") + " seed=" + this.DeterministicRandSeed;
        }

        /// <summary>
        /// Resets the scheduler.
        /// </summary>
        void IScheduler.Reset()
        {
            // setup stack for next execution
            this.Index = 0;
            this.NumOfSchedulingPoints = 0;
            this.BoundCount = 0;
            this.DeterministicRandom = new Random(this.DeterministicRandSeed);
        }

        public void DoDelayBounding()
        {
            this.DelayBounding = true;
        }

        public void SetBound(int bound)
        {
            this.MaxBoundCount = bound;
        }

        public int MaxStackIndex()
        {
            return this.ScheduleStack.Count - 1;
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
                    Console.Write(sc.Id + " [" + sc.IsDone + "], ");
                }
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// A scheduling choice. Contains an integer that represents
    /// a machine Id and a boolean that is true if the choice has
    /// been previously explored.
    /// </summary>
    internal class SChoice
    {
        public int Id;
        public bool IsDone;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">Id</param>
        public SChoice(int id)
        {
            this.Id = id;
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
    

    //    private void PrunePOR(int currTid, List<TidEntry> orderedList)
    //    {
    //        if (orderedList.Count > 0 && orderedList[0].eventType == EventType.TAKE)
    //        {
    //            for (int i = 1; i < orderedList.Count; ++i)
    //            {
    //                orderedList[i].done = true;
    //            }
    //        }
    //    }

    //    private void PruneBounded(int currTid, List<TidEntry> orderedList)
    //    {
    //        if (maxBoundCount >= 0)
    //        {
    //            Debug.Assert(boundCount <= maxBoundCount);
    //            if (delayBounding)
    //            {
    //                // Delay bounding
    //                for (int i = 1; i < orderedList.Count; ++i)
    //                {
    //                    if (boundCount + i > maxBoundCount)
    //                    {
    //                        orderedList[i].done = true;
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                // Preemption bounding
    //                if (orderedList.Count > 0 && currTid == orderedList[0].tid)
    //                {
    //                    for (int i = 1; i < orderedList.Count; ++i)
    //                    {
    //                        if (boundCount + 1 > maxBoundCount)
    //                        {
    //                            orderedList[i].done = true;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    public bool GetRandomBool()
    //    {
    //        return deterministicRandom.Next(2) != 0;
    //    }

    //    public int GetRandomInt(int ceiling)
    //    {
    //        return deterministicRandom.Next(ceiling);
    //    }

    //    private void UpdateBoundCount(int currTid, int nextTid, List<TidEntry> orderedList, int nextThreadIndexInOrderedList)
    //    {
    //        if (delayBounding)
    //        {
    //            boundCount += nextThreadIndexInOrderedList;
    //        }
    //        else
    //        {
    //            // preemption bounding
    //            if (nextTid != currTid && currTid == orderedList[0].tid)
    //            {
    //                boundCount++;
    //            }
    //        }
    //    }

    //    public ThreadInfo ReachedSchedulingPoint(int currTid, List<ThreadInfo> threadList)
    //    {
    //        Debug.Assert(index <= MaxStackIndex());

    //        ThreadInfo currThreadInfo = threadList[currTid];
    //        if (!(currThreadInfo.Enabled && currThreadInfo.eventType == EventType.TAKE))
    //        {
    //            numSchedPoints++;
    //        }

    //        int nextThreadIndexInOrderedList = -1;

    //        // create list of TidEntry
    //        var orderedList = threadList
    //            .ShiftLeft(currTid)
    //            .Where((ti) => ti.Enabled)
    //            .Select((ti) => new TidEntry(ti.Id, ti.eventType))
    //            .ToList();

    //        PruneBounded(currTid, orderedList);
    //        PrunePOR(currTid, orderedList);

    //        if (index == MaxStackIndex())
    //        {
    //            var tidEntry = stack.Last().First((entry) => !entry.done);
    //            Debug.Assert(currTid == tidEntry.tid && !tidEntry.done);

    //            // push onto stack
    //            stack.Add(orderedList);
    //            index++;

    //            // pick next thread (the first thread)
    //            if (orderedList.Count > 0)
    //            {
    //                Debug.Assert(!orderedList.First().done);
    //                nextThreadIndexInOrderedList = 0;
    //            }
    //        }
    //        else
    //        {
    //            Debug.Assert(index < MaxStackIndex());
    //            var topOfStack = stack.ElementAt(index);
    //            var currEntry = topOfStack.First((entry) => !entry.done);

    //            // we executed the first entry that was not done.
    //            Debug.Assert(currEntry.tid == currTid);

    //            // check that enabled threads match
    //            index++;
    //            topOfStack = stack.ElementAt(index);

    //            if (topOfStack.Count != orderedList.Count)
    //            {
    //                throw new NondeterminismException();
    //            }

    //            for (int i = 0; i < topOfStack.Count; ++i)
    //            {
    //                var a = topOfStack.ElementAt(i);
    //                var b = orderedList.ElementAt(i);
    //                if (a.tid != b.tid || a.eventType != b.eventType)
    //                {
    //                    throw new NondeterminismException();
    //                }
    //            }

    //            // next thread is the first thread that is not done
    //            nextThreadIndexInOrderedList = topOfStack.FindIndex((entry) => !entry.done);
    //        }

    //        if (nextThreadIndexInOrderedList == -1)
    //        {
    //            return null;
    //        }

    //        int nextTid = stack[index][nextThreadIndexInOrderedList].tid;

    //        UpdateBoundCount(currTid, nextTid, orderedList, nextThreadIndexInOrderedList);

    //        return threadList[nextTid];
    //    }

    //public static class ShiftList
    //{
    //    public static List<T> ShiftLeft<T>(this List<T> list, int shiftBy)
    //    {
    //        if (shiftBy == 0 || shiftBy == list.Count)
    //        {
    //            return new List<T>(list);
    //        }

    //        if (list.Count <= shiftBy)
    //        {
    //            throw new IndexOutOfRangeException();
    //        }

    //        var result = list.GetRange(shiftBy, list.Count - shiftBy);
    //        result.AddRange(list.GetRange(0, shiftBy));
    //        return result;
    //    }
    //}
    //}
