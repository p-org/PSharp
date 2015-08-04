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

using Microsoft.PSharp.Exploration;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.StateCaching
{
    /// <summary>
    /// Class implementing a P# state cache.
    /// </summary>
    internal sealed class StateCache
    {
        #region fields
        
        /// <summary>
        /// A map from trace steps to states.
        /// </summary>
        private Dictionary<TraceStep, State> StateMap;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal StateCache()
        {
            this.StateMap = new Dictionary<TraceStep, State>();
        }

        /// <summary>
        /// Captures a snapshot of the program state.
        /// </summary>
        /// <param name="traceStep">Trace step</param>
        internal void CaptureState(TraceStep traceStep)
        {
            var fingerprint = PSharpRuntime.GetProgramState();
            var enabledMachines = PSharpRuntime.BugFinder.GetEnabledMachines();
            var state = new State(fingerprint, enabledMachines, PSharpRuntime.LivenessChecker.GetMonitorStatus());

            if (traceStep.IsChoice)
            {
                Output.Debug(DebugType.Liveness, "<LivenessDebug> Captured program state '{0}' at nondeterministic " +
                    "choice '{1}-{2}'.", fingerprint.GetHashCode(), traceStep.NondetId, traceStep.Choice);
            }
            else
            {
                Output.Debug(DebugType.Liveness, "<LivenessDebug> Captured program state '{0}' at " +
                    "scheduling choice.", fingerprint.GetHashCode());
            }
            
            var stateExists = this.StateMap.Values.Any(val => val.Fingerprint.Equals(fingerprint));
            this.StateMap.Add(traceStep, state);

            if (stateExists && Configuration.CheckLiveness)
            {
                Output.Debug(DebugType.Liveness, "<LivenessDebug> Detected potential infinite execution.");
                PSharpRuntime.LivenessChecker.CheckLivenessAtTraceCycle(state.Fingerprint, this.StateMap);
            }
        }

        #endregion
    }
}
