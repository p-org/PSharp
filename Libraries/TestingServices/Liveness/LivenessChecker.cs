//-----------------------------------------------------------------------
// <copyright file="LivenessChecker.cs">
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

using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Class implementing the P# liveness property checker.
    /// </summary>
    internal sealed class LivenessChecker
    {
        #region fields

        /// <summary>
        /// The P# runtime.
        /// </summary>
        private PSharpBugFindingRuntime Runtime;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private List<Monitor> Monitors;

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        internal LivenessChecker(PSharpBugFindingRuntime runtime)
        {
            this.Runtime = runtime;
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
        /// Checks for any liveness property violations. This method
        /// checks the liveness temperature of each monitor, and
        /// reports an error if one of the liveness monitors has
        /// passed the temperature threshold.
        /// </summary>
        internal void CheckLivenessAtShedulingStep()
        {
            // Disable this check if the lasso detection algorithm
            // is enabled.
            if (this.Runtime.Configuration.CacheProgramState)
            {
                return;
            }

            foreach (var monitor in this.Monitors)
            {
                monitor.CheckLivenessTemperature();
            }
        }

        /// <summary>
        /// Checks for any liveness property violations. Requires
        /// the P# program to have naturally terminated.
        /// </summary>
        internal void CheckLivenessAtTermination()
        {
            // Checks if the program has naturally terminated.
            if (!this.Runtime.BugFinder.HasFullyExploredSchedule)
            {
                return;
            }

            foreach (var monitor in this.Monitors)
            {
                var stateName = "";
                if (monitor.IsInHotState(out stateName))
                {
                    string message = IO.Format("Monitor '{0}' detected liveness bug " +
                        "in hot state '{1}' at the end of program execution.",
                        monitor.GetType().Name, stateName);
                    this.Runtime.BugFinder.NotifyAssertionFailure(message, false);
                }
            }
        }

        /// <summary>
        /// Checks liveness at a schedule trace cycle.
        /// </summary>
        /// <param name="root">Cycle start</param>
        /// <param name="stateMap">Map of states</param>
        internal void CheckLivenessAtTraceCycle(Fingerprint root, Dictionary<ScheduleStep, State> stateMap)
        {
            var cycle = new Dictionary<ScheduleStep, State>();

            do
            {
                var scheduleStep = this.Runtime.ScheduleTrace.Pop();
                var state = stateMap[scheduleStep];
                cycle.Add(scheduleStep, state);

                IO.Debug("<LivenessDebug> Cycle contains {0} with {1}.",
                    scheduleStep.Type, state.Fingerprint.ToString());
                
                // The state can be safely removed, because the liveness detection
                // algorithm currently removes cycles, so a specific state can only
                // appear once in the schedule trace.
                stateMap.Remove(scheduleStep);
            }
            while (this.Runtime.ScheduleTrace.Peek() != null && !stateMap[
                this.Runtime.ScheduleTrace.Peek()].Fingerprint.Equals(root));
            
            if (!this.IsSchedulingFair(cycle))
            {
                IO.Debug("<LivenessDebug> Scheduling in cycle is unfair.");
                return;
            }
            else if (!this.IsNondeterminismFair(cycle))
            {
                IO.Debug("<LivenessDebug> Nondeterminism in cycle is unfair.");
                return;
            }

            IO.Debug("<LivenessDebug> Cycle execution is fair.");
            
            var hotMonitors = this.GetHotMonitors(cycle);
            foreach (var monitor in hotMonitors)
            {
                string message = IO.Format("Monitor '{0}' detected infinite execution that " +
                    "violates a liveness property.", monitor.GetType().Name);
                this.Runtime.BugFinder.NotifyAssertionFailure(message, false);
            }

            if (hotMonitors.Count > 0)
            {
                this.Runtime.BugFinder.Stop();
            }
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
        /// Checks if the scheduling is fair in a schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        private bool IsSchedulingFair(Dictionary<ScheduleStep, State> cycle)
        {
            var result = false;

            var enabledMachines = new HashSet<AbstractMachine>();
            var scheduledMachines = new HashSet<AbstractMachine>();

            var schedulingChoiceSteps= cycle.Where(
                val => val.Key.Type == ScheduleStepType.SchedulingChoice);
            foreach (var step in schedulingChoiceSteps)
            {
                scheduledMachines.Add(step.Key.ScheduledMachine);
                enabledMachines.UnionWith(step.Value.EnabledMachines);
            }

            foreach (var m in enabledMachines)
            {
                IO.Debug("<LivenessDebug> Enabled machine {0}.", m);
            }

            foreach (var m in scheduledMachines)
            {
                IO.Debug("<LivenessDebug> Scheduled machine {0}.", m);
            }

            if (enabledMachines.Count == scheduledMachines.Count)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Checks if the nondeterminism is fair in a schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        private bool IsNondeterminismFair(Dictionary<ScheduleStep, State> cycle)
        {
            var result = false;

            var trueChoices = new HashSet<string>();
            var falseChoices = new HashSet<string>();

            var fairNondeterministicChoiceSteps = cycle.Where(
                val => val.Key.Type == ScheduleStepType.FairNondeterministicChoice);
            foreach (var step in fairNondeterministicChoiceSteps)
            {
                if (step.Key.Choice)
                {
                    trueChoices.Add(step.Key.NondetId);
                }
                else
                {
                    falseChoices.Add(step.Key.NondetId);
                }
            }

            if (trueChoices.Count == falseChoices.Count)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Gets all monitors that are in hot state, but not in cold
        /// state during the schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        /// <returns>Monitors</returns>
        private HashSet<Monitor> GetHotMonitors(Dictionary<ScheduleStep, State> cycle)
        {
            var hotMonitors = new HashSet<Monitor>();

            foreach (var step in cycle)
            {
                foreach (var kvp in step.Value.MonitorStatus)
                {
                    if (kvp.Value == MonitorStatus.Hot)
                    {
                        hotMonitors.Add(kvp.Key);
                    }
                }
            }

            if (hotMonitors.Count > 0)
            {
                foreach (var step in cycle)
                {
                    foreach (var kvp in step.Value.MonitorStatus)
                    {
                        if (kvp.Value == MonitorStatus.Cold &&
                            hotMonitors.Contains(kvp.Key))
                        {
                            hotMonitors.Remove(kvp.Key);
                        }
                    }
                }
            }

            foreach (var m in hotMonitors)
            {
                IO.Debug($"<LivenessDebug> Monitor {m} remains in the hot " +
                    "state throughout the lasso.");
            }

            return hotMonitors;
        }

        #endregion
    }
}
