//-----------------------------------------------------------------------
// <copyright file="InteractiveStrategy.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Scheduling
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
            this.ExploredSteps = 0;
            this.PrioritizedOperationId = 0;
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool TryGetNext(out MachineInfo next, IEnumerable<MachineInfo> choices, MachineInfo current)
        {
            next = null;

            List<MachineInfo> availableMachines;
            if (this.Configuration.BoundOperations)
            {
                availableMachines = this.GetPrioritizedMachines(choices.ToList(), current);
            }
            else
            {
                choices = choices.OrderBy(machine => machine.Machine.Id.Value).ToList();
                availableMachines = choices.Where(
                    m => m.IsEnabled && !m.IsBlocked && !m.IsWaitingToReceive).ToList();
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
                        IO.PrintLine($">> [{idx}] '{m.Machine.Id}' with " +
                            $"operation id '{m.Machine.OperationId}'");
                    }
                    else
                    {
                        IO.PrintLine($">> [{idx}] '{m.Machine.Id}'");
                    }
                }

                IO.PrintLine($">> Choose machine to schedule [step '{this.ExploredSteps}']");

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
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
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

                IO.PrintLine($">> Choose true or false [step '{this.ExploredSteps}']");

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
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = 0;
            this.ExploredSteps++;

            var parsed = false;
            while (!parsed)
            {
                if (this.InputCache.Count >= this.ExploredSteps)
                {
                    var step = this.InputCache[this.ExploredSteps - 1];
                    if (step.Length > 0)
                    {
                        next = Convert.ToInt32(this.InputCache[this.ExploredSteps - 1]);
                    }
                    else
                    {
                        this.InputCache[this.ExploredSteps - 1] = "0";
                    }

                    parsed = true;
                    break;
                }

                IO.PrintLine($">> Choose an integer (< {maxValue}) [step '{this.ExploredSteps}']");

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
                        next = Convert.ToInt32(input);
                    }
                    catch (FormatException)
                    {
                        IO.PrintLine(">> Wrong format, please retry ...");
                        continue;
                    }
                }

                if (next >= maxValue)
                {
                    IO.PrintLine($">> {next} is >= {maxValue}, please retry ...");
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
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            var bound = (IsFair() ? this.Configuration.MaxFairSchedulingSteps : this.Configuration.MaxUnfairSchedulingSteps);

            if (bound == 0)
            {
                return false;
            }

            return this.ExploredSteps == bound;
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasFinished()
        {
            return false;
        }

        /// <summary>
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return false;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
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
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        private List<MachineInfo> GetPrioritizedMachines(List<MachineInfo> choices, MachineInfo current)
        {
            choices = choices.OrderBy(mi => mi.Machine.Id.Value).ToList();
            choices = choices.OrderBy(mi => mi.Machine.OperationId).ToList();

            MachineInfo priorityMachine = current;
            var prioritizedMachines = new List<MachineInfo>();
            if (current.Machine.OperationId == this.PrioritizedOperationId)
            {
                var currentMachineIdx = choices.IndexOf(current);
                prioritizedMachines = choices.GetRange(currentMachineIdx, choices.Count - currentMachineIdx);
                if (currentMachineIdx != 0)
                {
                    prioritizedMachines.AddRange(choices.GetRange(0, currentMachineIdx));
                }
            }
            else
            {
                priorityMachine = choices.First(mi => mi.Machine.OperationId == this.PrioritizedOperationId);
                var priorityMachineIdx = choices.IndexOf(priorityMachine);
                prioritizedMachines = choices.GetRange(priorityMachineIdx, choices.Count - priorityMachineIdx);
                if (priorityMachineIdx != 0)
                {
                    prioritizedMachines.AddRange(choices.GetRange(0, priorityMachineIdx));
                }
            }

            prioritizedMachines = prioritizedMachines.Where(
                mi => mi.IsEnabled && !mi.IsBlocked && !mi.IsWaitingToReceive).ToList();
            if (prioritizedMachines.Count == 0)
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

            IO.PrintLine($">> Replay up to first ?? steps [step '{this.ExploredSteps}']");

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

            IO.PrintLine($">> Jump to ?? step [step '{this.ExploredSteps}']");

            try
            {
                var steps = Convert.ToInt32(IO.GetLine());
                if (steps < this.ExploredSteps)
                {
                    IO.PrintLine(">> Expected integer greater than " +
                        $"{this.ExploredSteps}, please retry ...");
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
