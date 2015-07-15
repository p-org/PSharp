//-----------------------------------------------------------------------
// <copyright file="StateMap.cs" company="Microsoft">
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

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing a map of P# program states.
    /// </summary>
    internal sealed class StateMap
    {
        #region fields

        /// <summary>
        /// Map from fingerprints to program states.
        /// </summary>
        private Dictionary<Fingerprint, ProgramState> Map;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal StateMap()
        {
            this.Map = new Dictionary<Fingerprint, ProgramState>();
        }

        #endregion
    }
}
