// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing an event.
    /// </summary>
    [DataContract]
    public abstract class Event
    {
        /// <summary>
        /// Optional operation group id associated with this event.
        /// By default it is <see cref="Guid.Empty"/>.
        /// </summary>
        public Guid OperationGroupId { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class.
        /// </summary>
        protected Event()
        {
            this.OperationGroupId = Guid.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class
        /// with the specified operation group id.
        /// </summary>
        /// <param name="operationGroupId">The operation group id associated with this event.</param>
        protected Event(Guid operationGroupId)
        {
            this.OperationGroupId = operationGroupId;
        }
    }
}
