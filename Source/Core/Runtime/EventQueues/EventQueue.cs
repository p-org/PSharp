// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Implements a queue of events.
    /// </summary>
    internal sealed class EventQueue : IEventQueue
    {
        /// <summary>
        /// Manages the state of the machine that owns this queue.
        /// </summary>
        private readonly IMachineStateManager MachineStateManager;

        /// <summary>
        /// The internal queue.
        /// </summary>
        private readonly LinkedList<Event> Queue;

        /// <summary>
        /// The raised event, or null if no event has been raised.
        /// </summary>
        private Event RaisedEvent;

        /// <summary>
        /// Map from the types of events that the owner of the queue is waiting to receive
        /// to an optional predicate. If an event of one of these types is enqueued, then
        /// if there is no predicate, or if there is a predicate and evaluates to true, then
        /// the event is received, else the event is deferred.
        /// </summary>
        private Dictionary<Type, Func<Event, bool>> EventWaitTypes;

        /// <summary>
        /// Task completion source that contains the event obtained using an explicit receive.
        /// </summary>
        private TaskCompletionSource<Event> ReceiveCompletionSource;

        /// <summary>
        /// Checks if the queue is accepting new events.
        /// </summary>
        private bool IsClosed;

        /// <summary>
        /// The size of the queue.
        /// </summary>
        public int Size => this.Queue.Count;

        /// <summary>
        /// Checks if an event has been raised.
        /// </summary>
        public bool IsEventRaised => this.RaisedEvent != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueue"/> class.
        /// </summary>
        internal EventQueue(IMachineStateManager machineStateManager)
        {
            this.MachineStateManager = machineStateManager;
            this.Queue = new LinkedList<Event>();
            this.EventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            this.IsClosed = false;
        }

        /// <summary>
        /// Enqueues the specified event and its optional metadata.
        /// </summary>
        public EnqueueStatus Enqueue(Event e, EventInfo info)
        {
            EnqueueStatus enqueueStatus = EnqueueStatus.EventHandlerRunning;
            lock (this.Queue)
            {
                if (this.IsClosed)
                {
                    return EnqueueStatus.Dropped;
                }

                if (this.EventWaitTypes != null &&
                    this.EventWaitTypes.TryGetValue(e.GetType(), out Func<Event, bool> predicate) &&
                    (predicate is null || predicate(e)))
                {
                    this.EventWaitTypes = null;
                    enqueueStatus = EnqueueStatus.Received;
                }
                else
                {
                    this.Queue.AddLast(e);
                    if (!this.MachineStateManager.IsEventHandlerRunning)
                    {
                        this.MachineStateManager.IsEventHandlerRunning = true;
                        enqueueStatus = EnqueueStatus.EventHandlerNotRunning;
                    }
                }
            }

            if (enqueueStatus is EnqueueStatus.Received)
            {
                this.MachineStateManager.OnReceiveEvent(e, info);
                this.ReceiveCompletionSource.SetResult(e);
                return enqueueStatus;
            }
            else
            {
                this.MachineStateManager.OnEnqueueEvent(e, info);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Dequeues the next event, if there is one available.
        /// </summary>
        public (DequeueStatus status, Event e, EventInfo info) Dequeue()
        {
            // Try to get the raised event, if there is one. Raised events
            // have priority over the events in the inbox.
            if (this.RaisedEvent != null)
            {
                if (this.MachineStateManager.IsEventIgnoredInCurrentState(this.RaisedEvent, null))
                {
                    // TODO: should the user be able to raise an ignored event?
                    // The raised event is ignored in the current state.
                    this.RaisedEvent = null;
                }
                else
                {
                    Event e = this.RaisedEvent;
                    this.RaisedEvent = null;
                    return (DequeueStatus.Raised, e, null);
                }
            }

            lock (this.Queue)
            {
                // Try to dequeue the next event, if there is one.
                var node = this.Queue.First;
                while (node != null)
                {
                    // Iterates through the events in the inbox.
                    if (this.MachineStateManager.IsEventIgnoredInCurrentState(node.Value, null))
                    {
                        // Removes an ignored event.
                        var nextNode = node.Next;
                        this.Queue.Remove(node);
                        node = nextNode;
                        continue;
                    }
                    else if (this.MachineStateManager.IsEventDeferredInCurrentState(node.Value, null))
                    {
                        // Skips a deferred event.
                        node = node.Next;
                        continue;
                    }

                    // Found next event that can be dequeued.
                    this.Queue.Remove(node);
                    return (DequeueStatus.Success, node.Value, null);
                }

                // No event can be dequeued, so check if there is a default event handler.
                if (!this.MachineStateManager.IsDefaultHandlerInstalledInCurrentState())
                {
                    // There is no default event handler installed, so do not return an event.
                    // Setting 'IsEventHandlerRunning' must happen inside the lock as it needs
                    // to be synchronized with the enqueue and starting a new event handler.
                    this.MachineStateManager.IsEventHandlerRunning = false;
                    return (DequeueStatus.NotAvailable, null, null);
                }
            }

            // TODO: check op-id of default event.
            // A default event handler exists.
            return (DequeueStatus.Default, Default.Event, null);
        }

        /// <summary>
        /// Enqueues the specified raised event.
        /// </summary>
        public void Raise(Event e)
        {
            this.RaisedEvent = e;
            this.MachineStateManager.OnRaiseEvent(e, null);
        }

        /// <summary>
        /// Waits to receive an event of the specified type that satisfies an optional predicate.
        /// </summary>
        public Task<Event> ReceiveAsync(Type eventType, Func<Event, bool> predicate = null)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>
            {
                { eventType, predicate }
            };

            return this.ReceiveAsync(eventWaitTypes);
        }

        /// <summary>
        /// Waits to receive an event of the specified types.
        /// </summary>
        public Task<Event> ReceiveAsync(params Type[] eventTypes)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            foreach (var type in eventTypes)
            {
                eventWaitTypes.Add(type, null);
            }

            return this.ReceiveAsync(eventWaitTypes);
        }

        /// <summary>
        /// Waits to receive an event of the specified types that satisfy the specified predicates.
        /// </summary>
        public Task<Event> ReceiveAsync(params Tuple<Type, Func<Event, bool>>[] events)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            foreach (var e in events)
            {
                eventWaitTypes.Add(e.Item1, e.Item2);
            }

            return this.ReceiveAsync(eventWaitTypes);
        }

        /// <summary>
        /// Waits for an event to be enqueued based on the conditions defined in the event wait types.
        /// </summary>
        private Task<Event> ReceiveAsync(Dictionary<Type, Func<Event, bool>> eventWaitTypes)
        {
            Event e = null;
            lock (this.Queue)
            {
                var node = this.Queue.First;
                while (node != null)
                {
                    // Dequeue the first event that the caller waits to receive, if there is one in the queue.
                    if (eventWaitTypes.TryGetValue(node.Value.GetType(), out Func<Event, bool> predicate) &&
                        (predicate is null || predicate(node.Value)))
                    {
                        e = node.Value;
                        this.Queue.Remove(node);
                        break;
                    }

                    node = node.Next;
                }

                if (e is null)
                {
                    this.ReceiveCompletionSource = new TaskCompletionSource<Event>();
                    this.EventWaitTypes = eventWaitTypes;
                }
            }

            if (e is null)
            {
                // Note that 'EventWaitTypes' is racy, so should not be accessed outside
                // the lock, this is why we access 'eventWaitTypes' instead.
                this.MachineStateManager.OnWaitEvent(eventWaitTypes.Keys);
                return this.ReceiveCompletionSource.Task;
            }

            this.MachineStateManager.OnReceiveEventWithoutWaiting(e, null);
            return Task.FromResult(e);
        }

        /// <summary>
        /// Returns the cached state of the queue.
        /// </summary>
        public int GetCachedState() => 0;

        /// <summary>
        /// Closes the queue, which stops any further event enqueues.
        /// </summary>
        public void Close()
        {
            lock (this.Queue)
            {
                this.IsClosed = true;
            }
        }

        /// <summary>
        /// Disposes the queue resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            foreach (var e in this.Queue)
            {
                this.MachineStateManager.OnDropEvent(e, null);
            }

            this.Queue.Clear();
        }

        /// <summary>
        /// Disposes the queue resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
