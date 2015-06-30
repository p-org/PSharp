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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
        /// Set of all available states.
        /// </summary>
        private HashSet<MonitorState> States;

        /// <summary>
        /// The monitor state.
        /// </summary>
        private MonitorState State;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        private GotoStateTransitions GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current action bindings.
        /// </summary>
        private ActionBindings ActionBindings;

        /// <summary>
        /// Set of currently ignored event types.
        /// </summary>
        private HashSet<Type> IgnoredEvents;

        /// <summary>
        /// Is monitor running.
        /// </summary>
        private bool IsRunning;

        /// <summary>
        /// Is monitor halted.
        /// </summary>
        private bool IsHalted;

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
        /// Constructor.
        /// </summary>
        protected Monitor()
        {
            this.Inbox = new List<Event>();

            this.IsRunning = true;
            this.IsHalted = false;

            this.InitializeStateInformation();
            this.AssertStateValidity();
        }

        #endregion

        #region P# API methods

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
                    if (this.HasDefaultHandler())
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
                    if (this.IgnoredEvents.Contains(this.Inbox[idx].GetType()))
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
                if (!this.CanHandleEvent(e.GetType()))
                {
                    Output.Debug(DebugType.Runtime, "<ExitLog> Monitor '{0}' exiting state '{1}'.",
                        this, this.State);
                    this.State = null;
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
        /// Checks if the state can handle the given event type. An event
        /// can be handled if it is deferred, or leads to a transition or
        /// action binding. Ignored events have been removed.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean value</returns>
        private bool CanHandleEvent(Type e)
        {
            if (this.GotoTransitions.ContainsKey(e) ||
                this.ActionBindings.ContainsKey(e))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the state has a default handler.
        /// </summary>
        /// <returns></returns>
        private bool HasDefaultHandler()
        {
            if (this.GotoTransitions.ContainsKey(typeof(Default)) ||
                this.ActionBindings.ContainsKey(typeof(Default)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Initializes information about the states of the monitor.
        /// </summary>
        private void InitializeStateInformation()
        {
            this.StateTypes = new HashSet<Type>();
            this.States = new HashSet<MonitorState>();

            Type monitorType = this.GetType();
            Type initialStateType = null;

            while (monitorType != typeof(Monitor))
            {
                foreach (var s in monitorType.GetNestedTypes(BindingFlags.Instance |
                    BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    if (s.IsClass && s.IsSubclassOf(typeof(MonitorState)))
                    {
                        if (s.IsDefined(typeof(Start), false))
                        {
                            this.Assert(initialStateType == null, "Monitor ''{0}'' can not have " +
                                "more than one start states.\n", this.GetType().Name);
                            initialStateType = s;
                        }

                        this.Assert(s.BaseType == typeof(MonitorState), "State ''{0}'' is " +
                            "not of the correct type.\n", s.Name);
                        this.StateTypes.Add(s);
                    }
                }

                monitorType = monitorType.BaseType;
            }

            foreach (var type in this.StateTypes)
            {
                MonitorState state = Activator.CreateInstance(type) as MonitorState;
                state.InitializeState(this);
                this.States.Add(state);
            }

            var initialState = this.States.First(val => val.GetType().Equals(initialStateType));
            this.ConfigureStateTransitions(initialState);
            this.State = initialState;
        }

        /// <summary>
        /// Configures the state transitions of the monitor.
        /// </summary>
        /// <param name="state">State</param>
        private void ConfigureStateTransitions(MonitorState state)
        {
            this.GotoTransitions = state.GotoTransitions;
            this.ActionBindings = state.ActionBindings;
            this.IgnoredEvents = state.IgnoredEvents;
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

            var nextState = this.States.First(val => val.GetType().Equals(s));
            this.ConfigureStateTransitions(nextState);
            
            // The monitor transitions to the new state.
            this.State = nextState;

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
            this.Inbox.Clear();

            this.Trigger = null;
            this.Payload = null;
    }

        #endregion
    }
}
