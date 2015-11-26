//-----------------------------------------------------------------------
// <copyright file="InteractiveStrategy.cs" company="Microsoft">
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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Class representing an interactive scheduling strategy.
    /// </summary>
    public class InteractiveStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// The input cache.
        /// </summary>
        private List<string> InputCache;

        /// <summary>
        /// The maximum number of explored steps.
        /// </summary>
        private int MaxExploredSteps;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        private int ExploredSteps;

        /// <summary>
        /// The prioritized operation id.
        /// </summary>
        protected int PrioritizedOperationId;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public InteractiveStrategy(Configuration configuration)
        {
            this.Configuration = configuration;
            this.InputCache = new List<string>();
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
            this.PrioritizedOperationId = 0;
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="machines">Machines</param>
        /// <param name="currentMachine">Curent machine</param>
        /// <returns>Boolean value</returns>
        public bool TryGetNext(out MachineInfo next, List<MachineInfo> machines, MachineInfo currentMachine)
        {
            next = null;

            List<MachineInfo> availableMachines;
            if (this.Configuration.BoundOperations)
            {
                availableMachines = this.GetPrioritizedMachines(machines, currentMachine);
            }
            else
            {
                machines = machines.OrderBy(machine => machine.Machine.Id.Value).ToList();
                availableMachines = machines.Where(
                    m => m.IsEnabled && !m.IsBlocked && !m.IsWaiting).ToList();
            }
            
            if (availableMachines.Count == 0)
            {
                IO.PrintLine(">> No available machines to schedule ...");
                return false;
            }
            
            this.ExploredSteps++;

            var parsed = false;
            while (!parsed)
            {
                if (this.InputCache.Count >= this.ExploredSteps)
                {
                    var step = this.InputCache[this.ExploredSteps - 1];
                    int idx = 0;
                    if (step.Length > 0)
                    {
                        idx = Convert.ToInt32(step);
                    }
                    else
                    {
                        this.InputCache[this.ExploredSteps - 1] = "0";
                    }

                    next = availableMachines[idx];
                    parsed = true;
                    break;
                }

                IO.PrintLine(">> Available machines to schedule ...");
                for (int idx = 0; idx < availableMachines.Count; idx++)
                {
                    var m = availableMachines[idx];
                    if (this.Configuration.BoundOperations)
                    {
                        IO.PrintLine(">> [{0}] '{1}({2})' with operation id '{3}'",
                            idx, m.Machine, m.Machine.Id.MVal, m.Machine.OperationId);
                    }
                    else
                    {
                        IO.PrintLine(">> [{0}] '{1}({2})'", idx, m.Machine, m.Machine.Id.MVal);
                    }
                }

                IO.PrintLine(">> Choose machine to schedule [step '{0}']", this.ExploredSteps);

                var input = IO.GetLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.SchedulingIterations++;
                    this.ConfigureNextIteration();
                    return false;
                }
                else if (input.Equals("jump"))
                {
                    this.Jump();
                    continue;
                }
                else if (input.Equals("reset"))
                {
                    this.Configuration.SchedulingIterations++;
                    this.Reset();
                    return false;
                }
                else if (input.Length > 0)
                {
                    try
                    {
                        var idx = Convert.ToInt32(input);
                        if (idx < 0)
                        {
                            IO.PrintLine(">> Expected positive integer, please retry ...");
                            continue;
                        }

                        next = availableMachines[idx];
                        if (next == null)
                        {
                            IO.PrintLine(">> Unexpected id, please retry ...");
                            continue;
                        }

                        if (this.Configuration.BoundOperations)
                        {
                            this.PrioritizedOperationId = next.Machine.OperationId;
                        }
                    }
                    catch (FormatException)
                    {
                        IO.PrintLine(">> Wrong format, please retry ...");
                        continue;
                    }
                }
                else
                {
                    next = availableMachines[0];
                }

                this.InputCache.Add(input);
                parsed = true;
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
            this.ExploredSteps++;

            var parsed = false;
            while (!parsed)
            {
                if (this.InputCache.Count >= this.ExploredSteps)
                {
                    var step = this.InputCache[this.ExploredSteps - 1];
                    if (step.Length > 0)
                    {
                        next = Convert.ToBoolean(this.InputCache[this.ExploredSteps - 1]);
                    }
                    else
                    {
                        this.InputCache[this.ExploredSteps - 1] = "false";
                    }
                    
                    parsed = true;
                    break;
                }

                IO.PrintLine(">> Choose true or false [step '{0}']", this.ExploredSteps);

                var input = IO.GetLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.SchedulingIterations++;
                    this.ConfigureNextIteration();
                    return false;
                }
                else if (input.Equals("jump"))
                {
                    this.Jump();
                    continue;
                }
                else if (input.Equals("reset"))
                {
                    this.Configuration.SchedulingIterations++;
                    this.Reset();
                    return false;
                }
                else if (input.Length > 0)
                {
                    try
                    {
                        next = Convert.ToBoolean(input);
                    }
                    catch (FormatException)
                    {
                        IO.PrintLine(">> Wrong format, please retry ...");
                        continue;
                    }
                }

                this.InputCache.Add(input);
                parsed = true;
            }

            return true;
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            return this.ExploredSteps;
        }

        /// <summary>
        /// Returns the maximum explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetMaxExploredSteps()
        {
            return this.MaxExploredSteps;
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

            return this.ExploredSteps == this.GetDepthBound();
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
            this.MaxExploredSteps = Math.Max(this.MaxExploredSteps, this.ExploredSteps);
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.InputCache.Clear();
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "";
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the prioritized machines.
        /// </summary>
        /// <param name="machines">Machines</param>
        /// <param name="currentMachine">Curent machine</param>
        /// <returns>Boolean value</returns>
        private List<MachineInfo> GetPrioritizedMachines(List<MachineInfo> machines, MachineInfo currentMachine)
        {
            machines = machines.OrderBy(mi => mi.Machine.Id.Value).ToList();
            machines = machines.OrderBy(mi => mi.Machine.OperationId).ToList();

            MachineInfo priorityMachine = currentMachine;
            var prioritizedMachines = new List<MachineInfo>();
            if (currentMachine.Machine.OperationId == this.PrioritizedOperationId)
            {
                var currentMachineIdx = machines.IndexOf(currentMachine);
                prioritizedMachines = machines.GetRange(currentMachineIdx, machines.Count - currentMachineIdx);
                if (currentMachineIdx != 0)
                {
                    prioritizedMachines.AddRange(machines.GetRange(0, currentMachineIdx));
                }
            }
            else
            {
                priorityMachine = machines.First(mi => mi.Machine.OperationId == this.PrioritizedOperationId);
                var priorityMachineIdx = machines.IndexOf(priorityMachine);
                prioritizedMachines = machines.GetRange(priorityMachineIdx, machines.Count - priorityMachineIdx);
                if (priorityMachineIdx != 0)
                {
                    prioritizedMachines.AddRange(machines.GetRange(0, priorityMachineIdx));
                }
            }

            prioritizedMachines = prioritizedMachines.Where(mi => mi.IsEnabled && !mi.IsBlocked && !mi.IsWaiting).ToList();
            if (machines.Count == 0)
            {
                return prioritizedMachines;
            }

            return prioritizedMachines;
        }

        /// <summary>
        /// Replays an earlier point of the execution.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool Replay()
        {
            var result = true;

            IO.PrintLine(">> Replay up to first ?? steps [step '{0}']", this.ExploredSteps);

            try
            {
                var steps = Convert.ToInt32(IO.GetLine());
                if (steps < 0)
                {
                    IO.PrintLine(">> Expected positive integer, please retry ...");
                    result = false;
                }

                this.RemoveFromInputCache(steps);
            }
            catch (FormatException)
            {
                IO.PrintLine(">> Wrong format, please retry ...");
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Jumps to a later point in the execution.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool Jump()
        {
            var result = true;

            IO.PrintLine(">> Jump to ?? step [step '{0}']", this.ExploredSteps);

            try
            {
                var steps = Convert.ToInt32(IO.GetLine());
                if (steps < this.ExploredSteps)
                {
                    IO.PrintLine(">> Expected integer greater than {0}, please retry ...",
                        this.ExploredSteps);
                    result = false;
                }

                this.AddInInputCache(steps);
            }
            catch (FormatException)
            {
                IO.PrintLine(">> Wrong format, please retry ...");
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Adds in the input cache.
        /// </summary>
        /// <param name="steps">Number of steps</param>
        private void AddInInputCache(int steps)
        {
            if (steps > this.InputCache.Count)
            {
                this.InputCache.AddRange(Enumerable.Repeat("", steps - this.InputCache.Count));
            }
        }

        /// <summary>
        /// Removes from the input cache.
        /// </summary>
        /// <param name="steps">Number of steps</param>
        private void RemoveFromInputCache(int steps)
        {
            if (steps > 0 && steps < this.InputCache.Count)
            {
                this.InputCache.RemoveRange(steps, this.InputCache.Count - steps);
            }
            else if (steps == 0)
            {
                this.InputCache.Clear();
            }
        }

        #endregion
    }
}
