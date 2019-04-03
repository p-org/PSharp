﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.Timers;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state-machine.
    /// </summary>
    public abstract class Machine : BaseMachine
    {
        /// <summary>
        /// Is the machine state cached yet?
        /// </summary>
        private static ConcurrentDictionary<Type, bool> MachineStateCached =
            new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Map from machine types to a set of all
        /// possible states types.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Map from machine types to a set of all
        /// available states.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<MachineState>> StateMap =
            new ConcurrentDictionary<Type, HashSet<MachineState>>();

        /// <summary>
        /// Map from machine types to a set of all
        /// available actions.
        /// </summary>
        private static ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MachineActionMap =
            new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();

        /// <summary>
        /// A stack of machine states. The state on the top of
        /// the stack represents the current state.
        /// </summary>
        private readonly Stack<MachineState> StateStack;

        /// <summary>
        /// A stack of maps that determine event handling action for
        /// each event type. These maps do not keep transition handlers.
        /// This stack has always the same height as StateStack.
        /// </summary>
        private readonly Stack<Dictionary<Type, EventActionHandler>> ActionHandlerStack;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current push state transitions.
        /// </summary>
        internal Dictionary<Type, PushStateTransition> PushTransitions;

        /// <summary>
        /// Map from action names to actions.
        /// </summary>
        private readonly Dictionary<string, CachedAction> ActionMap;

        /// <summary>
        /// Inbox of the state-machine. Incoming events are
        /// queued here. Events are dequeued to be processed.
        /// </summary>
        private readonly LinkedList<EventInfo> Inbox;

        /// <summary>
        /// Gets the raised event. If no event has been raised
        /// this will return null.
        /// </summary>
        private EventInfo RaisedEvent;

        /// <summary>
        /// A list of event wait handlers. They denote the types of events that
        /// the machine is currently waiting to arrive. Each handler contains an
        /// optional predicate and an optional action. If the predicate evaluates
        /// to false, then the received event is deferred. The optional action
        /// executes when the event is received.
        /// </summary>
        private readonly List<EventWaitHandler> EventWaitHandlers;

        /// <summary>
        /// Map that contains the active timers.
        /// </summary>
        private readonly Dictionary<TimerInfo, IMachineTimer> Timers;

        /// <summary>
        /// Completion source that contains the event obtained
        /// using the receive statement.
        /// </summary>
        private TaskCompletionSource<Event> ReceiveCompletionSource;

        /// <summary>
        /// Is the machine running.
        /// </summary>
        private bool IsRunning;

        /// <summary>
        /// Is pop invoked in the current action.
        /// </summary>
        private bool IsPopInvoked;

        /// <summary>
        /// User OnException asked for the machine to be gracefully halted
        /// (suppressing the exception)
        /// </summary>
        private bool OnExceptionRequestedGracefulHalt;

        /// <summary>
        /// The logger installed to the P# runtime.
        /// </summary>
        protected ILogger Logger => this.Runtime.Logger;

        /// <summary>
        /// Gets the <see cref="Type"/> of the current state.
        /// </summary>
        protected internal Type CurrentState
        {
            get
            {
                if (this.StateStack.Count == 0)
                {
                    return null;
                }

                return this.StateStack.Peek().GetType();
            }
        }

        /// <summary>
        /// Gets the current action handler map.
        /// </summary>
        private Dictionary<Type, EventActionHandler> CurrentActionHandlerMap
        {
            get
            {
                if (this.ActionHandlerStack.Count == 0)
                {
                    return null;
                }

                return this.ActionHandlerStack.Peek();
            }
        }

        /// <summary>
        /// Gets the name of the current state.
        /// </summary>
        internal string CurrentStateName
        {
            get
            {
                return this.CurrentState == null
                    ? string.Empty
                    : $"{this.CurrentState.DeclaringType}.{StateGroup.GetQualifiedStateName(this.CurrentState)}";
            }
        }

        /// <summary>
        /// Gets the latest received <see cref="Event"/>, or null if
        /// no <see cref="Event"/> has been received.
        /// </summary>
        protected internal Event ReceivedEvent { get; private set; }

        /// <summary>
        /// User-defined hashed state of the machine. Override to improve the
        /// accuracy of liveness checking when state-caching is enabled.
        /// </summary>
        protected virtual int HashedState => 0;

        /// <summary>
        /// Unique id of the group of operations that is
        /// associated with the next operation.
        /// </summary>
        protected Guid OperationGroupId
        {
            get => this.Info.OperationGroupId;

            set
            {
                this.Info.OperationGroupId = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Machine"/> class.
        /// </summary>
        protected Machine()
        {
            this.Inbox = new LinkedList<EventInfo>();
            this.StateStack = new Stack<MachineState>();
            this.ActionHandlerStack = new Stack<Dictionary<Type, EventActionHandler>>();
            this.ActionMap = new Dictionary<string, CachedAction>();
            this.EventWaitHandlers = new List<EventWaitHandler>();
            this.Timers = new Dictionary<TimerInfo, IMachineTimer>();

            this.IsRunning = true;
            this.IsPopInvoked = false;
            this.OnExceptionRequestedGracefulHalt = false;
        }

        /// <summary>
        /// Creates a new machine of the specified type and with the specified
        /// optional <see cref="Event"/>. This <see cref="Event"/> can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Optional initialization event.</param>
        /// <returns>The unique machine id.</returns>
        protected MachineId CreateMachine(Type type, Event e = null)
        {
            return this.Runtime.CreateMachine(null, type, null, e, this, null);
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and with the
        /// specified optional <see cref="Event"/>. This <see cref="Event"/> can
        /// only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Optional friendly machine name used for logging.</param>
        /// <param name="e">Optional initialization event.</param>
        /// <returns>The unique machine id.</returns>
        protected MachineId CreateMachine(Type type, string friendlyName, Event e = null)
        {
            return this.Runtime.CreateMachine(null, type, friendlyName, e, this, null);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Optional friendly machine name used for logging.</param>
        /// <param name="e">Optional initialization event.</param>
        protected void CreateMachine(MachineId mid, Type type, string friendlyName, Event e = null)
        {
            this.Runtime.CreateMachine(mid, type, friendlyName, e, this, null);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and with the specified
        /// optional <see cref="Event"/>. This <see cref="Event"/> can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        protected MachineId CreateRemoteMachine(Type type, string endpoint, Event e = null)
        {
            return this.Runtime.CreateRemoteMachine(type, null, endpoint, e, this, null);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and name, and with the
        /// specified optional <see cref="Event"/>. This <see cref="Event"/> can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        protected MachineId CreateRemoteMachine(Type type, string friendlyName,
            string endpoint, Event e = null)
        {
            return this.Runtime.CreateRemoteMachine(type, friendlyName, endpoint, e, this, null);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to the specified machine.
        /// </summary>
        /// <param name="mid">The id of the target machine.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="options">Optional send parameters.</param>
        protected void Send(MachineId mid, Event e, SendOptions options = null)
        {
            // If the target machine is null, then report an error and exit.
            this.Assert(mid != null, $"Machine '{this.Id}' is sending to a null machine.");

            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{this.Id}' is sending a null event.");
            this.Runtime.SendEvent(mid, e, this, options);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters</param>
        protected void RemoteSend(MachineId mid, Event e, SendOptions options = null)
        {
            // If the target machine is null, then report an error and exit.
            this.Assert(mid != null, $"Machine '{this.Id}' is sending to a null machine.");

            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{this.Id}' is sending a null event.");
            this.Runtime.SendEventRemotely(mid, e, this, options);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        /// <param name="e">The event to send.</param>
        protected void Monitor<T>(Event e)
        {
            this.Monitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="e">The event to send.</param>
        protected void Monitor(Type type, Event e)
        {
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{this.Id}' is sending a null event.");
            this.Runtime.Monitor(type, this, e);
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action.
        /// </summary>
        /// <typeparam name="S">Type of the state.</typeparam>
        protected void Goto<S>()
            where S : MachineState
        {
#pragma warning disable 618
            this.Goto(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action.
        /// </summary>
        /// <param name="s">Type of the state.</param>
        [Obsolete("Goto(typeof(T)) is deprecated; use Goto<T>() instead.")]
        protected void Goto(Type s)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Goto while halted.");

            // If the state is not a state of the machine, then report an error and exit.
            this.Assert(
                StateTypeMap[this.GetType()].Any(val => val.DeclaringType.Equals(s.DeclaringType) && val.Name.Equals(s.Name)),
                $"Machine '{this.Id}' is trying to transition to non-existing state '{s.Name}'.");
            this.Raise(new GotoStateEvent(s));
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action, pushing current state on the stack.
        /// </summary>
        /// <typeparam name="S">Type of the state.</typeparam>
        protected void Push<S>()
            where S : MachineState
        {
#pragma warning disable 618
            this.Push(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action, pushing current state on the stack.
        /// </summary>
        /// <param name="s">Type of the state.</param>
        [Obsolete("Push(typeof(T)) is deprecated; use Push<T>() instead.")]
        protected void Push(Type s)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Push while halted.");

            // If the state is not a state of the machine, then report an error and exit.
            this.Assert(
                StateTypeMap[this.GetType()].Any(val => val.DeclaringType.Equals(s.DeclaringType) && val.Name.Equals(s.Name)),
                $"Machine '{this.Id}' is trying to transition to non-existing state '{s.Name}'.");
            this.Raise(new PushStateEvent(s));
        }

        /// <summary>
        /// Raises an <see cref="Event"/> internally at the end of the current action.
        /// </summary>
        /// <param name="e">The event to raise.</param>
        protected void Raise(Event e)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Raise while halted.");

            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{this.Id}' is raising a null event.");

            var eventOrigin = new EventOriginInfo(this.Id, this.GetType().Name, GetStateNameForLogging(this.CurrentState));
            this.RaisedEvent = new EventInfo(e, eventOrigin);
            this.Runtime.NotifyRaisedEvent(this, this.RaisedEvent);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types.
        /// </summary>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> Receive(params Type[] eventTypes)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Receive while halted.");
            this.Runtime.NotifyReceiveCalled(this);

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
        /// <param name="eventType">The event type.</param>
        /// <param name="predicate">The used predicate.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> Receive(Type eventType, Func<Event, bool> predicate)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Receive while halted.");
            this.Runtime.NotifyReceiveCalled(this);

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
        /// <param name="events">Event types and predicates.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> Receive(params Tuple<Type, Func<Event, bool>>[] events)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Receive while halted.");
            this.Runtime.NotifyReceiveCalled(this);

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

        /// <summary>
        /// Pops the current <see cref="MachineState"/> from the state stack
        /// at the end of the current action.
        /// </summary>
        protected void Pop()
        {
            this.Runtime.NotifyPop(this);
            this.IsPopInvoked = true;
        }

        /// <summary>
        /// Starts a timer that sends a <see cref="TimerElapsedEvent"/> to this machine after the
        /// specified due time. The timer accepts an optional payload to be used during timeout.
        /// The timer is automatically disposed after it timeouts. To manually stop and dispose
        /// the timer, invoke the <see cref="StopTimer"/> method.
        /// </summary>
        /// <param name="dueTime">The amount of time to wait before sending the first timeout event.</param>
        /// <param name="payload">Optional payload of the timeout event.</param>
        /// <returns>Handle that contains information about the timer.</returns>
        protected TimerInfo StartTimer(TimeSpan dueTime, object payload = null)
        {
            // The specified due time and period must be valid.
            this.Assert(dueTime.TotalMilliseconds >= 0, $"Machine '{this.Id}' registered a timer with a negative due time.");
            return this.RegisterTimer(dueTime, Timeout.InfiniteTimeSpan, payload);
        }

        /// <summary>
        /// Starts a periodic timer that sends a <see cref="TimerElapsedEvent"/> to this machine
        /// after the specified due time, and then repeats after each specified period. The timer
        /// accepts an optional payload to be used during timeout. The timer can be stopped by
        /// invoking the <see cref="StopTimer"/> method.
        /// </summary>
        /// <param name="dueTime">The amount of time to wait before sending the first timeout event.</param>
        /// <param name="period">The time interval between timeout events.</param>
        /// <param name="payload">Optional payload of the timeout event.</param>
        /// <returns>Handle that contains information about the timer.</returns>
        protected TimerInfo StartPeriodicTimer(TimeSpan dueTime, TimeSpan period, object payload = null)
        {
            // The specified due time and period must be valid.
            this.Assert(dueTime.TotalMilliseconds >= 0, $"Machine '{this.Id}' registered a periodic timer with a negative due time.");
            this.Assert(period.TotalMilliseconds >= 0, $"Machine '{this.Id}' registered a periodic timer with a negative period.");
            return this.RegisterTimer(dueTime, period, payload);
        }

        /// <summary>
        /// Stops and disposes the specified timer.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected void StopTimer(TimerInfo info)
        {
            this.Assert(info.OwnerId == this.Id, "Machine '{0}' is not allowed to dispose timer '{1}', which is owned by machine '{2}'.",
                this.Id, info, info.OwnerId);
            this.UnregisterTimer(info);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool Random()
        {
            return this.Runtime.GetNondeterministicBooleanChoice(this, 2);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate a number in the range [0..maxValue), where 0
        /// triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool Random(int maxValue)
        {
            return this.Runtime.GetNondeterministicBooleanChoice(this, maxValue);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool FairRandom(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var havocId = string.Format("{0}_{1}_{2}_{3}_{4}", this.Id.Name, this.CurrentStateName,
                callerMemberName, callerFilePath, callerLineNumber);
            return this.Runtime.GetFairNondeterministicBooleanChoice(this, havocId);
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic integer.</returns>
        protected int RandomInteger(int maxValue)
        {
            return this.Runtime.GetNondeterministicIntegerChoice(this, maxValue);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws
        /// an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate)
        {
            this.Runtime.Assert(predicate);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws
        /// an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, params object[] args)
        {
            this.Runtime.Assert(predicate, s, args);
        }

        /// <summary>
        /// Enqueues the specified <see cref="EventInfo"/>.
        /// </summary>
        internal void Enqueue(EventInfo eventInfo, ref bool runNewHandler)
        {
            lock (this.Inbox)
            {
                if (this.Info.IsHalted)
                {
                    return;
                }

                EventWaitHandler eventWaitHandler = this.EventWaitHandlers.FirstOrDefault(
                    val => val.EventType == eventInfo.EventType &&
                           val.Predicate(eventInfo.Event));
                if (eventWaitHandler != null)
                {
                    this.EventWaitHandlers.Clear();
                    this.Runtime.NotifyReceivedEvent(this, eventInfo);
                    this.ReceiveCompletionSource.SetResult(eventInfo.Event);
                    return;
                }

                this.Runtime.Logger.OnEnqueue(this.Id, eventInfo.EventName);

                this.Inbox.AddLast(eventInfo);

                if (eventInfo.Event.Assert >= 0)
                {
                    var eventCount = this.Inbox.Count(val => val.EventType.Equals(eventInfo.EventType));
                    this.Assert(
                        eventCount <= eventInfo.Event.Assert,
                        $"There are more than {eventInfo.Event.Assert} instances of '{eventInfo.EventName}' in the input queue of machine '{this}'");
                }

                if (eventInfo.Event.Assume >= 0)
                {
                    var eventCount = this.Inbox.Count(val => val.EventType.Equals(eventInfo.EventType));
                    this.Assert(
                        eventCount <= eventInfo.Event.Assume,
                        $"There are more than {eventInfo.Event.Assume} instances of '{eventInfo.EventName}' in the input queue of machine '{this}'");
                }

                if (!this.IsRunning && this.Runtime.CheckStartEventHandler(this))
                {
                    this.IsRunning = true;
                    runNewHandler = true;
                }
            }
        }

        /// <summary>
        /// Dequeues the next available <see cref="EventInfo"/> from the
        /// inbox if there is one available, else returns null.
        /// </summary>
        /// <param name="checkOnly">Only check if event can get dequeued, do not modify inbox.</param>
        internal EventInfo TryDequeueEvent(bool checkOnly = false)
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
                        if (!(tup.Value is IgnoreAction))
                        {
                            continue;
                        }

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

                if (this.IsIgnored(currentEventInfo))
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

        /// <summary>
        /// Returns the raised <see cref="EventInfo"/> if
        /// there is one available, else returns null.
        /// </summary>
        private EventInfo TryGetRaisedEvent()
        {
            EventInfo raisedEventInfo = null;
            if (this.RaisedEvent != null)
            {
                raisedEventInfo = this.RaisedEvent;
                this.RaisedEvent = null;

                // Checks if the raised event is ignored.
                if (this.IsIgnored(raisedEventInfo))
                {
                    raisedEventInfo = null;
                }
            }

            return raisedEventInfo;
        }

        /// <summary>
        /// Returns the default <see cref="EventInfo"/>.
        /// </summary>
        private EventInfo GetDefaultEvent()
        {
            this.Runtime.Logger.OnDefault(this.Id, this.CurrentStateName);

            var eventOrigin = new EventOriginInfo(this.Id, this.GetType().Name, GetStateNameForLogging(this.CurrentState));
            return new EventInfo(new Default(), eventOrigin);
        }

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        internal async Task<bool> RunEventHandler()
        {
            if (this.Info.IsHalted)
            {
                return true;
            }

            bool completed = false;
            Event previouslyDequeuedEvent = null;

            while (!this.Info.IsHalted && this.Runtime.IsRunning)
            {
                var defaultHandling = false;
                var dequeued = false;

                // Try to get the raised event, if there is one. Raised events
                // have priority over the events in the inbox.
                EventInfo nextEventInfo = this.TryGetRaisedEvent();

                if (nextEventInfo == null)
                {
                    var hasDefaultHandler = this.HasDefaultHandler();
                    if (hasDefaultHandler)
                    {
                        this.Runtime.NotifyDefaultEventHandlerCheck(this);
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
                            this.IsRunning = false;
                            break;
                        }
                    }
                }

                if (dequeued)
                {
                    // Notifies the runtime for a new event to handle. This is only used
                    // during bug-finding and operation bounding, because the runtime has
                    // to schedule a machine when a new operation is dequeued.
                    this.Runtime.NotifyDequeuedEvent(this, nextEventInfo);
                }
                else if (defaultHandling)
                {
                    // If the default event was handled, then notify the runtime.
                    // This is only used during bug-finding, because the runtime
                    // has to schedule a machine between default handlers.
                    this.Runtime.NotifyDefaultHandlerFired(this);
                }
                else
                {
                    this.Runtime.NotifyHandleRaisedEvent(this, nextEventInfo);
                }

                // Assigns the received event.
                this.ReceivedEvent = nextEventInfo.Event;

                if (dequeued)
                {
                    // Inform the user of a successful dequeue once ReceivedEvent is set.
                    previouslyDequeuedEvent = nextEventInfo.Event;
                    await this.OnEventDequeueAsync(previouslyDequeuedEvent);
                }

                if (nextEventInfo.Event is TimerElapsedEvent timeoutEvent &&
                    timeoutEvent.Info.Period.TotalMilliseconds < 0)
                {
                    // If the timer is not periodic, then dispose it.
                    this.UnregisterTimer(timeoutEvent.Info);
                }

                // Handles next event.
                await this.HandleEvent(nextEventInfo.Event);

                if (this.RaisedEvent == null && previouslyDequeuedEvent != null && !this.Info.IsHalted)
                {
                    // Inform the user that the machine is done handling the current event.
                    // The machine will either go idle or dequeue its next event.
                    await this.OnEventHandledAsync(previouslyDequeuedEvent);
                    previouslyDequeuedEvent = null;
                }
            }

            return completed;
        }

        /// <summary>
        /// Handles the specified <see cref="Event"/>.
        /// </summary>
        private async Task HandleEvent(Event e)
        {
            this.Info.CurrentActionCalledTransitionStatement = false;
            var currentState = this.CurrentStateName;

            while (true)
            {
                if (this.CurrentState == null)
                {
                    // If the stack of states is empty and the event
                    // is halt, then terminate the machine.
                    if (e.GetType().Equals(typeof(Halt)))
                    {
                        this.HaltMachine();
                        return;
                    }

                    var unhandledEx = new UnhandledEventException(currentState, e, "Unhandled Event");
                    if (this.OnUnhandledEventExceptionHandler("HandleEvent", unhandledEx))
                    {
                        this.HaltMachine();
                        return;
                    }
                    else
                    {
                        // If the event cannot be handled then report an error and exit.
                        this.Assert(false, $"Machine '{this.Id}' received event '{e.GetType().FullName}' that cannot be handled.");
                    }
                }

                if (e.GetType() == typeof(GotoStateEvent))
                {
                    // Checks if the event is a goto state event.
                    Type targetState = (e as GotoStateEvent).State;
                    await this.GotoState(targetState, null);
                }
                else if (e.GetType() == typeof(PushStateEvent))
                {
                    // Checks if the event is a push state event.
                    Type targetState = (e as PushStateEvent).State;
                    await this.PushState(targetState);
                }
                else if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger a goto state transition.
                    var transition = this.GotoTransitions[e.GetType()];
                    await this.GotoState(transition.TargetState, transition.Lambda);
                }
                else if (this.GotoTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    var transition = this.GotoTransitions[typeof(WildCardEvent)];
                    await this.GotoState(transition.TargetState, transition.Lambda);
                }
                else if (this.PushTransitions.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger a push state transition.
                    Type targetState = this.PushTransitions[e.GetType()].TargetState;
                    await this.PushState(targetState);
                }
                else if (this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    Type targetState = this.PushTransitions[typeof(WildCardEvent)].TargetState;
                    await this.PushState(targetState);
                }
                else if (this.CurrentActionHandlerMap.ContainsKey(e.GetType()) &&
                    this.CurrentActionHandlerMap[e.GetType()] is ActionBinding)
                {
                    // Checks if the event can trigger an action.
                    var handler = this.CurrentActionHandlerMap[e.GetType()] as ActionBinding;
                    await this.Do(handler.Name);
                }
                else if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent))
                    && this.CurrentActionHandlerMap[typeof(WildCardEvent)] is ActionBinding)
                {
                    var handler = this.CurrentActionHandlerMap[typeof(WildCardEvent)] as ActionBinding;
                    await this.Do(handler.Name);
                }
                else
                {
                    // If the current state cannot handle the event.
                    await this.ExecuteCurrentStateOnExit(null);
                    if (this.Info.IsHalted)
                    {
                        return;
                    }

                    this.DoStatePop();
                    this.Runtime.Logger.OnPopUnhandledEvent(this.Id, this.CurrentStateName, e.GetType().FullName);
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Invokes an action.
        /// </summary>
        private async Task Do(string actionName)
        {
            var cachedAction = this.ActionMap[actionName];
            this.Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, this.ReceivedEvent);
            await this.ExecuteAction(cachedAction);
            this.Runtime.NotifyCompletedAction(this, cachedAction.MethodInfo, this.ReceivedEvent);

            if (this.IsPopInvoked)
            {
                // Performs the state transition, if pop was invoked during the action.
                await this.PopState();
            }
        }

        /// <summary>
        /// Executes the on entry function of the current state.
        /// </summary>
        private async Task ExecuteCurrentStateOnEntry()
        {
            this.Runtime.NotifyEnteredState(this);

            CachedAction entryAction = null;
            if (this.StateStack.Peek().EntryAction != null)
            {
                entryAction = this.ActionMap[this.StateStack.Peek().EntryAction];
            }

            // Invokes the entry action of the new state,
            // if there is one available.
            if (entryAction != null)
            {
                this.Runtime.NotifyInvokedAction(this, entryAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(entryAction);
                this.Runtime.NotifyCompletedAction(this, entryAction.MethodInfo, this.ReceivedEvent);
            }

            if (this.IsPopInvoked)
            {
                // Performs the state transition, if pop was invoked during the action.
                await this.PopState();
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        /// <param name="eventHandlerExitActionName">Action name</param>
        private async Task ExecuteCurrentStateOnExit(string eventHandlerExitActionName)
        {
            this.Runtime.NotifyExitedState(this);

            CachedAction exitAction = null;
            if (this.StateStack.Peek().ExitAction != null)
            {
                exitAction = this.ActionMap[this.StateStack.Peek().ExitAction];
            }

            this.Info.IsInsideOnExit = true;

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                this.Runtime.NotifyInvokedAction(this, exitAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(exitAction);
                this.Runtime.NotifyCompletedAction(this, exitAction.MethodInfo, this.ReceivedEvent);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null)
            {
                CachedAction eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                this.Runtime.NotifyInvokedAction(this, eventHandlerExitAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(eventHandlerExitAction);
                this.Runtime.NotifyCompletedAction(this, eventHandlerExitAction.MethodInfo, this.ReceivedEvent);
            }

            this.Info.IsInsideOnExit = false;
        }

        /// <summary>
        /// An exception filter that calls OnFailure, which can choose to fast-fail the app
        /// to get a full dump.
        /// </summary>
        /// <param name="action">The machine action being executed when the failure occurred.</param>
        /// <param name="ex">The exception being tested.</param>
        private bool InvokeOnFailureExceptionFilter(CachedAction action, Exception ex)
        {
            // This is called within the exception filter so the stack has not yet been unwound.
            // If OnFailure does not fail-fast, return false to process the exception normally.
            this.Runtime.RaiseOnFailureEvent(new MachineActionExceptionFilterException(action.MethodInfo.Name, ex));
            return false;
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        private async Task ExecuteAction(CachedAction cachedAction)
        {
            try
            {
                if (cachedAction.IsAsync)
                {
                    try
                    {
                        // We have no reliable stack for awaited operations.
                        await cachedAction.ExecuteAsync();
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(cachedAction.MethodInfo.Name, ex))
                    {
                        // user handled the exception, return normally
                    }
                }
                else
                {
                    // Use an exception filter to call OnFailure before the stack has been unwound.
                    try
                    {
                        cachedAction.Execute();
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(cachedAction.MethodInfo.Name, ex))
                    {
                        // user handled the exception, return normally
                    }
                    catch (Exception ex) when (!this.OnExceptionRequestedGracefulHalt && this.InvokeOnFailureExceptionFilter(cachedAction, ex))
                    {
                        // If InvokeOnFailureExceptionFilter does not fail-fast, it returns
                        // false to process the exception normally.
                    }
                }
            }
            catch (Exception ex)
            {
                Exception innerException = ex;
                while (innerException is TargetInvocationException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is AggregateException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is ExecutionCanceledException)
                {
                    this.Info.IsHalted = true;
                    Debug.WriteLine("<Exception> ExecutionCanceledException was " +
                        $"thrown from Machine '{this.Id}'.");
                }
                else if (innerException is TaskSchedulerException)
                {
                    this.Info.IsHalted = true;
                    Debug.WriteLine("<Exception> TaskSchedulerException was " +
                        $"thrown from Machine '{this.Id}'.");
                }
                else if (this.OnExceptionRequestedGracefulHalt)
                {
                    // Gracefully halt.
                    this.HaltMachine();
                }
                else
                {
                    // Reports the unhandled exception.
                    this.ReportUnhandledException(innerException, cachedAction.MethodInfo.Name);
                }
            }
        }

        /// <summary>
        /// Performs a goto transition to the specified state.
        /// </summary>
        private async Task GotoState(Type s, string onExitActionName)
        {
            this.Logger.OnGoto(this.Id, this.CurrentStateName,
                $"{s.DeclaringType}.{GetStateNameForLogging(s)}");

            // The machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExit(onExitActionName);
            if (this.Info.IsHalted)
            {
                return;
            }

            this.DoStatePop();

            var nextState = StateMap[this.GetType()].First(val
                => val.GetType().Equals(s));

            // The machine transitions to the new state.
            this.DoStatePush(nextState);

            // The machine performs the on entry action of the new state.
            await this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a push transition to the specified state.
        /// </summary>
        private async Task PushState(Type s)
        {
            this.Runtime.Logger.OnPush(this.Id, this.CurrentStateName, s.FullName);

            var nextState = StateMap[this.GetType()].First(val => val.GetType().Equals(s));
            this.DoStatePush(nextState);

            // The machine performs the on entry statements of the new state.
            await this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a pop transition from the current state.
        /// </summary>
        private async Task PopState()
        {
            this.IsPopInvoked = false;
            var prevStateName = this.CurrentStateName;

            // The machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExit(null);
            if (this.Info.IsHalted)
            {
                return;
            }

            this.DoStatePop();
            this.Runtime.Logger.OnPop(this.Id, prevStateName, this.CurrentStateName);

            // Watch out for an extra pop.
            this.Assert(this.CurrentState != null, $"Machine '{this.Id}' popped with no matching push.");
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is pushed on to the stack.
        /// </summary>
        private void DoStatePush(MachineState state)
        {
            this.GotoTransitions = state.GotoTransitions;
            this.PushTransitions = state.PushTransitions;

            // Gets existing map for actions.
            var eventHandlerMap = this.CurrentActionHandlerMap == null ?
                new Dictionary<Type, EventActionHandler>() :
                new Dictionary<Type, EventActionHandler>(this.CurrentActionHandlerMap);

            // Updates the map with defer annotations.
            foreach (var deferredEvent in state.DeferredEvents)
            {
                if (deferredEvent.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[deferredEvent] = new DeferAction();
                    break;
                }

                eventHandlerMap[deferredEvent] = new DeferAction();
            }

            // Updates the map with actions.
            foreach (var actionBinding in state.ActionBindings)
            {
                if (actionBinding.Key.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[actionBinding.Key] = actionBinding.Value;
                    break;
                }

                eventHandlerMap[actionBinding.Key] = actionBinding.Value;
            }

            // Updates the map with ignores.
            foreach (var ignoreEvent in state.IgnoredEvents)
            {
                if (ignoreEvent.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[ignoreEvent] = new IgnoreAction();
                    break;
                }

                eventHandlerMap[ignoreEvent] = new IgnoreAction();
            }

            // Removes the ones on which transitions are defined.
            foreach (var eventType in this.GotoTransitions.Keys.Union(this.PushTransitions.Keys))
            {
                if (eventType.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    break;
                }

                eventHandlerMap.Remove(eventType);
            }

            this.StateStack.Push(state);
            this.ActionHandlerStack.Push(eventHandlerMap);
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is popped.
        /// </summary>
        private void DoStatePop()
        {
            this.StateStack.Pop();
            this.ActionHandlerStack.Pop();

            if (this.StateStack.Count > 0)
            {
                this.GotoTransitions = this.StateStack.Peek().GotoTransitions;
                this.PushTransitions = this.StateStack.Peek().PushTransitions;
            }
            else
            {
                this.GotoTransitions = null;
                this.PushTransitions = null;
            }
        }

        /// <summary>
        /// Waits for an event to arrive.
        /// </summary>
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
                        this.Runtime.Logger.OnReceive(this.Id, this.CurrentStateName, currentEventInfo.EventName, wasBlocked: false);
                        this.EventWaitHandlers.Clear();
                        this.ReceiveCompletionSource.SetResult(currentEventInfo.Event);
                        eventInfoInInbox = currentEventInfo;
                        this.Inbox.Remove(node);
                        break;
                    }

                    node = nextNode;
                }
            }

            this.Runtime.NotifyWaitEvents(this, eventInfoInInbox);

            return this.ReceiveCompletionSource.Task;
        }

        /// <summary>
        /// Checks if the specified event should be ignored.
        /// </summary>
        private bool IsIgnored(EventInfo eventInfo)
        {
            if (eventInfo.Event is TimerElapsedEvent timeoutEvent &&
                !this.Timers.ContainsKey(timeoutEvent.Info))
            {
                // The timer that created this timeout event is not active.
                return true;
            }

            // If a transition is defined, then the event is not ignored.
            if (this.GotoTransitions.ContainsKey(eventInfo.EventType) ||
                this.PushTransitions.ContainsKey(eventInfo.EventType) ||
                this.GotoTransitions.ContainsKey(typeof(WildCardEvent)) ||
                this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
            {
                return false;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(eventInfo.EventType))
            {
                return this.CurrentActionHandlerMap[eventInfo.EventType] is IgnoreAction;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentActionHandlerMap[typeof(WildCardEvent)] is IgnoreAction)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified event should be deferred.
        /// </summary>
        private bool IsDeferred(Type e)
        {
            // if transition is defined, then no
            if (this.GotoTransitions.ContainsKey(e) || this.PushTransitions.ContainsKey(e) ||
                this.GotoTransitions.ContainsKey(typeof(WildCardEvent)) ||
                this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
            {
                return false;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(e))
            {
                return this.CurrentActionHandlerMap[e] is DeferAction;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentActionHandlerMap[typeof(WildCardEvent)] is DeferAction)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the machine has a default handler.
        /// </summary>
        private bool HasDefaultHandler()
        {
            return this.CurrentActionHandlerMap.ContainsKey(typeof(Default)) ||
                this.GotoTransitions.ContainsKey(typeof(Default)) ||
                this.PushTransitions.ContainsKey(typeof(Default));
        }

        /// <summary>
        /// Returns the cached state of this machine.
        /// </summary>
        internal int GetCachedState()
        {
            unchecked
            {
                var hash = 19;

                hash = (hash * 31) + this.GetType().GetHashCode();
                hash = (hash * 31) + this.Id.Value.GetHashCode();
                hash = (hash * 31) + this.IsRunning.GetHashCode();

                hash = (hash * 31) + this.Info.IsHalted.GetHashCode();
                hash = (hash * 31) + this.Info.ProgramCounter;

                if (this.Runtime.Configuration.EnableUserDefinedStateHashing)
                {
                    // Adds the user-defined hashed machine state.
                    hash = (hash * 31) + this.HashedState;
                }

                foreach (var state in this.StateStack)
                {
                    hash = (hash * 31) + state.GetType().GetHashCode();
                }

                foreach (var e in this.Inbox)
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
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        internal Task GotoStartState(Event e)
        {
            this.ReceivedEvent = e;
            return this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Initializes information about the states of the machine.
        /// </summary>
        internal void InitializeStateInformation()
        {
            Type machineType = this.GetType();

            if (MachineStateCached.TryAdd(machineType, false))
            {
                // Caches the available state types for this machine type.
                if (StateTypeMap.TryAdd(machineType, new HashSet<Type>()))
                {
                    Type baseType = machineType;
                    while (baseType != typeof(Machine))
                    {
                        foreach (var s in baseType.GetNestedTypes(BindingFlags.Instance |
                            BindingFlags.NonPublic | BindingFlags.Public |
                            BindingFlags.DeclaredOnly))
                        {
                            this.ExtractStateTypes(s);
                        }

                        baseType = baseType.BaseType;
                    }
                }

                // Caches the available state instances for this machine type.
                if (StateMap.TryAdd(machineType, new HashSet<MachineState>()))
                {
                    foreach (var type in StateTypeMap[machineType])
                    {
                        Type stateType = type;
                        if (type.IsAbstract)
                        {
                            continue;
                        }

                        if (type.IsGenericType)
                        {
                            // If the state type is generic (only possible if inherited by a
                            // generic machine declaration), then iterate through the base
                            // machine classes to identify the runtime generic type, and use
                            // it to instantiate the runtime state type. This type can be
                            // then used to create the state constructor.
                            Type declaringType = this.GetType();
                            while (!declaringType.IsGenericType ||
                                !type.DeclaringType.FullName.Equals(declaringType.FullName.Substring(
                                0, declaringType.FullName.IndexOf('['))))
                            {
                                declaringType = declaringType.BaseType;
                            }

                            if (declaringType.IsGenericType)
                            {
                                stateType = type.MakeGenericType(declaringType.GetGenericArguments());
                            }
                        }

                        ConstructorInfo constructor = stateType.GetConstructor(Type.EmptyTypes);
                        var lambda = Expression.Lambda<Func<MachineState>>(
                            Expression.New(constructor)).Compile();
                        MachineState state = lambda();

                        try
                        {
                            state.InitializeState();
                        }
                        catch (InvalidOperationException ex)
                        {
                            this.Assert(false, $"Machine '{this.Id}' {ex.Message} in state '{state}'.");
                        }

                        StateMap[machineType].Add(state);
                    }
                }

                // Caches the actions declarations for this machine type.
                if (MachineActionMap.TryAdd(machineType, new Dictionary<string, MethodInfo>()))
                {
                    foreach (var state in StateMap[machineType])
                    {
                        if (state.EntryAction != null &&
                            !MachineActionMap[machineType].ContainsKey(state.EntryAction))
                        {
                            MachineActionMap[machineType].Add(
                                state.EntryAction,
                                this.GetActionWithName(state.EntryAction));
                        }

                        if (state.ExitAction != null &&
                            !MachineActionMap[machineType].ContainsKey(state.ExitAction))
                        {
                            MachineActionMap[machineType].Add(
                                state.ExitAction,
                                this.GetActionWithName(state.ExitAction));
                        }

                        foreach (var transition in state.GotoTransitions)
                        {
                            if (transition.Value.Lambda != null &&
                                !MachineActionMap[machineType].ContainsKey(transition.Value.Lambda))
                            {
                                MachineActionMap[machineType].Add(
                                    transition.Value.Lambda,
                                    this.GetActionWithName(transition.Value.Lambda));
                            }
                        }

                        foreach (var action in state.ActionBindings)
                        {
                            if (!MachineActionMap[machineType].ContainsKey(action.Value.Name))
                            {
                                MachineActionMap[machineType].Add(
                                    action.Value.Name,
                                    this.GetActionWithName(action.Value.Name));
                            }
                        }
                    }
                }

                // Cache completed.
                lock (MachineStateCached)
                {
                    MachineStateCached[machineType] = true;
                    System.Threading.Monitor.PulseAll(MachineStateCached);
                }
            }
            else if (!MachineStateCached[machineType])
            {
                lock (MachineStateCached)
                {
                    while (!MachineStateCached[machineType])
                    {
                        System.Threading.Monitor.Wait(MachineStateCached);
                    }
                }
            }

            // Populates the map of actions for this machine instance.
            foreach (var kvp in MachineActionMap[machineType])
            {
                this.ActionMap.Add(kvp.Key, new CachedAction(kvp.Value, this));
            }

            var initialStates = StateMap[machineType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, $"Machine '{this.Id}' must declare a start state.");
            this.Assert(initialStates.Count == 1, $"Machine '{this.Id}' can not declare more than one start states.");

            this.DoStatePush(initialStates.Single());

            this.AssertStateValidity();
        }

        /// <summary>
        /// Registers a new timer using the specified configuration.
        /// </summary>
        private TimerInfo RegisterTimer(TimeSpan dueTime, TimeSpan period, object payload)
        {
            var info = new TimerInfo(this.Id, dueTime, period, payload);
            var timer = this.Runtime.CreateMachineTimer(info, this);
            this.Logger.OnCreateTimer(info);
            this.Timers.Add(info, timer);
            return info;
        }

        /// <summary>
        /// Unregisters the specified timer.
        /// </summary>
        private void UnregisterTimer(TimerInfo info)
        {
            if (!this.Timers.TryGetValue(info, out IMachineTimer timer))
            {
                this.Assert(info.OwnerId == this.Id, "Timer '{0}' is already disposed.", info);
            }

            this.Logger.OnStopTimer(info);
            this.Timers.Remove(info);
            timer.Dispose();
        }

        /// <summary>
        /// Returns the names of the events that the machine
        /// is waiting to receive. This is not thread safe.
        /// </summary>
        internal string GetEventWaitHandlerNames()
        {
            string events = string.Empty;
            foreach (var ewh in this.EventWaitHandlers)
            {
                events += " '" + ewh.EventType.FullName + "'";
            }

            return events;
        }

        /// <summary>
        /// Returns the type of the state at the specified state
        /// stack index, if there is one.
        /// </summary>
        internal Type GetStateTypeAtStackIndex(int index)
        {
            return this.StateStack.ElementAtOrDefault(index)?.GetType();
        }

        /// <summary>
        /// Processes a type, looking for machine states.
        /// </summary>
        private void ExtractStateTypes(Type type)
        {
            Stack<Type> stack = new Stack<Type>();
            stack.Push(type);

            while (stack.Count > 0)
            {
                Type nextType = stack.Pop();

                if (nextType.IsClass && nextType.IsSubclassOf(typeof(MachineState)))
                {
                    StateTypeMap[this.GetType()].Add(nextType);
                }
                else if (nextType.IsClass && nextType.IsSubclassOf(typeof(StateGroup)))
                {
                    // Adds the contents of the group of states to the stack.
                    foreach (var t in nextType.GetNestedTypes(BindingFlags.Instance |
                        BindingFlags.NonPublic | BindingFlags.Public |
                        BindingFlags.DeclaredOnly))
                    {
                        this.Assert(
                            t.IsSubclassOf(typeof(StateGroup)) ||
                            t.IsSubclassOf(typeof(MachineState)), $"'{t.Name}' is neither a group of states nor a state.");
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        private MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo method = null;
            Type machineType = this.GetType();

            do
            {
                method = machineType.GetMethod(
                    actionName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    Type.DefaultBinder, Array.Empty<Type>(), null);
                machineType = machineType.BaseType;
            }
            while (method == null && machineType != typeof(Machine));

            this.Assert(method != null, "Cannot detect action declaration '{0}' in machine '{1}'.", actionName, this.GetType().Name);
            this.Assert(method.GetParameters().Length == 0, "Action '{0}' in machine '{1}' must have 0 formal parameters.",
                method.Name, this.GetType().Name);

            if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            {
                this.Assert(method.ReturnType == typeof(Task), "Async action '{0}' in machine '{1}' must have 'Task' return type.",
                    method.Name, this.GetType().Name);
            }
            else
            {
                this.Assert(method.ReturnType == typeof(void), "Action '{0}' in machine '{1}' must have 'void' return type.",
                    method.Name, this.GetType().Name);
            }

            return method;
        }

        /// <summary>
        /// Returns the state name to be used for logging purposes.
        /// </summary>
        internal static string GetStateNameForLogging(Type state) =>
            state == null ? "None" : StateGroup.GetQualifiedStateName(state);

        /// <summary>
        /// Returns the set of all states in the machine (for code coverage).
        /// </summary>
        internal override HashSet<string> GetAllStates()
        {
            this.Assert(
                StateMap.ContainsKey(this.GetType()),
                $"Machine '{this.Id}' hasn't populated its states yet.");

            var allStates = new HashSet<string>();
            foreach (var state in StateMap[this.GetType()])
            {
                allStates.Add(StateGroup.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine (for code coverage).
        /// </summary>
        internal override HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            this.Assert(
                StateMap.ContainsKey(this.GetType()),
                $"Machine '{this.Id}' hasn't populated its states yet.");

            var pairs = new HashSet<Tuple<string, string>>();
            foreach (var state in StateMap[this.GetType()])
            {
                foreach (var binding in state.ActionBindings)
                {
                    pairs.Add(Tuple.Create(StateGroup.GetQualifiedStateName(state.GetType()), binding.Key.Name));
                }

                foreach (var transition in state.GotoTransitions)
                {
                    pairs.Add(Tuple.Create(StateGroup.GetQualifiedStateName(state.GetType()), transition.Key.Name));
                }

                foreach (var pushtransition in state.PushTransitions)
                {
                    pairs.Add(Tuple.Create(StateGroup.GetQualifiedStateName(state.GetType()), pushtransition.Key.Name));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Check machine for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(StateTypeMap[this.GetType()].Count > 0, $"Machine '{this.Id}' must have one or more states.");
            this.Assert(this.StateStack.Peek() != null, $"Machine '{this.Id}' must not have a null current state.");
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private void ReportUnhandledException(Exception ex, string actionName)
        {
            string state = "<unknown>";
            if (this.CurrentState != null)
            {
                state = this.CurrentStateName;
            }

            this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                $"in machine '{this.Id}', state '{state}', action '{actionName}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        /// <summary>
        /// Invokes user callback when a machine receives an event it cannot handle
        /// </summary>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="ex">The exception thrown by the machine.</param>
        /// <returns>False if the exception should continue to get thrown, true if the machine should gracefully halt.</returns>
        private bool OnUnhandledEventExceptionHandler(string methodName, UnhandledEventException ex)
        {
            this.Logger.OnMachineExceptionThrown(this.Id, ex.CurrentStateName, methodName, ex);

            var ret = this.OnException(methodName, ex);
            this.OnExceptionRequestedGracefulHalt = false;
            switch (ret)
            {
                case OnExceptionOutcome.HaltMachine:
                case OnExceptionOutcome.HandledException:
                    this.Logger.OnMachineExceptionHandled(this.Id, ex.CurrentStateName, methodName, ex);
                    this.OnExceptionRequestedGracefulHalt = true;
                    return true;
                case OnExceptionOutcome.ThrowException:
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Invokes user callback when a machine throws an exception.
        /// </summary>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="ex">The exception thrown by the machine.</param>
        /// <returns>False if the exception should continue to get thrown, true if it was handled in this method.</returns>
        private bool OnExceptionHandler(string methodName, Exception ex)
        {
            if (ex is ExecutionCanceledException)
            {
                // internal exception, used by PsharpTester
                return false;
            }

            this.Logger.OnMachineExceptionThrown(this.Id, this.CurrentStateName, methodName, ex);

            var ret = this.OnException(methodName, ex);
            this.OnExceptionRequestedGracefulHalt = false;

            switch (ret)
            {
                case OnExceptionOutcome.ThrowException:
                    return false;
                case OnExceptionOutcome.HandledException:
                    this.Logger.OnMachineExceptionHandled(this.Id, this.CurrentStateName, methodName, ex);
                    return true;
                case OnExceptionOutcome.HaltMachine:
                    this.OnExceptionRequestedGracefulHalt = true;
                    return false;
            }

            return false;
        }

        /// <summary>
        /// User callback when a machine throws an exception.
        /// </summary>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="ex">The exception thrown by the machine.</param>
        /// <returns>The action that the runtime should take.</returns>
        protected virtual OnExceptionOutcome OnException(string methodName, Exception ex)
        {
            return OnExceptionOutcome.ThrowException;
        }

        /// <summary>
        /// User callback that is invoked when the machine successfully dequeues
        /// an event from its inbox. This method is not called when the dequeue
        /// happens via a Receive statement.
        /// </summary>
        protected virtual Task OnEventDequeueAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// User callback that is invoked when the machine finishes handling a dequeued event,
        /// unless the handler of the dequeued event raised an event or caused the machine to
        /// halt (either normally or due to an exception). Unless this callback raises an event,
        /// the machine will either become idle or dequeue the next event from its inbox.
        /// </summary>
        protected virtual Task OnEventHandledAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// User callback that is invoked when a machine halts.
        /// </summary>
        protected virtual void OnHalt()
        {
        }

        /// <summary>
        /// Resets the static caches.
        /// </summary>
        internal static void ResetCaches()
        {
            StateTypeMap.Clear();
            StateMap.Clear();
            MachineActionMap.Clear();
        }

        /// <summary>
        /// Halts the machine.
        /// </summary>
        private void HaltMachine()
        {
            // Dispose any active timers.
            foreach (var timer in this.Timers.Keys.ToList())
            {
                this.UnregisterTimer(timer);
            }

            var inboxContents = new LinkedList<EventInfo>();

            lock (this.Inbox)
            {
                this.Info.IsHalted = true;
                inboxContents = new LinkedList<EventInfo>(this.Inbox);

                this.Inbox.Clear();
                this.EventWaitHandlers.Clear();
                this.ReceivedEvent = null;
            }

            this.Runtime.NotifyHalted(this, inboxContents);

            // Invoke user callback outside the lock.
            this.OnHalt();
        }
    }
}
