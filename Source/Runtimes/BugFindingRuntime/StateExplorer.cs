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
        internal Trace Trace;

        /// <summary>
        /// Unique fingerprints
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
                Console.WriteLine("< IDENTIFIED POTENTIAL LASO >");
                PSharpRuntime.LivenessChecker.CheckLivenessAtTraceCycle(traceStep.Fingerprint, this.Trace);
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

            if (this.Trace.Count > 0)
            {
                this.Trace[this.Trace.Count - 1].Next = traceStep;
                traceStep.Previous = this.Trace[this.Trace.Count - 1];
            }

            return traceStep;
        }

        #endregion
    }
}
