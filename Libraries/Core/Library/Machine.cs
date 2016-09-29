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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state-machine.
    /// </summary>
    public abstract class Machine : AbstractMachine
    {
        #region fields

        /// <summary>
        /// Map from machine types to a set of all
        /// possible states types.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap;

        /// <summary>
        /// Map from machine types to a set of all
        /// available states.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<MachineState>> StateMap;

        /// <summary>
        /// Map from machine types to a set of all
        /// available actions.
        /// </summary>
        private static ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MachineActionMap;

        /// <summary>
        /// A stack of machine states. The state on the top of
        /// the stack represents the current state.
        /// </summary>
        private Stack<MachineState> StateStack;

        /// <summary>
        /// A stack of maps that determine event handling action for
        /// each event type. These maps do not keep transition handlers.
        /// This stack has always the same height as StateStack.
        /// </summary>
        private Stack<Dictionary<Type, EventActionHandler>> ActionHandlerStack;

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
        private Dictionary<string, MethodInfo> ActionMap;

        /// <summary>
        /// Is machine running.
        /// </summary>
        private bool IsRunning;

        /// <summary>
        /// Is machine halted.
        /// </summary>
        private bool IsHalted;

        /// <summary>
        /// Is machine waiting to receive an event.
        /// </summary>
        internal bool IsWaitingToReceive;

        /// <summary>
        /// Inbox of the state-machine. Incoming events are
        /// queued here. Events are dequeued to be processed.
        /// </summary>
        private List<EventInfo> Inbox;

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
        private List<EventWaitHandler> EventWaitHandlers;

        /// <summary>
        /// Event obtained using the receive statement.
        /// </summary>
        private Event EventViaReceiveStatement;

        /// <summary>
        /// Program counter used for state-caching. Distinguishes
        /// scheduling from non-deterministic choices.
        /// </summary>
        private int ProgramCounter;

        /// <summary>
        /// Gets the current state.
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
        /// Gets the current state name.
        /// </summary>
        internal string CurrentStateName
        {
            get
            {
                return $"{this.CurrentState.DeclaringType}." +
                    $"{this.CurrentState.Name}";
            }
        }

        /// <summary>
        /// Gets the latest received event, or null if no event
        /// has been received.
        /// </summary>
        protected internal Event ReceivedEvent { get; private set; }

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Machine()
        {
            StateTypeMap = new ConcurrentDictionary<Type, HashSet<Type>>();
            StateMap = new ConcurrentDictionary<Type, HashSet<MachineState>>();
            MachineActionMap = new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Machine()
            : base()
        {
            this.Inbox = new List<EventInfo>();
            this.StateStack = new Stack<MachineState>();
            this.ActionHandlerStack = new Stack<Dictionary<Type, EventActionHandler>>();
            this.ActionMap = new Dictionary<string, MethodInfo>();
            this.EventWaitHandlers = new List<EventWaitHandler>();

            this.IsRunning = true;
            this.IsHalted = false;
            this.IsWaitingToReceive = false;
        }

        #endregion

        #region P# user API

        /// <summary>
        /// Creates a new machine of the specified type and with the
        /// specified optional event. This event can only be used to
        /// access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        protected MachineId CreateMachine(Type type, Event e = null)
        {
            return base.Runtime.TryCreateMachine(this, type, null, e);
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and
        /// with the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        protected MachineId CreateMachine(Type type, string friendlyName, Event e = null)
        {
            return base.Runtime.TryCreateMachine(this, type, friendlyName, e);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and with
        /// the specified optional event. This event can only be used to
        /// access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        protected MachineId CreateRemoteMachine(Type type, string endpoint, Event e = null)
        {
            return base.Runtime.TryCreateRemoteMachine(this, type, null, endpoint, e);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and name, and
        /// with the specified optional event. This event can only be used to
        /// access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        protected MachineId CreateRemoteMachine(Type type, string friendlyName,
            string endpoint, Event e = null)
        {
            return base.Runtime.TryCreateRemoteMachine(this, type, friendlyName, endpoint, e);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        protected void Send(MachineId mid, Event e, bool isStarter = false)
        {
            // If the target machine is null, then report an error and exit.
            this.Assert(mid != null, $"Machine '{base.Id}' is sending to a null machine.");
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{base.Id}' is sending a null event.");
            base.Runtime.Send(this, mid, e, isStarter);
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        protected void RemoteSend(MachineId mid, Event e, bool isStarter = false)
        {
            // If the target machine is null, then report an error and exit.
            this.Assert(mid != null, $"Machine '{base.Id}' is sending to a null machine.");
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{base.Id}' is sending a null event.");
            base.Runtime.SendRemotely(this, mid, e, isStarter);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        protected void Monitor<T>(Event e)
        {
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{base.Id}' is sending a null event.");
            base.Runtime.Monitor<T>(this, e);
        }

        /// <summary>
        /// Returns from the execution context, and transitions
        /// the machine to the specified state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        protected void Goto(Type s)
        {
            // If the state is not a state of the machine, then report an error and exit.
            this.Assert(StateTypeMap[this.GetType()].Any(val
                => val.DeclaringType.Equals(s.DeclaringType) &&
                val.Name.Equals(s.Name)), $"Machine '{base.Id}' " +
                $"is trying to transition to non-existing state '{s.Name}'.");
            this.Raise(new GotoStateEvent(s));
        }

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        protected void Raise(Event e, bool isStarter = false)
        {
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{base.Id}' is raising a null event.");
            this.RaisedEvent = new EventInfo(e, new EventOriginInfo(
                base.Id, this.GetType().Name, Machine.GetQualifiedStateName(this.CurrentState)));
            base.Runtime.NotifyRaisedEvent(this, this.RaisedEvent, isStarter);
        }

        /// <summary>
        /// Blocks and waits to receive an event of the specified types.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        /// <returns>Event received</returns>
        protected internal Event Receive(params Type[] eventTypes)
        {
            base.Runtime.NotifyReceiveCalled(this);

            lock (this.Inbox)
            {
                foreach (var type in eventTypes)
                {
                    this.EventWaitHandlers.Add(new EventWaitHandler(type));
                }
            }

            this.WaitOnEvent();

            var received = this.EventViaReceiveStatement;
            this.EventViaReceiveStatement = null;
            return received;
        }

        /// <summary>
        /// Blocks and waits to receive an event of the specified type
        /// that satisfies the specified predicate.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="predicate">Predicate</param>
        /// <returns>Event received</returns>
        protected internal Event Receive(Type eventType, Func<Event, bool> predicate)
        {
            base.Runtime.NotifyReceiveCalled(this);

            lock (this.Inbox)
            {
                this.EventWaitHandlers.Add(new EventWaitHandler(eventType, predicate));
            }

            this.WaitOnEvent();

            var received = this.EventViaReceiveStatement;
            this.EventViaReceiveStatement = null;
            return received;
        }

        /// <summary>
        /// Blocks and waits to receive an event of the specified types
        /// that satisfy the specified predicates.
        /// </summary>
        /// <param name="events">Event types and predicates</param>
        /// <returns>Event received</returns>
        protected internal Event Receive(params Tuple<Type, Func<Event, bool>>[] events)
        {
            base.Runtime.NotifyReceiveCalled(this);

            lock (this.Inbox)
            {
                foreach (var e in events)
                {
                    this.EventWaitHandlers.Add(new EventWaitHandler(e.Item1, e.Item2));
                }
            }

            this.WaitOnEvent();

            var received = this.EventViaReceiveStatement;
            this.EventViaReceiveStatement = null;
            return received;
        }

        /// <summary>
        /// Pops the current state from the state stack.
        /// </summary>
        protected void Pop()
        {
            base.Runtime.NotifyPop(this);

            // The machine performs the on exit action of the current state.
            this.ExecuteCurrentStateOnExit(null);
            if (this.IsHalted)
            {
                return;
            }

            this.DoStatePop();

            if (this.CurrentState == null)
            {
                base.Runtime.Log($"<PopLog> Machine '{base.Id}' popped.");
            }
            else
            {
                base.Runtime.Log($"<PopLog> Machine '{base.Id}' popped " +
                    $"and reentered state '{this.CurrentStateName}'.");
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected bool Random()
        {
            this.ProgramCounter++;
            return base.Runtime.GetNondeterministicBooleanChoice(this, 2);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate a number in the range [0..maxValue), where 0
        /// triggers true.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        protected bool Random(int maxValue)
        {
            this.ProgramCounter++;
            return base.Runtime.GetNondeterministicBooleanChoice(this, maxValue);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected bool FairRandom()
        {
            this.ProgramCounter++;
            return base.Runtime.GetNondeterministicBooleanChoice(this, 2);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected bool FairRandom(int uniqueId)
        {
            this.ProgramCounter++;
            var havocId = base.Id.Name + "_" + this.CurrentStateName + "_" + uniqueId;
            return base.Runtime.GetFairNondeterministicBooleanChoice(this, havocId);
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate an integer in the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
        protected int RandomInteger(int maxValue)
        {
            this.ProgramCounter++;
            return base.Runtime.GetNondeterministicIntegerChoice(this, maxValue);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        protected void Assert(bool predicate)
        {
            base.Runtime.Assert(predicate);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        protected void Assert(bool predicate, string s, params object[] args)
        {
            base.Runtime.Assert(predicate, s, args);
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        /// <param name="e">Event</param>
        internal void GotoStartState(Event e)
        {
            this.ReceivedEvent = e;
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Enqueues the event wrapper.
        /// </summary>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="runNewHandler">Run a new handler</param>
        internal void Enqueue(EventInfo eventInfo, ref bool runNewHandler)
        {
            lock (this.Inbox)
            {
                if (this.IsHalted)
                {
                    return;
                }

                EventWaitHandler eventWaitHandler = this.EventWaitHandlers.FirstOrDefault(
                    val => val.EventType == eventInfo.EventType &&
                           val.Predicate(eventInfo.Event));
                if (eventWaitHandler != null)
                {
                    this.EventViaReceiveStatement = eventInfo.Event;
                    this.EventWaitHandlers.Clear();
                    base.Runtime.NotifyReceivedEvent(this, eventInfo);
                    return;
                }

                base.Runtime.Log($"<EnqueueLog> Machine '{base.Id}' " +
                    $"enqueued event '{eventInfo.EventName}'.");

                this.Inbox.Add(eventInfo);

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

                if (!this.IsRunning)
                {
                    this.IsRunning = true;
                    runNewHandler = true;
                }
            }
        }

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        internal void RunEventHandler()
        {
            if (this.IsHalted)
            {
                return;
            }

            EventInfo nextEventInfo = null;

            while (!this.IsHalted)
            {
                var defaultHandling = false;
                var dequeued = false;
                lock (this.Inbox)
                {
                    dequeued = this.GetNextEvent(out nextEventInfo);
                    this.ProgramCounter = 0;

                    // Check if next event to process is null.
                    if (nextEventInfo == null)
                    {
                        if (this.HasDefaultHandler())
                        {
                            base.Runtime.Log($"<DefaultLog> Machine '{base.Id}' " +
                                "is executing the default handler in state " +
                                $"'{this.CurrentStateName}'.");

                            nextEventInfo = new EventInfo(new Default(), new EventOriginInfo(
                                base.Id, this.GetType().Name, Machine.GetQualifiedStateName(this.CurrentState)));
                            defaultHandling = true;
                        }
                        else
                        {
                            this.IsRunning = false;
                            break;
                        }
                    }
                }

                // Notifies the runtime for a new event to handle. This is only used
                // during bug-finding and operation bounding, because the runtime has
                // to schedule a machine when a new operation is dequeued.
                if (dequeued)
                {
                    base.Runtime.NotifyDequeuedEvent(this, nextEventInfo);
                }
                else
                {
                    base.Runtime.NotifyHandleRaisedEvent(this, nextEventInfo);
                }

                // Assigns the received event.
                this.ReceivedEvent = nextEventInfo.Event;

                // Handles next event.
                this.HandleEvent(nextEventInfo.Event);

                // If the default event was handled, then notify the runtime.
                // This is only used during bug-finding, because the runtime
                // has to schedule a machine between default handlers.
                if (defaultHandling)
                {
                    base.Runtime.NotifyDefaultHandlerFired();
                }
            }
        }

        /// <summary>
        /// Sets the operation priority of the queue to the specified operation id.
        /// </summary>
        /// <param name="opid">OperationId</param>
        internal void SetQueueOperationPriority(int opid)
        {
            lock (this.Inbox)
            {
                // Iterate through the events in the inbox, and give priority
                // to the first event with the specified operation id.
                for (int idx = 0; idx < this.Inbox.Count; idx++)
                {
                    if (idx == 0 && this.Inbox[idx].OperationId == opid)
                    {
                        break;
                    }
                    else if (this.Inbox[idx].OperationId == opid)
                    {
                        var prioritizedEvent = this.Inbox[idx];

                        var sameSenderConflict = false;
                        for (int prev = 0; prev < idx; prev++)
                        {
                            if (this.Inbox[prev].OriginInfo.SenderMachineId.Equals(
                                prioritizedEvent.OriginInfo.SenderMachineId))
                            {
                                sameSenderConflict = true;
                            }
                        }

                        if (!sameSenderConflict)
                        {
                            this.Inbox.RemoveAt(idx);
                            this.Inbox.Insert(0, prioritizedEvent);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the specified operation id is pending
        /// execution by the machine.
        /// </summary>
        /// <param name="opid">OperationId</param>
        /// <returns>Boolean</returns>
        internal override bool IsOperationPending(int opid)
        {
            foreach (var e in this.Inbox)
            {
                if (e.OperationId == opid)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the cached state of this machine.
        /// </summary>
        /// <returns>Hash value</returns>
        internal int GetCachedState()
        {
            unchecked
            {
                var hash = 19;

                hash = hash + 31 * this.GetType().GetHashCode();
                hash = hash + 31 * base.Id.Value.GetHashCode();
                hash = hash + 31 * this.IsRunning.GetHashCode();
                hash = hash + 31 * this.IsHalted.GetHashCode();

                hash = hash + 31 * this.ProgramCounter;

                foreach (var state in this.StateStack)
                {
                    hash = hash * 31 + state.GetType().GetHashCode();
                }

                foreach (var e in this.Inbox)
                {
                    hash = hash * 31 + e.EventType.GetHashCode();
                }

                return hash;
            }
        }

        #endregion

        #region event handling methods

        /// <summary>
        /// Gets the next available event. It gives priority to raised events,
        /// else deqeues from the inbox. Returns false if the next event was
        /// not dequeued. It returns a null event if no event is available.
        /// </summary>
        /// <param name="nextEventInfo">EventInfo</param>
        /// <returns>Boolean</returns>
        private bool GetNextEvent(out EventInfo nextEventInfo)
        {
            bool dequeued = false;
            nextEventInfo = null;

            // Raised events have priority.
            if (this.RaisedEvent != null)
            {
                nextEventInfo = this.RaisedEvent;

                this.RaisedEvent = null;

                // Checks if the raised event is ignored.
                if (this.IsIgnored(nextEventInfo.EventType))
                {
                    nextEventInfo = null;
                }
            }
            // If there is no raised event, then dequeue.
            else if (this.Inbox.Count > 0)
            {
                // Iterates through the events in the inbox.
                for (int idx = 0; idx < this.Inbox.Count; idx++)
                {
                    // Removes an ignored event.
                    if (this.Inbox[idx].EventType.IsGenericType)
                    {
                        var genericTypeDefinition = this.Inbox[idx].EventType.GetGenericTypeDefinition();
                        var genericIgnoredTypes = this.CurrentActionHandlerMap
                            .Where(tup => tup.Value is IgnoreAction)
                            .Select(tup => tup.Key)
                            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition().Equals(
                                genericTypeDefinition.GetGenericTypeDefinition()));
                        if (genericIgnoredTypes.Count() > 0)
                        {
                            this.Inbox.RemoveAt(idx);
                            idx--;
                            continue;
                        }
                    }

                    if (this.IsIgnored(this.Inbox[idx].EventType))
                    {
                        this.Inbox.RemoveAt(idx);
                        idx--;
                        continue;
                    }

                    // Dequeues the first event that is not deferred.
                    if (!this.IsDeferred(this.Inbox[idx].EventType))
                    {
                        nextEventInfo = this.Inbox[idx];
                        this.Inbox.RemoveAt(idx);
                        dequeued = true;
                        break;
                    }
                }
            }

            return dequeued;
        }

        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="e">Event to handle</param>
        private void HandleEvent(Event e)
        {
            base.CurrentActionCalledRGP = false;

            while (true)
            {
                if (this.CurrentState == null)
                {
                    // If the stack of states is empty and the event
                    // is halt, then terminate the machine.
                    if (e.GetType().Equals(typeof(Halt)))
                    {
                        lock (this.Inbox)
                        {
                            this.IsHalted = true;
                            this.CleanUpResources();
                            base.Runtime.NotifyHalted(this);
                        }

                        return;
                    }

                    // If the event cannot be handled then report an error and exit.
                    this.Assert(false, $"Machine '{base.Id}' received event " +
                        $"'{e.GetType().FullName}' that cannot be handled.");
                }

                // Checks if the event is a goto state event.
                if (e.GetType() == typeof(GotoStateEvent))
                {
                    Type targetState = (e as GotoStateEvent).State;
                    this.GotoState(targetState, null);
                }
                // Checks if the event can trigger a goto state transition.
                else if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    var transition = this.GotoTransitions[e.GetType()];
                    this.GotoState(transition.TargetState, transition.Lambda);
                }
                else if (this.GotoTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    var transition = this.GotoTransitions[typeof(WildCardEvent)];
                    this.GotoState(transition.TargetState, transition.Lambda);
                }
                // Checks if the event can trigger a push state transition.
                else if (this.PushTransitions.ContainsKey(e.GetType()))
                {
                    Type targetState = this.PushTransitions[e.GetType()].TargetState;
                    this.PushState(targetState);
                }
                else if (this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    Type targetState = this.PushTransitions[typeof(WildCardEvent)].TargetState;
                    this.PushState(targetState);
                }
                // Checks if the event can trigger an action.
                else if (this.CurrentActionHandlerMap.ContainsKey(e.GetType()) &&
                    this.CurrentActionHandlerMap[e.GetType()] is ActionBinding)
                {
                    var handler = this.CurrentActionHandlerMap[e.GetType()] as ActionBinding;
                    this.Do(handler.Name);
                }
                else if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent))
                    && this.CurrentActionHandlerMap[typeof(WildCardEvent)] is ActionBinding)
                {
                    var handler = this.CurrentActionHandlerMap[typeof(WildCardEvent)] as ActionBinding;
                    this.Do(handler.Name);
                }
                // If the current state cannot handle the event.
                else
                {
                    // The machine performs the on exit action of the current state.
                    this.ExecuteCurrentStateOnExit(null);
                    if (this.IsHalted)
                    {
                        return;
                    }

                    this.DoStatePop();

                    if (this.CurrentState == null)
                    {
                        base.Runtime.Log($"<PopLog> Machine '{base.Id}' " +
                            $"popped with unhandled event '{e.GetType().FullName}'.");
                    }
                    else
                    {
                        base.Runtime.Log($"<PopLog> Machine '{base.Id}' popped " +
                            $"with unhandled event '{e.GetType().FullName}' and " +
                            $"reentered state '{this.CurrentStateName}.");
                    }

                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Waits for an event to arrive.
        /// </summary>
        private void WaitOnEvent()
        {
            lock (this.Inbox)
            {
                // Iterate through the events in the inbox.
                for (int idx = 0; idx < this.Inbox.Count; idx++)
                {
                    // Dequeues the first event that the machine waits
                    // to receive, if there is one in the inbox.
                    EventWaitHandler eventWaitHandler = this.EventWaitHandlers.FirstOrDefault(
                        val => val.EventType == this.Inbox[idx].EventType &&
                               val.Predicate(this.Inbox[idx].Event));
                    if (eventWaitHandler != null)
                    {
                        this.EventViaReceiveStatement = this.Inbox[idx].Event;
                        this.EventWaitHandlers.Clear();
                        this.Inbox.RemoveAt(idx);
                        break;
                    }
                }

                if (this.EventViaReceiveStatement == null)
                {
                    this.IsWaitingToReceive = true;
                }
            }

            if (this.IsWaitingToReceive)
            {
                string events = "";

                lock (this.Inbox)
                {
                    foreach (var ewh in this.EventWaitHandlers)
                    {
                        events += " '" + ewh.EventType.FullName + "'";
                    }
                }

                base.Runtime.NotifyWaitEvents(this, events);
                this.IsWaitingToReceive = false;
            }
        }

        /// <summary>
        /// Checks if the machine ignores the specified event.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean</returns>
        private bool IsIgnored(Type e)
        {
            // If a transition is defined, then the event is not ignored.
            if (this.GotoTransitions.ContainsKey(e) || this.PushTransitions.ContainsKey(e) ||
                this.GotoTransitions.ContainsKey(typeof(WildCardEvent)) ||
                this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
            {
                return false;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(e))
            {
                return this.CurrentActionHandlerMap[e] is IgnoreAction;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentActionHandlerMap[typeof(WildCardEvent)] is IgnoreAction)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the machine defers the specified event.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean</returns>
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
        /// <returns></returns>
        private bool HasDefaultHandler()
        {
            return this.CurrentActionHandlerMap.ContainsKey(typeof(Default)) ||
                this.GotoTransitions.ContainsKey(typeof(Default)) ||
                this.PushTransitions.ContainsKey(typeof(Default));
        }

        /// <summary>
        /// Performs a goto transition to the specified state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="onExitActionName">Action name</param>
        private void GotoState(Type s, string onExitActionName)
        {
            // The machine performs the on exit action of the current state.
            this.ExecuteCurrentStateOnExit(onExitActionName);
            if (this.IsHalted)
            {
                return;
            }

            this.DoStatePop();
            
            var nextState = StateMap[this.GetType()].First(val
                => val.GetType().Equals(s));

            // The machine transitions to the new state.
            this.DoStatePush(nextState);

            // The machine performs the on entry action of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a push transition to the specified state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        private void PushState(Type s)
        {
            base.Runtime.Log($"<PushLog> Machine '{base.Id}' pushed.");

            var nextState = StateMap[this.GetType()].First(val => val.GetType().Equals(s));
            this.DoStatePush(nextState);

            // The machine performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Invokes an action.
        /// </summary>
        /// <param name="actionName">Action name</param>
        [DebuggerStepThrough]
        private void Do(string actionName)
        {
            MethodInfo action = this.ActionMap[actionName];
            base.Runtime.NotifyInvokedAction(this, action, this.ReceivedEvent);

            try
            {
                action.Invoke(this, null);
            }
            catch (Exception ex)
            {
                this.IsHalted = true;

                Exception innerException = ex;
                while (innerException is TargetInvocationException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is AggregateException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is OperationCanceledException)
                {
                    IO.Debug("<Exception> OperationCanceledException was " +
                        $"thrown from Machine '{base.Id}'.");
                }
                else if (innerException is TaskSchedulerException)
                {
                    IO.Debug("<Exception> TaskSchedulerException was thrown from " +
                        $"thrown from Machine '{base.Id}'.");
                }
                else
                {
                    if (Debugger.IsAttached ||
                        base.Runtime.Configuration.ThrowInternalExceptions)
                    {
                        throw innerException;
                    }
                    
                    // Handles generic exception.
                    this.ReportGenericAssertion(innerException);
                }
            }
        }

        #endregion

        #region state transitioning methods

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
        /// Configures the state transitions of the machine
        /// when a state is pushed on to the stack.
        /// </summary>
        /// <param name="state">State that is to be pushed on to the top of the stack</param>
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
        /// Executes the on entry function of the current state.
        /// </summary>
        [DebuggerStepThrough]
        private void ExecuteCurrentStateOnEntry()
        {
            base.Runtime.NotifyEnteredState(this);
            
            MethodInfo entryAction = null;
            if (this.StateStack.Peek().EntryAction != null)
            {
                entryAction = this.ActionMap[this.StateStack.Peek().EntryAction];
            }

            try
            {
                // Invokes the entry action of the new state,
                // if there is one available.
                if (entryAction != null)
                {
                    base.Runtime.NotifyInvokedAction(this, entryAction, this.ReceivedEvent);
                    entryAction.Invoke(this, null);
                }
            }
            catch (Exception ex)
            {
                this.IsHalted = true;

                Exception innerException = ex;
                while (innerException is TargetInvocationException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is AggregateException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is OperationCanceledException)
                {
                    IO.Debug("<Exception> OperationCanceledException was " +
                        $"thrown from Machine '{base.Id}'.");
                }
                else if (innerException is TaskSchedulerException)
                {
                    IO.Debug("<Exception> TaskSchedulerException was thrown from " +
                        $"thrown from Machine '{base.Id}'.");
                }
                else
                {
                    if (Debugger.IsAttached ||
                        base.Runtime.Configuration.ThrowInternalExceptions)
                    {
                        throw innerException;
                    }

                    // Handles generic exception.
                    this.ReportGenericAssertion(innerException);
                }
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        /// <param name="eventHandlerExitActionName">Action name</param>
        [DebuggerStepThrough]
        private void ExecuteCurrentStateOnExit(string eventHandlerExitActionName)
        {
            base.Runtime.NotifyExitedState(this);

            MethodInfo exitAction = null;
            if (this.StateStack.Peek().ExitAction != null)
            {
                exitAction = this.ActionMap[this.StateStack.Peek().ExitAction];
            }

            try
            {
                base.InsideOnExit = true;

                // Invokes the exit action of the current state,
                // if there is one available.
                if (exitAction != null)
                {
                    base.Runtime.NotifyInvokedAction(this, exitAction, this.ReceivedEvent);
                    exitAction.Invoke(this, null);
                }

                // Invokes the exit action of the event handler,
                // if there is one available.
                if (eventHandlerExitActionName != null)
                {
                    MethodInfo eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                    base.Runtime.NotifyInvokedAction(this, eventHandlerExitAction, this.ReceivedEvent);
                    eventHandlerExitAction.Invoke(this, null);
                }
            }
            catch (Exception ex)
            {
                this.IsHalted = true;

                Exception innerException = ex;
                while (innerException is TargetInvocationException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is AggregateException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is OperationCanceledException)
                {
                    IO.Debug("<Exception> OperationCanceledException was " +
                        $"thrown from Machine '{base.Id}'.");
                }
                else if (innerException is TaskSchedulerException)
                {
                    IO.Debug("<Exception> TaskSchedulerException was thrown from " +
                        $"thrown from Machine '{base.Id}'.");
                }
                else
                {
                    if (Debugger.IsAttached ||
                        base.Runtime.Configuration.ThrowInternalExceptions)
                    {
                        throw innerException;
                    }

                    // Handles generic exception.
                    this.ReportGenericAssertion(innerException);
                }
            }
            finally
            {
                base.InsideOnExit = false;
            }
        }

        /// <summary>
        /// Initializes information about the states of the machine.
        /// </summary>
        internal void InitializeStateInformation()
        {
            Type machineType = this.GetType();

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

                    state.InitializeState();
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
                        MachineActionMap[machineType].Add(state.EntryAction,
                            this.GetActionWithName(state.EntryAction));
                    }

                    if (state.ExitAction != null &&
                        !MachineActionMap[machineType].ContainsKey(state.ExitAction))
                    {
                        MachineActionMap[machineType].Add(state.ExitAction,
                            this.GetActionWithName(state.ExitAction));
                    }

                    foreach (var transition in state.GotoTransitions)
                    {
                        if (transition.Value.Lambda != null &&
                            !MachineActionMap[machineType].ContainsKey(transition.Value.Lambda))
                        {
                            MachineActionMap[machineType].Add(transition.Value.Lambda,
                                this.GetActionWithName(transition.Value.Lambda));
                        }
                    }

                    foreach (var action in state.ActionBindings)
                    {
                        if (!MachineActionMap[machineType].ContainsKey(action.Value.Name))
                        {
                            MachineActionMap[machineType].Add(action.Value.Name,
                                this.GetActionWithName(action.Value.Name));
                        }
                    }
                }
            }
            
            // Populates the map of actions for this machine instance.
            foreach (var kvp in MachineActionMap[machineType])
            {
                this.ActionMap.Add(kvp.Key, kvp.Value);
            }

            var initialStates = StateMap[machineType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, $"Machine '{base.Id}' must declare a start state.");
            this.Assert(initialStates.Count == 1, $"Machine '{base.Id}' " +
                "can not declare more than one start states.");

            this.DoStatePush(initialStates.Single());

            this.AssertStateValidity();
        }

        /// <summary>
        /// Processes a type, looking for machine states.
        /// </summary>
        /// <param name="type">Type</param>
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
                        this.Assert(t.IsSubclassOf(typeof(StateGroup)) ||
                            t.IsSubclassOf(typeof(MachineState)), $"'{t.Name}' " +
                            $"is neither a group of states nor a state.");
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <returns>MethodInfo</returns>
        private MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo method = null;
            Type machineType = this.GetType();

            do
            {
                method = machineType.GetMethod(actionName, BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.FlattenHierarchy);
                machineType = machineType.BaseType;
            }
            while (method == null && machineType != typeof(Machine));

            this.Assert(method != null, "Cannot detect action declaration '{0}' " +
                "in machine '{1}'.", actionName, this.GetType().Name);
            this.Assert(method.GetParameters().Length == 0, "Action '{0}' in machine " +
                "'{1}' must have 0 formal parameters.", method.Name, this.GetType().Name);
            this.Assert(method.ReturnType == typeof(void), "Action '{0}' in machine " +
                "'{1}' must have 'void' return type.", method.Name, this.GetType().Name);
            
            return method;
        }

        #endregion

        #region code coverage methods

        /// <summary>
        /// Returns the set of all states in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the machine</returns>
        internal HashSet<string> GetAllStates()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()),
                $"Machine '{base.Id}' hasn't populated its states yet.");

            var allStates = new HashSet<string>();
            foreach (var state in StateMap[this.GetType()])
            {
                allStates.Add(GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all (states, registered event) pairs in the machine</returns>
        internal HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()),
                $"Machine '{base.Id}' hasn't populated its states yet.");

            var pairs = new HashSet<Tuple<string, string>>();
            foreach (var state in StateMap[this.GetType()])
            {
                foreach (var binding in state.ActionBindings)
                {
                    pairs.Add(Tuple.Create(GetQualifiedStateName(state.GetType()), binding.Key.Name));
                }

                foreach (var transition in state.GotoTransitions)
                {
                    pairs.Add(Tuple.Create(GetQualifiedStateName(state.GetType()), transition.Key.Name));
                }

                foreach (var pushtransition in state.PushTransitions)
                {
                    pairs.Add(Tuple.Create(GetQualifiedStateName(state.GetType()), pushtransition.Key.Name));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Returns the qualified (MachineGroup) name of a MachineState
        /// </summary>
        /// <param name="state">State</param>
        /// <returns>Qualified state name</returns>
        internal static string GetQualifiedStateName(Type state)
        {
            var name = state.Name;

            while(state.DeclaringType != null)
            {
                if (!state.DeclaringType.IsSubclassOf(typeof(StateGroup))) break;
                name = string.Format("{0}.{1}", state.DeclaringType.Name, name);
                state = state.DeclaringType;
            }

            return name;
        }

        #endregion

            #region error checking

            /// <summary>
            /// Check machine for state related errors.
            /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(StateTypeMap[this.GetType()].Count > 0, $"Machine '{base.Id}' " +
                "must have one or more states.");
            this.Assert(this.StateStack.Peek() != null, $"Machine '{base.Id}' " +
                "must not have a null current state.");
        }

        /// <summary>
        /// Reports the generic assertion and raises a generic
        /// runtime assertion error.
        /// </summary>
        /// <param name="ex">Exception</param>
        private void ReportGenericAssertion(Exception ex)
        {
            this.Assert(false, $"Exception '{ex.GetType()}' was thrown " +
                $"in machine '{base.Id}', '{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        #endregion

        #region cleanup methods

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
        /// Cleans up resources at machine termination.
        /// </summary>
        private void CleanUpResources()
        {
            this.Inbox.Clear();
            this.EventWaitHandlers.Clear();
            this.ReceivedEvent = null;
        }

        #endregion
    }
}
