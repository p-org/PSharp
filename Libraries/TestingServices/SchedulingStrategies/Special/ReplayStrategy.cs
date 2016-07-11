//-----------------------------------------------------------------------
// <copyright file="ReplayStrategy.cs">
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

using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing a replaying scheduling strategy.
    /// </summary>
    internal class ReplayStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// The P# program schedule trace.
        /// </summary>
        private ScheduleTrace ScheduleTrace;

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
        /// <param name="trace">ScheduleTrace</param>
        public ReplayStrategy(Configuration configuration, ScheduleTrace trace)
        {
            this.Configuration = configuration;
            this.ScheduleTrace = trace;
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
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
            var availableMachines = choices.Where(
                m => m.IsEnabled && !m.IsBlocked && !m.IsWaitingToReceive).ToList();
            if (availableMachines.Count == 0)
            {
                availableMachines = choices.Where(m => m.IsWaitingToReceive).ToList();
                if (availableMachines.Count == 0)
                {
                    next = null;
                    return false;
                }
            }

            if (this.ExploredSteps >= this.ScheduleTrace.Count)
            {
                IO.Error.ReportAndExit("Trace is not reproducible: execution is longer than trace.");
            }

            ScheduleStep nextStep = this.ScheduleTrace[this.ExploredSteps];
            if (nextStep.Type != ScheduleStepType.SchedulingChoice)
            {
                IO.Error.ReportAndExit("Trace is not reproducible: next step is not a scheduling choice.");
            }

            next = availableMachines.FirstOrDefault(m => m.Machine.Id.Type.Equals(
                nextStep.ScheduledMachineType) &&
                m.Machine.Id.Value == nextStep.ScheduledMachineId);
            if (next == null)
            {
                IO.Error.ReportAndExit("Trace is not reproducible: cannot detect machine with type " +
                    $"'{nextStep.ScheduledMachineType}' and id '{nextStep.ScheduledMachineId}'.");
            }

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextChoice(int maxValue, out bool next)
        {
            if (this.ExploredSteps >= this.ScheduleTrace.Count)
            {
                IO.Error.ReportAndExit("Trace is not reproducible: execution is longer than trace.");
            }

            ScheduleStep nextStep = this.ScheduleTrace[this.ExploredSteps];
            if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
            {
                IO.Error.ReportAndExit("Trace is not reproducible: next step is not a nondeterministic choice.");
            }

            next = nextStep.Choice;
            this.ExploredSteps++;

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
        /// Returns the maximum explored steps in all iterations.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetMaxExploredSteps()
        {
            return this.MaxExploredSteps;
        }

        /// <summary>  
        /// Returns the maximum number of scheduling steps to explore.
        /// </summary> 
        /// <returns>Max scheduling steps</returns>
        public int GetMaxSchedulingSteps()
        {
            return this.Configuration.MaxSchedulingSteps;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (this.Configuration.MaxSchedulingSteps == 0)
            {
                return false;
            }

            return this.ExploredSteps == this.GetMaxSchedulingSteps();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasFinished()
        {
            return true;
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
            this.MaxExploredSteps = 0;
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
    }
}
