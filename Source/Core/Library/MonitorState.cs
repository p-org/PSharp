//-----------------------------------------------------------------------
// <copyright file="MonitorState.cs" company="Microsoft">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state of a monitor.
    /// </summary>
    public abstract class MonitorState
    {
        #region fields

        /// <summary>
        /// Handle to the monitor that owns this state instance.
        /// </summary>
        protected internal Monitor Monitor;

        /// <summary>
        /// Handle to the latest received event type.
        /// If there was no event received yet the returned
        /// value is null.
        /// </summary>
        protected Type Trigger
        {
            get { return this.Monitor.Trigger; }
        }

        /// <summary>
        /// Handle to the payload of the last received event.
        /// If the last received event does not have a payload,
        /// a null value is returned.
        /// </summary>
        protected Object Payload
        {
            get { return this.Monitor.Payload; }
        }

        /// <summary>
        /// Dictionary containing all the goto state transitions.
        /// </summary>
        internal GotoStateTransitions GotoTransitions;

        /// <summary>
        /// Dictionary containing all the action bindings.
        /// </summary>
        internal ActionBindings ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        #endregion

        #region P# internal methods

        /// <summary>
        /// Initializes the state.
        /// </summary>
        internal void InitializeState()
        {
            this.IgnoredEvents = this.DefineIgnoredEvents();
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
        /// Checks if the state can handle the given event type. An event
        /// can be handled if it is deferred, or leads to a transition or
        /// action binding. Ignored events have been removed.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean value</returns>
        internal bool CanHandleEvent(Type e)
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
        internal bool HasDefaultHandler()
        {
            if (this.GotoTransitions.ContainsKey(typeof(Default)) ||
                this.ActionBindings.ContainsKey(typeof(Default)))
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
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        protected void Raise(Event e)
        {
            this.Monitor.Raise(e);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        protected void Assert(bool predicate)
        {
            this.Monitor.Assert(predicate);
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
            this.Monitor.Assert(predicate, s, args);
        }

        #endregion
    }
}
