//-----------------------------------------------------------------------
// <copyright file="Monitor.cs" company="Microsoft">
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
using System.Reflection;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a monitor.
    /// </summary>
    public abstract class Monitor
    {
        #region fields

        /// <summary>
        /// Set of all possible states types.
        /// </summary>
        private HashSet<Type> StateTypes;

        /// <summary>
        /// A stack of monitor states. In reality the monitor
        /// can have only one state, as it does not support a
        /// push transition.
        /// </summary>
        private Stack<MonitorState> StateStack;

        /// <summary>
        /// True if monitor has halted.
        /// </summary>
        private bool IsHalted;

        /// <summary>
        /// Collection of all possible goto state transitions.
        /// </summary>
        private Dictionary<Type, GotoStateTransitions> GotoTransitions;

        /// <summary>
        /// Collection of all possible action bindings.
        /// </summary>
        private Dictionary<Type, ActionBindings> ActionBindings;

        /// <summary>
        /// Inbox of the monitor. Incoming events are queued here.
        /// Events are dequeued to be processed.
        /// </summary>
        private List<Event> Inbox;

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

        #region monitor constructors

        /// <summary>
        /// Constructor of the MOnitor class.
        /// </summary>
        protected Monitor()
        {
            this.Inbox = new List<Event>();

            this.StateStack = new Stack<MonitorState>();
            this.IsHalted = false;

            this.GotoTransitions = this.DefineGotoStateTransitions();
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
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        protected internal void Raise(Event e)
        {
            Output.Debug(DebugType.Runtime, "<RaiseLog> Monitor {0} raised event {1}.", this, e);
            this.HandleEvent(e);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        protected internal void Assert(bool predicate)
        {
            Runtime.Assert(predicate);
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
            Runtime.Assert(predicate, s, args);
        }

        #endregion

        #region factory methods

        /// <summary>
        /// Factory class for creating monitors.
        /// </summary>
        public static class Factory
        {
            /// <summary>
            /// Creates a new monitor of type T with an optional payload.
            /// </summary>
            /// <typeparam name="T">Type of monitor</typeparam>
            /// <param name="payload">Optional payload</param>
            public static void CreateMonitor<T>(params Object[] payload)
            {
                Runtime.Assert(typeof(T).IsSubclassOf(typeof(Monitor)), "Type '{0}' is not " +
                    "a subclass of Monitor.\n", typeof(T).Name);
                Runtime.TryCreateNewMonitorInstance<T>(payload);
            }

            /// <summary>
            /// Creates a new monitor of type T with an optional payload.
            /// </summary>
            /// <param name="m">Type of monitor</param>
            /// <param name="payload">Optional payload</param>
            internal static void CreateMonitor(Type m, params Object[] payload)
            {
                Runtime.Assert(m.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a " +
                    "subclass of Monitor.\n", m.Name);
                Runtime.TryCreateNewMonitorInstance(m, payload);
            }
        }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Starts the monitor with an optional payload.
        /// </summary>
        /// /// <param name="payload">Optional payload</param>
        internal void Start(params Object[] payload)
        {
            this.GotoInitialState(payload);
        }

        /// <summary>
        /// Enqueues an event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        internal void Enqueue(Event e, string sender)
        {
            if (!this.IsHalted)
            {
                Output.Debug(DebugType.Runtime, "<EnqueueLog> Monitor {0} enqueued event {1}.",
                    this, e.GetType());

                this.Inbox.Add(e);
                Event nextEvent = this.DequeueNextEvent();
                this.HandleEvent(nextEvent);
            }
        }

        #endregion

        #region private monitor methods

        /// <summary>
        /// Dequeues the next available event. If no event is
        /// available returns null.
        /// </summary>
        /// <returns>Next event</returns>
        private Event DequeueNextEvent()
        {
            Event nextEvent = null;

            if (this.Inbox.Count > 0)
            {
                // Iterate through the event in the inbox.
                for (int idx = 0; idx < this.Inbox.Count; idx++)
                {
                    // Remove an ignored event.
                    if (this.StateStack.Peek().IgnoredEvents.Contains(this.Inbox[idx].GetType()))
                    {
                        this.Inbox.RemoveAt(idx);
                        idx--;
                        continue;
                    }

                    nextEvent = this.Inbox[idx];
                    Output.Debug(DebugType.Runtime, "<DequeueLog> Monitor {0} dequeued event {1}.",
                        this, nextEvent.GetType());

                    this.Inbox.RemoveAt(idx);
                    break;
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
            if (e == null)
            {
                if (this.StateStack.Peek().HasDefaultHandler())
                {
                    e = new Default();
                }
                else
                {
                    return;
                }
            }

            this.Trigger = e.GetType();
            this.Payload = e.Payload;

            while (true)
            {
                if (this.StateStack.Count == 0)
                {
                    // If the stack of states is empty and the event
                    // is halt, then terminate the monitor.
                    if (e.GetType().Equals(typeof(Halt)))
                    {
                        Output.Debug(DebugType.Runtime, "<HaltLog> Monitor {0} halted.", this);
                        this.IsHalted = true;
                        this.CleanUpResources();
                        return;
                    }

                    // If the event cannot be handled then report an error and exit.
                    Runtime.Assert(false, "Monitor '{0}' received event '{1}' that cannot be " +
                        "handled in state '{2}'.\n", this.GetType().Name, e.GetType().Name,
                        this.StateStack.Peek().GetType().Name);
                }

                // If current state cannot handle the event then pop the state.
                if (!this.StateStack.Peek().CanHandleEvent(e.GetType()))
                {
                    this.StateStack.Pop();
                    continue;
                }

                // Checks if the event can trigger a goto state transition.
                if (this.StateStack.Peek().GotoTransitions.ContainsKey(e.GetType()))
                {
                    var transition = this.StateStack.Peek().GotoTransitions[e.GetType()];
                    Type targetState = transition.Item1;
                    Action onExitAction = transition.Item2;
                    this.Goto(targetState, onExitAction);
                }
                // Checks if the event can trigger an action.
                else if (this.StateStack.Peek().ActionBindings.ContainsKey(e.GetType()))
                {
                    Action action = this.StateStack.Peek().ActionBindings[e.GetType()];
                    this.Do(action);
                }

                break;
            }

            if (!this.IsHalted)
            {
                this.Trigger = null;
                this.Payload = null;

                Event nextEvent = this.DequeueNextEvent();
                this.HandleEvent(nextEvent);
            }
        }

        /// <summary>
        /// Initializes information about the states of the monitor.
        /// </summary>
        private void InitializeStateInformation()
        {
            this.StateTypes = new HashSet<Type>();

            Type monitorType = this.GetType();
            Type initialState = null;

            while (monitorType != typeof(Monitor))
            {
                foreach (var s in monitorType.GetNestedTypes(BindingFlags.Instance |
                    BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    if (s.IsClass && s.IsSubclassOf(typeof(MonitorState)))
                    {
                        if (s.IsDefined(typeof(Initial), false))
                        {
                            Runtime.Assert(initialState == null, "Monitor '{0}' can not have " +
                                "more than one initial states.\n", this.GetType().Name);
                            initialState = s;
                        }

                        Runtime.Assert(s.BaseType == typeof(MonitorState), "State '{0}' is " +
                            "not of the correct type.\n", s.Name);
                        this.StateTypes.Add(s);
                    }
                }

                monitorType = monitorType.BaseType;
            }

            this.StateStack.Push(this.InitializeState(initialState));
        }

        /// <summary>
        /// Initializes a state of the given type.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="withPushStmt">Was push stmt used?</param>
        /// <returns>State</returns>
        private MonitorState InitializeState(Type s, bool withPushStmt = false)
        {
            MonitorState state = Activator.CreateInstance(s) as MonitorState;
            state.InitializeState();
            state.Monitor = this;

            GotoStateTransitions sst = null;
            ActionBindings ab = null;

            this.GotoTransitions.TryGetValue(s, out sst);
            this.ActionBindings.TryGetValue(s, out ab);

            if (sst == null) state.GotoTransitions = new GotoStateTransitions();
            else state.GotoTransitions = sst;

            state.PushTransitions = new PushStateTransitions();

            if (ab == null) state.ActionBindings = new ActionBindings();
            else state.ActionBindings = ab;

            return state;
        }

        /// <summary>
        /// Executes the initial state with an optional payload.
        /// </summary>
        /// /// <param name="payload">Optional payload</param>
        private void GotoInitialState(params Object[] payload)
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

            // Performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a goto transition to the given state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="onExit">Goto on exit action</param>
        private void Goto(Type s, Action onExit)
        {
            // The monitor performs the on exit statements of the current state.
            this.ExecuteCurrentStateOnExit(onExit);
            this.StateStack.Pop();
            // The monitor transitions to the new state.
            MonitorState nextState = this.InitializeState(s);
            this.StateStack.Push(nextState);
            // The monitor performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs an action.
        /// </summary>
        /// <param name="a">Action</param>
        private void Do(Action a)
        {
            try
            {
                a();
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
            try
            {
                // Performs the on entry statements of the new state.
                this.StateStack.Peek().ExecuteEntryFunction();
                Output.Debug(DebugType.Runtime, "<StateLog> Monitor {0} entered state {1}.",
                    this, this.StateStack.Peek());
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
            Output.Debug(DebugType.Runtime, "<ExitLog> Monitor {0} exiting state {1}.",
                this, this.StateStack.Peek());

            try
            {
                // Performs the on exit statements of the current state.
                this.StateStack.Peek().ExecuteExitFunction();
                if (onExit != null)
                {
                    onExit();
                }
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
        /// Returns a string that represents the current monitor.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.GetType().Name;
        }

        #endregion

        #region error checking

        /// <summary>
        /// Check monitor for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            Runtime.Assert(this.StateTypes.Count > 0, "Monitor '{0}' must " +
                "have one or more states.\n", this.GetType().Name);
            Runtime.Assert(this.StateStack.Peek() != null, "Monitor '{0}' " +
                "must not have a null current state.\n", this.GetType().Name);
        }

        /// <summary>
        /// Reports the generic assertion and raises a generic
        /// runtime assertion error.
        /// </summary>
        /// <param name="ex">Exception</param>
        private void ReportGenericAssertion(Exception ex)
        {
            Runtime.Assert(false, "Exception '{0}' was thrown in monitor '{1}'. The stack " +
                "trace is:\n{2}\n", ex.GetType(), this.GetType().Name, ex.StackTrace);
        }

        #endregion

        #region cleanup methods

        /// <summary>
        /// Cleans up resources at monitor termination.
        /// </summary>
        private void CleanUpResources()
        {
            this.StateTypes.Clear();
            this.GotoTransitions.Clear();
            this.ActionBindings.Clear();
            this.Inbox.Clear();

            this.Trigger = null;
            this.Payload = null;
    }

        #endregion
    }
}
