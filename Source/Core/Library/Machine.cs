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
        /// Monotonically increasing machine ID counter.
        /// </summary>
        private static int IdCounter = 0;

        /// <summary>
        /// Dispatcher used to communicate with the P# runtime.
        /// </summary>
        internal static IDispatcher Dispatcher;

        #endregion

        #region fields

        /// <summary>
        /// Unique machine ID.
        /// </summary>
        internal readonly int Id;

        /// <summary>
        /// Lock used by the machine.
        /// </summary>
        private Object Lock;

        /// <summary>
        /// Set of all possible states types.
        /// </summary>
        private HashSet<Type> StateTypes;

        /// <summary>
        /// A stack of machine states. The state on the top of
        /// the stack represents the current state.
        /// </summary>
        private Stack<MachineState> StateStack;

        /// <summary>
        /// The status of the machine.
        /// </summary>
        private MachineStatus Status;

        /// <summary>
        /// Collection of all possible goto state transitions.
        /// </summary>
        private Dictionary<Type, GotoStateTransitions> GotoTransitions;

        /// <summary>
        /// Collection of all possible push state transitions.
        /// </summary>
        private Dictionary<Type, PushStateTransitions> PushTransitions;

        /// <summary>
        /// Collection of all possible action bindings.
        /// </summary>
        private Dictionary<Type, ActionBindings> ActionBindings;

        /// <summary>
        /// Inbox of the state machine. Incoming events are queued here.
        /// Events are dequeued to be processed.
        /// </summary>
        private List<Event> Inbox;

        /// <summary>
        /// A raised event. Null if there is no event raised.
        /// </summary>
        private Event RaisedEvent;

        /// <summary>
        /// Handle to the latest received event type.
        /// If there was no event received yet the returned
        /// value is null.
        /// </summary>
        protected internal Type Trigger;

        /// <summary>
        /// Handle to the payload of the last received event.
        /// If the last received event does not have a payload,
        /// a null value is returned.
        /// </summary>
        protected internal Object Payload;

        #endregion

        #region machine constructors

        /// <summary>
        /// Constructor of the Machine class.
        /// </summary>
        protected Machine()
        {
            this.Id = Machine.IdCounter++;
            this.Lock = new Object();

            this.Inbox = new List<Event>();

            this.StateStack = new Stack<MachineState>();
            this.Status = MachineStatus.None;

            this.GotoTransitions = this.DefineGotoStateTransitions();
            this.PushTransitions = this.DefinePushStateTransitions();
            this.ActionBindings = this.DefineActionBindings();

            this.InitializeStateInformation();
            this.AssertStateValidity();
        }

        #endregion

        #region P# API methods

        /// <summary>
        /// Defines all possible goto state transitions for each state.
        /// It must return a dictionary where a key represents
        /// a state and a value represents the state's transitions.
        /// </summary>
        /// <returns>Dictionary<Type, StateTransitions></returns>
        protected virtual Dictionary<Type, GotoStateTransitions> DefineGotoStateTransitions()
        {
            return new Dictionary<Type, GotoStateTransitions>();
        }

        /// <summary>
        /// Defines all possible push state transitions for each state.
        /// It must return a dictionary where a key represents
        /// a state and a value represents the state's transitions.
        /// </summary>
        /// <returns>Dictionary<Type, StateTransitions></returns>
        protected virtual Dictionary<Type, PushStateTransitions> DefinePushStateTransitions()
        {
            return new Dictionary<Type, PushStateTransitions>();
        }

        /// <summary>
        /// Defines all possible action bindings for each state.
        /// It must return a dictionary where a key represents
        /// a state and a value represents the state's action bindings.
        /// </summary>
        /// <returns>Dictionary<Type, ActionBindings></returns>
        protected virtual Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            return new Dictionary<Type, ActionBindings>();
        }

        /// <summary>
        /// Creates a new machine of type T with an optional payload.
        /// </summary>
        /// <typeparam name="T">Type of machine</typeparam>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine</returns>
        protected internal T Create<T>(params Object[] payload)
        {
            return Machine.Dispatcher.TryCreateNewMachineInstance<T>(this, payload);
        }

        /// <summary>
        /// Creates a new monitor of type T with an optional payload.
        /// </summary>
        /// <typeparam name="T">Type of monitor</typeparam>
        /// <param name="payload">Optional payload</param>
        protected internal void CreateMonitor<T>(params Object[] payload)
        {
            Machine.Dispatcher.TryCreateNewMonitorInstance<T>(payload);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <param name="e">Event</param>
        protected internal void Send(Machine m, Event e)
        {
            Output.Debug(DebugType.Runtime, "<SendLog> Machine {0}({1}) sent event {2} " +
                "to machine {3}({4}).", this, this.Id, e.GetType(), m, m.Id);
            Machine.Dispatcher.Send(this, m, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        protected internal void Monitor<T>(Event e)
        {
            Machine.Dispatcher.Monitor<T>(e);
        }

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        protected internal void Raise(Event e)
        {
            Output.Debug(DebugType.Runtime, "<RaiseLog> Machine {0}({1}) raised " +
                "event {2}.", this, this.Id, e);
            this.RaisedEvent = e;
        }

        /// <summary>
        /// Pops the current state from the state stack.
        /// </summary>
        protected internal void Pop()
        {
            Output.Debug(DebugType.Runtime, "<ExitLog> Machine {0}({1}) exiting state {2}.",
                this, this.Id, this.StateStack.Peek());

            this.StateStack.Pop();
            
            if (this.StateStack.Count == 0)
            {
                Output.Debug(DebugType.Runtime, "<PopLog> Machine {0}({1}) popped.",
                    this, this.Id);
            }
            else
            {
                Output.Debug(DebugType.Runtime, "<PopLog> Machine {0}({1}) popped and " +
                    "reentered state {2}.", this, this.Id, this.StateStack.Peek());
            }
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

        #region factory methods

        /// <summary>
        /// Factory class for creating machines internally.
        /// </summary>
        internal static class Factory
        {
            /// <summary>
            /// Creates a new machine of type T with an optional payload.
            /// </summary>
            /// <param name="m">Type of machine</param>
            /// <param name="payload">Optional payload</param>
            /// <returns>Machine</returns>
            internal static Machine Create(Type m, params Object[] payload)
            {
                return Machine.Dispatcher.TryCreateNewMachineInstance(m, payload);
            }
        }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Runs the machine.
        /// </summary>
        internal void Run()
        {
            lock (this.Lock)
            {
                if (this.Status == MachineStatus.Halted)
                {
                    return;
                }
                else if (this.Status == MachineStatus.None)
                {
                    this.Status = MachineStatus.Running;
                    this.ExecuteCurrentStateOnEntry();
                }
                else if (this.Status == MachineStatus.Waiting)
                {
                    this.Status = MachineStatus.Running;
                }

                this.RunEventHandler();

                if (this.Status != MachineStatus.Halted)
                {
                    this.Status = MachineStatus.Waiting;
                }
            }
        }

        /// <summary>
        /// Enqueues an event.
        /// </summary>
        /// <param name="e">Event</param>
        internal void Enqueue(Event e)
        {
            lock (this.Inbox)
            {
                if (this.Status != MachineStatus.Halted)
                {
                    Output.Debug(DebugType.Runtime, "<EnqueueLog> Machine {0}({1}) enqueued event {2}.",
                        this, this.Id, e.GetType());
                    this.Inbox.Add(e);

                    if (e.Assert >= 0)
                    {
                        var eventCount = this.Inbox.Count(val => val.GetType().Equals(e.GetType()));
                        this.Assert(eventCount <= e.Assert, "There are more than {0} instances of {1} " +
                            "in the input queue of machine '{1}'", e.Assert, e.GetType().Name, this);
                    }
                }
            }
        }

        /// <summary>
        /// Assigns the optional initial payload.
        /// </summary>
        /// <param name="payload">Optional payload</param>
        internal void AssignInitialPayload(params Object[] payload)
        {
            if (payload.Length == 0)
            {
                this.Payload = null;
            }
            else if (payload.Length == 1)
            {
                this.Payload = payload[0];
            }
            else
            {
                this.Payload = payload;
            }
        }

        /// <summary>
        /// Resets the machine ID counter.
        /// </summary>
        internal static void ResetMachineIDCounter()
        {
            Machine.IdCounter = 0;
        }

        #endregion

        #region private machine methods

        /// <summary>
        /// Runs the event handler. The handlers terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        private void RunEventHandler()
        {
            Event nextEvent = null;
            while (this.Status == MachineStatus.Running)
            {
                lock (this.Inbox)
                {
                    nextEvent = this.GetNextEvent();
                }

                // Check if next event to process is null.
                if (nextEvent == null)
                {
                    if (this.StateStack.Peek().HasDefaultHandler())
                    {
                        nextEvent = new Default();
                    }
                    else
                    {
                        break;
                    }
                }

                // Assign trigger and payload.
                this.Trigger = nextEvent.GetType();
                this.Payload = nextEvent.Payload;
                
                // Handle next event.
                this.HandleEvent(nextEvent);

                // Reset trigger and payload.
                this.Trigger = null;
                this.Payload = null;
            }
        }

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
            }
            // If there is no raised event, then dequeue.
            else if (this.Inbox.Count > 0)
            {
                // Iterate through the events in the inbox.
                for (int idx = 0; idx < this.Inbox.Count; idx++)
                {
                    // Remove an ignored event.
                    if (this.StateStack.Peek().IgnoredEvents.Contains(this.Inbox[idx].GetType()))
                    {
                        this.Inbox.RemoveAt(idx);
                        idx--;
                        continue;
                    }

                    // Dequeue the first event that is not handled by the state,
                    // or is not deferred.
                    if (!this.StateStack.Peek().CanHandleEvent(this.Inbox[idx].GetType()) ||
                        !this.StateStack.Peek().DeferredEvents.Contains(this.Inbox[idx].GetType()))
                    {
                        nextEvent = this.Inbox[idx];
                        Output.Debug(DebugType.Runtime, "<DequeueLog> Machine {0}({1}) dequeued event {2}.",
                            this, this.Id, nextEvent.GetType());

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
                                "{0}({1}) halted.", this, this.Id);
                            this.Status = MachineStatus.Halted;
                            this.CleanUpResources();
                        }
                        
                        return;
                    }

                    // If the event cannot be handled then report an error and exit.
                    this.Assert(false, "Machine '{0}' received event '{1}' that cannot be handled.",
                        this.GetType().Name, e.GetType().Name);
                }

                // If current state cannot handle the event then pop the state.
                if (!this.StateStack.Peek().CanHandleEvent(e.GetType()))
                {
                    Output.Debug(DebugType.Runtime, "<ExitLog> Machine {0}({1}) exiting state {2}.",
                        this, this.Id, this.StateStack.Peek());
                    
                    this.StateStack.Pop();
                    if (this.StateStack.Count == 0)
                    {
                        Output.Debug(DebugType.Runtime, "<PopLog> Machine {0}({1}) popped with " +
                            "unhandled event {2}.", this, this.Id, e.GetType().Name);
                    }
                    else
                    {
                        Output.Debug(DebugType.Runtime, "<PopLog> Machine {0}({1}) popped with " +
                            "unhandled event {2} and reentered state {3}.",
                            this, this.Id, e.GetType().Name, this.StateStack.Peek());
                    }
                    
                    continue;
                }
                
                // Checks if the event can trigger a goto state transition.
                if (this.StateStack.Peek().GotoTransitions.ContainsKey(e.GetType()))
                {
                    var transition = this.StateStack.Peek().GotoTransitions[e.GetType()];
                    Type targetState = transition.Item1;
                    Action onExitAction = transition.Item2;
                    this.GotoState(targetState, onExitAction);
                }
                // Checks if the event can trigger a push state transition.
                else if (this.StateStack.Peek().PushTransitions.ContainsKey(e.GetType()))
                {
                    Type targetState = this.StateStack.Peek().PushTransitions[e.GetType()];
                    this.PushState(targetState);
                }
                // Checks if the event can trigger an action.
                else if (this.StateStack.Peek().ActionBindings.ContainsKey(e.GetType()))
                {
                    Action action = this.StateStack.Peek().ActionBindings[e.GetType()];
                    this.Do(action);
                }

                break;
            }
        }

        /// <summary>
        /// Initializes information about the states of the machine.
        /// </summary>
        private void InitializeStateInformation()
        {
            this.StateTypes = new HashSet<Type>();

            Type machineType = this.GetType();
            Type initialState = null;

            while (machineType != typeof(Machine))
            {
                foreach (var s in machineType.GetNestedTypes(BindingFlags.Instance |
                    BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    if (s.IsClass && s.IsSubclassOf(typeof(MachineState)))
                    {
                        if (s.IsDefined(typeof(Initial), false))
                        {
                            this.Assert(initialState == null, "Machine '{0}' can not have " +
                                "more than one initial states.", this.GetType().Name);
                            initialState = s;
                        }

                        this.Assert(s.BaseType == typeof(MachineState), "State '{0}' is " +
                            "not of the correct type.", s.Name);
                        this.StateTypes.Add(s);
                    }
                }

                machineType = machineType.BaseType;
            }

            this.StateStack.Push(this.InitializeState(initialState));
        }

        /// <summary>
        /// Initializes a state of the given type.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="withPushStmt">Was push stmt used?</param>
        /// <returns>State</returns>
        private MachineState InitializeState(Type s, bool withPushStmt = false)
        {
            MachineState state = Activator.CreateInstance(s) as MachineState;
            state.InitializeState();
            state.Machine = this;

            GotoStateTransitions sst = null;
            PushStateTransitions cst = null;
            ActionBindings ab = null;

            this.GotoTransitions.TryGetValue(s, out sst);
            this.PushTransitions.TryGetValue(s, out cst);
            this.ActionBindings.TryGetValue(s, out ab);

            if (sst == null) state.GotoTransitions = new GotoStateTransitions();
            else state.GotoTransitions = sst;

            if (cst == null) state.PushTransitions = new PushStateTransitions();
            else state.PushTransitions = cst;

            if (ab == null) state.ActionBindings = new ActionBindings();
            else state.ActionBindings = ab;

            // If push statement was used do the following logic.
            if (withPushStmt)
            {
                //foreach (var e in Machine.Dispatcher.GetRegisteredEventTypes())
                //{
                //    if (!state.CanHandleEvent(e))
                //    {
                //        state.DeferredEvents.Add(e);
                //    }
                //}
            }
            // If the state stack is non-empty, update the data structures
            // with the following logic.
            else if (this.StateStack.Count > 0)
            {
                var lowerState = this.StateStack.Peek();

                foreach (var e in lowerState.DeferredEvents)
                {
                    if (!state.CanHandleEvent(e))
                    {
                        state.DeferredEvents.Add(e);
                    }
                }

                foreach (var e in lowerState.IgnoredEvents)
                {
                    if (!state.CanHandleEvent(e))
                    {
                        state.IgnoredEvents.Add(e);
                    }
                }

                foreach (var action in lowerState.ActionBindings)
                {
                    if (!state.CanHandleEvent(action.Key))
                    {
                        state.ActionBindings.Add(action.Key, action.Value);
                    }
                }
            }

            return state;
        }

        /// <summary>
        /// Performs a goto transition to the given state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="onExit">Goto on exit action</param>
        private void GotoState(Type s, Action onExit)
        {
            // The machine performs the on exit statements of the current state.
            this.ExecuteCurrentStateOnExit(onExit);
            if (this.Status == MachineStatus.Halted)
            {
                return;
            }

            this.StateStack.Pop();
            // The machine transitions to the new state.
            MachineState nextState = this.InitializeState(s);
            this.StateStack.Push(nextState);
            // The machine performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a push transition to the given state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        private void PushState(Type s)
        {
            Output.Debug(DebugType.Runtime, "<PushLog> Machine {0}({1}) pushed.",
                this, this.Id);

            MachineState nextState = this.InitializeState(s);
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
            Output.Debug(DebugType.Runtime, "<ActionLog> Machine {0}({1}) executed " +
                "action in state {2}.", this, this.Id, this.StateStack.Peek());

            try
            {
                a();
            }
            catch (TaskCanceledException)
            {
                this.Status = MachineStatus.Halted;
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
            Output.Debug(DebugType.Runtime, "<StateLog> Machine {0}({1}) entering " +
                "state {2}.", this, this.Id, this.StateStack.Peek());

            try
            {
                // Performs the on entry statements of the new state.
                this.StateStack.Peek().ExecuteEntryFunction();
            }
            catch (TaskCanceledException)
            {
                this.Status = MachineStatus.Halted;
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
            Output.Debug(DebugType.Runtime, "<ExitLog> Machine {0}({1}) exiting " +
                "state {2}.", this, this.Id, this.StateStack.Peek());

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
                this.Status = MachineStatus.Halted;
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

            return this.Id == m.Id;
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

            return this.Id == m.Id;
        }

        /// <summary>
        /// Hash function.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
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
            this.Assert(false, "Exception '{0}' was thrown in machine '{1}', '{2}':\n   {3}\n" +
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
            this.GotoTransitions.Clear();
            this.PushTransitions.Clear();
            this.ActionBindings.Clear();
            this.Inbox.Clear();

            this.Trigger = null;
            this.Payload = null;
        }

        #endregion
    }
}
