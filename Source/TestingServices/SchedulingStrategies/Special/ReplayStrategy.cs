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
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
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
        private Configuration Configuration;

        /// <summary>
        /// The P# program schedule trace.
        /// </summary>
        private ScheduleTrace ScheduleTrace;

        /// <summary>
        /// Is the scheduler that produced the
        /// schedule trace fair?
        /// </summary>
        private bool IsSchedulerFair;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

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
            Configuration = configuration;
            ScheduleTrace = trace;
            ScheduledSteps = 0;
            IsSchedulerFair = isFair;
            ErrorText = "";
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
            var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                next = null;
                return false;
            }

            try
            {
                if (ScheduledSteps >= ScheduleTrace.Count)
                {
                    ErrorText = "Trace is not reproducible: execution is longer than trace.";
                    throw new InvalidOperationException(ErrorText);
                }

                ScheduleStep nextStep = ScheduleTrace[ScheduledSteps];
                if (nextStep.Type != ScheduleStepType.SchedulingChoice)
                {
                    ErrorText = "Trace is not reproducible: next step is not a scheduling choice.";
                    throw new InvalidOperationException(ErrorText);
                }
                
                next = enabledChoices.FirstOrDefault(choice => choice.Id == nextStep.ScheduledMachineId);
                if (next == null)
                {
                    ErrorText = $"Trace is not reproducible: cannot detect id '{nextStep.ScheduledMachineId}'.";
                    throw new InvalidOperationException(ErrorText);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!Configuration.DisableEnvironmentExit)
                {
                    Error.ReportAndExit(ex.Message);
                }

                next = null;
                return false;
            }

            ScheduledSteps++;

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
                if (ScheduledSteps >= ScheduleTrace.Count)
                {
                    ErrorText = "Trace is not reproducible: execution is longer than trace.";
                    throw new InvalidOperationException(ErrorText);
                }

                nextStep = ScheduleTrace[ScheduledSteps];
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
                {
                    ErrorText = "Trace is not reproducible: next step is not a nondeterministic choice.";
                    throw new InvalidOperationException(ErrorText);
                }

                if (nextStep.BooleanChoice == null)
                {
                    ErrorText = "Trace is not reproducible: next step is not a nondeterministic boolean choice.";
                    throw new InvalidOperationException(ErrorText);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!Configuration.DisableEnvironmentExit)
                {
                    Error.ReportAndExit(ex.Message);
                }

                next = false;
                return false;
            }

            next = nextStep.BooleanChoice.Value;
            ScheduledSteps++;

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
                if (ScheduledSteps >= ScheduleTrace.Count)
                {
                    ErrorText = "Trace is not reproducible: execution is longer than trace.";
                    throw new InvalidOperationException(ErrorText);
                }

                nextStep = ScheduleTrace[ScheduledSteps];
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
                {
                    ErrorText = "Trace is not reproducible: next step is not a nondeterministic choice.";
                    throw new InvalidOperationException(ErrorText);
                }

                if (nextStep.IntegerChoice == null)
                {
                    ErrorText = "Trace is not reproducible: next step is not a nondeterministic integer choice.";
                    throw new InvalidOperationException(ErrorText);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!Configuration.DisableEnvironmentExit)
                {
                    Error.ReportAndExit(ex.Message);
                }

                next = 0;
                return false;
            }

            next = nextStep.IntegerChoice.Value;
            ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public bool PrepareForNextIteration()
        {
            ScheduledSteps = 0;
            return false;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            ScheduledSteps = 0;
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
        {
            return ScheduledSteps;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            return false;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return IsSchedulerFair;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "Replay";
        }

        #endregion
    }
}
