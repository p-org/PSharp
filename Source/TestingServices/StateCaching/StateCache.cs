//-----------------------------------------------------------------------
// <copyright file="StateCache.cs">
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
        /// The P# bug-finding runtime.
        /// </summary>
        private BugFindingRuntime Runtime;

        /// <summary>
        /// Map from schedule steps to states.
        /// </summary>
        private Dictionary<ScheduleStep, State> StateMap;

        /// <summary>
        /// Set of fingerprints.
        /// </summary>
        private HashSet<Fingerprint> Fingerprints;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">BugFindingRuntime</param>
        internal StateCache(BugFindingRuntime runtime)
        {
            Runtime = runtime;
            StateMap = new Dictionary<ScheduleStep, State>();
            Fingerprints = new HashSet<Fingerprint>();
        }

        /// <summary>
        /// Returns the state corresponding to the
        /// specified schedule step.
        /// </summary>
        /// <param name="key">ScheduleStep</param>
        /// <returns>State</returns>
        internal State this[ScheduleStep key]
        {
            get
            {
                return StateMap[key];
            }
        }

        /// <summary>
        /// Captures a snapshot of the program state.
        /// </summary>
        /// <param name="state">Captured state</param>
        /// <param name="scheduleStep">ScheduleStep</param>
        /// <param name="monitors">List of monitors</param>
        /// <returns>True if state already exists</returns>
        internal bool CaptureState(out State state, ScheduleStep scheduleStep, List<Monitor> monitors)
        {
            var fingerprint = Runtime.GetProgramState();
            var enabledMachineIds = Runtime.Scheduler.GetEnabledSchedulableIds();
            state = new State(fingerprint, enabledMachineIds, GetMonitorStatus(monitors));

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
            
            //var stateExists = StateMap.Values.Any(val => val.Fingerprint.Equals(fingerprint));
            var stateExists = Fingerprints.Any(val => val.Equals(fingerprint));

            StateMap.Add(scheduleStep, state);
            Fingerprints.Add(fingerprint);

            return stateExists;
        }

        /// <summary>
        /// Removes the specified schedule step from the cache.
        /// </summary>
        /// <param name="scheduleStep">ScheduleStep</param>
        internal void Remove(ScheduleStep scheduleStep)
        {
            StateMap.Remove(scheduleStep);
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
