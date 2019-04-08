// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Interface of a queue of events.
    /// </summary>
    internal interface IEventQueue : IDisposable
    {
        /// <summary>
        /// The size of the queue.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Checks if an event has been raised.
        /// </summary>
        bool IsEventRaised { get; }

        /// <summary>
        /// Enqueues the specified event.
        /// </summary>
        EnqueueStatus Enqueue(EventInfo eventInfo);

        /// <summary>
        /// Dequeues the next event if there is one available, else returns null.
        /// </summary>
        DequeueStatus Dequeue(out EventInfo eventInfo);

        /// <summary>
        /// Enqueues the specified raised event.
        /// </summary>
        void Raise(Event e);

        /// <summary>
        /// Waits to receive an event of the specified type that satisfies an optional predicate.
        /// </summary>
        Task<Event> ReceiveAsync(Type eventType, Func<Event, bool> predicate = null);

        /// <summary>
        /// Waits to receive an event of the specified types.
        /// </summary>
        Task<Event> ReceiveAsync(params Type[] eventTypes);

        /// <summary>
        /// Waits to receive an event of the specified types that satisfy the specified predicates.
        /// </summary>
        Task<Event> ReceiveAsync(params Tuple<Type, Func<Event, bool>>[] events);

        /// <summary>
        /// Returns the cached state of the queue.
        /// </summary>
        int GetCachedState();

        /// <summary>
        /// Closes the queue, which stops any further event enqueues.
        /// </summary>
        void Close();
    }
}
