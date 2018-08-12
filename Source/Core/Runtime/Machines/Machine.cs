//-----------------------------------------------------------------------
// <copyright file="Machine.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a P# machine.
    /// </summary>
    public abstract class Machine : BaseMachine
    {
        /// <summary>
        /// The manager of the runtime that executes this machine.
        /// </summary>
        private protected IMachineRuntimeManager RuntimeManager { get; private set; }

        /// <summary>
        /// Inbox of the machine. Incoming events are queued here.
        /// Events are dequeued to be processed.
        /// </summary>
        private readonly LinkedList<EventInfo> Inbox;

        /// <summary>
        /// A list of event wait handlers. They denote the types of events that
        /// the machine is currently waiting to arrive. Each handler contains an
        /// optional predicate and an optional action. If the predicate evaluates
        /// to false, then the received event is deferred. The optional action
        /// executes when the event is received.
        /// </summary>
        private readonly List<EventWaitHandler> EventWaitHandlers;

        /// <summary>
        /// Completion source that contains the event obtained
        /// using the receive statement.
        /// </summary>
        private TaskCompletionSource<Event> ReceiveCompletionSource;

        #region initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Machine() : base()
        {
            this.Inbox = new LinkedList<EventInfo>();
            this.EventWaitHandlers = new List<EventWaitHandler>();
        }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        /// <param name="runtimeManager">The runtime manager.</param>
        /// <param name="mid">The id of this machine.</param>
        /// <param name="info">The metadata of this machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal Task InitializeAsync(IMachineRuntimeManager runtimeManager, MachineId mid, MachineInfo info)
        {
            this.RuntimeManager = runtimeManager;
            return base.InitializeAsync(runtimeManager, mid, info);
        }

        #endregion

        #region user interface

        /// <summary>
        /// Creates a new machine of the specified type and with the specified
        /// optional <see cref="Event"/>. This <see cref="Event"/> can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        //[Obsolete("Please use Machine.CreateMachineAsync(...) instead.")]
        protected MachineId CreateMachine(Type type, Event e = null)
        {
            return this.CreateMachineAsync(type, e).Result;
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and with the
        /// specified optional <see cref="Event"/>. This <see cref="Event"/> can
        /// only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="e">Event</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        //[Obsolete("Please use Machine.CreateMachineAsync(...) instead.")]
        protected MachineId CreateMachine(Type type, string friendlyName, Event e = null)
        {
            return this.CreateMachineAsync(type, friendlyName, e).Result;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        //[Obsolete("Please use Machine.CreateMachineAsync(...) instead.")]
        protected MachineId CreateMachine(MachineId mid, Type type, Event e = null)
        {
            return this.CreateMachineAsync(mid, type, e).Result;
        }

        /// <summary>
        /// Creates a new machine of the specified type and with the specified
        /// optional <see cref="Event"/>. This <see cref="Event"/> can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        protected Task<MachineId> CreateMachineAsync(Type type, Event e = null)
        {
            return this.RuntimeManager.CreateMachineAsync(null, type, null, e, this, null);
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and with the
        /// specified optional <see cref="Event"/>. This <see cref="Event"/> can
        /// only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        protected Task<MachineId> CreateMachineAsync(Type type, string friendlyName, Event e = null)
        {
            return this.RuntimeManager.CreateMachineAsync(null, type, friendlyName, e, this, null);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        protected Task<MachineId> CreateMachineAsync(MachineId mid, Type type, Event e = null)
        {
            return this.RuntimeManager.CreateMachineAsync(mid, type, mid.FriendlyName, e, this, null);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters</param>
        //[Obsolete("Please use Machine.SendAsync(...) instead.")]
        protected void Send(MachineId mid, Event e, SendOptions options = null)
        {
            this.SendAsync(mid, e, options).Wait();
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        protected Task SendAsync(MachineId mid, Event e, SendOptions options = null)
        {
            // If the target machine is null, then report an error and exit.
            this.Assert(mid != null, $"Machine '{this.Id}' is sending to a null machine.");
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{this.Id}' is sending a null event.");
            return this.RuntimeManager.SendEventAsync(mid, e, this, options);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the received <see cref="Event"/>.</returns>
        protected internal virtual Task<Event> Receive(params Type[] eventTypes)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Receive while halted.");
            this.RuntimeManager.NotifyReceiveCalled(this);

            lock (this.Inbox)
            {
                this.ReceiveCompletionSource = new TaskCompletionSource<Event>();
                foreach (var type in eventTypes)
                {
                    this.EventWaitHandlers.Add(new EventWaitHandler(type));
                }
            }

            return this.WaitOnEvent();
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified type
        /// that satisfies the specified predicate.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="predicate">Predicate</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the received <see cref="Event"/>.</returns>
        protected internal virtual Task<Event> Receive(Type eventType, Func<Event, bool> predicate)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Receive while halted.");
            this.RuntimeManager.NotifyReceiveCalled(this);

            lock (this.Inbox)
            {
                this.ReceiveCompletionSource = new TaskCompletionSource<Event>();
                this.EventWaitHandlers.Add(new EventWaitHandler(eventType, predicate));
            }

            return this.WaitOnEvent();
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types
        /// that satisfy the specified predicates.
        /// </summary>
        /// <param name="events">Event types and predicates</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the received <see cref="Event"/>.</returns>
        protected internal virtual Task<Event> Receive(params Tuple<Type, Func<Event, bool>>[] events)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Receive while halted.");
            this.RuntimeManager.NotifyReceiveCalled(this);

            lock (this.Inbox)
            {
                this.ReceiveCompletionSource = new TaskCompletionSource<Event>();
                foreach (var e in events)
                {
                    this.EventWaitHandlers.Add(new EventWaitHandler(e.Item1, e.Item2));
                }
            }

            return this.WaitOnEvent();
        }

        #endregion

        #region inbox accessing

        /// <summary>
        /// Enqueues the specified <see cref="EventInfo"/>.
        /// </summary>
        /// <param name="eventInfo">The event metadata.</param>
        /// <returns>The machine status after the enqueue.</returns>
        internal override MachineStatus Enqueue(EventInfo eventInfo)
        {
            lock (this.Inbox)
            {
                if (this.Info.IsHalted)
                {
                    return MachineStatus.IsHalted;
                }

                EventWaitHandler eventWaitHandler = this.EventWaitHandlers.FirstOrDefault(
                    val => val.EventType == eventInfo.EventType &&
                           val.Predicate(eventInfo.Event));
                if (eventWaitHandler != null)
                {
                    this.EventWaitHandlers.Clear();
                    this.RuntimeManager.NotifyReceivedEvent(this, eventInfo);
                    this.ReceiveCompletionSource.SetResult(eventInfo.Event);
                    return MachineStatus.EventHandlerRunning;
                }

                this.RuntimeManager.Logger.OnEnqueue(this.Id, eventInfo.EventName);

                this.Inbox.AddLast(eventInfo);

                if (eventInfo.Event.Assert >= 0)
                {
                    var eventCount = this.Inbox.Count(val => val.EventType.Equals(eventInfo.EventType));
                    this.Assert(eventCount <= eventInfo.Event.Assert, "There are more than " +
                        $"{eventInfo.Event.Assert} instances of '{eventInfo.EventName}' " +
                        $"in the input queue of machine '{this}'");
                }

                if (eventInfo.Event.Assume >= 0)
                {
                    var eventCount = this.Inbox.Count(val => val.EventType.Equals(eventInfo.EventType));
                    this.Assert(eventCount <= eventInfo.Event.Assume, "There are more than " +
                        $"{eventInfo.Event.Assume} instances of '{eventInfo.EventName}' " +
                        $"in the input queue of machine '{this}'");
                }

                if (!this.IsActive)
                {
                    if (this.RuntimeManager.IsTestingModeEnabled && this.TryDequeueEvent(true) == null)
                    {
                        return MachineStatus.NextEventUnavailable;
                    }
                    else
                    {
                        this.IsActive = true;
                        return MachineStatus.EventHandlerNotRunning;
                    }
                }
            }

            return MachineStatus.EventHandlerRunning;
        }

        /// <summary>
        /// Dequeues the next available event from the inbox if there
        /// is one available, else returns null.
        /// </summary>
        /// <param name="checkOnly">Only check if event can get dequeued, do not modify inbox</param>
        /// <returns>The result is the dequeued event.</returns>
        private EventInfo TryDequeueEvent(bool checkOnly = false)
        {
            EventInfo nextAvailableEventInfo = null;

            // Iterates through the events in the inbox.
            var node = this.Inbox.First;
            while (node != null)
            {
                var nextNode = node.Next;
                var currentEventInfo = node.Value;
                if (currentEventInfo.EventType.IsGenericType)
                {
                    var genericTypeDefinition = currentEventInfo.EventType.GetGenericTypeDefinition();
                    var ignored = false;
                    foreach (var tup in this.CurrentActionHandlerMap)
                    {
                        if (!(tup.Value is IgnoreAction)) continue;
                        if (tup.Key.IsGenericType && tup.Key.GetGenericTypeDefinition().Equals(
                            genericTypeDefinition.GetGenericTypeDefinition()))
                        {
                            ignored = true;
                            break;
                        }
                    }

                    if (ignored)
                    {
                        if (!checkOnly)
                        {
                            // Removes an ignored event.
                            this.Inbox.Remove(node);
                        }

                        node = nextNode;
                        continue;
                    }
                }

                if (this.IsIgnored(currentEventInfo.EventType))
                {
                    if (!checkOnly)
                    {
                        // Removes an ignored event.
                        this.Inbox.Remove(node);
                    }

                    node = nextNode;
                    continue;
                }

                // Skips a deferred event.
                if (!this.IsDeferred(currentEventInfo.EventType))
                {
                    nextAvailableEventInfo = currentEventInfo;
                    if (!checkOnly)
                    {
                        this.Inbox.Remove(node);
                    }

                    break;
                }

                node = nextNode;
            }

            return nextAvailableEventInfo;
        }

        #endregion

        #region event and action handling

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal override async Task<bool> RunEventHandlerAsync()
        {
            if (this.Info.IsHalted)
            {
                return true;
            }

            bool completed = false;
            while (!this.Info.IsHalted && this.RuntimeManager.IsRunning)
            {
                var defaultHandling = false;
                var dequeued = false;

                // Try to get the raised event, if there is one. Raised events
                // have priority over the events in the inbox.
                EventInfo nextEventInfo = this.TryGetRaisedEvent();

                if (nextEventInfo == null)
                {
                    var hasDefaultHandler = HasDefaultHandler();
                    if (hasDefaultHandler)
                    {
                        this.RuntimeManager.NotifyDefaultEventHandlerCheck(this);
                    }

                    lock (this.Inbox)
                    {
                        // Try to dequeue the next event, if there is one.
                        nextEventInfo = this.TryDequeueEvent();
                        dequeued = nextEventInfo != null;

                        if (nextEventInfo == null && hasDefaultHandler)
                        {
                            // Else, get the default event.
                            nextEventInfo = this.GetDefaultEvent();
                            defaultHandling = true;
                        }

                        if (nextEventInfo == null)
                        {
                            completed = true;
                            this.IsActive = false;
                            break;
                        }
                    }
                }

                if (dequeued)
                {
                    // Notifies the runtime for a new event to handle. This is only used
                    // during testing and operation bounding, because the runtime has to
                    // schedule a machine when a new operation is dequeued.
                    this.RuntimeManager.NotifyDequeuedEvent(this, nextEventInfo);
                }
                else if (defaultHandling)
                {
                    // If the default event was handled, then notify the runtime.
                    // This is only used during testing, because the runtime has
                    // to schedule a machine between default handlers.
                    this.RuntimeManager.NotifyDefaultHandlerFired(this);
                }
                else
                {
                    this.RuntimeManager.NotifyHandleRaisedEvent(this, nextEventInfo);
                }

                // Assigns the received event.
                this.ReceivedEvent = nextEventInfo.Event;

                // Handles next event.
                await this.HandleEvent(nextEventInfo.Event);
            }

            return completed;
        }

        /// <summary>
        /// Waits for an event to arrive.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation. The task result is the received <see cref="Event"/>.</returns>
        private Task<Event> WaitOnEvent()
        {
            // Dequeues the first event that the machine waits
            // to receive, if there is one in the inbox.
            EventInfo eventInfoInInbox = null;
            lock (this.Inbox)
            {
                var node = this.Inbox.First;
                while (node != null)
                {
                    var nextNode = node.Next;
                    var currentEventInfo = node.Value;

                    EventWaitHandler eventWaitHandler = this.EventWaitHandlers.FirstOrDefault(
                        val => val.EventType == currentEventInfo.EventType &&
                               val.Predicate(currentEventInfo.Event));

                    if (eventWaitHandler != null)
                    {
                        this.RuntimeManager.Logger.OnReceive(this.Id, this.CurrentStateName, currentEventInfo.EventName, wasBlocked: false);
                        this.EventWaitHandlers.Clear();
                        this.ReceiveCompletionSource.SetResult(currentEventInfo.Event);
                        eventInfoInInbox = currentEventInfo;
                        this.Inbox.Remove(node);
                        break;
                    }

                    node = nextNode;
                }
            }

            this.RuntimeManager.NotifyWaitEvents(this, eventInfoInInbox);

            return this.ReceiveCompletionSource.Task;
        }

        #endregion

        #region state caching

        /// <summary>
        /// Returns the cached state of this machine.
        /// </summary>
        /// <returns>Hash value</returns>
        internal override int GetCachedState()
        {
            unchecked
            {
                var hash = base.GetCachedState();

                foreach (var state in this.StateStack)
                {
                    hash = hash * 31 + state.GetType().GetHashCode();
                }

                foreach (var e in this.Inbox)
                {
                    hash = hash * 31 + e.EventType.GetHashCode();
                    if (this.RuntimeManager.Configuration.EnableUserDefinedStateHashing)
                    {
                        // Adds the user-defined hashed event state.
                        hash = hash * 31 + e.Event.HashedState;
                    }
                }

                return hash;
            }
        }

        #endregion

        #region utilities

        /// <summary>
        /// Returns the names of the events that the machine
        /// is waiting to receive. This is not thread safe.
        /// </summary>
        /// <returns>string</returns>
        internal string GetEventWaitHandlerNames()
        {
            string events = String.Empty;
            foreach (var ewh in this.EventWaitHandlers)
            {
                events += " '" + ewh.EventType.FullName + "'";
            }

            return events;
        }

        /// <summary>
        /// Returns the base machine type of the user machine.
        /// </summary>
        /// <returns>The machine type.</returns>
        private protected override Type GetMachineType() => typeof(Machine);

        #endregion

        #region cleanup methods

        /// <summary>
        /// Halts the machine.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns> 
        private protected override Task HaltMachineAsync()
        {
            lock (this.Inbox)
            {
                this.Info.IsHalted = true;
                this.RuntimeManager.NotifyHalted(this, this.Inbox);
                this.Inbox.Clear();
                this.EventWaitHandlers.Clear();
                this.ReceivedEvent = null;
            }

            return base.HaltMachineAsync();
        }

        #endregion
    }
}
