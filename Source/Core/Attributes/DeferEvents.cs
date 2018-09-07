// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring what events should be deferred in
    /// a machine state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DeferEvents : Attribute
    {
        /// <summary>
        /// Event types.
        /// </summary>
        internal Type[] Events;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        public DeferEvents(params Type[] eventTypes)
        {
            this.Events = eventTypes;
        }
    }
}
