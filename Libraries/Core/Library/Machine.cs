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
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        internal GotoStateTransitions GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current push state transitions.
        /// </summary>
        internal PushStateTransitions PushTransitions;

        /// <summary>
        /// Dictionary containing all the current action bindings.
        /// </summary>
        internal ActionBindings ActionBindings;

        /// <summary>
        /// Map from action names to actions.
        /// </summary>
        private Dictionary<string, MethodInfo> ActionMap;

        /// <summary>
        /// Set of currently ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        /// <summary>
        /// Set of currently deferred event types.
        /// </summary>
        internal HashSet<Type> DeferredEvents;

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
        /// Event obtained via Receive
        /// </summary>
        internal Event EventViaReceive;

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
                base.Id, this.GetType().Name, this.CurrentState.Name));
            base.Runtime.NotifyRaisedEvent(this, this.RaisedEvent, isStarter);
        }

        /// <summary>
        /// Blocks and waits to receive an event of the specified types.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        /// <returns>Event received</returns>
        protected internal Event Receive(params Type[] eventTypes)
        {
            foreach (var type in eventTypes)
            {
                this.EventWaitHandlers.Add(new EventWaitHandler(type));
            }

            this.WaitOnEvent();
            return this.EventViaReceive;
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
            this.EventWaitHandlers.Add(new EventWaitHandler(eventType, predicate));
            this.WaitOnEvent();
            return this.EventViaReceive;
        }

        /// <summary>
        /// Blocks and waits to receive an event of the specified types
        /// that satisfy the specified predicates.
        /// </summary>
        /// <param name="events">Event types and predicates</param>
        /// <returns>Event received</returns>
        protected internal Event Receive(params Tuple<Type, Func<Event, bool>>[] events)
        {
            foreach (var e in events)
            {
                this.EventWaitHandlers.Add(new EventWaitHandler(e.Item1, e.Item2));
            }

            this.WaitOnEvent();
            return this.EventViaReceive;
        }

        /// <summary>
        /// Pops the current state from the state stack.
        /// </summary>
        protected void Pop()
        {
            // The machine performs the on exit action of the current state.
            this.ExecuteCurrentStateOnExit(null);
            if (this.IsHalted)
            {
                return;
            }

            this.StateStack.Pop();
            
            if (this.CurrentState == null)
            {
                base.Runtime.Log($"<PopLog> Machine '{base.Id}' popped.");
            }
            else
            {
                base.Runtime.Log($"<PopLog> Machine '{base.Id}' popped " +
                    $"and reentered state '{this.CurrentStateName}'.");
                this.ConfigureStateTransitions(this.StateStack.Peek());
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected bool Random()
        {
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
            return base.Runtime.GetNondeterministicBooleanChoice(this, maxValue);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected bool FairRandom()
        {
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
                    this.EventViaReceive = eventInfo.Event;
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

                    // Check if next event to process is null.
                    if (nextEventInfo == null)
                    {
                        if (this.HasDefaultHandler())
                        {
                            base.Runtime.Log($"<DefaultLog> Machine '{base.Id}' " +
                                "is executing the default handler in state " +
                                $"'{this.CurrentStateName}'.");

                            nextEventInfo = new EventInfo(new Default(), new EventOriginInfo(
                                base.Id, this.GetType().Name, this.CurrentState.Name));
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
                if (this.IgnoredEvents.Contains(nextEventInfo.EventType))
                {
                    nextEventInfo = null;
                }
            }
            // If there is no raised event, then dequeue.
            else if (this.Inbox.Count > 0)
            {
                // Iterate through the events in the inbox.
                for (int idx = 0; idx < this.Inbox.Count; idx++)
                {
                    // Remove an ignored event.
                    if (this.IgnoredEvents.Contains(this.Inbox[idx].EventType))
                    {
                        this.Inbox.RemoveAt(idx);
                        idx--;
                        continue;
                    }

                    // Dequeue the first event that is not handled by the state,
                    // or is not deferred.
                    if (!this.CanHandleEvent(this.Inbox[idx].EventType) ||
                        !this.DeferredEvents.Contains(this.Inbox[idx].EventType))
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
                            base.Runtime.NotifyHalted(this);
                            this.IsHalted = true;
                            this.CleanUpResources();
                        }
                        
                        return;
                    }

                    // If the event cannot be handled then report an error and exit.
                    this.Assert(false, $"Machine '{base.Id}' received event " +
                        $"'{e.GetType().FullName}' that cannot be handled.");
                }

                // If current state cannot handle the event then pop the state.
                if (!this.CanHandleEvent(e.GetType()))
                {
                    // The machine performs the on exit action of the current state.
                    this.ExecuteCurrentStateOnExit(null);
                    if (this.IsHalted)
                    {
                        return;
                    }

                    this.StateStack.Pop();

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
                        this.ConfigureStateTransitions(this.StateStack.Peek());
                    }
                    
                    continue;
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
                    this.GotoState(transition.Item1, transition.Item2);
                }
                // Checks if the event can trigger a push state transition.
                else if (this.PushTransitions.ContainsKey(e.GetType()))
                {
                    Type targetState = this.PushTransitions[e.GetType()];
                    this.PushState(targetState);
                }
                // Checks if the event can trigger an action.
                else if (this.ActionBindings.ContainsKey(e.GetType()))
                {
                    string actionName = this.ActionBindings[e.GetType()];
                    this.Do(actionName);
                }

                break;
            }
        }

        /// <summary>
        /// Waits for an event to arrive.
        /// </summary>
        private void WaitOnEvent()
        {
            this.EventViaReceive = null;

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
                        this.EventViaReceive = this.Inbox[idx].Event;
                        this.EventWaitHandlers.Clear();
                        this.Inbox.RemoveAt(idx);
                        break;
                    }
                }

                if (this.EventViaReceive == null)
                {
                    this.IsWaitingToReceive = true;
                }
            }

            if (this.IsWaitingToReceive)
            {
                string events = "";
                foreach (var ewh in this.EventWaitHandlers)
                {
                    events += " '" + ewh.EventType.FullName + "'";
                }
                
                base.Runtime.NotifyWaitEvents(this, events);
                this.IsWaitingToReceive = false;
            }
        }

        /// <summary>
        /// Checks if the machine can handle the specified event type. An event
        /// can be handled if it is deferred, or leads to a transition or
        /// action binding.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean</returns>
        private bool CanHandleEvent(Type e)
        {
            if (this.DeferredEvents.Contains(e) ||
                this.GotoTransitions.ContainsKey(e) ||
                this.PushTransitions.ContainsKey(e) ||
                this.ActionBindings.ContainsKey(e) ||
                e == typeof(GotoStateEvent))
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
            if (this.GotoTransitions.ContainsKey(typeof(Default)) ||
                this.PushTransitions.ContainsKey(typeof(Default)) ||
                this.ActionBindings.ContainsKey(typeof(Default)))
            {
                return true;
            }

            return false;
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

            this.StateStack.Pop();
            
            var nextState = StateMap[this.GetType()].First(val
                => val.GetType().Equals(s));
            this.ConfigureStateTransitions(nextState);

            // The machine transitions to the new state.
            this.StateStack.Push(nextState);

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
            this.ConfigureStateTransitions(nextState);

            // The machine transitions to the new state.
            this.StateStack.Push(nextState);

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
            base.Runtime.NotifyInvokedAction(this, action);

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
                    if (Debugger.IsAttached)
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
        /// Configures the state transitions of the machine.
        /// </summary>
        /// <param name="state">State</param>
        private void ConfigureStateTransitions(MachineState state)
        {
            this.GotoTransitions = state.GotoTransitions;
            this.PushTransitions = state.PushTransitions;
            this.ActionBindings = state.ActionBindings;
            this.IgnoredEvents = state.IgnoredEvents;
            this.DeferredEvents = state.DeferredEvents;

            // If the state stack is non-empty, update the data structures
            // with the following logic.
            if (this.StateStack.Count > 0)
            {
                var lowerState = this.StateStack.Peek();

                foreach (var e in lowerState.DeferredEvents)
                {
                    if (!this.CanHandleEvent(e))
                    {
                        this.DeferredEvents.Add(e);
                    }
                }

                foreach (var e in lowerState.IgnoredEvents)
                {
                    if (!this.CanHandleEvent(e))
                    {
                        this.IgnoredEvents.Add(e);
                    }
                }

                foreach (var action in lowerState.ActionBindings)
                {
                    if (!this.CanHandleEvent(action.Key))
                    {
                        this.ActionBindings.Add(action.Key, action.Value);
                    }
                }
            }
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
                    base.Runtime.NotifyInvokedAction(this, entryAction);
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
                    if (Debugger.IsAttached)
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
                // Invokes the exit action of the current state,
                // if there is one available.
                if (exitAction != null)
                {
                    base.Runtime.NotifyInvokedAction(this, exitAction);
                    exitAction.Invoke(this, null);
                }

                // Invokes the exit action of the event handler,
                // if there is one available.
                if (eventHandlerExitActionName != null)
                {
                    MethodInfo eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                    base.Runtime.NotifyInvokedAction(this, eventHandlerExitAction);
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
                    if (Debugger.IsAttached)
                    {
                        throw innerException;
                    }

                    // Handles generic exception.
                    this.ReportGenericAssertion(innerException);
                }
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
                        if (transition.Value.Item2 != null &&
                            !MachineActionMap[machineType].ContainsKey(transition.Value.Item2))
                        {
                            MachineActionMap[machineType].Add(transition.Value.Item2,
                                this.GetActionWithName(transition.Value.Item2));
                        }
                    }

                    foreach (var action in state.ActionBindings)
                    {
                        if (!MachineActionMap[machineType].ContainsKey(action.Value))
                        {
                            MachineActionMap[machineType].Add(action.Value,
                                this.GetActionWithName(action.Value));
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
            
            this.ConfigureStateTransitions(initialStates.Single());
            this.StateStack.Push(initialStates.Single());

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
