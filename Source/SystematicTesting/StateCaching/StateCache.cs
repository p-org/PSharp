//-----------------------------------------------------------------------
// <copyright file="StateCache.cs" company="Microsoft">
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

            if (stateExists && this.Runtime.Configuration.CheckLiveness)
            {
                IO.Debug("<LivenessDebug> Detected potential infinite execution.");
                this.Runtime.LivenessChecker.CheckLivenessAtTraceCycle(state.Fingerprint, this.StateMap);
            }
        }

        #endregion
    }
}
