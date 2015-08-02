//-----------------------------------------------------------------------
// <copyright file="StateExplorer.cs" company="Microsoft">
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
    /// Class implementing the P# program state explorer.
    /// </summary>
    internal sealed class StateExplorer
    {
        #region fields

        /// <summary>
        /// Cached program trace.
        /// </summary>
        private Trace Trace;

        /// <summary>
        /// Unique fingerprints in the trace.
        /// </summary>
        private HashSet<Fingerprint> Fingerprints;

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        internal StateExplorer()
        {
            this.Trace = new Trace();
            this.Fingerprints = new HashSet<Fingerprint>();
        }

        /// <summary>
        /// Caches the program state at a scheduling choice.
        /// </summary>
        /// <param name="scheduledMachine">Scheduled machine</param>
        internal void CacheStateAtSchedulingChoice(Machine scheduledMachine)
        {
            var enabledMachines = PSharpRuntime.BugFinder.GetEnabledMachines();
            var traceStep = this.GetSchedulingChoiceTraceStep(scheduledMachine, enabledMachines);

            this.Trace.Push(traceStep);

            if (this.Fingerprints.Contains(traceStep.Fingerprint) && Configuration.CheckLiveness)
            {
                Output.Debug(DebugType.Liveness, "<LivenessDebug> Detected potential infinite execution.");
                PSharpRuntime.LivenessChecker.CheckLivenessAtTraceCycle(traceStep.Fingerprint, this.Trace);
                this.RemoveNonExistingFingerprints();
            }

            this.Fingerprints.Add(traceStep.Fingerprint);
        }

        /// <summary>
        /// Caches the program state at a nondeterministic choice.
        /// </summary>
        /// <param name="uniqueId">Unique nondet id</param>
        /// <param name="choice">Choice</param>
        internal void CacheStateAtNondeterministicChoice(string uniqueId, bool choice)
        {
            var enabledMachines = PSharpRuntime.BugFinder.GetEnabledMachines();
            var traceStep = this.GetNondeterministicChoiceTraceStep(uniqueId, choice, enabledMachines);

            this.Trace.Push(traceStep);

            if (this.Fingerprints.Contains(traceStep.Fingerprint) && Configuration.CheckLiveness)
            {
                Output.Debug(DebugType.Liveness, "<LivenessDebug> Detected potential infinite execution.");
                PSharpRuntime.LivenessChecker.CheckLivenessAtTraceCycle(traceStep.Fingerprint, this.Trace);
                this.RemoveNonExistingFingerprints();
            }

            this.Fingerprints.Add(traceStep.Fingerprint);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the current scheduling choice as a program trace step.
        /// </summary>
        /// <param name="scheduledMachine">Scheduled machine</param>
        /// <param name="enabledMachines">Enabled machines</param>
        /// <returns>TraceStep</returns>
        private TraceStep GetSchedulingChoiceTraceStep(Machine scheduledMachine,
            HashSet<Machine> enabledMachines)
        {
            var fingerprint = PSharpRuntime.CaptureProgramState();
            var traceStep = TraceStep.CreateSchedulingChoice(fingerprint, scheduledMachine,
                enabledMachines, PSharpRuntime.LivenessChecker.GetMonitorStatus());

            Output.Debug(DebugType.Liveness, "<LivenessDebug> Captured program state '{0}' at " +
                "scheduling choice.", fingerprint.GetHashCode());

            return traceStep;
        }

        /// <summary>
        /// Returns the current nondeterministic choice as a program trace step.
        /// </summary>
        /// <param name="uniqueId">Unique nondet id</param>
        /// <param name="choice">Choice</param>
        /// <param name="enabledMachines">Enabled machines</param>
        /// <returns>TraceStep</returns>
        private TraceStep GetNondeterministicChoiceTraceStep(string uniqueId, bool choice,
            HashSet<Machine> enabledMachines)
        {
            var fingerprint = PSharpRuntime.CaptureProgramState();
            var traceStep = TraceStep.CreateNondeterministicChoice(fingerprint, uniqueId,
                choice, enabledMachines, PSharpRuntime.LivenessChecker.GetMonitorStatus());

            Output.Debug(DebugType.Liveness, "<LivenessDebug> Captured program state '{0}' at " +
                "nondeterministic choice '{1}-{2}'.", fingerprint.GetHashCode(), uniqueId, choice);

            return traceStep;
        }

        /// <summary>
        /// Removes non-existing fingerprints.
        /// </summary>
        private void RemoveNonExistingFingerprints()
        {
            foreach (var fingerprint in this.Fingerprints.ToList())
            {
                if (!this.Trace.Contains(fingerprint))
                {
                    this.Fingerprints.Remove(fingerprint);
                }
            }
        }

        #endregion
    }
}
