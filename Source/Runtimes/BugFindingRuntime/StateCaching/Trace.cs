//-----------------------------------------------------------------------
// <copyright file="Trace.cs" company="Microsoft">
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing a P# program trace. A trace is a series of
    /// transitions from some initial state to some end state.
    /// </summary>
    internal sealed class Trace : IEnumerable, IEnumerable<ProgramState>
    {
        #region fields

        /// <summary>
        /// The steps of the trace.
        /// </summary>
        private List<ProgramState> Steps;

        /// <summary>
        /// The number of steps in the trace.
        /// </summary>
        internal int Count
        {
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Index for the trace.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>ProgramState</returns>
        internal ProgramState this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal Trace()
        {
            this.Steps = new List<ProgramState>();
        }

        /// <summary>
        /// Adds a new program state in the trace.
        /// </summary>
        /// <param name="state">ProgramState</param>
        internal void AddStep(ProgramState state)
        {
            this.Steps.Add(state);
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator<ProgramState> IEnumerable<ProgramState>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        #endregion
    }
}
