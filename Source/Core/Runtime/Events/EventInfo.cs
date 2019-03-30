// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Contains an <see cref="Event"/>, and its associated metadata.
    /// </summary>
    [DataContract]
    internal class EventInfo
    {
        /// <summary>
        /// Contained event.
        /// </summary>
        internal Event Event { get; private set; }

        /// <summary>
        /// Event type.
        /// </summary>
        internal Type EventType { get; private set; }

        /// <summary>
        /// Event name.
        /// </summary>
        [DataMember]
        internal string EventName { get; private set; }

        /// <summary>
        /// The step from which this event was sent.
        /// </summary>
        internal int SendStep { get; }

        /// <summary>
        /// Information regarding the event origin.
        /// </summary>
        [DataMember]
        internal EventOriginInfo OriginInfo { get; private set; }

        /// <summary>
        /// The operation group id associated with this event.
        /// </summary>
        internal Guid OperationGroupId { get; private set; }

        /// <summary>
        /// Is this a must-handle event?
        /// </summary>
        internal bool MustHandle { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e)
        {
            this.Event = e;
            this.EventType = e.GetType();
            this.EventName = this.EventType.FullName;
            this.MustHandle = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e, EventOriginInfo originInfo)
            : this(e)
        {
            this.OriginInfo = originInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e, EventOriginInfo originInfo, int sendStep)
            : this(e, originInfo)
        {
            this.SendStep = sendStep;
        }

        /// <summary>
        /// Sets the operation group id associated with this event.
        /// </summary>
        internal void SetOperationGroupId(Guid operationGroupId)
        {
            this.OperationGroupId = operationGroupId;
        }

        /// <summary>
        /// Sets the MustHandle flag of the event.
        /// </summary>
        internal void SetMustHandle(bool mustHandle)
        {
            this.MustHandle = mustHandle;
        }
    }
}
