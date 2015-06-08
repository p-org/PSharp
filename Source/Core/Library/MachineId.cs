//-----------------------------------------------------------------------
// <copyright file="MachineId.cs" company="Microsoft">
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
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Unique machine id.
    /// </summary>
    public sealed class MachineId
    {
        #region fields

        /// <summary>
        /// Handler to the machine object.
        /// </summary>
        internal readonly Object Machine;

        /// <summary>
        /// Id value.
        /// </summary>
        internal readonly int Value;

        #endregion

        #region static fields

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        private static int IdCounter = 0;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machine">Machine object</param>
        internal MachineId(Object machine)
        {
            this.Machine = machine;
            this.Value = MachineId.IdCounter++;
        }

        /// <summary>
        /// Resets the machine ID counter.
        /// </summary>
        internal static void ResetMachineIDCounter()
        {
            MachineId.IdCounter = 0;
        }

        #endregion
    }
}
