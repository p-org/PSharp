//-----------------------------------------------------------------------
// <copyright file="LivenessChecker.cs" company="Microsoft">
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

using Microsoft.PSharp.StateCaching;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing the P# liveness property checker.
    /// </summary>
    internal sealed class LivenessChecker
    {
        #region fields

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private List<Monitor> Monitors;

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        internal LivenessChecker()
        {
            this.Monitors = new List<Monitor>();
        }

        /// <summary>
        /// Registers a new monitor.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        internal void RegisterMonitor(Monitor monitor)
        {
            this.Monitors.Add(monitor);
        }

        /// <summary>
        /// Runs the liveness checker and reports any liveness property violations.
        /// Requires the P# program to have reached quiescence.
        /// </summary>
        internal void Run()
        {
            if (!Configuration.CheckLiveness)
            {
                return;
            }

            foreach (var monitor in this.Monitors)
            {
                var stateName = "";
                if (monitor.IsInHotState(out stateName))
                {
                    string message = Output.Format("Monitor '{0}' detected liveness property " +
                        "violation in hot state '{1}'.", monitor.GetType().Name, stateName);
                    ErrorReporter.Report(message);
                    PSharpRuntime.BugFinder.NotifyAssertionFailure(false);
                }
            }
        }

        /// <summary>
        /// Checks liveness at a trace cycle.
        /// </summary>
        /// <param name="root">Cycle start</param>
        internal void CheckLivenessAtTraceCycle(Fingerprint root, Trace trace)
        {
            var cycle = new List<TraceStep>();

            do
            {
                Output.Log("<LivenessDebug> Cycle contains program state with fingerprint '{0}'.",
                    trace.Peek().Fingerprint.ToString());
                cycle.Add(trace.Pop());
            }
            while (!trace.Peek().Fingerprint.Equals(root));

            if (!this.IsSchedulingFair(cycle))
            {
                Output.Log("<LivenessDebug> Cycle execution is unfair.");
                return;
            }

            Output.Log("<LivenessDebug> Cycle execution is fair.");

            var hotMonitors = this.GetHotMonitors(cycle);
            foreach (var monitor in hotMonitors)
            {
                string message = Output.Format("Monitor '{0}' detected infinite execution that " +
                    "violates a liveness property.", monitor.GetType().Name);
                ErrorReporter.Report(message);
                PSharpRuntime.BugFinder.NotifyAssertionFailure(false);
            }

            PSharpRuntime.BugFinder.Stop();
        }

        /// <summary>
        /// Returns the monitor status.
        /// </summary>
        /// <returns>Monitor status</returns>
        internal Dictionary<Monitor, MonitorStatus> GetMonitorStatus()
        {
            var monitors = new Dictionary<Monitor, MonitorStatus>();
            foreach (var monitor in this.Monitors)
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

                monitors.Add(monitor, status);
            }

            return monitors;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Checks if the scheduling is fair in a trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        private bool IsSchedulingFair(List<TraceStep> cycle)
        {
            var result = false;

            var enabledMachines = new HashSet<Machine>();
            var scheduledMachines = new HashSet<Machine>();

            foreach (var step in cycle)
            {
                scheduledMachines.Add(step.ScheduledMachine);
                enabledMachines.UnionWith(step.EnabledMachines);
            }

            if (enabledMachines.Count == scheduledMachines.Count)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Gets all monitors that are in hot state, but not in cold
        /// state during the trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        /// <returns>Monitors</returns>
        private HashSet<Monitor> GetHotMonitors(List<TraceStep> cycle)
        {
            var monitors = new HashSet<Monitor>();

            foreach (var step in cycle)
            {
                foreach (var kvp in step.MonitorStatus)
                {
                    if (kvp.Value == MonitorStatus.Hot)
                    {
                        monitors.Add(kvp.Key);
                    }
                }
            }

            if (monitors.Count > 0)
            {
                foreach (var step in cycle)
                {
                    foreach (var kvp in step.MonitorStatus)
                    {
                        if (kvp.Value == MonitorStatus.Cold &&
                            monitors.Contains(kvp.Key))
                        {
                            monitors.Remove(kvp.Key);
                        }
                    }
                }
            }

            return monitors;
        }

        #endregion
    }
}
