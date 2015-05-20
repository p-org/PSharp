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
    public class BugFinder
    {
        /// <summary>
        /// The scheduler to be used for bug-finding.
        /// </summary>
        private IScheduler Scheduler;

        /// <summary>
        /// List of active machines to schedule.
        /// </summary>
        private List<Machine> ActiveMachines;

        /// <summary>
        /// Map from machines to their infos.
        /// </summary>
        private Dictionary<Machine, MachineInfo> MachineInfoMap;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BugFinder()
        {
            this.Scheduler = Runtime.Options.Scheduler;
            this.ActiveMachines = new List<Machine>();
            this.MachineInfoMap = new Dictionary<Machine, MachineInfo>();
        }

        /// <summary>
        /// Schedule the next machine to execute.
        /// </summary>
        /// <param name="machine">Machine</param>
        public void Schedule(Machine machine)
        {
            this.MachineInfoMap[machine].IsActive = false;

            var next = this.Scheduler.Next(this.ActiveMachines);
            if (next == null)
            {
                return;
            }

            Utilities.WriteSchedule("<ScheduleLog> Machine {0}({1}) is scheduled.",
                machine, machine.Id);

            if (machine.Id != next.Id)
            {
                lock (next)
                {
                    this.MachineInfoMap[next].IsActive = true;
                    System.Threading.Monitor.PulseAll(next);
                }

                if (!this.MachineInfoMap[machine].IsPaused &&
                    !this.MachineInfoMap[machine].IsHalted)
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
        }

        /// <summary>
        /// Notify that the machine has finished its current event
        /// handling loop.
        /// </summary>
        /// <param name="machine">Machine</param>
        public void NotifyHandlerStarted(Machine machine)
        {
            if (!this.ActiveMachines.Contains(machine))
            {
                this.ActiveMachines.Add(machine);
                if (!this.MachineInfoMap.ContainsKey(machine))
                {
                    this.MachineInfoMap.Add(machine, new MachineInfo(machine.Id));
                }
            }

            if (this.ActiveMachines.Count == 1)
            {
                this.MachineInfoMap[machine].IsActive = true;
            }
            
            lock (machine)
            {
                while (!this.MachineInfoMap[machine].IsActive)
                {
                    System.Threading.Monitor.Wait(machine);
                }
            }
        }

        /// <summary>
        /// Notify that the machine has paused its current event
        /// handling loop, because its event queue is empty.
        /// </summary>
        /// <param name="machine">Machine</param>
        public void NotifyHandlerPaused(Machine machine)
        {
            this.MachineInfoMap[machine].IsActive = false;
            this.MachineInfoMap[machine].IsPaused = true;
            this.ActiveMachines.Remove(machine);
            this.Schedule(machine);
        }

        /// <summary>
        /// Notify that the machine has halted.
        /// </summary>
        /// <param name="machine">Machine</param>
        public void NotifyMachineHalted(Machine machine)
        {
            this.MachineInfoMap[machine].IsActive = false;
            this.MachineInfoMap[machine].IsHalted = true;
            this.ActiveMachines.Remove(machine);
            this.Schedule(machine);
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
        /// <param name="machine">Machine</param>
        public void NotifyAssertionFailure()
        {

        }

        /// <summary>
        /// Resets the state of the bug-finder.
        /// </summary>
        public void Reset()
        {
            this.Scheduler.Reset();
            this.ActiveMachines.Clear();
        }
    }
}
