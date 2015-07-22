//-----------------------------------------------------------------------
// <copyright file="Machine.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state machine.
    /// </summary>
    public abstract class Machine
    {
        #region static fields

        /// <summary>
        /// Dispatcher used to communicate with the P# runtime.
        /// </summary>
        internal static IDispatcher Dispatcher;

        #endregion

        #region fields

        /// <summary>
        /// Unique machine id.
        /// </summary>
        public readonly MachineId Id;

        /// <summary>
        /// Set of all possible states types.
        /// </summary>
        private HashSet<Type> StateTypes;

        /// <summary>
        /// Set of all available states.
        /// </summary>
        private HashSet<MachineState> States;

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
        /// Inbox of the state machine. Incoming events are queued here.
        /// Events are dequeued to be processed.
        /// </summary>
        private List<Event> Inbox;

        /// <summary>
        /// Gets the raised event. If no event has been raised this will
        /// return null.
        /// </summary>
        private Event RaisedEvent;

        /// <summary>
        /// Gets the latest received event type. If no event has been
        /// received this will return null.
        /// </summary>
        protected internal Type Trigger { get; private set; }

        /// <summary>
        /// Gets the latest received payload. If no payload has been
        /// received this will return null.
        /// </summary>
        protected internal Object Payload { get; private set; }

        #endregion

        #region machine constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Machine()
        {
            this.Id = new MachineId(this.GetType());

            this.Inbox = new List<Event>();
            this.StateStack = new Stack<MachineState>();

            this.IsRunning = true;
            this.IsHalted = false;

            this.InitializeStateInformation();
            this.AssertStateValidity();
        }

        #endregion

        #region P# API methods

        /// <summary>
        /// Creates a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        protected internal MachineId CreateMachine(Type type, params Object[] payload)
        {
            return Machine.Dispatcher.TryCreateMachine(type, payload);
        }

        /// <summary>
        /// Creates a new remote machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        protected internal MachineId CreateRemoteMachine(Type type, params Object[] payload)
        {
            return Machine.Dispatcher.TryCreateRemoteMachine(type, payload);
        }

        /// <summary>
        /// Creates a new model of the given real machine with an optional payload.
        /// </summary>
        /// <param name="model">Type of the model machine</param>
        /// <param name="real">Type of the real machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        protected internal MachineId CreateModelForMachine(Type model, Type real, params Object[] payload)
        {
            if (Configuration.RunDynamicAnalysis)
            {
                return Machine.Dispatcher.TryCreateMachine(model, payload);
            }
            else
            {
                return Machine.Dispatcher.TryCreateMachine(real, payload);
            }
        }

        /// <summary>
        /// Tries to create a new local or remote model of the given real machine
        /// with an optional payload.
        /// </summary>
        /// <param name="model">Type of the model machine</param>
        /// <param name="real">Type of the real machine</param>
        /// <param name="isRemote">Create in another node</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        protected internal MachineId CreateModelForMachine(Type model, Type real, bool isRemote, params Object[] payload)
        {
            if (Configuration.RunDynamicAnalysis)
            {
                return Machine.Dispatcher.TryCreateMachine(model, isRemote, payload);
            }
            else
            {
                return Machine.Dispatcher.TryCreateMachine(real, isRemote, payload);
            }
        }

        /// <summary>
        /// Creates a new monitor of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="payload">Optional payload</param>
        protected internal void CreateMonitor(Type type, params Object[] payload)
        {
            Machine.Dispatcher.TryCreateMonitor(type, payload);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="m">Machine id</param>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        protected internal void Send(MachineId mid, Event e, params Object[] payload)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Machine '{0}' is sending a null event.", this.GetType().Name);

            e.AssignPayload(payload);
            Output.Debug(DebugType.Runtime, "<SendLog> Machine '{0}({1})' sent event '{2}' " +
                "to '{3}({4})'.", this, this.Id.MVal, e.GetType(), mid.Type, mid.MVal);
            Machine.Dispatcher.Send(mid, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        protected internal void Monitor<T>(Event e, params Object[] payload)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Machine '{0}' is sending a null event.", this.GetType().Name);

            e.AssignPayload(payload);
            Machine.Dispatcher.Monitor<T>(e);
        }

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        protected internal void Raise(Event e, params Object[] payload)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Machine '{0}' is raising a null event.", this.GetType().Name);

            e.AssignPayload(payload);
            Output.Debug(DebugType.Runtime, "<RaiseLog> Machine '{0}({1})' raised " +
                "event '{2}'.", this, this.Id.MVal, e);
            this.RaisedEvent = e;
        }

        /// <summary>
        /// Pops the current state from the state stack.
        /// </summary>
        protected internal void Pop()
        {
            // The machine performs the on exit action of the current state.
            this.ExecuteCurrentStateOnExit(null);
            if (this.IsHalted)
            {
                return;
            }

            this.StateStack.Pop();
            
            if (this.StateStack.Count == 0)
            {
                Output.Debug(DebugType.Runtime, "<PopLog> Machine '{0}({1})' popped.",
                    this, this.Id.MVal);
            }
            else
            {
                Output.Debug(DebugType.Runtime, "<PopLog> Machine '{0}({1})' popped and " +
                    "reentered state '{2}'.", this, this.Id.MVal, this.StateStack.Peek());
                this.ConfigureStateTransitions(this.StateStack.Peek());
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected internal bool Nondet()
        {
            return Machine.Dispatcher.Nondet();
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected internal bool FairNondet()
        {
            return Machine.Dispatcher.Nondet();
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal bool FairNondet(int uniqueId)
        {
            var havocId = this.GetType().Name + "_" + this.StateStack.Peek().
                GetType().Name + "_" + uniqueId;
            return Machine.Dispatcher.FairNondet(havocId);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        protected internal void Assert(bool predicate)
        {
            Machine.Dispatcher.Assert(predicate);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        protected internal void Assert(bool predicate, string s, params object[] args)
        {
            Machine.Dispatcher.Assert(predicate, s, args);
        }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Initializes the machine with an optional payload
        /// </summary>
        /// <param name="payload">Optional payload</param>
        internal void AssignInitialPayload(params Object[] payload)
        {
            object initPayload = null;
            if (payload.Length > 1)
            {
                initPayload = payload;
            }
            else if (payload.Length == 1)
            {
                initPayload = payload[0];
            }

            this.Payload = initPayload;
        }

        /// <summary>
        /// Transitions to the start state and executes the
        /// entry action, if there is any.
        /// </summary>
        internal void GotoStartState()
        {
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Enqueues an event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="runHandler">Run the handler</param>
        internal void Enqueue(Event e, ref bool runHandler)
        {
            lock (this.Inbox)
            {
                if (this.IsHalted)
                {
                    return;
                }

                Output.Debug(DebugType.Runtime, "<EnqueueLog> Machine '{0}({1})' enqueued " +
                        "event < ____{2} >.", this, this.Id.MVal, e.GetType());

                this.Inbox.Add(e);

                if (e.Assert >= 0)
                {
                    var eventCount = this.Inbox.Count(val => val.GetType().Equals(e.GetType()));
                    this.Assert(eventCount <= e.Assert, "There are more than {0} instances of '{1}' " +
                        "in the input queue of machine '{1}'", e.Assert, e.GetType().Name, this);
                }

                if (e.Assume >= 0)
                {
                    var eventCount = this.Inbox.Count(val => val.GetType().Equals(e.GetType()));
                    this.Assert(eventCount <= e.Assume, "There are more than {0} instances of '{1}' " +
                        "in the input queue of machine '{2}'", e.Assume, e.GetType().Name, this);
                }

                if (!this.IsRunning)
                {
                    this.IsRunning = true;
                    runHandler = true;
                }
            }
        }

        /// <summary>
        /// Runs the event handler. The handlers terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        internal void RunEventHandler()
        {
            if (this.IsHalted)
            {
                return;
            }

            Event nextEvent = null;
            while (!this.IsHalted)
            {
                var defaultHandling = false;
                lock (this.Inbox)
                {
                    nextEvent = this.GetNextEvent();

                    // Check if next event to process is null.
                    if (nextEvent == null)
                    {
                        if (this.HasDefaultHandler())
                        {
                            nextEvent = new Default();
                            defaultHandling = true;
                        }
                        else
                        {
                            this.IsRunning = false;
                            break;
                        }
                    }
                }

                // Assign trigger and payload.
                this.Trigger = nextEvent.GetType();
                this.Payload = nextEvent.Payload;

                // Handle next event.
                this.HandleEvent(nextEvent);

                if (defaultHandling)
                {
                    Machine.Dispatcher.NotifyDefaultHandler();
                }
            }
        }

        #endregion

        #region private machine methods

        /// <summary>
        /// Gets the next available event. It gives priority to raised events,
        /// else deqeues from the inbox. Returns null if no event is available.
        /// </summary>
        /// <returns>Next event</returns>
        private Event GetNextEvent()
        {
            Event nextEvent = null;

            // Raised events have priority.
            if (this.RaisedEvent != null)
            {
                nextEvent = this.RaisedEvent;
                this.RaisedEvent = null;

                // Checks if the raised event is ignored.
                if (this.IgnoredEvents.Contains(nextEvent.GetType()))
                {
                    Output.Log("<IgnoreLog> Machine '{0}({1})' ignored event < ____{2} >.",
                        this.GetType().Name, this.Id.MVal, nextEvent.GetType());
                    nextEvent = null;
                }
            }
            // If there is no raised event, then dequeue.
            else if (this.Inbox.Count > 0)
            {
                // Iterate through the events in the inbox.
                for (int idx = 0; idx < this.Inbox.Count; idx++)
                {
                    // Remove an ignored event.
                    if (this.IgnoredEvents.Contains(this.Inbox[idx].GetType()))
                    {
                        Output.Log("<IgnoreLog> Machine '{0}({1})' ignored event < ____{2} >.",
                            this.GetType().Name, this.Id.MVal, this.Inbox[idx].GetType());
                        this.Inbox.RemoveAt(idx);
                        idx--;
                        continue;
                    }

                    // Dequeue the first event that is not handled by the state,
                    // or is not deferred.
                    if (!this.CanHandleEvent(this.Inbox[idx].GetType()) ||
                        !this.DeferredEvents.Contains(this.Inbox[idx].GetType()))
                    {
                        nextEvent = this.Inbox[idx];
                        Output.Debug(DebugType.Runtime, "<DequeueLog> Machine '{0}({1})' dequeued " +
                            "event < ____{2} >.", this.GetType().Name, this.Id.MVal, nextEvent.GetType());

                        this.Inbox.RemoveAt(idx);
                        break;
                    }
                }
            }

            return nextEvent;
        }

        /// <summary>
        /// Handles the given event.
        /// </summary>
        /// <param name="e">Event to handle</param>
        private void HandleEvent(Event e)
        {
            while (true)
            {
                if (this.StateStack.Count == 0)
                {
                    // If the stack of states is empty and the event
                    // is halt, then terminate the machine.
                    if (e.GetType().Equals(typeof(Halt)))
                    {
                        lock (this.Inbox)
                        {
                            Output.Debug(DebugType.Runtime, "<HaltLog> Machine " +
                                "'{0}({1})' halted.", this.GetType().Name, this.Id.MVal);
                            this.IsHalted = true;
                            this.CleanUpResources();
                        }
                        
                        return;
                    }

                    // If the event cannot be handled then report an error and exit.
                    this.Assert(false, "Machine '{0}' received event '{1}' that cannot be handled.",
                        this.GetType().Name, e.GetType().Name);
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

                    if (this.StateStack.Count == 0)
                    {
                        Output.Debug(DebugType.Runtime, "<PopLog> Machine '{0}({1})' popped with " +
                            "unhandled event '{2}'.", this, this.Id.MVal, e.GetType().Name);
                    }
                    else
                    {
                        Output.Debug(DebugType.Runtime, "<PopLog> Machine '{0}({1})' popped with " +
                            "unhandled event '{2}' and reentered state '{3}.",
                            this, this.Id.MVal, e.GetType().Name, this.StateStack.Peek());
                        this.ConfigureStateTransitions(this.StateStack.Peek());
                    }
                    
                    continue;
                }

                // Checks if the event can trigger a goto state transition.
                if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    var transition = this.GotoTransitions[e.GetType()];
                    Type targetState = transition.Item1;
                    Action onExitAction = transition.Item2;
                    this.GotoState(targetState, onExitAction);
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
                    Action action = this.ActionBindings[e.GetType()];
                    this.Do(action);
                }

                break;
            }
        }

        /// <summary>
        /// Checks if the machine can handle the given event type. An event
        /// can be handled if it is deferred, or leads to a transition or
        /// action binding. Ignored events have been removed.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean value</returns>
        private bool CanHandleEvent(Type e)
        {
            if (this.DeferredEvents.Contains(e) ||
                this.GotoTransitions.ContainsKey(e) ||
                this.PushTransitions.ContainsKey(e) ||
                this.ActionBindings.ContainsKey(e))
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
        /// Initializes information about the states of the machine.
        /// </summary>
        private void InitializeStateInformation()
        {
            this.StateTypes = new HashSet<Type>();
            this.States = new HashSet<MachineState>();

            Type machineType = this.GetType();
            Type initialStateType = null;

            while (machineType != typeof(Machine))
            {
                foreach (var s in machineType.GetNestedTypes(BindingFlags.Instance |
                    BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    if (s.IsClass && s.IsSubclassOf(typeof(MachineState)))
                    {
                        if (s.IsDefined(typeof(Start), false))
                        {
                            this.Assert(initialStateType == null, "Machine '{0}' can not have " +
                                "more than one start states.", this.GetType().Name);
                            initialStateType = s;
                        }

                        this.Assert(s.BaseType == typeof(MachineState), "State '{0}' is " +
                            "not of the correct type.", s.Name);
                        this.StateTypes.Add(s);
                    }
                }

                machineType = machineType.BaseType;
            }

            foreach (var type in this.StateTypes)
            {
                MachineState state = Activator.CreateInstance(type) as MachineState;
                state.InitializeState(this);
                this.States.Add(state);
            }

            var initialState = this.States.First(val => val.GetType().Equals(initialStateType));
            this.ConfigureStateTransitions(initialState);
            this.StateStack.Push(initialState);
        }
        
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
        /// Performs a goto transition to the given state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="onExit">Goto on exit action</param>
        private void GotoState(Type s, Action onExit)
        {
            // The machine performs the on exit action of the current state.
            this.ExecuteCurrentStateOnExit(onExit);
            if (this.IsHalted)
            {
                return;
            }

            this.StateStack.Pop();
            
            var nextState = this.States.First(val => val.GetType().Equals(s));
            this.ConfigureStateTransitions(nextState);

            // The machine transitions to the new state.
            this.StateStack.Push(nextState);

            // The machine performs the on entry action of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a push transition to the given state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        private void PushState(Type s)
        {
            Output.Debug(DebugType.Runtime, "<PushLog> Machine '{0}({1})' pushed.",
                this, this.Id.MVal);

            var nextState = this.States.First(val => val.GetType().Equals(s));
            this.ConfigureStateTransitions(nextState);

            // The machine transitions to the new state.
            this.StateStack.Push(nextState);

            // The machine performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs an action.
        /// </summary>
        /// <param name="a">Action</param>
        private void Do(Action a)
        {
            Output.Debug(DebugType.Runtime, "<ActionLog> Machine '{0}({1})' executed " +
                "action in state '{2}'.", this, this.Id.MVal, this.StateStack.Peek());

            try
            {
                a();
            }
            catch (TaskCanceledException)
            {
                Output.Log("<Exception> TaskCanceledException was thrown from Machine " +
                    "'{0}({1})'.", this, this.Id.MVal);
                this.IsHalted = true;
            }
            catch (Exception ex)
            {
                // Handles generic exception.
                this.ReportGenericAssertion(ex);
            }
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Executes the on entry function of the current state.
        /// </summary>
        private void ExecuteCurrentStateOnEntry()
        {
            Output.Debug(DebugType.Runtime, "<StateLog> Machine '{0}({1})' entering " +
                "state '{2}'.", this, this.Id.MVal, this.StateStack.Peek());

            try
            {
                // Performs the on entry statements of the new state.
                this.StateStack.Peek().ExecuteEntryFunction();
            }
            catch (TaskCanceledException)
            {
                Output.Log("<Exception> TaskCanceledException was thrown from Machine " +
                    "'{0}({1})'.", this, this.Id.MVal);
                this.IsHalted = true;
            }
            catch (Exception ex)
            {
                // Handles generic exception.
                this.ReportGenericAssertion(ex);
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        /// <param name="onExit">Goto on exit action</param>
        private void ExecuteCurrentStateOnExit(Action onExit)
        {
            Output.Debug(DebugType.Runtime, "<ExitLog> Machine '{0}({1})' exiting " +
                "state '{2}'.", this, this.Id.MVal, this.StateStack.Peek());

            try
            {
                // Performs the on exit statements of the current state.
                this.StateStack.Peek().ExecuteExitFunction();
                if (onExit != null)
                {
                    onExit();
                }
            }
            catch (TaskCanceledException)
            {
                Output.Log("<Exception> TaskCanceledException was thrown from Machine " +
                    "'{0}({1})'.", this, this.Id.MVal);
                this.IsHalted = true;
            }
            catch (Exception ex)
            {
                // Handles generic exception.
                this.ReportGenericAssertion(ex);
            }
        }

        #endregion

        #region generic public and override methods

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
                hash = hash + 31 * this.Id.Value.GetHashCode();
                hash = hash + 31 * this.IsRunning.GetHashCode();
                hash = hash + 31 * this.IsHalted.GetHashCode();

                foreach (var state in this.StateStack)
                {
                    hash = hash * 31 + state.GetType().GetHashCode();
                }

                foreach (var e in this.Inbox)
                {
                    hash = hash * 31 + e.GetType().GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified machine is equal
        /// to the current machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <returns>Boolean value</returns>
        public bool Equals(Machine m)
        {
            if (m == null)
            {
                return false;
            }

            return this.Id.MVal == m.Id.MVal;
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean value</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Machine m = obj as Machine;
            if (m == null)
            {
                return false;
            }

            return this.Id.MVal == m.Id.MVal;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.MVal.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.GetType().Name;
        }

        #endregion

        #region error checking

        /// <summary>
        /// Check machine for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(this.StateTypes.Count > 0, "Machine '{0}' must " +
                "have one or more states.", this.GetType().Name);
            this.Assert(this.StateStack.Peek() != null, "Machine '{0}' " +
                "must not have a null current state.", this.GetType().Name);
        }

        /// <summary>
        /// Reports the generic assertion and raises a generic
        /// runtime assertion error.
        /// </summary>
        /// <param name="ex">Exception</param>
        private void ReportGenericAssertion(Exception ex)
        {
            this.Assert(false,
                "Exception '{0}' was thrown in machine '{1}', '{2}':\n" +
                "   {3}\n" +
                "The stack trace is:\n{4}",
                ex.GetType(), this.GetType().Name, ex.Source, ex.Message, ex.StackTrace);
        }

        #endregion

        #region cleanup methods

        /// <summary>
        /// Cleans up resources at machine termination.
        /// </summary>
        private void CleanUpResources()
        {
            this.StateTypes.Clear();
            this.Inbox.Clear();

            this.Trigger = null;
            this.Payload = null;
        }

        #endregion
    }
}
