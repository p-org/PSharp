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
using System.ComponentModel;
using System.Reflection;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state of a P# monitor.
    /// </summary>
    public abstract class MonitorState
    {
        #region fields

        /// <summary>
        /// The entry action, if the OnEntry is not overriden.
        /// </summary>
        private Action EntryAction;

        /// <summary>
        /// The exit action, if the OnExit is not overriden.
        /// </summary>
        private Action ExitAction;

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

        /// <summary>
        /// Returns true if this is a hot state.
        /// </summary>
        internal bool IsHot { get; private set; }

        /// <summary>
        /// Returns true if this is a cold state.
        /// </summary>
        internal bool IsCold { get; private set; }

        /// <summary>
        /// Handle to the monitor that owns this state instance.
        /// </summary>
        protected Monitor Monitor { get; private set; }

        /// <summary>
        /// Gets the latest received payload. If no payload has been
        /// received this will return null.
        /// </summary>
        protected Type Trigger
        {
            get { return this.Monitor.Trigger; }
        }

        /// <summary>
        /// Gets the latest received payload. If no payload has been
        /// received this will return null.
        /// </summary>
        protected Object Payload
        {
            get { return this.Monitor.Payload; }
        }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        protected MonitorState() { }

        /// <summary>
        /// Initializes the state.
        /// <param name="monitor">Monitor</param>
        /// <param name="isHot">Is hot</param>
        /// <param name="isCold">Is cold</param>
        internal void InitializeState(Monitor monitor, bool isHot, bool isCold)
        {
            this.Monitor = monitor;

            this.IsHot = isHot;
            this.IsCold = isCold;

            this.GotoTransitions = new GotoStateTransitions();
            this.ActionBindings = new ActionBindings();

            this.IgnoredEvents = new HashSet<Type>();

            var entryAttribute = this.GetType().GetCustomAttribute(typeof(OnEntry), false) as OnEntry;
            var exitAttribute = this.GetType().GetCustomAttribute(typeof(OnExit), false) as OnExit;

            if (entryAttribute != null)
            {
                var method = this.Monitor.GetType().GetMethod(entryAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Monitor, method);
                this.EntryAction = action;
            }

            if (exitAttribute != null)
            {
                var method = this.Monitor.GetType().GetMethod(exitAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Monitor, method);
                this.ExitAction = action;
            }

            var gotoAttributes = this.GetType().GetCustomAttributes(typeof(OnEventGotoState), false)
                as OnEventGotoState[];
            var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoAction), false)
                as OnEventDoAction[];

            foreach (var attr in gotoAttributes)
            {
                if (attr.Action == null)
                {
                    this.GotoTransitions.Add(attr.Event, attr.State);
                }
                else
                {
                    var method = this.Monitor.GetType().GetMethod(attr.Action,
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Monitor, method);
                    this.GotoTransitions.Add(attr.Event, attr.State, action);
                }
            }

            foreach (var attr in doAttributes)
            {
                var method = this.Monitor.GetType().GetMethod(attr.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Monitor, method);
                this.ActionBindings.Add(attr.Event, action);
            }

            var ignoreEventsAttribute = this.GetType().GetCustomAttribute(typeof(IgnoreEvents), false) as IgnoreEvents;

            if (ignoreEventsAttribute != null)
            {
                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
            }
        }

        /// <summary>
        /// Executes the on entry function.
        /// </summary>
        internal void ExecuteEntryFunction()
        {
            if (this.EntryAction != null)
            {
                this.EntryAction();
            }
            else
            {
                this.OnEntry();
            }
        }

        /// <summary>
        /// Executes the on exit function.
        /// </summary>
        internal void ExecuteExitFunction()
        {
            if (this.ExitAction != null)
            {
                this.ExitAction();
            }
            else
            {
                this.OnExit();
            }
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
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        protected void Raise(Event e, params Object[] payload)
        {
            this.Monitor.Raise(e, payload);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected internal bool Nondet()
        {
            return this.Monitor.Nondet();
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected internal bool FairNondet()
        {
            return this.Monitor.FairNondet();
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
            return this.Monitor.FairNondet(uniqueId);
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
