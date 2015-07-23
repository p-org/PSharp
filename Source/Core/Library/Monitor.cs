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
using System.ComponentModel;
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

        #region monitor constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Monitor()
        {
            this.InitializeStateInformation();
            this.AssertStateValidity();
        }

        #endregion

        #region P# API methods

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        protected internal void Raise(Event e, params Object[] payload)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Monitor '{0}' is raising a null event.", this.GetType().Name);

            e.AssignPayload(payload);
            Output.Debug(DebugType.Runtime, "<RaiseLog> Monitor '{0}' " +
                "raised event '{1}'.", this, e);
            this.HandleEvent(e);
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
            return Machine.Dispatcher.FairNondet();
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal bool FairNondet(int uniqueId)
        {
            var havocId = this.GetType().Name + "_" + this.State.GetType().Name + "_" + uniqueId;
            return Machine.Dispatcher.FairNondet(havocId);
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
        /// Notifies the monitor to handle the received event.
        /// </summary>
        /// <param name="e">Event</param>
        internal void MonitorEvent(Event e)
        {
            Output.Debug(DebugType.Runtime, "<EnqueueLog> Monitor '{0}' is processing " +
                "event '{1}'.", this, e.GetType());
            this.HandleEvent(e);
        }

        /// <summary>
        /// Returns true if the monitor is in a hot state.
        /// </summary>
        /// <returns>Boolean value</returns>
        internal bool IsInHotState()
        {
            return this.State.IsHot;
        }

        /// <summary>
        /// Returns true if the monitor is in a hot state. Also outputs
        /// the name of the current state.
        /// </summary>
        /// <param name="stateName">State name</param>
        /// <returns>Boolean value</returns>
        internal bool IsInHotState(out string stateName)
        {
            stateName = this.State.GetType().Name;
            return this.State.IsHot;
        }

        /// <summary>
        /// Returns true if the monitor is in a cold state.
        /// </summary>
        /// <returns>Boolean value</returns>
        internal bool IsInColdState()
        {
            return this.State.IsCold;
        }

        /// <summary>
        /// Returns true if the monitor is in a cold state. Also outputs
        /// the name of the current state.
        /// </summary>
        /// <param name="stateName">State name</param>
        /// <returns>Boolean value</returns>
        internal bool IsInColdState(out string stateName)
        {
            stateName = this.State.GetType().Name;
            return this.State.IsCold;
        }

        #endregion

        #region private monitor methods

        /// <summary>
        /// Handles the given event.
        /// </summary>
        /// <param name="e">Event to handle</param>
        private void HandleEvent(Event e)
        {
            // Do not process an ignored event.
            if (this.IgnoredEvents.Contains(e.GetType()))
            {
                Output.Log("<IgnoreLog> Monitor '{0}' ignored event '{1}'.",
                    this.GetType().Name, e.GetType());
                return;
            }

            // Assign trigger and payload.
            this.Trigger = e.GetType();
            this.Payload = e.Payload;

            while (true)
            {
                if (this.State == null)
                {
                    // If the event cannot be handled then report an error and exit.
                    this.Assert(false, "Monitor '{0}' received event '{1}' that cannot be handled.",
                        this.GetType().Name, e.GetType().Name);
                }

                // If current state cannot handle the event then null the state.
                if (!this.CanHandleEvent(e.GetType()))
                {
                    Output.Debug(DebugType.Runtime, "<ExitLog> Monitor '{0}' exiting state '{1}'.",
                        this, this.State.GetType().Name);
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
                            this.Assert(initialStateType == null, "Monitor '{0}' can not have " +
                                "more than one start states.\n", this.GetType().Name);
                            initialStateType = s;
                        }

                        this.Assert(s.BaseType == typeof(MonitorState), "State '{0}' is " +
                            "not of the correct type.\n", s.Name);
                        this.StateTypes.Add(s);
                    }
                }

                monitorType = monitorType.BaseType;
            }

            foreach (var type in this.StateTypes)
            {
                var isHot = false;
                var isCold = false;
                
                var hotAttribute = type.GetCustomAttribute(typeof(Hot), false) as Hot;
                if (hotAttribute != null)
                {
                    isHot = true;
                }

                var coldAttribute = type.GetCustomAttribute(typeof(Cold), false) as Cold;
                if (coldAttribute != null)
                {
                    isCold = true;
                }

                MonitorState state = Activator.CreateInstance(type) as MonitorState;
                state.InitializeState(this, isHot, isCold);

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
                "action in state '{1}'.", this, this.State.GetType().Name);

            try
            {
                a();
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                {
                    throw ex;
                }

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
            var liveness = "";
            if (this.IsInHotState())
            {
                liveness = "'hot' ";
            }
            else if (this.IsInColdState())
            {
                liveness = "'cold' ";
            }

            Output.Debug(DebugType.Runtime, "<StateLog> Monitor '{0}' entering " +
                liveness + "state '{1}'.", this, this.State.GetType().Name);

            try
            {
                // Performs the on entry statements of the new state.
                this.State.ExecuteEntryFunction();
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                {
                    throw ex;
                }

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
                "state '{1}'.", this, this.State.GetType().Name);

            try
            {
                // Performs the on exit statements of the current state.
                this.State.ExecuteExitFunction();
                if (onExit != null)
                {
                    onExit();
                }
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                {
                    throw ex;
                }

                // Handles generic exception.
                this.ReportGenericAssertion(ex);
            }
        }

        #endregion

        #region generic public and override methods

        /// <summary>
        /// Returns the hashed state of this monitor.
        /// </summary>
        /// <returns></returns>
        protected virtual int GetHashedState()
        {
            return 0;
        }

        /// <summary>
        /// Returns the cached state of this monitor.
        /// </summary>
        /// <returns>Hash value</returns>
        internal int GetCachedState()
        {
            unchecked
            {
                var hash = 19;

                hash = hash + 31 * this.GetType().GetHashCode();
                hash = hash + 31 * this.State.GetType().GetHashCode();

                // Adds the user-defined hashed state.
                hash = hash + 31 * this.GetHashedState(); 

                return hash;
            }
        }

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
            this.Assert(this.StateTypes.Count > 0, "Monitor '{0}' must " +
                "have one or more states.\n", this.GetType().Name);
            this.Assert(this.State != null, "Monitor '{0}' " +
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
    }
}
