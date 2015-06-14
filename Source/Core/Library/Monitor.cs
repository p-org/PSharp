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
using System.Threading.Tasks;

using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a monitor.
    /// </summary>
    public abstract class Monitor
    {
        #region static fields

        /// <summary>
        /// Dispatcher used to communicate with the P# runtime.
        /// </summary>
        internal static IDispatcher Dispatcher;

        #endregion

        #region fields

        /// <summary>
        /// Set of all possible states types.
        /// </summary>
        private HashSet<Type> StateTypes;

        /// <summary>
        /// The monitor state.
        /// </summary>
        private MonitorState State;

        /// <summary>
        /// Is monitor running.
        /// </summary>
        private bool IsRunning;

        /// <summary>
        /// Is monitor halted.
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

        #region monitor constructors

        /// <summary>
        /// Constructor of the MOnitor class.
        /// </summary>
        protected Monitor()
        {
            this.Inbox = new List<Event>();

            this.IsRunning = true;
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
            Output.Debug(DebugType.Runtime, "<RaiseLog> Monitor '{0}' " +
                "raised event '{1}'.", this, e);
            this.HandleEvent(e);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected internal bool Nondet()
        {
            return Machine.Dispatcher.Nondet();
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        protected internal void Assert(bool predicate)
        {
            Monitor.Dispatcher.Assert(predicate);
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
            Monitor.Dispatcher.Assert(predicate, s, args);
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
        /// Transitions to the initial state and executes the
        /// entry action, if there is any.
        /// </summary>
        internal void GotoInitialState()
        {
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Enqueues an event.
        /// </summary>
        /// <param name="e">Event</param>
        internal void Enqueue(Event e)
        {
            if (this.IsHalted)
            {
                return;
            }

            Output.Debug(DebugType.Runtime, "<EnqueueLog> Monitor '{0}' enqueued event '{1}'.",
                    this, e.GetType());

            this.Inbox.Add(e);
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
            else if (!this.IsRunning)
            {
                this.IsRunning = true;
            }

            Event nextEvent = null;
            while (!this.IsHalted)
            {
                nextEvent = this.GetNextEvent();

                // Check if next event to process is null.
                if (nextEvent == null)
                {
                    if (this.State.HasDefaultHandler())
                    {
                        nextEvent = new Default();
                    }
                    else
                    {
                        this.IsRunning = false;
                        break;
                    }
                }

                // Assign trigger and payload.
                this.Trigger = nextEvent.GetType();
                this.Payload = nextEvent.Payload;

                // Handle next event.
                this.HandleEvent(nextEvent);
            }
        }

        #endregion

        #region private monitor methods

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
                    if (this.State.IgnoredEvents.Contains(this.Inbox[idx].GetType()))
                    {
                        this.Inbox.RemoveAt(idx);
                        idx--;
                        continue;
                    }

                    nextEvent = this.Inbox[idx];
                    Output.Debug(DebugType.Runtime, "<DequeueLog> Monitor '{0}' dequeued event '{1}'.",
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
            while (true)
            {
                if (this.State == null)
                {
                    // If the stack of states is empty and the event
                    // is halt, then terminate the monitor.
                    if (e.GetType().Equals(typeof(Halt)))
                    {
                        Output.Debug(DebugType.Runtime, "<HaltLog> Monitor " +
                                "'{0}' halted.", this);
                        this.IsHalted = true;
                        this.CleanUpResources();
                        return;
                    }

                    // If the event cannot be handled then report an error and exit.
                    this.Assert(false, "Monitor ''{0}'' received event ''{1}'' that cannot be handled.",
                        this.GetType().Name, e.GetType().Name);
                }

                // If current state cannot handle the event then null the state.
                if (!this.State.CanHandleEvent(e.GetType()))
                {
                    Output.Debug(DebugType.Runtime, "<ExitLog> Monitor '{0}' exiting state '{1}'.",
                        this, this.State);
                    this.State = null;
                    continue;
                }

                // Checks if the event can trigger a goto state transition.
                if (this.State.GotoTransitions.ContainsKey(e.GetType()))
                {
                    var transition = this.State.GotoTransitions[e.GetType()];
                    Type targetState = transition.Item1;
                    Action onExitAction = transition.Item2;
                    this.GotoState(targetState, onExitAction);
                }
                // Checks if the event can trigger an action.
                else if (this.State.ActionBindings.ContainsKey(e.GetType()))
                {
                    Action action = this.State.ActionBindings[e.GetType()];
                    this.Do(action);
                }

                break;
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
                            this.Assert(initialState == null, "Monitor ''{0}'' can not have " +
                                "more than one initial states.\n", this.GetType().Name);
                            initialState = s;
                        }

                        this.Assert(s.BaseType == typeof(MonitorState), "State ''{0}'' is " +
                            "not of the correct type.\n", s.Name);
                        this.StateTypes.Add(s);
                    }
                }

                monitorType = monitorType.BaseType;
            }

            this.State = this.InitializeState(initialState);
        }

        /// <summary>
        /// Initializes a state of the given type.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <returns>State</returns>
        private MonitorState InitializeState(Type s)
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

            if (ab == null) state.ActionBindings = new ActionBindings();
            else state.ActionBindings = ab;

            return state;
        }

        /// <summary>
        /// Performs a goto transition to the given state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="onExit">Goto on exit action</param>
        private void GotoState(Type s, Action onExit)
        {
            // The monitor performs the on exit statements of the current state.
            this.ExecuteCurrentStateOnExit(onExit);
            if (this.IsHalted)
            {
                return;
            }
            
            // The monitor transitions to the new state.
            this.State = this.InitializeState(s);
            // The monitor performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs an action.
        /// </summary>
        /// <param name="a">Action</param>
        private void Do(Action a)
        {
            Output.Debug(DebugType.Runtime, "<ActionLog> Monitor '{0}' executed " +
                "action in state '{1}'.", this, this.State);

            try
            {
                a();
            }
            catch (TaskCanceledException)
            {
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
            Output.Debug(DebugType.Runtime, "<StateLog> Monitor '{0}' entering " +
                "state '{1}'.", this, this.State);

            try
            {
                // Performs the on entry statements of the new state.
                this.State.ExecuteEntryFunction();
            }
            catch (TaskCanceledException)
            {
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
            Output.Debug(DebugType.Runtime, "<ExitLog> Monitor '{0}' exiting " +
                "state '{1}'.", this, this.State);

            try
            {
                // Performs the on exit statements of the current state.
                this.State.ExecuteExitFunction();
                if (onExit != null)
                {
                    onExit();
                }
            }
            catch (TaskCanceledException)
            {
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
            this.Assert(this.StateTypes.Count > 0, "Monitor ''{0}'' must " +
                "have one or more states.\n", this.GetType().Name);
            this.Assert(this.State != null, "Monitor ''{0}'' " +
                "must not have a null current state.\n", this.GetType().Name);
        }

        /// <summary>
        /// Reports the generic assertion and raises a generic
        /// runtime assertion error.
        /// </summary>
        /// <param name="ex">Exception</param>
        private void ReportGenericAssertion(Exception ex)
        {
            this.Assert(false, "Exception '{0}' was thrown in monitor '{1}', '{2}':\n   {3}\n" +
                "The stack trace is:\n{4}",
                ex.GetType(), this.GetType().Name, ex.Source, ex.Message, ex.StackTrace);
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
