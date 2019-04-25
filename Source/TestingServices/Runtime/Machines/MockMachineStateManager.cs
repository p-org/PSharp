﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// Implements a machine state manager that is used for testing purposes.
    /// </summary>
    internal class MockMachineStateManager : IMachineStateManager
    {
        /// <summary>
        /// The runtime that executes the machine being managed.
        /// </summary>
        private readonly TestingRuntime Runtime;

        /// <summary>
        /// The machine being managed.
        /// </summary>
        private readonly Machine Machine;

        /// <summary>
        /// True if the event handler of the machine is running, else false.
        /// </summary>
        public bool IsEventHandlerRunning { get; set; }

        /// <summary>
        /// Program counter used for state-caching. Distinguishes
        /// scheduling from non-deterministic choices.
        /// </summary>
        internal int ProgramCounter;

        /// <summary>
        /// True if a transition statement was called in the current action, else false.
        /// </summary>
        internal bool IsTransitionStatementCalledInCurrentAction;

        /// <summary>
        /// True if the machine is executing an on exit action, else false.
        /// </summary>
        internal bool IsInsideOnExit;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockMachineStateManager"/> class.
        /// </summary>
        internal MockMachineStateManager(TestingRuntime runtime, Machine machine)
        {
            this.Runtime = runtime;
            this.Machine = machine;
            this.IsEventHandlerRunning = true;
            this.ProgramCounter = 0;
            this.IsTransitionStatementCalledInCurrentAction = false;
            this.IsInsideOnExit = false;
        }

        /// <summary>
        /// Returns the cached state of the machine.
        /// </summary>
        public int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + this.IsEventHandlerRunning.GetHashCode();
                hash = (hash * 31) + this.ProgramCounter;
                return hash;
            }
        }

        /// <summary>
        /// Checks if the specified event is ignored in the current machine state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventIgnoredInCurrentState(Event e, EventInfo eventInfo) => this.Machine.IsEventIgnoredInCurrentState(e);

        /// <summary>
        /// Checks if the specified event is deferred in the current machine state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventDeferredInCurrentState(Event e, EventInfo eventInfo) => this.Machine.IsEventDeferredInCurrentState(e);

        /// <summary>
        /// Checks if a default handler is installed in the current machine state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDefaultHandlerInstalledInCurrentState() => this.Machine.IsDefaultHandlerInstalledInCurrentState();

        /// <summary>
        /// Notifies the machine that an event has been enqueued.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnEnqueueEvent(Event e, EventInfo eventInfo)
        {
            this.Runtime.Logger.OnEnqueue(this.Machine.Id, e.GetType().FullName);
        }

        /// <summary>
        /// Notifies the machine that an event has been raised.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRaisedEvent(Event e, EventInfo eventInfo)
        {
            this.Runtime.NotifyRaisedEvent(this.Machine, e, eventInfo);
        }

        /// <summary>
        /// Notifies the machine that it is waiting to receive an event of one of the specified types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NotifyWaitEvent(IEnumerable<Type> eventTypes)
        {
            this.Runtime.NotifyWaitEvent(this.Machine, eventTypes);
        }

        /// <summary>
        /// Notifies the machine that an event it was waiting to receive has been enqueued.
        /// </summary>
        public void OnReceiveEvent(Event e, EventInfo eventInfo)
        {
            this.Runtime.NotifyReceivedEvent(this.Machine, e, eventInfo);
        }

        /// <summary>
        /// Notifies the machine that an event it was waiting to receive was already in the
        /// event queue when the machine invoked the receive statement.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NotifyReceivedEventWithoutWaiting(Event e, EventInfo eventInfo)
        {
            this.Runtime.NotifyReceivedEventWithoutWaiting(this.Machine, e, eventInfo);
        }

        /// <summary>
        /// Notifies the machine that an event has been dropped.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDroppedEvent(Event e, EventInfo eventInfo)
        {
            this.Runtime.Assert(!eventInfo.MustHandle, "Machine '{0}' halted before dequeueing must-handle event '{1}'.",
                this.Machine.Id, e.GetType().FullName);
            this.Runtime.TryHandleDroppedEvent(e, this.Machine.Id);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0)
        {
            this.Runtime.Assert(predicate, s, arg0);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1)
        {
            this.Runtime.Assert(predicate, s, arg0, arg1);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            this.Runtime.Assert(predicate, s, arg0, arg1, arg2);
        }

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, params object[] args)
        {
            this.Runtime.Assert(predicate, s, args);
        }
    }
}
