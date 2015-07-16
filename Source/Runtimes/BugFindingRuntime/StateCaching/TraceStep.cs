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
        /// Map from monitors to their liveness-specific state.
        /// </summary>
        internal Dictionary<Monitor, MonitorStatus> Monitors;

        /// <summary>
        /// Map from machines to their enabled status. Only relevant
        /// if this is a scheduling trace step.
        /// </summary>
        internal Dictionary<Machine, bool> EnabledMachines;

        /// <summary>
        /// The non-deterministic choice id. Only relevant if
        /// this is a choice trace step.
        /// </summary>
        internal int ChoiceId;

        /// <summary>
        /// The non-deterministic choice value. Only relevant if
        /// this is a choice trace step.
        /// </summary>
        internal bool Choice;

        #endregion

        #region internal API

        /// <summary>
        /// Creates a scheduling choice trace step.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateSchedulingChoice(Fingerprint fingerprint)
        {
            var traceStep = new TraceStep();

            traceStep.IsChoice = false;
            traceStep.Fingerprint = fingerprint;

            traceStep.EnabledMachines = new Dictionary<Machine, bool>();

            return traceStep;
        }

        /// <summary>
        /// Creates a nondeterministic choice trace step.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <param name="id">Id of the choice</param>
        /// <param name="choice">Choice value</param>
        /// <returns>TraceStep</returns>
        internal static TraceStep CreateNondeterministicChoice(Fingerprint fingerprint, int id, bool choice)
        {
            var traceStep = new TraceStep();

            traceStep.IsChoice = true;
            traceStep.Fingerprint = fingerprint;

            traceStep.EnabledMachines = null;

            traceStep.ChoiceId = id;
            traceStep.Choice = choice;

            return traceStep;
        }

        #endregion

        #region private API

        /// <summary>
        /// Constructor.
        /// </summary>
        private TraceStep()
        {
            this.Monitors = new Dictionary<Monitor, MonitorStatus>();
        }

        #endregion
    }
}
