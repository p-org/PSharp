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

namespace Microsoft.PSharp.StateCaching
{
    /// <summary>
    /// Class implementing a map of program states (represented
    /// as trace steps).
    /// </summary>
    internal sealed class StateMap
    {
        #region fields

        /// <summary>
        /// Map from fingerprints to program states.
        /// </summary>
        private Dictionary<Fingerprint, TraceStep> Map;

        /// <summary>
        /// The number of unique states.
        /// </summary>
        internal int Count
        {
            get { return this.Map.Count; }
        }

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal StateMap()
        {
            this.Map = new Dictionary<Fingerprint, TraceStep>();
        }

        /// <summary>
        /// Updates the program state map.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <param name="state">ProgramState</param>
        internal void Update(Fingerprint fingerprint, TraceStep state)
        {
            if (this.Map.ContainsKey(fingerprint))
            {
                this.Map[fingerprint] = state;
            }
            else
            {
                this.Map.Add(fingerprint, state);
            }
        }

        /// <summary>
        /// Returns true if the map contains the given fingerprint.
        /// </summary>
        /// <param name="fingerprint">Fingerprint</param>
        /// <returns>Boolean value</returns>
        internal bool Contains(Fingerprint fingerprint)
        {
            return this.Map.ContainsKey(fingerprint);
        }

        #endregion
    }
}
