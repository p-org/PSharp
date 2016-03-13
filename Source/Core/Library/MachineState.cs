//-----------------------------------------------------------------------
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
using System.Reflection;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state of a P# machine.
    /// </summary>
    public abstract class MachineState
    {
        #region fields

        /// <summary>
        /// Handle to the machine that owns this state instance.
        /// </summary>
        private Machine Machine;

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
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Machine, method);
                this.EntryAction = action;
            }

            if (exitAttribute != null)
            {
                var method = this.Machine.GetType().GetMethod(exitAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
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
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
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
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                this.Machine.Assert(method.GetParameters().Length == 0, "Action '{0}' in machine '{1}' " +
                    "must have 0 formal parameters.", method.Name, this.Machine.GetType().Name);
                this.Machine.Assert(method.ReturnType == typeof(void), "Action '{0}' in machine '{1}' " +
                    "must have 'void' return type.", method.Name, this.Machine.GetType().Name);

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
        }

        #endregion
    }
}
