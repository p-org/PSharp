//-----------------------------------------------------------------------
// <copyright file="ProgramState.cs" company="Microsoft">
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
using System.Threading.Tasks;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing the P# bug-finding scheduler.
    /// </summary>
    internal sealed class ProgramState
    {
        #region fields

        internal int HashValue;

        internal Dictionary<Machine, bool> EnabledMachines;

        internal Dictionary<int, bool> Choices;

        internal Dictionary<Monitor, bool> Monitors;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal ProgramState()
        {
            this.HashValue = 0;
            this.EnabledMachines = new Dictionary<Machine, bool>();
            this.Choices = new Dictionary<int, bool>();
            this.Monitors = new Dictionary<Monitor, bool>();
        }

        #endregion
    }
}
