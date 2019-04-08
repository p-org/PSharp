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
        private readonly LinkedList<EventInfo> Queue;

        /// <summary>
        /// The raised event, or null if no event has been raised.
        /// </summary>
        private EventInfo RaisedEvent;

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
            this.Queue = new LinkedList<EventInfo>();
            this.EventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            this.IsClosed = false;
        }

        /// <summary>
        /// Enqueues the specified event.
        /// </summary>
        public EnqueueStatus Enqueue(EventInfo eventInfo)
        {
            if (this.IsClosed)
            {
                return EnqueueStatus.Dropped;
            }

            if (this.EventWaitTypes.TryGetValue(eventInfo.EventType, out Func<Event, bool> predicate) &&
                (predicate is null || predicate(eventInfo.Event)))
            {
                this.EventWaitTypes.Clear();
                this.Machine.Info.IsWaitingToReceive = false;
                this.Runtime.Logger.OnReceive(this.Machine.Id, this.Machine.CurrentStateName, eventInfo.EventName, wasBlocked: true);
                this.Runtime.NotifyReceivedEvent(this.Machine, eventInfo);
                this.ReceiveCompletionSource.SetResult(eventInfo.Event);
                return EnqueueStatus.EventHandlerRunning;
            }

            this.Machine.Runtime.Logger.OnEnqueue(this.Machine.Id, eventInfo.EventName);

            this.Queue.AddLast(eventInfo);

            if (eventInfo.Event.Assert >= 0)
            {
                var eventCount = this.Queue.Count(val => val.EventType.Equals(eventInfo.EventType));
                this.Runtime.Assert(eventCount <= eventInfo.Event.Assert,
                    "There are more than {0} instances of '{1}' in the input queue of machine '{2}'.",
                    eventInfo.Event.Assert, eventInfo.EventName, this);
            }

            if (eventInfo.Event.Assume >= 0)
            {
                var eventCount = this.Queue.Count(val => val.EventType.Equals(eventInfo.EventType));
                this.Runtime.Assert(eventCount <= eventInfo.Event.Assume,
                    "There are more than {0} instances of '{1}' in the input queue of machine '{2}'.",
                    eventInfo.Event.Assume, eventInfo.EventName, this);
            }

            if (!this.Machine.Info.IsEventHandlerRunning)
            {
                if (this.TryDequeueEvent(true) is null)
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
        /// Dequeues the next event if there is one available, else returns null.
        /// </summary>
        public DequeueStatus Dequeue(out EventInfo eventInfo)
        {
            // Try to get the raised event, if there is one. Raised events
            // have priority over the events in the inbox.
            if (this.RaisedEvent != null)
            {
                if (this.Machine.IsEventIgnoredInCurrentState(this.RaisedEvent.Event))
                {
                    // TODO: should the user be able to raise an ignored event?
                    // The raised event is ignored in the current state.
                    this.RaisedEvent = null;
                }
                else
                {
                    eventInfo = this.RaisedEvent;
                    this.RaisedEvent = null;
                    return DequeueStatus.Raised;
                }
            }

            // TODO: try optimize this out of the hot path?
            var hasDefaultHandler = this.Machine.IsDefaultHandlerInstalledInCurrentState();
            if (hasDefaultHandler)
            {
                this.Runtime.NotifyDefaultEventHandlerCheck(this.Machine);
            }

            // Try to dequeue the next event, if there is one.
            eventInfo = this.TryDequeueEvent();
            if (eventInfo is null)
            {
                if (hasDefaultHandler)
                {
                    // Else, get the default event.
                    var eventOrigin = new EventOriginInfo(this.Machine.Id, this.GetType().Name,
                    NameResolver.GetStateNameForLogging(this.Machine.CurrentState));
                    eventInfo = new EventInfo(Default.Event, eventOrigin);
                    return DequeueStatus.Default;
                }

                this.Machine.Info.IsEventHandlerRunning = false;
                return DequeueStatus.NotAvailable;
            }

            return DequeueStatus.Success;
        }

        /// <summary>
        /// Dequeues the next event if there is one available, else returns null.
        /// </summary>
        private EventInfo TryDequeueEvent(bool checkOnly = false)
        {
            EventInfo nextAvailableEventInfo = null;

            // Iterates through the events in the inbox.
            var node = this.Queue.First;
            while (node != null)
            {
                var nextNode = node.Next;
                var currentEventInfo = node.Value;

                if (this.Machine.IsEventIgnoredInCurrentState(currentEventInfo.Event))
                {
                    if (!checkOnly)
                    {
                        // Removes an ignored event.
                        this.Queue.Remove(node);
                    }

                    node = nextNode;
                    continue;
                }

                // Skips a deferred event.
                if (!this.Machine.IsEventDeferredInCurrentState(currentEventInfo.Event))
                {
                    nextAvailableEventInfo = currentEventInfo;
                    if (!checkOnly)
                    {
                        this.Queue.Remove(node);
                    }

                    break;
                }

                node = nextNode;
            }

            return nextAvailableEventInfo;
        }

        /// <summary>
        /// Enqueues the specified raised event.
        /// </summary>
        public void Raise(Event e)
        {
            var eventOrigin = new EventOriginInfo(this.Machine.Id, this.GetType().Name,
                NameResolver.GetStateNameForLogging(this.Machine.CurrentState));
            this.RaisedEvent = new EventInfo(e, eventOrigin);
            this.Runtime.NotifyRaisedEvent(this.Machine, this.RaisedEvent);
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

            this.ReceiveCompletionSource = new TaskCompletionSource<Event>();
            this.EventWaitTypes = eventWaitTypes;

            EventInfo eventInfo = null;
            var node = this.Queue.First;
            while (node != null)
            {
                // Dequeues the first event that the caller waits to receive, if there is one in the queue.
                if (this.EventWaitTypes.TryGetValue(node.Value.EventType, out Func<Event, bool> predicate) &&
                    (predicate is null || predicate(node.Value.Event)))
                {
                    this.Runtime.Logger.OnReceive(this.Machine.Id, this.Machine.CurrentStateName, node.Value.EventName, wasBlocked: false);
                    this.EventWaitTypes.Clear();
                    this.ReceiveCompletionSource.SetResult(node.Value.Event);
                    eventInfo = node.Value;
                    this.Queue.Remove(node);
                    break;
                }

                node = node.Next;
            }

            if (eventInfo is null)
            {
                this.Machine.Info.IsWaitingToReceive = true;

                var eventWaitTypesArray = this.EventWaitTypes.Keys.ToArray();
                if (eventWaitTypesArray.Length == 1)
                {
                    this.Runtime.Logger.OnWait(this.Machine.Id, this.Machine.CurrentStateName, eventWaitTypesArray[0]);
                    this.Runtime.NotifyWaitEvent(this.Machine, eventWaitTypesArray[0]);
                }
                else
                {
                    this.Runtime.Logger.OnWait(this.Machine.Id, this.Machine.CurrentStateName, eventWaitTypesArray);
                    this.Runtime.NotifyWaitEvent(this.Machine, eventWaitTypesArray);
                }
            }
            else
            {
                this.Runtime.NotifyReceivedEventWithoutWaiting(this.Machine, eventInfo);
            }

            return this.ReceiveCompletionSource.Task;
        }

        /// <summary>
        /// Returns the cached state of the queue.
        /// </summary>
        public int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                foreach (var e in this.Queue)
                {
                    hash = (hash * 31) + e.EventType.GetHashCode();
                    if (this.Runtime.Configuration.EnableUserDefinedStateHashing)
                    {
                        // Adds the user-defined hashed event state.
                        hash = (hash * 31) + e.Event.HashedState;
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

            var mustHandleEvent = this.Queue.FirstOrDefault(ev => ev.MustHandle);
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
                foreach (var eventInfo in this.Queue)
                {
                    this.Runtime.TryHandleDroppedEvent(eventInfo.Event, this.Machine.Id);
                }
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
