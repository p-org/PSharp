// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing an interactive scheduling strategy.
    /// </summary>
    internal sealed class InteractiveStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private IO.ILogger Logger;

        /// <summary>
        /// The input cache.
        /// </summary>
        private List<string> InputCache;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        private int ExploredSteps;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        public InteractiveStrategy(Configuration configuration, IO.ILogger logger)
        {
            this.Logger = logger ?? new ConsoleLogger();
            this.Configuration = configuration;
            this.InputCache = new List<string>();
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            next = null;

            List<ISchedulable> enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();

            if (enabledChoices.Count == 0)
            {
                this.Logger.WriteLine(">> No available machines to schedule ...");
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

                    next = enabledChoices[idx];
                    parsed = true;
                    break;
                }

                this.Logger.WriteLine(">> Available machines to schedule ...");
                for (int idx = 0; idx < enabledChoices.Count; idx++)
                {
                    var choice = enabledChoices[idx];
                    this.Logger.WriteLine($">> [{idx}] '{choice.Name}'");
                }

                this.Logger.WriteLine($">> Choose machine to schedule [step '{this.ExploredSteps}']");

                var input = Console.ReadLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.SchedulingIterations++;
                    this.PrepareForNextIteration();
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
                            this.Logger.WriteLine(">> Expected positive integer, please retry ...");
                            continue;
                        }

                        next = enabledChoices[idx];
                        if (next == null)
                        {
                            this.Logger.WriteLine(">> Unexpected id, please retry ...");
                            continue;
                        }
                    }
                    catch (FormatException)
                    {
                        this.Logger.WriteLine(">> Wrong format, please retry ...");
                        continue;
                    }
                }
                else
                {
                    next = enabledChoices[0];
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

                this.Logger.WriteLine($">> Choose true or false [step '{this.ExploredSteps}']");

                var input = Console.ReadLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.SchedulingIterations++;
                    this.PrepareForNextIteration();
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
                        this.Logger.WriteLine(">> Wrong format, please retry ...");
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

                this.Logger.WriteLine($">> Choose an integer (< {maxValue}) [step '{this.ExploredSteps}']");

                var input = Console.ReadLine();
                if (input.Equals("replay"))
                {
                    if (!this.Replay())
                    {
                        continue;
                    }

                    this.Configuration.SchedulingIterations++;
                    this.PrepareForNextIteration();
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
                        this.Logger.WriteLine(">> Wrong format, please retry ...");
                        continue;
                    }
                }

                if (next >= maxValue)
                {
                    this.Logger.WriteLine($">> {next} is >= {maxValue}, please retry ...");
                }

                this.InputCache.Add(input);
                parsed = true;
            }

            return true;
        }

        /// <summary>
        /// Forces the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public bool PrepareForNextIteration()
        {
            this.ExploredSteps = 0;
            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.InputCache.Clear();
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
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
            var bound = (this.IsFair() ? this.Configuration.MaxFairSchedulingSteps :
                this.Configuration.MaxUnfairSchedulingSteps);

            if (bound == 0)
            {
                return false;
            }

            return this.ExploredSteps >= bound;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return false;
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
        /// Replays an earlier point of the execution.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool Replay()
        {
            var result = true;

            this.Logger.WriteLine($">> Replay up to first ?? steps [step '{this.ExploredSteps}']");

            try
            {
                var steps = Convert.ToInt32(Console.ReadLine());
                if (steps < 0)
                {
                    this.Logger.WriteLine(">> Expected positive integer, please retry ...");
                    result = false;
                }

                this.RemoveFromInputCache(steps);
            }
            catch (FormatException)
            {
                this.Logger.WriteLine(">> Wrong format, please retry ...");
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

            this.Logger.WriteLine($">> Jump to ?? step [step '{this.ExploredSteps}']");

            try
            {
                var steps = Convert.ToInt32(Console.ReadLine());
                if (steps < this.ExploredSteps)
                {
                    this.Logger.WriteLine(">> Expected integer greater than " +
                        $"{this.ExploredSteps}, please retry ...");
                    result = false;
                }

                this.AddInInputCache(steps);
            }
            catch (FormatException)
            {
                this.Logger.WriteLine(">> Wrong format, please retry ...");
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
