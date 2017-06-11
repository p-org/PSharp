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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing a replaying scheduling strategy.
    /// </summary>
    internal sealed class ReplayStrategy : ISchedulingStrategy
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

        /// <summary>
        /// Is the scheduler that produced the
        /// schedule trace fair?
        /// </summary>
        private bool IsSchedulerFair;

        /// <summary>
        /// Text describing a replay error.
        /// </summary>
        internal string ErrorText { get; private set; }

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="trace">ScheduleTrace</param>
        /// <param name="isFair">Is scheduler fair</param>
        public ReplayStrategy(Configuration configuration, ScheduleTrace trace, bool isFair)
        {
            this.Configuration = configuration;
            this.ScheduleTrace = trace;
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
            this.IsSchedulerFair = isFair;
            this.ErrorText = "";
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool TryGetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                next = null;
                return false;
            }

            try
            {
                if (this.ExploredSteps >= this.ScheduleTrace.Count)
                {
                    this.ErrorText = "Trace is not reproducible: execution is longer than trace.";
                    throw new InvalidOperationException(this.ErrorText);
                }

                ScheduleStep nextStep = this.ScheduleTrace[this.ExploredSteps];
                if (nextStep.Type != ScheduleStepType.SchedulingChoice)
                {
                    this.ErrorText = "Trace is not reproducible: next step is not a scheduling choice.";
                    throw new InvalidOperationException(this.ErrorText);
                }
                
                next = enabledChoices.FirstOrDefault(choice => choice.Id == nextStep.ScheduledMachineId);
                if (next == null)
                {
                    this.ErrorText = $"Trace is not reproducible: cannot detect id '{nextStep.ScheduledMachineId}'.";
                    throw new InvalidOperationException(this.ErrorText);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!this.Configuration.DisableEnvironmentExit)
                {
                    Error.ReportAndExit(ex.Message);
                }

                next = null;
                return false;
            }

            this.ExploredSteps++;

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
            ScheduleStep nextStep = null;

            try
            {
                if (this.ExploredSteps >= this.ScheduleTrace.Count)
                {
                    this.ErrorText = "Trace is not reproducible: execution is longer than trace.";
                    throw new InvalidOperationException(this.ErrorText);
                }

                nextStep = this.ScheduleTrace[this.ExploredSteps];
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
                {
                    this.ErrorText = "Trace is not reproducible: next step is not a nondeterministic choice.";
                    throw new InvalidOperationException(this.ErrorText);
                }

                if (nextStep.BooleanChoice == null)
                {
                    this.ErrorText = "Trace is not reproducible: next step is not a nondeterministic boolean choice.";
                    throw new InvalidOperationException(this.ErrorText);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!this.Configuration.DisableEnvironmentExit)
                {
                    Error.ReportAndExit(ex.Message);
                }

                next = false;
                return false;
            }

            next = nextStep.BooleanChoice.Value;
            this.ExploredSteps++;

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
            ScheduleStep nextStep = null;

            try
            {
                if (this.ExploredSteps >= this.ScheduleTrace.Count)
                {
                    this.ErrorText = "Trace is not reproducible: execution is longer than trace.";
                    throw new InvalidOperationException(this.ErrorText);
                }

                nextStep = this.ScheduleTrace[this.ExploredSteps];
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
                {
                    this.ErrorText = "Trace is not reproducible: next step is not a nondeterministic choice.";
                    throw new InvalidOperationException(this.ErrorText);
                }

                if (nextStep.IntegerChoice == null)
                {
                    this.ErrorText = "Trace is not reproducible: next step is not a nondeterministic integer choice.";
                    throw new InvalidOperationException(this.ErrorText);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!this.Configuration.DisableEnvironmentExit)
                {
                    Error.ReportAndExit(ex.Message);
                }

                next = 0;
                return false;
            }

            next = nextStep.IntegerChoice.Value;
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
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
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
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return this.IsSchedulerFair;
        }

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        /// <returns>False if all schedules have been explored</returns>
        public bool PrepareForNextIteration()
        {
            this.MaxExploredSteps = Math.Max(this.MaxExploredSteps, this.ExploredSteps);
            this.ExploredSteps = 0;
            return false;
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
