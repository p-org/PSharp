//-----------------------------------------------------------------------
// <copyright file="State.cs" company="Microsoft">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.StateCaching
{
    /// <summary>
    /// Class implementing a P# program state.
    /// </summary>
    internal sealed class State
    {
        #region fields

        /// <summary>
        /// The fingerprint of the trace step.
        /// </summary>
        internal Fingerprint Fingerprint { get; private set; }

        /// <summary>
        /// Map from monitors to their liveness status.
        /// </summary>
        internal Dictionary<Monitor, MonitorStatus> MonitorStatus;

        /// <summary>
        /// The enabled machines. Only relevant if this is a scheduling
        /// trace step.
        /// </summary>
        internal HashSet<BaseMachine> EnabledMachines;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <param name="enabledMachines">Enabled machines</param>
        /// <param name="monitorStatus">Monitor status</param>
        internal State(Fingerprint fingerprint, HashSet<BaseMachine> enabledMachines,
            Dictionary<Monitor, MonitorStatus> monitorStatus)
        {
            this.Fingerprint = fingerprint;
            this.EnabledMachines = enabledMachines;
            this.MonitorStatus = monitorStatus;
        }

        #endregion
    }
}
