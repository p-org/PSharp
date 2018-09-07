﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

namespace Microsoft.PSharp.TestingServices.StateCaching
{
    /// <summary>
    /// Class implementing a P# state cache.
    /// </summary>
    internal sealed class StateCache
    {
        /// <summary>
        /// The P# testing runtime.
        /// </summary>
        private readonly TestingRuntime Runtime;

        /// <summary>
        /// Set of fingerprints.
        /// </summary>
        private readonly HashSet<Fingerprint> Fingerprints;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">TestingRuntime</param>
        internal StateCache(TestingRuntime runtime)
        {
            Runtime = runtime;
            Fingerprints = new HashSet<Fingerprint>();
        }

        /// <summary>
        /// Captures a snapshot of the program state.
        /// </summary>
        /// <param name="state">Captured state</param>
        /// <param name="fingerprint">Fingerprint</param>
        /// <param name="fingerprintIndexMap">Fingerprint to schedule step index map</param>
        /// <param name="scheduleStep">ScheduleStep</param>
        /// <param name="monitors">List of monitors</param>
        /// <returns>True if state already exists</returns>
        internal bool CaptureState(out State state, out Fingerprint fingerprint, Dictionary<Fingerprint, List<int>> fingerprintIndexMap,
            ScheduleStep scheduleStep, List<Monitor> monitors)
        {
            fingerprint = Runtime.GetProgramState();
            var enabledMachineIds = Runtime.Scheduler.GetEnabledSchedulableIds();
            state = new State(fingerprint, enabledMachineIds, GetMonitorStatus(monitors));

            if (Debug.IsEnabled)
            {
                if (scheduleStep.Type == ScheduleStepType.SchedulingChoice)
                {
                    Debug.WriteLine("<LivenessDebug> Captured program state '{0}' at " +
                        "scheduling choice.", fingerprint.GetHashCode());
                }
                else if (scheduleStep.Type == ScheduleStepType.NondeterministicChoice &&
                    scheduleStep.BooleanChoice != null)
                {
                    Debug.WriteLine("<LivenessDebug> Captured program state '{0}' at nondeterministic " +
                        "choice '{1}'.", fingerprint.GetHashCode(), scheduleStep.BooleanChoice.Value);
                }
                else if (scheduleStep.Type == ScheduleStepType.FairNondeterministicChoice &&
                    scheduleStep.BooleanChoice != null)
                {
                    Debug.WriteLine("<LivenessDebug> Captured program state '{0}' at fair nondeterministic choice " +
                        "'{1}-{2}'.", fingerprint.GetHashCode(), scheduleStep.NondetId, scheduleStep.BooleanChoice.Value);
                }
                else if (scheduleStep.Type == ScheduleStepType.NondeterministicChoice &&
                    scheduleStep.IntegerChoice != null)
                {
                    Debug.WriteLine("<LivenessDebug> Captured program state '{0}' at nondeterministic " +
                        "choice '{1}'.", fingerprint.GetHashCode(), scheduleStep.IntegerChoice.Value);
                }
            }

            var stateExists = Fingerprints.Contains(fingerprint);
            Fingerprints.Add(fingerprint);
            scheduleStep.State = state;

            if (!fingerprintIndexMap.ContainsKey(fingerprint))
            {
                var hs = new List<int> { scheduleStep.Index };
                fingerprintIndexMap.Add(fingerprint, hs);
            }
            else 
            {
                fingerprintIndexMap[fingerprint].Add(scheduleStep.Index);
            }

            return stateExists;
        }

        /// <summary>
        /// Returns the monitor status.
        /// </summary>
        /// <param name="monitors">List of monitors</param>
        /// <returns>Monitor status</returns>
        private Dictionary<Monitor, MonitorStatus> GetMonitorStatus(List<Monitor> monitors)
        {
            var monitorStatus = new Dictionary<Monitor, MonitorStatus>();
            foreach (var monitor in monitors)
            {
                MonitorStatus status = MonitorStatus.None;
                if (monitor.IsInHotState())
                {
                    status = MonitorStatus.Hot;
                }
                else if (monitor.IsInColdState())
                {
                    status = MonitorStatus.Cold;
                }

                monitorStatus.Add(monitor, status);
            }

            return monitorStatus;
        }
    }
}
