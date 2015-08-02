﻿//-----------------------------------------------------------------------
// <copyright file="MachineState.cs" company="Microsoft">
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
    /// Abstract class representing a state of a state machine.
    /// </summary>
    public abstract class MachineState
    {
        #region fields

        /// <summary>
        /// The entry action, if the OnEntry is not overriden.
        /// </summary>
        internal Action EntryAction;

        /// <summary>
        /// The exit action, if the OnExit is not overriden.
        /// </summary>
        internal Action ExitAction;

        /// <summary>
        /// Dictionary containing all the goto state transitions.
        /// </summary>
        internal GotoStateTransitions GotoTransitions;

        /// <summary>
        /// Dictionary containing all the push state transitions.
        /// </summary>
        internal PushStateTransitions PushTransitions;

        /// <summary>
        /// Dictionary containing all the action bindings.
        /// </summary>
        internal ActionBindings ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        /// <summary>
        /// Set of deferred event types.
        /// </summary>
        internal HashSet<Type> DeferredEvents;

        /// <summary>
        /// Unique machine ID.
        /// </summary>
        protected MachineId Id
        {
            get { return this.Machine.Id; }
        }

        /// <summary>
        /// Handle to the machine that owns this state instance.
        /// </summary>
        protected Machine Machine { get; private set; }

        /// <summary>
        /// Gets the latest received payload. If no payload has been
        /// received this will return null.
        /// </summary>
        protected Type Trigger
        {
            get { return this.Machine.Trigger; }
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

        #endregion

        #region P# internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        protected MachineState() { }

        /// <summary>
        /// Initializes the state.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void InitializeState(Machine machine)
        {
            this.Machine = machine;

            this.GotoTransitions = new GotoStateTransitions();
            this.PushTransitions = new PushStateTransitions();
            this.ActionBindings = new ActionBindings();

            this.IgnoredEvents = new HashSet<Type>();
            this.DeferredEvents = new HashSet<Type>();

            var entryAttribute = this.GetType().GetCustomAttribute(typeof(OnEntry), false) as OnEntry;
            var exitAttribute = this.GetType().GetCustomAttribute(typeof(OnExit), false) as OnExit;

            if (entryAttribute != null)
            {
                var method = this.Machine.GetType().GetMethod(entryAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Machine, method);
                this.EntryAction = action;
            }

            if (exitAttribute != null)
            {
                var method = this.Machine.GetType().GetMethod(exitAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Machine, method);
                this.ExitAction = action;
            }

            var gotoAttributes = this.GetType().GetCustomAttributes(typeof(OnEventGotoState), false)
                as OnEventGotoState[];
            var pushAttributes = this.GetType().GetCustomAttributes(typeof(OnEventPushState), false)
                as OnEventPushState[];
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
                    var method = this.Machine.GetType().GetMethod(attr.Action,
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Machine, method);
                    this.GotoTransitions.Add(attr.Event, attr.State, action);
                }
            }

            foreach (var attr in pushAttributes)
            {
                this.PushTransitions.Add(attr.Event, attr.State);
            }

            foreach (var attr in doAttributes)
            {
                var method = this.Machine.GetType().GetMethod(attr.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Machine, method);
                this.ActionBindings.Add(attr.Event, action);
            }

            var ignoreEventsAttribute = this.GetType().GetCustomAttribute(typeof(IgnoreEvents), false) as IgnoreEvents;
            var deferEventsAttribute = this.GetType().GetCustomAttribute(typeof(DeferEvents), false) as DeferEvents;

            if (ignoreEventsAttribute != null)
            {
                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
            }

            if (deferEventsAttribute != null)
            {
                this.DeferredEvents.UnionWith(deferEventsAttribute.Events);
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
        /// Creates a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        protected internal MachineId CreateMachine(Type type, params Object[] payload)
        {
            return this.Machine.CreateMachine(type, payload);
        }

        /// <summary>
        /// Creates a new remote machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        protected internal MachineId CreateRemoteMachine(Type type, params Object[] payload)
        {
            return this.Machine.CreateRemoteMachine(type, payload);
        }

        /// <summary>
        /// Creates a new monitor of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="payload">Optional payload</param>
        protected internal void CreateMonitor(Type type, params Object[] payload)
        {
            this.Machine.CreateMonitor(type, payload);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="mid">Machine id</param>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        protected void Send(MachineId mid, Event e, params Object[] payload)
        {
            this.Machine.Send(mid, e, payload);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        protected internal void Monitor<T>(Event e, params Object[] payload)
        {
            this.Machine.Monitor<T>(e, payload);
        }

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        protected void Raise(Event e, params Object[] payload)
        {
            this.Machine.Raise(e, payload);
        }

        /// <summary>
        /// Pops the current state from the push state stack.
        /// </summary>
        protected void Pop()
        {
            this.Machine.Pop();
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected internal bool Nondet()
        {
            return this.Machine.Nondet();
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected internal bool FairNondet()
        {
            return this.Machine.FairNondet();
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
            return this.Machine.FairNondet(uniqueId);
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
    }
}
