//-----------------------------------------------------------------------
// <copyright file="State.cs" company="Microsoft">
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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state of a state machine.
    /// </summary>
    public abstract class State
    {
        #region fields

        /// <summary>
        /// Handle to the machine that owns this state instance.
        /// </summary>
        protected internal Machine Machine;

        /// <summary>
        /// Handle to the latest received event type.
        /// If there was no event received yet the returned
        /// value is null.
        /// </summary>
        protected Type Message
        {
            get { return this.Machine.Message; }
        }

        /// <summary>
        /// Handle to the payload of the last received event.
        /// If the last received event does not have a payload,
        /// a null value is returned.
        /// </summary>
        protected Object Payload
        {
            get { return this.Machine.Payload; }
        }

        /// <summary>
        /// Dictionary containing all the goto state transitions.
        /// </summary>
        private GotoStateTransitions GotoTransitions;

        /// <summary>
        /// Dictionary containing all the push state transitions.
        /// </summary>
        private PushStateTransitions PushTransitions;

        /// <summary>
        /// Dictionary containing all the action bindings.
        /// </summary>
        private ActionBindings ActionBindings;

        /// <summary>
        /// Set of deferred event types.
        /// </summary>
        private HashSet<Type> DeferredEvents;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        private HashSet<Type> IgnoredEvents;

        #endregion

        #region P# internal methods

        /// <summary>
        /// Initializes the state.
        /// </summary>
        /// <param name="sst">Goto state transitions</param>
        /// <param name="cst">Push state transitions</param>
        /// <param name="ab">Action bindings</param>
        internal void InitializeState(GotoStateTransitions sst,
            PushStateTransitions cst, ActionBindings ab)
        {
            if (sst == null) this.GotoTransitions = new GotoStateTransitions();
            else this.GotoTransitions = sst;

            if (cst == null) this.PushTransitions = new PushStateTransitions();
            else this.PushTransitions = cst;

            if (ab == null) this.ActionBindings = new ActionBindings();
            else this.ActionBindings = ab;

            this.DeferredEvents = this.DefineDeferredEvents();
            this.IgnoredEvents = this.DefineIgnoredEvents();

            this.DoErrorChecking();
        }

        /// <summary>
        /// Checks if the state contains a goto state transition
        /// triggered from the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool ContainsGotoTransition(Event e)
        {
            return this.GotoTransitions.ContainsKey(e.GetType());
        }

        /// <summary>
        /// Checks if the state contains a push state transition
        /// triggered from the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool ContainsPushTransition(Event e)
        {
            return this.PushTransitions.ContainsKey(e.GetType());
        }

        /// <summary>
        /// Checks if the state contains an action binding
        /// triggered from the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool ContainsActionBinding(Event e)
        {
            return this.ActionBindings.ContainsKey(e.GetType());
        }

        /// <summary>
        /// Checks if the given event is deferred in this state.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool IsDeferred(Event e)
        {
            return this.DeferredEvents.Contains(e.GetType());
        }

        /// <summary>
        /// Checks if the given event is ignored in this state.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool IsIgnored(Event e)
        {
            return this.IgnoredEvents.Contains(e.GetType());
        }

        /// <summary>
        /// Returns the type of the state that is the target of
        /// the goto transition triggered by the given event, and
        /// an optional lambda function which can override the
        /// default OnExit function of the exiting state.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Type of the state</returns>
        internal Tuple<Type, Action> GetGotoTransition(Event e)
        {
            return this.GotoTransitions[e.GetType()];
        }

        /// <summary>
        /// Returns the type of the state that is the target of
        /// the push transition triggered by the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Type of the state</returns>
        internal Type GetPushTransition(Event e)
        {
            return this.PushTransitions[e.GetType()];
        }

        /// <summary>
        /// Returns the action that is triggered by the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Action</returns>
        internal Action GetActionBinding(Event e)
        {
            return this.ActionBindings[e.GetType()];
        }

        /// <summary>
        /// Executes the on entry function.
        /// </summary>
        internal void ExecuteEntryFunction()
        {
            this.OnEntry();
        }

        /// <summary>
        /// Executes the on exit function.
        /// </summary>
        internal void ExecuteExitFunction()
        {
            this.OnExit();
        }

        /// <summary>
        /// Checks if the state can handle the given event. An event
        /// can be handled if it is deferred, or leads to a transition
        /// or action binding. Ignored events have been removed.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool CanHandleEvent(Event e)
        {
            if (this.IsDeferred(e) ||
                this.ContainsGotoTransition(e) ||
                this.ContainsPushTransition(e) ||
                this.ContainsActionBinding(e))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region P# API methods

        /// <summary>
        /// Method to be executed when entering the state.
        /// </summary>
        protected virtual void OnEntry() { }

        /// <summary>
        /// Method to be executed when exiting the state.
        /// </summary>
        protected virtual void OnExit() { }

        /// <summary>
        /// Defines all event types that are deferred by this state.
        /// </summary>
        /// <returns>Set of event types</returns>
        protected virtual HashSet<Type> DefineDeferredEvents()
        {
            return new HashSet<Type>();
        }

        /// <summary>
        /// Defines all event types that are ignored by this state.
        /// </summary>
        /// <returns>Set of event types</returns>
        protected virtual HashSet<Type> DefineIgnoredEvents()
        {
            return new HashSet<Type>();
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <param name="e">Event</param>
        protected void Send(Machine m, Event e)
        {
            this.Machine.Send(m, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        protected internal void Invoke<T>(Event e)
        {
            this.Machine.Invoke<T>(e);
        }

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        protected void Raise(Event e)
        {
            this.Machine.Raise(e);
        }

        /// <summary>
        /// Pops the current state from the push state stack.
        /// </summary>
        protected void Return()
        {
            this.Machine.Return();
        }

        /// <summary>
        /// Stop listening to events and delete the machine.
        /// </summary>
        protected void Delete()
        {
            this.Machine.Delete();
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        protected void Assert(bool predicate)
        {
            this.Machine.Assert(predicate);
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
            this.Machine.Assert(predicate, s, args);
        }

        #endregion

        #region factory methods

        internal static class Factory
        {
            /// <summary>
            /// Create a new state.
            /// </summary>
            /// <param name="s">Type of state</param>
            /// <returns></returns>
            internal static State CreateState(Type s)
            {
                return Activator.CreateInstance(s) as State;
            }
        }

        #endregion

        #region error checking

        /// <summary>
        /// Check machine for errors.
        /// </summary>
        private void DoErrorChecking()
        {
            List<Type> events = new List<Type>();

            events.AddRange(this.GotoTransitions.Keys());
            events.AddRange(this.PushTransitions.Keys());
            events.AddRange(this.ActionBindings.Keys());

            for (int i = 0; i < events.Count; i++)
            {
                for (int j = 0; j < events.Count; j++)
                {
                    if (i == j)
                        continue;
                    Runtime.Assert(events[i] != events[j], "State '{0}' can trigger more " +
                        "than one state transition or action binding when receiving " +
                        "event '{1}'.\n", this.GetType().Name, events[i].Name);
                }
            }
        }

        #endregion
    }
}
