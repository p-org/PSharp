// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Strategy for detecting liveness property violations using the "temperature"
    /// method. It contains a nested <see cref="ISchedulingStrategy"/> that is used
    /// for scheduling decisions. Note that liveness property violations are checked
    /// only if the nested strategy is fair.
    /// </summary>
    internal sealed class TemperatureCheckingStrategy : LivenessCheckingStrategy
    {
        /// <summary>
        /// Creates a liveness strategy that checks the specific monitors
        /// for liveness property violations, and uses the specified
        /// strategy for scheduling decisions.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="monitors">List of monitors</param>
        /// <param name="strategy">ISchedulingStrategy</param>
        internal TemperatureCheckingStrategy(Configuration configuration, List<Monitor> monitors, ISchedulingStrategy strategy)
            : base(configuration, monitors, strategy)
        { }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public override bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            CheckLivenessTemperature();
            return SchedulingStrategy.GetNext(out next, choices, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public override bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            CheckLivenessTemperature();
            return SchedulingStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public override bool GetNextIntegerChoice(int maxValue, out int next)
        {
            CheckLivenessTemperature();
            return SchedulingStrategy.GetNextIntegerChoice(maxValue, out next);
        }

        /// <summary>
        /// Checks the liveness temperature of each monitor, and
        /// reports an error if one of the liveness monitors has
        /// passed the temperature threshold.
        /// </summary>
        private void CheckLivenessTemperature()
        {
            if (IsFair())
            {
                foreach (var monitor in Monitors)
                {
                    monitor.CheckLivenessTemperature();
                }
            }
        }
    }
}
