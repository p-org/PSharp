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

using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.StateCaching
{
    /// <summary>
    /// Class implementing a P# state cache.
    /// </summary>
    internal sealed class StateCache
    {
        #region fields

        /// <summary>
        /// The P# runtime.
        /// </summary>
        private PSharpBugFindingRuntime Runtime;

        /// <summary>
        /// A map from schedule steps to states.
        /// </summary>
        private Dictionary<ScheduleStep, State> StateMap;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        internal StateCache(PSharpBugFindingRuntime runtime)
        {
            this.Runtime = runtime;
            this.StateMap = new Dictionary<ScheduleStep, State>();
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
                return this.StateMap[key];
            }
        }

        /// <summary>
        /// Captures a snapshot of the program state.
        /// </summary>
        /// <param name="scheduleStep">ScheduleStep</param>
        internal void CaptureState(ScheduleStep scheduleStep)
        {
            var fingerprint = this.Runtime.GetProgramState();
            var enabledMachines = this.Runtime.BugFinder.GetEnabledMachines();
            var state = new State(fingerprint, enabledMachines, this.Runtime.LivenessChecker.GetMonitorStatus());

            if (scheduleStep.Type == ScheduleStepType.SchedulingChoice)
            {
                IO.Debug("<LivenessDebug> Captured program state '{0}' at " +
                    "scheduling choice.", fingerprint.GetHashCode());
            }
            else if (scheduleStep.Type == ScheduleStepType.NondeterministicChoice &&
                scheduleStep.BooleanChoice != null)
            {
                IO.Debug("<LivenessDebug> Captured program state '{0}' at nondeterministic " +
                    "choice '{1}'.", fingerprint.GetHashCode(), scheduleStep.BooleanChoice.Value);
            }
            else if (scheduleStep.Type == ScheduleStepType.FairNondeterministicChoice &&
                scheduleStep.BooleanChoice != null)
            {
                IO.Debug("<LivenessDebug> Captured program state '{0}' at fair nondeterministic choice " +
                    "'{1}-{2}'.", fingerprint.GetHashCode(), scheduleStep.NondetId, scheduleStep.BooleanChoice.Value);
            }
            else if (scheduleStep.Type == ScheduleStepType.NondeterministicChoice &&
                scheduleStep.IntegerChoice != null)
            {
                IO.Debug("<LivenessDebug> Captured program state '{0}' at nondeterministic " +
                    "choice '{1}'.", fingerprint.GetHashCode(), scheduleStep.IntegerChoice.Value);
            }

            var stateExists = this.StateMap.Values.Any(val => val.Fingerprint.Equals(fingerprint));

            this.StateMap.Add(scheduleStep, state);

            if (stateExists)
            {
                IO.Debug("<LivenessDebug> Detected potential infinite execution.");
                this.Runtime.LivenessChecker.CheckLivenessAtTraceCycle(state.Fingerprint);
            }
        }

        /// <summary>
        /// Removes the specified schedule step from the cache.
        /// </summary>
        /// <param name="scheduleStep">ScheduleStep</param>
        internal void Remove(ScheduleStep scheduleStep)
        {
            this.StateMap.Remove(scheduleStep);
        }

        #endregion
    }
}
