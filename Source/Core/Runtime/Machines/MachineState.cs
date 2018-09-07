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
    /// Abstract class representing a state of a P# machine.
    /// </summary>
    public abstract class MachineState
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
        /// Dictionary containing all the push state transitions.
        /// </summary>
        internal Dictionary<Type, PushStateTransition> PushTransitions;

        /// <summary>
        /// Dictionary containing all the action bindings.
        /// </summary>
        internal Dictionary<Type, ActionBinding> ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        /// <summary>
        /// Set of deferred event types.
        /// </summary>
        internal HashSet<Type> DeferredEvents;

        /// <summary>
        /// True if this is the start state.
        /// </summary>
        internal bool IsStart { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected MachineState() { }

        /// <summary>
        /// Initializes the state.
        /// </summary>
        internal void InitializeState()
        {
            this.IsStart = false;

            this.GotoTransitions = new Dictionary<Type, GotoStateTransition>();
            this.PushTransitions = new Dictionary<Type, PushStateTransition>();
            this.ActionBindings = new Dictionary<Type, ActionBinding>();

            this.IgnoredEvents = new HashSet<Type>();
            this.DeferredEvents = new HashSet<Type>();

            var entryAttribute = this.GetType().GetCustomAttribute(typeof(OnEntry), true) as OnEntry;
            var exitAttribute = this.GetType().GetCustomAttribute(typeof(OnExit), true) as OnExit;

            if (entryAttribute != null)
            {
                this.EntryAction = entryAttribute.Action;
            }

            if (exitAttribute != null)
            {
                this.ExitAction = exitAttribute.Action;
            }

            // Events with already declared handlers.
            var handledEvents = new HashSet<Type>();

            // Install event handlers.
            this.InstallGotoTransitions(handledEvents);
            this.InstallPushTransitions(handledEvents);
            this.InstallActionHandlers(handledEvents);
            this.InstallIgnoreHandlers(handledEvents);
            this.InstallDeferHandlers(handledEvents);

            if (this.GetType().IsDefined(typeof(Start), false))
            {
                this.IsStart = true;
            }
        }

        /// <summary>
        /// Declares goto event handlers, if there are any.
        /// </summary>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InstallGotoTransitions(HashSet<Type> handledEvents)
        {
            var gotoAttributes = this.GetType().GetCustomAttributes(typeof(OnEventGotoState), false)
                as OnEventGotoState[];

            foreach (var attr in gotoAttributes)
            {
                CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                if (attr.Action == null)
                {
                    this.GotoTransitions.Add(attr.Event, new GotoStateTransition(attr.State));
                }
                else
                {
                    this.GotoTransitions.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                }

                handledEvents.Add(attr.Event);
            }

            this.InheritGotoTransitions(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits goto event handlers from a base state, if there is one.
        /// </summary>
        /// <param name="baseState">Base state.</param>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InheritGotoTransitions(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            var gotoAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventGotoState), false)
                as OnEventGotoState[];

            var gotoTransitionsInherited = new Dictionary<Type, GotoStateTransition>();
            foreach (var attr in gotoAttributesInherited)
            {
                if (this.GotoTransitions.ContainsKey(attr.Event))
                {
                    continue;
                }

                CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                if (attr.Action == null)
                {
                    gotoTransitionsInherited.Add(attr.Event, new GotoStateTransition(attr.State));
                }
                else
                {
                    gotoTransitionsInherited.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                }

                handledEvents.Add(attr.Event);
            }

            foreach (var kvp in gotoTransitionsInherited)
            {
                this.GotoTransitions.Add(kvp.Key, kvp.Value);
            }

            this.InheritGotoTransitions(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Declares push event handlers, if there are any.
        /// </summary>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InstallPushTransitions(HashSet<Type> handledEvents)
        {
            var pushAttributes = this.GetType().GetCustomAttributes(typeof(OnEventPushState), false)
                as OnEventPushState[];

            foreach (var attr in pushAttributes)
            {
                CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                this.PushTransitions.Add(attr.Event, new PushStateTransition(attr.State));
                handledEvents.Add(attr.Event);
            }

            this.InheritPushTransitions(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits push event handlers from a base state, if there is one.
        /// </summary>
        /// <param name="baseState">Base state.</param>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InheritPushTransitions(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            var pushAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventPushState), false)
                as OnEventPushState[];

            var pushTransitionsInherited = new Dictionary<Type, PushStateTransition>();
            foreach (var attr in pushAttributesInherited)
            {
                if (this.PushTransitions.ContainsKey(attr.Event))
                {
                    continue;
                }

                CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                pushTransitionsInherited.Add(attr.Event, new PushStateTransition(attr.State));
                handledEvents.Add(attr.Event);
            }

            foreach (var kvp in pushTransitionsInherited)
            {
                this.PushTransitions.Add(kvp.Key, kvp.Value);
            }

            this.InheritPushTransitions(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Declares action event handlers, if there are any.
        /// </summary>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InstallActionHandlers(HashSet<Type> handledEvents)
        {
            var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoAction), false)
                as OnEventDoAction[];

            foreach (var attr in doAttributes)
            {
                CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                this.ActionBindings.Add(attr.Event, new ActionBinding(attr.Action));
                handledEvents.Add(attr.Event);
            }

            this.InheritActionHandlers(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits action event handlers from a base state, if there is one.
        /// </summary>
        /// <param name="baseState">Base state.</param>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InheritActionHandlers(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            var doAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventDoAction), false)
                as OnEventDoAction[];

            var actionBindingsInherited = new Dictionary<Type, ActionBinding>();
            foreach (var attr in doAttributesInherited)
            {
                if (this.ActionBindings.ContainsKey(attr.Event))
                {
                    continue;
                }

                CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                actionBindingsInherited.Add(attr.Event, new ActionBinding(attr.Action));
                handledEvents.Add(attr.Event);
            }

            foreach (var kvp in actionBindingsInherited)
            {
                this.ActionBindings.Add(kvp.Key, kvp.Value);
            }

            this.InheritActionHandlers(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Declares ignore event handlers, if there are any.
        /// </summary>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InstallIgnoreHandlers(HashSet<Type> handledEvents)
        {
            var ignoreEventsAttribute = this.GetType().GetCustomAttribute(typeof(IgnoreEvents), false) as IgnoreEvents;

            if (ignoreEventsAttribute != null)
            {
                foreach (var e in ignoreEventsAttribute.Events)
                {
                    CheckEventHandlerAlreadyDeclared(e, handledEvents);
                }

                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
                handledEvents.UnionWith(ignoreEventsAttribute.Events);
            }

            this.InheritIgnoreHandlers(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits ignore event handlers from a base state, if there is one.
        /// </summary>
        /// <param name="baseState">Base state.</param>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InheritIgnoreHandlers(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            var ignoreEventsAttribute = baseState.GetCustomAttribute(typeof(IgnoreEvents), false) as IgnoreEvents;

            if (ignoreEventsAttribute != null)
            {
                foreach (var e in ignoreEventsAttribute.Events)
                {
                    if (this.IgnoredEvents.Contains(e))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(e, baseState, handledEvents);
                }

                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
                handledEvents.UnionWith(ignoreEventsAttribute.Events);
            }

            this.InheritIgnoreHandlers(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Declares defer event handlers, if there are any.
        /// </summary>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InstallDeferHandlers(HashSet<Type> handledEvents)
        {
            var deferEventsAttribute = this.GetType().GetCustomAttribute(typeof(DeferEvents), false) as DeferEvents;
            if (deferEventsAttribute != null)
            {
                foreach (var e in deferEventsAttribute.Events)
                {
                    CheckEventHandlerAlreadyDeclared(e, handledEvents);
                }

                this.DeferredEvents.UnionWith(deferEventsAttribute.Events);
                handledEvents.UnionWith(deferEventsAttribute.Events);
            }

            this.InheritDeferHandlers(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits defer event handlers from a base state, if there is one.
        /// </summary>
        /// <param name="baseState">Base state.</param>
        /// <param name="handledEvents">Set of handled events.</param>
        private void InheritDeferHandlers(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            var deferEventsAttribute = baseState.GetCustomAttribute(typeof(DeferEvents), false) as DeferEvents;
            if (deferEventsAttribute != null)
            {
                foreach (var e in deferEventsAttribute.Events)
                {
                    if (this.DeferredEvents.Contains(e))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(e, baseState, handledEvents);
                }

                this.DeferredEvents.UnionWith(deferEventsAttribute.Events);
                handledEvents.UnionWith(deferEventsAttribute.Events);
            }

            this.InheritDeferHandlers(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Checks if an event handler has been already declared.
        /// </summary>
        /// <param name="e">Event.</param>
        /// <param name="handledEvents">Set of handled events.</param>
        private void CheckEventHandlerAlreadyDeclared(Type e, HashSet<Type> handledEvents)
        {
            if (handledEvents.Contains(e))
            {
                throw new InvalidOperationException($"declared multiple handlers for '{e}'");
            }
        }

        /// <summary>
        /// Checks if an event handler has been already inherited.
        /// </summary>
        /// <param name="e">Event.</param>
        /// <param name="baseState">Base state.</param>
        /// <param name="handledEvents">Set of handled events.</param>
        private void CheckEventHandlerAlreadyInherited(Type e, Type baseState, HashSet<Type> handledEvents)
        {
            if (handledEvents.Contains(e))
            {
                throw new InvalidOperationException($"inherited multiple handlers for '{e}' from state '{baseState}'");
            }
        }
    }
}
