// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class that contains a P# event, and its
    /// associated information.
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
        /// Creates a new <see cref="EventInfo"/>.
        /// </summary>
        /// <param name="e">Event</param>
        internal EventInfo(Event e)
        {
            Event = e;
            EventType = e.GetType();
            EventName = EventType.FullName;
            MustHandle = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="originInfo">EventOriginInfo</param>
        internal EventInfo(Event e, EventOriginInfo originInfo) : this(e)
        {
            OriginInfo = originInfo;
        }

        /// <summary>
        /// Sets the operation group id associated with this event.
        /// </summary>
        /// <param name="operationGroupId">Operation group id.</param>
        internal void SetOperationGroupId(Guid operationGroupId)
        {
            this.OperationGroupId = operationGroupId;
        }

        /// <summary>
        /// Sets the MustHandle flag of the event
        /// </summary>
        /// <param name="mustHandle">MustHandle flag</param>
        internal void SetMustHandle(bool mustHandle)
        {
            this.MustHandle = mustHandle;
        }

        /// <summary>
        /// Construtor.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="originInfo">EventOriginInfo</param>
        /// <param name="sendStep">int</param>
        internal EventInfo(Event e, EventOriginInfo originInfo, int sendStep)
            : this(e, originInfo)
        {
            SendStep = sendStep;
        }
    }
}
