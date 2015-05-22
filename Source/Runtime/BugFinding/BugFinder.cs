//-----------------------------------------------------------------------
// <copyright file="BugFinder.cs" company="Microsoft">
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

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.BugFinding
{
    /// <summary>
    /// Class implementing the P# bug-finding scheduler.
    /// </summary>
    public sealed class BugFinder
    {
        #region fields

        /// <summary>
        /// The scheduler to be used for bug-finding.
        /// </summary>
        private IScheduler Scheduler;

        /// <summary>
        /// Lock used by the bug-finder.
        /// </summary>
        private Object Lock;

        /// <summary>
        /// List of active machines to schedule.
        /// </summary>
        private List<Machine> ActiveMachines;

        /// <summary>
        /// Map from machines to their infos.
        /// </summary>
        private Dictionary<Machine, MachineInfo> MachineInfoMap;

        /// <summary>
        /// Is the bug-finder running.
        /// </summary>
        private bool IsRunning;

        /// <summary>
        /// True if a bug was found.
        /// </summary>
        public bool BugFound
        {
            get; private set;
        }

        #endregion

        #region public bug-finder methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="scheduler">Scheduler</param>
        public BugFinder(IScheduler scheduler)
        {
            this.Scheduler = scheduler;
            this.Lock = new Object();
            this.ActiveMachines = new List<Machine>();
            this.MachineInfoMap = new Dictionary<Machine, MachineInfo>();
            this.BugFound = false;
            this.IsRunning = true;
        }

        #endregion

        #region internal bug-finder methods

        /// <summary>
        /// Schedules the next machine to execute.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="sleep">Sleep</param>
        internal void Schedule(Machine machine, bool sleep = true)
        {
            this.MachineInfoMap[machine].IsActive = false;

            Machine next = null;
            if (!this.Scheduler.TryGetNext(out next, this.ActiveMachines))
            {
                Utilities.WriteSchedule("<ScheduleLog> Schedule explored.",
                    machine, machine.Id);
                this.Close(machine);
                return;
            }

            Utilities.WriteSchedule("<ScheduleLog> Machine {0}({1}) is scheduled.",
                next, next.Id);
            this.MachineInfoMap[next].IsActive = true;

            if (machine.Id != next.Id)
            {
                lock (next)
                {
                    System.Threading.Monitor.PulseAll(next);
                }

                if (sleep)
                {
                    lock (machine)
                    {
                        while (!this.MachineInfoMap[machine].IsActive)
                        {
                            System.Threading.Monitor.Wait(machine);
                        }
                    }
                }
            }

            this.ExitIfScheduleFinished(machine);
        }

        /// <summary>
        /// Notify that a new task for the given machine has been created.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void NotifyNewTaskCreated(Machine machine)
        {
            this.ExitIfScheduleFinished(machine);
            this.TryAddActiveMachine(machine);

            if (this.ActiveMachines.Count == 1)
            {
                this.MachineInfoMap[machine].IsActive = true;
            }

            this.MachineInfoMap[machine].PendingTasks++;
        }

        /// <summary>
        /// Notify that the task has started.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void NotifyNewTaskStarted(Machine machine)
        {
            this.ExitIfScheduleFinished(machine);

            lock (machine)
            {
                while (!this.MachineInfoMap[machine].IsActive)
                {
                    System.Threading.Monitor.Wait(machine);
                }
            }

            this.ExitIfScheduleFinished(machine);
        }

        /// <summary>
        /// Notify that the task has completed.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void NotifyTaskCompleted(Machine machine)
        {            
            if (!this.IsRunning)
            {
                return;
            }

            this.MachineInfoMap[machine].PendingTasks--;
            if (this.MachineInfoMap[machine].PendingTasks == 0)
            {
                this.ActiveMachines.Remove(machine);
                this.Schedule(machine, false);
            }
            else
            {
                lock (machine)
                {
                    System.Threading.Monitor.PulseAll(machine);
                }
            }
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
        internal void NotifyAssertionFailure()
        {
            this.BugFound = true;
            this.Close(null);
        }

        #endregion

        #region private bug-finder methods

        /// <summary>
        /// Tries to add a new active machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void TryAddActiveMachine(Machine machine)
        {
            if (!this.ActiveMachines.Contains(machine))
            {
                this.ActiveMachines.Add(machine);
                if (!this.MachineInfoMap.ContainsKey(machine))
                {
                    this.MachineInfoMap.Add(machine, new MachineInfo(machine.Id));
                }
            }
        }

        /// <summary>
        /// Forces the task to exit of the schedule has finished.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void ExitIfScheduleFinished(Machine machine)
        {
            lock (this.Lock)
            {
                if (!this.IsRunning)
                {
                    throw new ScheduleCancelledException();
                }
            }
        }

        /// <summary>
        /// Terminates the bug-finding scheduler.
        /// </summary>
        private void Close(Machine m)
        {
            lock (this.Lock)
            {
                if (!this.IsRunning)
                {
                    return;
                }

                this.IsRunning = false;
            }

            foreach (var machine in this.ActiveMachines)
            {
                this.MachineInfoMap[machine].IsActive = true;
                machine.ForceHalt();
                lock (machine)
                {
                    System.Threading.Monitor.PulseAll(machine);
                }
            }
            
            throw new ScheduleCancelledException();
        }
        
        #endregion
    }
}
