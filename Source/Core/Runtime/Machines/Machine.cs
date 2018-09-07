// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a P# machine.
    /// </summary>
    public class Machine : BaseMachine, IMachine
    {
        /// <summary>
        /// The runtime manager that executes this machine.
        /// </summary>
        private protected IRuntimeMachineManager RuntimeManager;

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

        /// <summary>
        /// The unique machine id.
        /// </summary>
        protected internal MachineId Id { get; private set; }

        /// <summary>
        /// The unique machine id.
        /// </summary>
        MachineId IMachine.Id => this.Id;

        /// <summary>
        /// Stores machine-related information, which can used
        /// for scheduling and testing.
        /// </summary>
        MachineInfo IMachine.Info => base.Info;

        /// <summary>
        /// The unique name of this machine.
        /// </summary>
        string IMachine.Name => base.Name;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        Dictionary<Type, GotoStateTransition> IMachine.GotoTransitions => base.GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current push state transitions.
        /// </summary>
        Dictionary<Type, PushStateTransition> IMachine.PushTransitions => base.PushTransitions;

        /// <summary>
        /// Gets the <see cref="Type"/> of the current state.
        /// </summary>
        Type IMachine.CurrentState => base.CurrentState;

        /// <summary>
        /// Gets the name of the current state.
        /// </summary>
        string IMachine.CurrentStateName => base.CurrentStateName;

        /// <summary>
        /// The logger installed to the P# runtime.
        /// </summary>
        protected ILogger Logger => this.RuntimeManager.Logger;

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
        /// <param name="runtimeManager">The runtime machine manager.</param>
        /// <param name="mid">The id of this machine.</param>
        /// <param name="info">The metadata of this machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal Task InitializeAsync(IRuntimeMachineManager runtimeManager, MachineId mid, MachineInfo info)
        {
            this.RuntimeManager = runtimeManager;
            this.Id = mid;
            return base.InitializeAsync(info, $"Machine '{mid}'");
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
            return this.CreateMachineAsync(type, null, e);
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
            return this.RuntimeManager.CreateMachineAsync(null, type, friendlyName, e, null, this.Id, this.Info, this.CurrentStateName);
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
            return this.RuntimeManager.CreateMachineAsync(mid, type, mid.FriendlyName, e, null, this.Id, this.Info, this.CurrentStateName);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">The id of the target machine.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="options">Optional parameters.</param>
        //[Obsolete("Please use Machine.SendAsync(...) instead.")]
        protected void Send(MachineId mid, Event e, SendOptions options = null)
        {
            this.SendAsync(mid, e, options).Wait();
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">The id of the target machine.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="options">Optional parameters.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        protected Task SendAsync(MachineId mid, Event e, SendOptions options = null)
        {
            // If the target machine is null, then report an error and exit.
            this.CheckProperty(mid != null, $"{this.Name} is sending to a null machine.");
            // If the event is null, then report an error and exit.
            this.CheckProperty(e != null, $"{this.Name} is sending a null event.");
            return this.RuntimeManager.SendEventAsync(mid, e, options, this.Id, this.Info, this.CurrentState, this.CurrentStateName);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the received <see cref="Event"/>.</returns>
        protected internal Task<Event> Receive(params Type[] eventTypes)
        {
            this.CheckProperty(!this.Info.IsHalted, $"{this.Name} invoked Receive while halted.");
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
        protected internal Task<Event> Receive(Type eventType, Func<Event, bool> predicate)
        {
            this.CheckProperty(!this.Info.IsHalted, $"{this.Name} invoked Receive while halted.");
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
        protected internal Task<Event> Receive(params Tuple<Type, Func<Event, bool>>[] events)
        {
            this.CheckProperty(!this.Info.IsHalted, $"{this.Name} invoked Receive while halted.");
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

        // TODO: add async version of receive

        /// <summary>
        /// Raises an <see cref="Event"/> internally at the end of the current action.
        /// </summary>
        /// <param name="e">Event</param>
        protected void Raise(Event e)
        {
            this.CheckProperty(!this.Info.IsHalted, $"{this.Name} invoked Raise while halted.");
            // If the event is null, then report an error and exit.
            this.CheckProperty(e != null, $"{this.Name} is raising a null event.");
            this.RaisedEvent = new EventInfo(e, new EventOriginInfo(this.Id, this.GetType().Name,
                StateGroup.GetQualifiedStateName(this.CurrentState)));
            this.RuntimeManager.NotifyRaisedEvent(this, this.RaisedEvent);
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action.
        /// </summary>
        /// <typeparam name="S">Type of the state</typeparam>
        protected void Goto<S>() where S : MachineState
        {
#pragma warning disable 618
            Goto(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action. Deprecated in favor of Goto&lt;T&gt;().
        /// </summary>
        /// <param name="s">Type of the state</param>
        [Obsolete("Goto(typeof(T)) is deprecated; use Goto<T>() instead.")]
        protected void Goto(Type s)
        {
            this.CheckProperty(!this.Info.IsHalted, $"{this.Name} invoked Goto while halted.");
            // If the state is not a state of the machine, then report an error and exit.
            this.CheckProperty(StateTypeMap[this.GetType()].Any(val
                => val.DeclaringType.Equals(s.DeclaringType) &&
                val.Name.Equals(s.Name)), $"{this.Name} is trying to transition to non-existing state '{s.Name}'.");
            this.Raise(new GotoStateEvent(s));
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action, pushing current state on the stack.
        /// </summary>
        /// <typeparam name="S">Type of the state</typeparam>
        protected void Push<S>() where S : MachineState
        {
#pragma warning disable 618
            Push(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action, pushing current state on the stack.
        /// Deprecated in favor of Push&lt;T&gt;().
        /// </summary>
        /// <param name="s">Type of the state</param>
        [Obsolete("Push(typeof(T)) is deprecated; use Push<T>() instead.")]
        protected void Push(Type s)
        {
            this.CheckProperty(!this.Info.IsHalted, $"{this.Name} invoked Push while halted.");
            // If the state is not a state of the machine, then report an error and exit.
            this.CheckProperty(StateTypeMap[this.GetType()].Any(val
                => val.DeclaringType.Equals(s.DeclaringType) &&
                val.Name.Equals(s.Name)), $"{this.Name} is trying to transition to non-existing state '{s.Name}'.");
            this.Raise(new PushStateEvent(s));
        }

        /// <summary>
        /// Pops the current <see cref="MachineState"/> from the state stack
        /// at the end of the current action.
        /// </summary>
        protected void Pop()
        {
            if (this.RuntimeManager.IsTestingModeEnabled)
            {
                this.RuntimeManager.NotifyPopAction(this, this.CurrentState, base.StateStack.ElementAtOrDefault(1)?.GetType());
            }
            else
            {
                this.RuntimeManager.NotifyPopAction(this, null, null);
            }

            this.IsPopInvoked = true;
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        protected void Monitor<T>(Event e)
        {
            this.Monitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="e">Event</param>
        protected void Monitor(Type type, Event e)
        {
            // If the event is null, then report an error and exit.
            this.CheckProperty(e != null, $"{this.Name} is sending a null event.");
            this.RuntimeManager.Monitor(type, this.Id, this.Info, this.CurrentState, e);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>The nondeterministic boolean choice.</returns>
        protected bool Random()
        {
            var result = this.RuntimeManager.GetNondeterministicBooleanChoice(this.Id, this.Info, this.CurrentStateName, 2);
            this.RuntimeManager.Logger.OnRandom(this.Id, result);
            return result;
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate a number in the range [0..maxValue), where 0
        /// triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        protected bool Random(int maxValue)
        {
            var result = this.RuntimeManager.GetNondeterministicBooleanChoice(this.Id, this.Info, this.CurrentStateName, maxValue);
            this.RuntimeManager.Logger.OnRandom(this.Id, result);
            return result;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerMemberName">CallerMemberName</param>
        /// <param name="callerFilePath">CallerFilePath</param>
        /// <param name="callerLineNumber">CallerLineNumber</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        protected bool FairRandom(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var havocId = string.Format("{0}_{1}_{2}_{3}_{4}", this.Id.Name, this.CurrentStateName,
                callerMemberName, callerFilePath, callerLineNumber);
            var result = this.RuntimeManager.GetFairNondeterministicBooleanChoice(this.Id, this.Info, this.CurrentStateName, havocId);
            this.RuntimeManager.Logger.OnRandom(this.Id, result);
            return result;
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate an integer in the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic integer choice.</returns>
        protected int RandomInteger(int maxValue)
        {
            var result = this.RuntimeManager.GetNondeterministicIntegerChoice(this.Id, this.Info, this.CurrentStateName, maxValue);
            this.RuntimeManager.Logger.OnRandom(this.Id, result);
            return result;
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws
        /// an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        protected void Assert(bool predicate)
        {
            this.CheckProperty(predicate);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws
        /// an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        protected void Assert(bool predicate, string s, params object[] args)
        {
            this.CheckProperty(predicate, s, args);
        }

        #endregion

        #region inbox accessing

        /// <summary>
        /// Enqueues the specified <see cref="EventInfo"/>.
        /// </summary>
        /// <param name="eventInfo">The event metadata.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result
        /// is the machine status after the enqueue.
        /// </returns>
        Task<MachineStatus> IMachine.EnqueueAsync(EventInfo eventInfo)
        {
            lock (this.Inbox)
            {
                if (this.Info.IsHalted)
                {
                    return Task.FromResult(MachineStatus.IsHalted);
                }

                EventWaitHandler eventWaitHandler = this.EventWaitHandlers.FirstOrDefault(
                    val => val.EventType == eventInfo.EventType &&
                           val.Predicate(eventInfo.Event));
                if (eventWaitHandler != null)
                {
                    this.EventWaitHandlers.Clear();
                    this.RuntimeManager.NotifyReceivedEvent(this, eventInfo);
                    this.ReceiveCompletionSource.SetResult(eventInfo.Event);
                    return Task.FromResult(MachineStatus.EventHandlerRunning);
                }

                this.RuntimeManager.Logger.OnEnqueue(this.Id, eventInfo.EventName);

                this.Inbox.AddLast(eventInfo);

                if (eventInfo.Event.Assert >= 0)
                {
                    var eventCount = this.Inbox.Count(val => val.EventType.Equals(eventInfo.EventType));
                    this.CheckProperty(eventCount <= eventInfo.Event.Assert, "There are more than " +
                        $"{eventInfo.Event.Assert} instances of '{eventInfo.EventName}' " +
                        $"in the input queue of machine '{this}'");
                }

                if (eventInfo.Event.Assume >= 0)
                {
                    var eventCount = this.Inbox.Count(val => val.EventType.Equals(eventInfo.EventType));
                    this.CheckProperty(eventCount <= eventInfo.Event.Assume, "There are more than " +
                        $"{eventInfo.Event.Assume} instances of '{eventInfo.EventName}' " +
                        $"in the input queue of machine '{this}'");
                }

                if (!this.IsActive)
                {
                    if (this.RuntimeManager.IsTestingModeEnabled && this.TryDequeueEvent(true) == null)
                    {
                        return Task.FromResult(MachineStatus.NextEventUnavailable);
                    }
                    else
                    {
                        this.IsActive = true;
                        return Task.FromResult(MachineStatus.EventHandlerNotRunning);
                    }
                }
            }

            return Task.FromResult(MachineStatus.EventHandlerRunning);
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
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Task IMachine.GotoStartStateAsync(Event e) => this.GotoStartStateAsync(e);

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
            Event previouslyDequeuedEvent = null;

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

                if (dequeued)
                {
                    // Inform the user of a successful dequeue once ReceivedEvent is set.
                    previouslyDequeuedEvent = nextEventInfo.Event;
                    await this.OnEventDequeueAsync(previouslyDequeuedEvent);
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
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Task<bool> IMachine.RunEventHandlerAsync() => this.RunEventHandlerAsync();

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

            if (this.RuntimeManager.IsTestingModeEnabled)
            {
                string eventNames = String.Empty;
                foreach (var ewh in this.EventWaitHandlers)
                {
                    eventNames += " '" + ewh.EventType.FullName + "'";
                }

                this.RuntimeManager.NotifyWaitEvents(this, eventInfoInInbox, eventNames);
            }
            else
            {
                this.RuntimeManager.NotifyWaitEvents(this, eventInfoInInbox, String.Empty);
            }

            return this.ReceiveCompletionSource.Task;
        }

        /// <summary>
        /// Returns the default <see cref="EventInfo"/>.
        /// </summary>
        /// <returns>EventInfo</returns>
        private EventInfo GetDefaultEvent()
        {
            this.RuntimeManager.Logger.OnDefault(this.Id, this.CurrentStateName);
            return new EventInfo(new Default(), new EventOriginInfo(
                this.Id, this.GetType().Name, StateGroup.GetQualifiedStateName(this.CurrentState)));
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
                var hash = 19;

                hash = hash * 31 + this.GetType().GetHashCode();
                hash = hash * 31 + this.Id.Value.GetHashCode();
                hash = hash * 31 + this.IsActive.GetHashCode();

                hash = hash * 31 + this.Info.IsHalted.GetHashCode();
                hash = hash * 31 + this.Info.ProgramCounter;

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

                if (this.RuntimeManager.Configuration.EnableUserDefinedStateHashing)
                {
                    // Adds the user-defined hashed machine state.
                    hash = hash * 31 + HashedState;
                }

                return hash;
            }
        }

        /// <summary>
        /// Returns the cached state of this machine.
        /// </summary>
        /// <returns>Hash value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IMachine.GetCachedState() => this.GetCachedState();

        #endregion

        #region code coverage methods

        /// <summary>
        /// Returns the set of all states in the machine (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the machine.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        HashSet<string> IMachine.GetAllStates() => base.GetAllStates();

        /// <summary>
        /// Returns the set of all (state, registered event) pairs in the machine (for code coverage).
        /// </summary>
        /// <returns>Set of all (state, registered event) pairs in the machine.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        HashSet<(string state, string e)> IMachine.GetAllStateEventPairs() => base.GetAllStateEventPairs();

        /// <summary>
        /// Returns the current state transition. Used for code coverage.
        /// </summary>
        /// <param name="eventInfo">The metadata of the event that caused the current transition.</param>
        (string machine, string originState, string destState, string edgeLabel) IMachine.GetCurrentStateTransition(EventInfo eventInfo) =>
            base.GetCurrentStateTransition(eventInfo);

        #endregion

        #region error checking

        /// <summary>
        /// Checks if the specified property holds.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        private protected override void CheckProperty(bool predicate)
        {
            this.RuntimeManager.Assert(predicate);
        }

        /// <summary>
        /// Checks if the specified property holds.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        private protected override void CheckProperty(bool predicate, string s, params object[] args)
        {
            this.RuntimeManager.Assert(predicate, s, args);
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="actionName">Action name</param>
        private protected override void ReportUnhandledException(Exception ex, string actionName)
        {
            string state = "<unknown>";
            if (this.CurrentState != null)
            {
                state = this.CurrentStateName;
            }

            this.RuntimeManager.WrapAndThrowException(ex, $"{this.Name} threw exception '{ex.GetType()}' " +
                $"in state '{state}', action '{actionName}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        /// <summary>
        /// An exception filter that calls OnFailure, which can choose to fast-fail the app
        /// to get a full dump.
        /// </summary>
        /// <param name="ex">The exception being tested</param>
        /// <param name="action">The machine action being executed when the failure occurred</param>
        /// <returns></returns>
        private protected override bool InvokeOnFailureExceptionFilter(CachedAction action, Exception ex)
        {
            // This is called within the exception filter so the stack has not yet been unwound.
            // If OnFailure does not fail-fast, return false to process the exception normally.
            this.RuntimeManager.RaiseOnFailureEvent(new MachineActionExceptionFilterException(action.MethodInfo.Name, ex));
            return false;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that the machine entered a state.
        /// </summary>
        private protected override void NotifyEnteredState()
        {
            this.RuntimeManager.NotifyEnteredState(this);
        }

        /// <summary>
        /// Notifies that the machine exited a state.
        /// </summary>
        private protected override void NotifyExitedState()
        {
            this.RuntimeManager.NotifyExitedState(this);
        }

        /// <summary>
        /// Notifies that the machine is performing a 'goto' transition to the specified state.
        /// </summary>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="newStateName">The target state.</param>
        private protected override void NotifyGotoState(string currStateName, string newStateName)
        {
            this.RuntimeManager.NotifyGotoState(this, currStateName, newStateName);
        }

        /// <summary>
        /// Notifies that the machine is performing a 'push' transition to the specified state.
        /// </summary>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="newStateName">The target state.</param>
        private protected override void NotifyPushState(string currStateName, string newStateName)
        {
            this.RuntimeManager.NotifyPushState(this, currStateName, newStateName);
        }

        /// <summary>
        /// Notifies that the machine is performing a 'pop' transition from the current state.
        /// </summary>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="restoredStateName">The name of the state being restored, if any.</param>
        private protected override void NotifyPopState(string currStateName, string restoredStateName)
        {
            this.RuntimeManager.NotifyPopState(this, currStateName, this.CurrentStateName);
        }

        /// <summary>
        /// Notifies that the machine popped its state because it cannot handle the current event.
        /// </summary>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        private protected override void NotifyPopUnhandledEvent(string currStateName, string eventName)
        {
            this.RuntimeManager.NotifyPopUnhandledEvent(this, currStateName, eventName);
        }

        /// <summary>
        /// Notifies that the machine invoked an action.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        private protected override void NotifyInvokedAction(MethodInfo action, Event receivedEvent)
        {
            this.RuntimeManager.NotifyInvokedAction(this, action, receivedEvent);
        }

        /// <summary>
        /// Notifies that the machine completed an action.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        private protected override void NotifyCompletedAction(MethodInfo action, Event receivedEvent)
        {
            this.RuntimeManager.NotifyCompletedAction(this, action, receivedEvent);
        }

        /// <summary>
        /// Notifies that the machine is throwing an exception.
        /// </summary>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        private protected override void NotifyMachineExceptionThrown(string currStateName, string actionName, Exception ex)
        {
            this.RuntimeManager.NotifyMachineExceptionThrown(this, currStateName, actionName, ex);
        }

        /// <summary>
        /// Notifies that the machine is using 'OnException' to handle a thrown exception.
        /// </summary>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        private protected override void NotifyMachineExceptionHandled(string currStateName, string actionName, Exception ex)
        {
            this.RuntimeManager.NotifyMachineExceptionHandled(this, currStateName, actionName, ex);
        }

        #endregion

        #region utilities

        /// <summary>
        /// Returns the base machine type of the user machine.
        /// </summary>
        /// <returns>The machine type.</returns>
        private protected override Type GetMachineType() => typeof(Machine);

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal
        /// to the current <see cref="object"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Machine m && this.GetType() == m.GetType())
            {
                return this.Id.Equals(m.Id);
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        public override string ToString()
        {
            return this.Id.Name;
        }

        #endregion

        #region user callbacks

        /// <summary>
        /// User callback that is invoked when the machine successfully dequeues
        /// an event from its inbox. This method is not called when the dequeue
        /// happens via a Receive statement.
        /// </summary>
        /// <param name="e">The dequeued event.</param>
        /// <returns></returns>
        protected virtual Task OnEventDequeueAsync(Event e)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// User callback that is invoked when the machine finishes handling a dequeued event,
        /// unless the handler of the dequeued event raised an event or caused the machine to
        /// halt (either normally or due to an exception). Unless this callback raises an event,
        /// the machine will either become idle or dequeue the next event from its inbox.
        /// </summary>
        /// <param name="e">The dequeued event whose handler has just finished.</param>
        /// <returns></returns>
        protected virtual Task OnEventHandledAsync(Event e)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region cleanup

        /// <summary>
        /// Halts the machine.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private protected override async Task HaltMachineAsync()
        {
            lock (this.Inbox)
            {
                this.Info.IsHalted = true;
                this.Logger.OnHalt(this.Id, this.Inbox.Count);

                if (this.RuntimeManager.IsTestingModeEnabled)
                {
                    var mustHandleEvent = this.Inbox.FirstOrDefault(ev => ev.MustHandle);
                    this.CheckProperty(mustHandleEvent == null,
                        "Machine '{0}' halted before dequeueing must-handle event '{1}'.",
                        this.Id, mustHandleEvent?.EventName ?? String.Empty);
                }

                this.Inbox.Clear();
                this.EventWaitHandlers.Clear();
                this.ReceivedEvent = null;
            }

            await this.RuntimeManager.NotifyHaltedAsync(this);
            // Invokes user callback outside the lock.
            await this.OnHaltAsync();
        }

        #endregion
    }
}
