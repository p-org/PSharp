// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
        /// The suffix strategy.
        /// </summary>
        private ISchedulingStrategy SuffixStrategy;

        /// <summary>
        /// Is the scheduler that produced the
        /// schedule trace fair?
        /// </summary>
        private bool IsSchedulerFair;

        /// <summary>
        /// Is the scheduler replaying the trace?
        /// </summary>
        private bool IsReplaying;

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
            : this(configuration, trace, isFair, null)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="trace">ScheduleTrace</param>
        /// <param name="isFair">Is scheduler fair</param>
        /// <param name="suffixStrategy">The suffix strategy.</param>
        public ReplayStrategy(Configuration configuration, ScheduleTrace trace, bool isFair, ISchedulingStrategy suffixStrategy)
        {
            Configuration = configuration;
            ScheduleTrace = trace;
            ScheduledSteps = 0;
            IsSchedulerFair = isFair;
            IsReplaying = true;
            SuffixStrategy = suffixStrategy;
            ErrorText = string.Empty;
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
            if (IsReplaying)
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
                    if (SuffixStrategy == null)
                    {
                        if (!Configuration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = null;
                        return false;
                    }
                    else
                    {
                        IsReplaying = false;
                        return SuffixStrategy.GetNext(out next, choices, current);
                    }
                }

                ScheduledSteps++;
                return true;
            }

            return SuffixStrategy.GetNext(out next, choices, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            if (IsReplaying)
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
                    if (SuffixStrategy == null)
                    {
                        if (!Configuration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = false;
                        return false;
                    }
                    else
                    {
                        IsReplaying = false;
                        return SuffixStrategy.GetNextBooleanChoice(maxValue, out next);
                    }
                }

                next = nextStep.BooleanChoice.Value;
                ScheduledSteps++;
                return true;
            }

            return SuffixStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            if (IsReplaying)
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
                    if (SuffixStrategy == null)
                    {
                        if (!Configuration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = 0;
                        return false;
                    }
                    else
                    {
                        IsReplaying = false;
                        return SuffixStrategy.GetNextIntegerChoice(maxValue, out next);
                    }
                }

                next = nextStep.IntegerChoice.Value;
                ScheduledSteps++;
                return true;
            }

            return SuffixStrategy.GetNextIntegerChoice(maxValue, out next);
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new NotImplementedException();
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
            if (SuffixStrategy != null)
            {
                return SuffixStrategy.PrepareForNextIteration();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            ScheduledSteps = 0;
            SuffixStrategy?.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
        {
            if (SuffixStrategy != null)
            {
                return ScheduledSteps + SuffixStrategy.GetScheduledSteps();
            }
            else
            {
                return ScheduledSteps;
            }
        }

        /// <summary>
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (SuffixStrategy != null)
            {
                return SuffixStrategy.HasReachedMaxSchedulingSteps();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            if (SuffixStrategy != null)
            {
                return SuffixStrategy.IsFair();
            }
            else
            {
                return IsSchedulerFair;
            }
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            if (SuffixStrategy != null)
            {
                return "Replay(" + SuffixStrategy.GetDescription() + ")";
            }
            else
            {
                return "Replay";
            }
        }

        #endregion
    }
}
