//-----------------------------------------------------------------------
// <copyright file="TraceStep.cs" company="Microsoft">
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

namespace Microsoft.PSharp.StateCaching
{
    /// <summary>
    /// Class implementing a program trace step.
    /// </summary>
    internal sealed class TraceStep
    {
        #region fields

        /// <summary>
        /// The fingerprint of the trace step.
        /// </summary>
        internal Fingerprint Fingerprint { get; private set; }

        /// <summary>
        /// True if the trace step is a non-deterministic choice.
        /// False if it is a scheduling choice.
        /// </summary>
        internal bool IsChoice { get; private set; }

        /// <summary>
        /// Map from monitors to their liveness status.
        /// </summary>
        internal Dictionary<Monitor, MonitorStatus> MonitorStatus;

        /// <summary>
        /// The scheduled machine. Only relevant if this is a scheduling
        /// trace step.
        /// </summary>
        internal Machine ScheduledMachine;

        /// <summary>
        /// The enabled machines. Only relevant if this is a scheduling
        /// trace step.
        /// </summary>
        internal HashSet<Machine> EnabledMachines;

        /// <summary>
        /// The non-deterministic choice id. Only relevant if
        /// this is a choice trace step.
        /// </summary>
        internal string NondetId;

        /// <summary>
        /// The non-deterministic choice value. Only relevant if
        /// this is a choice trace step.
        /// </summary>
        internal bool Choice;

        /// <summary>
        /// Previous trace step.
        /// </summary>
        internal TraceStep Previous;

        /// <summary>
        /// Next trace step.
        /// </summary>
        internal TraceStep Next;

        #endregion

        #region internal API

        /// <summary>
        /// Creates a scheduling choice trace step.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <param name="scheduledMachine">Scheduled machine</param>
        /// <param name="enabledMachines">Enabled machines</param>
        /// <param name="monitorStatus">Monitor status</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateSchedulingChoice(Fingerprint fingerprint, Machine scheduledMachine,
            HashSet<Machine> enabledMachines, Dictionary<Monitor, MonitorStatus> monitorStatus)
        {
            var traceStep = new TraceStep();

            traceStep.IsChoice = false;
            traceStep.Fingerprint = fingerprint;

            traceStep.ScheduledMachine = scheduledMachine;
            traceStep.EnabledMachines = enabledMachines;
            traceStep.MonitorStatus = monitorStatus;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        /// <summary>
        /// Creates a nondeterministic choice trace step.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <param name="uniqueId">Unique nondet id</param>
        /// <param name="choice">Choice</param>
        /// <param name="enabledMachines">Enabled machines</param>
        /// <param name="monitorStatus">Monitor status</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateNondeterministicChoice(Fingerprint fingerprint,
            string uniqueId, bool choice, HashSet<Machine> enabledMachines,
            Dictionary<Monitor, MonitorStatus> monitorStatus)
        {
            var traceStep = new TraceStep();

            traceStep.IsChoice = true;
            traceStep.Fingerprint = fingerprint;

            traceStep.NondetId = uniqueId;
            traceStep.Choice = choice;
            traceStep.EnabledMachines = enabledMachines;
            traceStep.MonitorStatus = monitorStatus;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        #endregion
    }
}
