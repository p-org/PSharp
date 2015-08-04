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
        /// Cached program trace.
        /// </summary>
        private Trace Trace;

        /// <summary>
        /// A map from trace steps to states.
        /// </summary>
        private Dictionary<TraceStep, State> StateMap;

        /// <summary>
        /// The unique fingerprints in the trace.
        /// </summary>
        private HashSet<Fingerprint> Fingerprints;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal StateCache()
        {
            this.Trace = new Trace();
            this.StateMap = new Dictionary<TraceStep, State>();
            this.Fingerprints = new HashSet<Fingerprint>();
        }

        /// <summary>
        /// Caches the program state at a scheduling choice.
        /// </summary>
        /// <param name="scheduledMachine">Scheduled machine</param>
        internal void CacheSchedulingChoice(Machine scheduledMachine)
        {
            var enabledMachines = PSharpRuntime.BugFinder.GetEnabledMachines();

            var traceStep = TraceStep.CreateSchedulingChoice(this.StateMap.Count, scheduledMachine);
            var fingerprint = PSharpRuntime.CaptureProgramState();
            var state = new State(fingerprint, enabledMachines, PSharpRuntime.LivenessChecker.GetMonitorStatus());

            Output.Debug(DebugType.Liveness, "<LivenessDebug> Captured program state '{0}' at " +
                "scheduling choice.", fingerprint.GetHashCode());

            this.Trace.Push(traceStep);
            this.StateMap.Add(traceStep, state);

            if (this.Fingerprints.Contains(state.Fingerprint) && Configuration.CheckLiveness)
            {
                Output.Debug(DebugType.Liveness, "<LivenessDebug> Detected potential infinite execution.");
                PSharpRuntime.LivenessChecker.CheckLivenessAtTraceCycle(state.Fingerprint, this.Trace, this.StateMap);
                this.RemoveNonExistingFingerprints();
            }

            this.Fingerprints.Add(state.Fingerprint);
        }

        /// <summary>
        /// Caches the program state at a nondeterministic choice.
        /// </summary>
        /// <param name="uniqueId">Unique nondet id</param>
        /// <param name="choice">Choice</param>
        internal void CacheNondeterministicChoice(string uniqueId, bool choice)
        {
            var enabledMachines = PSharpRuntime.BugFinder.GetEnabledMachines();

            var traceStep = TraceStep.CreateNondeterministicChoice(this.StateMap.Count, uniqueId, choice);
            var fingerprint = PSharpRuntime.CaptureProgramState();
            var state = new State(fingerprint, enabledMachines, PSharpRuntime.LivenessChecker.GetMonitorStatus());

            Output.Debug(DebugType.Liveness, "<LivenessDebug> Captured program state '{0}' at " +
                "nondeterministic choice '{1}-{2}'.", fingerprint.GetHashCode(), uniqueId, choice);

            this.Trace.Push(traceStep);
            this.StateMap.Add(traceStep, state);

            if (this.Fingerprints.Contains(state.Fingerprint) && Configuration.CheckLiveness)
            {
                Output.Debug(DebugType.Liveness, "<LivenessDebug> Detected potential infinite execution.");
                PSharpRuntime.LivenessChecker.CheckLivenessAtTraceCycle(state.Fingerprint, this.Trace, this.StateMap);
                this.RemoveNonExistingFingerprints();
            }

            this.Fingerprints.Add(state.Fingerprint);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Removes non-existing fingerprints.
        /// </summary>
        private void RemoveNonExistingFingerprints()
        {
            foreach (var fingerprint in this.Fingerprints.ToList())
            {
                if (!this.StateMap.Values.Any(val => val.Fingerprint.Equals(fingerprint)))
                {
                    this.Fingerprints.Remove(fingerprint);
                }
            }
        }

        #endregion
    }
}
