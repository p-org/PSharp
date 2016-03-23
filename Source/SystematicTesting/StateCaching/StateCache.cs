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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.SystematicTesting.Exploration;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.StateCaching
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
        /// A map from trace steps to states.
        /// </summary>
        private Dictionary<TraceStep, State> StateMap;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        internal StateCache(PSharpBugFindingRuntime runtime)
        {
            this.Runtime = runtime;
            this.StateMap = new Dictionary<TraceStep, State>();
        }

        /// <summary>
        /// Captures a snapshot of the program state.
        /// </summary>
        /// <param name="traceStep">Trace step</param>
        internal void CaptureState(TraceStep traceStep)
        {
            var fingerprint = this.Runtime.GetProgramState();
            var enabledMachines = this.Runtime.BugFinder.GetEnabledMachines();
            var state = new State(fingerprint, enabledMachines, this.Runtime.LivenessChecker.GetMonitorStatus());

            if (traceStep.Type == TraceStepType.SchedulingChoice)
            {
                IO.Debug("<LivenessDebug> Captured program state '{0}' at " +
                    "scheduling choice.", fingerprint.GetHashCode());
            }
            else if (traceStep.Type == TraceStepType.NondeterministicChoice)
            {
                IO.Debug("<LivenessDebug> Captured program state '{0}' at nondeterministic " +
                    "choice '{1}'.", fingerprint.GetHashCode(), traceStep.Choice);
            }
            else if (traceStep.Type == TraceStepType.FairNondeterministicChoice)
            {
                IO.Debug("<LivenessDebug> Captured program state '{0}' at fair nondeterministic choice " +
                    "'{1}-{2}'.", fingerprint.GetHashCode(), traceStep.NondetId, traceStep.Choice);
            }
            
            var stateExists = this.StateMap.Values.Any(val => val.Fingerprint.Equals(fingerprint));
            this.StateMap.Add(traceStep, state);

            if (stateExists)
            {
                IO.Debug("<LivenessDebug> Detected potential infinite execution.");
                this.Runtime.LivenessChecker.CheckLivenessAtTraceCycle(state.Fingerprint, this.StateMap);
            }
        }

        #endregion
    }
}
