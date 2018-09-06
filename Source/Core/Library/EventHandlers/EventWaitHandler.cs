﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Defines an event wait handler.
    /// </summary>
    internal class EventWaitHandler
    {
        #region fields

        /// <summary>
        /// Type of the event to handle.
        /// </summary>
        internal readonly Type EventType;

        /// <summary>
        /// Handle the event only if the
        /// predicate evaluates to true.
        /// </summary>
        internal readonly Func<Event, bool> Predicate;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType">Event type</param>
        internal EventWaitHandler(Type eventType)
        {
            this.EventType = eventType;
            this.Predicate = (Event e) => true;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="predicate">Predicate</param>
        internal EventWaitHandler(Type eventType, Func<Event, bool> predicate)
        {
            this.EventType = eventType;
            this.Predicate = predicate;
        }

        #endregion
    }
}
