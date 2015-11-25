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

            var availableMachines = machines.Where(
                m => m.IsEnabled && !m.IsBlocked && !m.IsWaiting).ToList();
            if (availableMachines.Count == 0)
            {
                return false;
            }

            this.ExploredSteps++;

            if (this.InputCache.Count >= this.ExploredSteps)
            {
                var id = Convert.ToInt32(this.InputCache[this.ExploredSteps - 1]);
                next = availableMachines.Find(m => m.Machine.Id.Value == id);
            }
            else
            {
                IO.PrintLine(">> Available machines to schedule ...");
                foreach (var m in availableMachines)
                    IO.PrintLine(">>   '{0}({1})' with id '{2}'", m.Machine, m.Machine.Id.MVal, m.Machine.Id.Value);
                IO.PrintLine(">> Choose id of machine to schedule [step '{0}']", this.ExploredSteps);

                var parsed = false;
                while (!parsed)
                {
                    var input = IO.GetLine();
                    if (input.Equals("replay"))
                    {
                        IO.PrintLine(">> Replay up to first ?? steps [step '{0}']", this.ExploredSteps);

                        try
                        {
                            var steps = Convert.ToInt32(IO.GetLine());
                            if (steps < 0)
                            {
                                IO.PrintLine(">> Expected positive integer, retry ...");
                                continue;
                            }

                            this.ManipulateInputCache(steps);
                        }
                        catch (FormatException)
                        {
                            IO.PrintLine(">> Wrong format, retry ...");
                            continue;
                        }

                        this.Configuration.SchedulingIterations++;
                        this.ConfigureNextIteration();
                        return false;
                    }
                    else if (input.Equals("reset"))
                    {
                        this.Configuration.SchedulingIterations++;
                        this.Reset();
                        return false;
                    }

                    try
                    {
                        var id = Convert.ToInt32(input);
                        next = availableMachines.Find(m => m.Machine.Id.Value == id);
                    }
                    catch (FormatException)
                    {
                        IO.PrintLine(">> Wrong format, retry ...");
                        continue;
                    }

                    this.InputCache.Add(input);
                    parsed = true;
                }
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

            if (this.InputCache.Count >= this.ExploredSteps)
            {
                next = Convert.ToBoolean(this.InputCache[this.ExploredSteps - 1]);
            }
            else
            {
                IO.PrintLine(">> Choose true or false [step '{0}']", this.ExploredSteps);

                var parsed = false;
                while (!parsed)
                {
                    var input = IO.GetLine();
                    if (input.Equals("replay"))
                    {
                        IO.PrintLine(">> Replay up to first ?? steps [step '{0}']", this.ExploredSteps);

                        try
                        {
                            var steps = Convert.ToInt32(IO.GetLine());
                            if (steps < 0)
                            {
                                IO.PrintLine(">> Expected positive integer, retry ...");
                                continue;
                            }

                            this.ManipulateInputCache(steps);
                        }
                        catch (FormatException)
                        {
                            IO.PrintLine(">> Wrong format, retry ...");
                            continue;
                        }

                        this.Configuration.SchedulingIterations++;
                        this.ConfigureNextIteration();
                        return false;
                    }
                    else if (input.Equals("reset"))
                    {
                        this.Configuration.SchedulingIterations++;
                        this.Reset();
                        return false;
                    }

                    try
                    {
                        next = Convert.ToBoolean(input);
                    }
                    catch (FormatException)
                    {
                        IO.PrintLine(">> Wrong format, retry ...");
                        continue;
                    }

                    this.InputCache.Add(input);
                    parsed = true;
                }
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
        /// Manipulates the input cache.
        /// </summary>
        /// <param name="steps">Number of steps</param>
        private void ManipulateInputCache(int steps)
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
