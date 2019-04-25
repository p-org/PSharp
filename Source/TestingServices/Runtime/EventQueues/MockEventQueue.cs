// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// Implements a queue of events that is used for testing purposes.
    /// </summary>
    internal sealed class MockEventQueue : IEventQueue
    {
        /// <summary>
        /// The runtime that executes the machine that owns this queue.
        /// </summary>
        private readonly BaseRuntime Runtime;

        /// <summary>
        /// The machine that owns this queue.
        /// </summary>
        private readonly Machine Machine;

        /// <summary>
        /// The internal queue.
        /// </summary>
        private readonly LinkedList<Event> Queue;

        /// <summary>
        /// The metadata of the enqueued events.
        /// </summary>
        private readonly LinkedList<EventInfo> Metadata;

        /// <summary>
        /// The raised event, or null if no event has been raised.
        /// </summary>
        private Event RaisedEvent;

        /// <summary>
        /// The metadata of the raised event, if there is one.
        /// </summary>
        private EventInfo RaisedEventMetadata;

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
        /// Initializes a new instance of the <see cref="MockEventQueue"/> class.
        /// </summary>
        internal MockEventQueue(BaseRuntime runtime, Machine machine)
        {
            this.Runtime = runtime;
            this.Machine = machine;
            this.Queue = new LinkedList<Event>();
            this.Metadata = new LinkedList<EventInfo>();
            this.EventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            this.IsClosed = false;
        }

        /// <summary>
        /// Enqueues the specified event and its optional metadata.
        /// </summary>
        public EnqueueStatus Enqueue(Event e, EventInfo info)
        {
            if (this.IsClosed)
            {
                return EnqueueStatus.Dropped;
            }

            if (this.EventWaitTypes.TryGetValue(e.GetType(), out Func<Event, bool> predicate) &&
                (predicate is null || predicate(e)))
            {
                this.EventWaitTypes.Clear();
                this.Machine.Info.IsWaitingToReceive = false;
                this.Runtime.NotifyReceivedEvent(this.Machine, e, info);
                this.ReceiveCompletionSource.SetResult(e);
                return EnqueueStatus.EventHandlerRunning;
            }

            this.Machine.Runtime.Logger.OnEnqueue(this.Machine.Id, info.EventName);

            this.Queue.AddLast(e);
            this.Metadata.AddLast(info);

            if (info.Assert >= 0)
            {
                var eventCount = this.Queue.Count(val => val.GetType().Equals(e.GetType()));
                this.Runtime.Assert(eventCount <= info.Assert,
                    "There are more than {0} instances of '{1}' in the input queue of machine '{2}'.",
                    info.Assert, info.EventName, this);
            }

            if (info.Assume >= 0)
            {
                var eventCount = this.Queue.Count(val => val.GetType().Equals(e.GetType()));
                this.Runtime.Assert(eventCount <= info.Assume,
                    "There are more than {0} instances of '{1}' in the input queue of machine '{2}'.",
                    info.Assume, info.EventName, this);
            }

            if (!this.Machine.Info.IsEventHandlerRunning)
            {
                if (this.TryDequeueEvent(true).e is null)
                {
                    return EnqueueStatus.NextEventUnavailable;
                }
                else
                {
                    this.Machine.Info.IsEventHandlerRunning = true;
                    return EnqueueStatus.EventHandlerNotRunning;
                }
            }

            return EnqueueStatus.EventHandlerRunning;
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
                if (this.Machine.IsEventIgnoredInCurrentState(this.RaisedEvent))
                {
                    // TODO: should the user be able to raise an ignored event?
                    // The raised event is ignored in the current state.
                    this.RaisedEvent = null;
                }
                else
                {
                    Event raisedEvent = this.RaisedEvent;
                    EventInfo raisedEventInfo = this.RaisedEventMetadata;
                    this.RaisedEvent = null;
                    this.RaisedEventMetadata = null;
                    return (DequeueStatus.Raised, raisedEvent, raisedEventInfo);
                }
            }

            var hasDefaultHandler = this.Machine.IsDefaultHandlerInstalledInCurrentState();
            if (hasDefaultHandler)
            {
                this.Runtime.NotifyDefaultEventHandlerCheck(this.Machine);
            }

            // Try to dequeue the next event, if there is one.
            var (e, info) = this.TryDequeueEvent();
            if (e != null)
            {
                // Found next event that can be dequeued.
                return (DequeueStatus.Success, e, info);
            }

            // No event can be dequeued, so check if there is a default event handler.
            if (!hasDefaultHandler)
            {
                // There is no default event handler installed, so do not return an event.
                this.Machine.Info.IsEventHandlerRunning = false;
                return (DequeueStatus.NotAvailable, null, null);
            }

            // TODO: check op-id of default event.
            // A default event handler exists.
            var eventOrigin = new EventOriginInfo(this.Machine.Id, this.GetType().Name,
                NameResolver.GetStateNameForLogging(this.Machine.CurrentState));
            return (DequeueStatus.Default, Default.Event, new EventInfo(Default.Event, eventOrigin));
        }

        /// <summary>
        /// Dequeues the next event and its metadata, if there is one available, else returns null.
        /// </summary>
        private (Event e, EventInfo info) TryDequeueEvent(bool checkOnly = false)
        {
            Event nextAvailableEvent = null;
            EventInfo nextAvailableEventInfo = null;

            // Iterates through the events and metadata in the inbox.
            var node = this.Queue.First;
            var nodeInfo = this.Metadata.First;
            while (node != null)
            {
                var nextNode = node.Next;
                var nextNodeInfo = nodeInfo.Next;
                var currentEvent = node.Value;
                var currentEventInfo = nodeInfo.Value;

                if (this.Machine.IsEventIgnoredInCurrentState(currentEvent))
                {
                    if (!checkOnly)
                    {
                        // Removes an ignored event.
                        this.Queue.Remove(node);
                        this.Metadata.Remove(nodeInfo);
                    }

                    node = nextNode;
                    nodeInfo = nextNodeInfo;
                    continue;
                }

                // Skips a deferred event.
                if (!this.Machine.IsEventDeferredInCurrentState(currentEvent))
                {
                    nextAvailableEvent = currentEvent;
                    nextAvailableEventInfo = currentEventInfo;
                    if (!checkOnly)
                    {
                        this.Queue.Remove(node);
                        this.Metadata.Remove(nodeInfo);
                    }

                    break;
                }

                node = nextNode;
                nodeInfo = nextNodeInfo;
            }

            return (nextAvailableEvent, nextAvailableEventInfo);
        }

        /// <summary>
        /// Enqueues the specified raised event.
        /// </summary>
        public void Raise(Event e)
        {
            var eventOrigin = new EventOriginInfo(this.Machine.Id, this.GetType().Name,
                NameResolver.GetStateNameForLogging(this.Machine.CurrentState));
            this.RaisedEvent = e;
            this.RaisedEventMetadata = new EventInfo(e, eventOrigin);
            this.Runtime.NotifyRaisedEvent(this.Machine, e, this.RaisedEventMetadata);
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
        /// Waits for an event to be enqueued.
        /// </summary>
        private Task<Event> ReceiveAsync(Dictionary<Type, Func<Event, bool>> eventWaitTypes)
        {
            this.Runtime.NotifyReceiveCalled(this.Machine);

            Event e = null;
            EventInfo info = null;
            var node = this.Queue.First;
            var nodeInfo = this.Metadata.First;
            while (node != null)
            {
                // Dequeue the first event that the caller waits to receive, if there is one in the queue.
                if (eventWaitTypes.TryGetValue(node.Value.GetType(), out Func<Event, bool> predicate) &&
                    (predicate is null || predicate(node.Value)))
                {
                    e = node.Value;
                    info = nodeInfo.Value;
                    this.Queue.Remove(node);
                    this.Metadata.Remove(nodeInfo);
                    break;
                }

                node = node.Next;
                nodeInfo = nodeInfo.Next;
            }

            if (e is null)
            {
                this.ReceiveCompletionSource = new TaskCompletionSource<Event>();
                this.EventWaitTypes = eventWaitTypes;
                this.Machine.Info.IsWaitingToReceive = true;
                this.Runtime.NotifyWaitEvent(this.Machine, this.EventWaitTypes.Keys);
                return this.ReceiveCompletionSource.Task;
            }

            this.Runtime.NotifyReceivedEventWithoutWaiting(this.Machine, e, info);
            return Task.FromResult(e);
        }

        /// <summary>
        /// Returns the cached state of the queue.
        /// </summary>
        public int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                foreach (var eventInfo in this.Metadata)
                {
                    hash = (hash * 31) + eventInfo.EventName.GetHashCode();
                    if (this.Runtime.Configuration.EnableUserDefinedStateHashing)
                    {
                        // Adds the user-defined hashed event state.
                        hash = (hash * 31) + eventInfo.HashedState;
                    }
                }

                return hash;
            }
        }

        /// <summary>
        /// Closes the queue, which stops any further event enqueues.
        /// </summary>
        public void Close()
        {
            this.IsClosed = true;

            var mustHandleEvent = this.Metadata.FirstOrDefault(ev => ev.MustHandle);
            this.Runtime.Assert(mustHandleEvent is null, "Machine '{0}' halted before dequeueing must-handle event '{1}'.",
                this.Machine.Id, mustHandleEvent?.EventName ?? string.Empty);
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

            if (this.Runtime.IsOnEventDroppedHandlerInstalled)
            {
                foreach (var e in this.Queue)
                {
                    this.Runtime.TryHandleDroppedEvent(e, this.Machine.Id);
                }
            }

            this.Queue.Clear();
            this.Metadata.Clear();
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
