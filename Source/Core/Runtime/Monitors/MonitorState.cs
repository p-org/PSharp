// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state of a P# monitor.
    /// </summary>
    public abstract class MonitorState
    {
        /// <summary>
        /// The entry action of the state.
        /// </summary>
        internal string EntryAction { get; private set; }

        /// <summary>
        /// The exit action of the state.
        /// </summary>
        internal string ExitAction { get; private set; }

        /// <summary>
        /// Dictionary containing all the goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the action bindings.
        /// </summary>
        internal Dictionary<Type, ActionBinding> ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        /// <summary>
        /// True if this is the start state.
        /// </summary>
        internal bool IsStart { get; private set; }

        /// <summary>
        /// Returns true if this is a hot state.
        /// </summary>
        internal bool IsHot { get; private set; }

        /// <summary>
        /// Returns true if this is a cold state.
        /// </summary>
        internal bool IsCold { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorState"/> class.
        /// </summary>
        protected MonitorState()
        {
        }

        /// <summary>
        /// Initializes the state.
        /// </summary>
        internal void InitializeState()
        {
            this.IsStart = false;
            this.IsHot = false;
            this.IsCold = false;

            this.GotoTransitions = new Dictionary<Type, GotoStateTransition>();
            this.ActionBindings = new Dictionary<Type, ActionBinding>();

            this.IgnoredEvents = new HashSet<Type>();

            var entryAttribute = this.GetType().GetCustomAttribute(typeof(OnEntryAttribute), false) as OnEntryAttribute;
            var exitAttribute = this.GetType().GetCustomAttribute(typeof(OnExitAttribute), false) as OnExitAttribute;

            if (entryAttribute != null)
            {
                this.EntryAction = entryAttribute.Action;
            }

            if (exitAttribute != null)
            {
                this.ExitAction = exitAttribute.Action;
            }

            var gotoAttributes = this.GetType().GetCustomAttributes(typeof(OnEventGotoStateAttribute), false)
                as OnEventGotoStateAttribute[];
            var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                as OnEventDoActionAttribute[];

            foreach (var attr in gotoAttributes)
            {
                if (attr.Action == null)
                {
                    this.GotoTransitions.Add(attr.Event, new GotoStateTransition(attr.State));
                }
                else
                {
                    this.GotoTransitions.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                }
            }

            foreach (var attr in doAttributes)
            {
                this.ActionBindings.Add(attr.Event, new ActionBinding(attr.Action));
            }

            var ignoreEventsAttribute = this.GetType().GetCustomAttribute(typeof(IgnoreEventsAttribute), false) as IgnoreEventsAttribute;

            if (ignoreEventsAttribute != null)
            {
                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
            }

            if (this.GetType().IsDefined(typeof(StartAttribute), false))
            {
                this.IsStart = true;
            }

            if (this.GetType().IsDefined(typeof(HotAttribute), false))
            {
                this.IsHot = true;
            }

            if (this.GetType().IsDefined(typeof(ColdAttribute), false))
            {
                this.IsCold = true;
            }
        }
    }
}
