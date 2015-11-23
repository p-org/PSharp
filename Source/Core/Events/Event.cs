//-----------------------------------------------------------------------
// <copyright file="Event.cs" company="Microsoft">
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
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing an event.
    /// </summary>
    [DataContract]
    public abstract class Event
    {
        #region fields
        
        /// <summary>
        /// The operation id of this event.
        /// </summary>
        internal int OperationId { get; private set; }

        /// <summary>
        /// Specifies that there must not be more than k instances
        /// of e in the input queue of any machine.
        /// </summary>
        [DataMember]
        protected internal readonly int Assert;

        /// <summary>
        /// Speciﬁes that during testing, an execution that increases
        /// the cardinality of e beyond k in some queue must not be
        /// generated.
        /// </summary>
        [DataMember]
        protected internal readonly int Assume;

        #endregion

        #region protected methods

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Event()
        {
            this.Assert = -1;
            this.Assume = -1;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="assert">Assert</param>
        /// <param name="assume">Assume</param>
        protected Event(int assert, int assume)
        {
            this.Assert = assert;
            this.Assume = assume;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Sets the operation id of this event.
        /// </summary>
        /// <param name="opid">OperationId</param>
        internal void SetOperationId(int opid)
        {
            this.OperationId = opid;
        }

        #endregion
    }
}
