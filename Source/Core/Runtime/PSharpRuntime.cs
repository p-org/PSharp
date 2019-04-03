﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Net;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.Timers;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Runtime for executing state-machines.
    /// </summary>
    public abstract class PSharpRuntime : IDisposable
    {
        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        internal Configuration Configuration;

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        internal long MachineIdCounter;

        /// <summary>
        /// Records if the runtime is running.
        /// </summary>
        internal volatile bool IsRunning;

        /// <summary>
        /// Map from unique machine ids to machines.
        /// </summary>
        protected ConcurrentDictionary<MachineId, Machine> MachineMap;

        /// <summary>
        /// Network provider used for remote communication.
        /// </summary>
        public INetworkProvider NetworkProvider { get; private set; }

        /// <summary>
        /// The installed logger.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Event that is fired when the P# program throws an exception.
        /// </summary>
        public event OnFailureHandler OnFailure;

        /// <summary>
        /// Callback that is fired when a P# event is dropped.
        /// </summary>
        public event OnEventDroppedHandler OnEventDropped;

        /// <summary>
        /// Creates a new state-machine runtime.
        /// </summary>
        /// <returns>Runtime</returns>
        public static PSharpRuntime Create()
        {
            return new ProductionRuntime(Configuration.Create());
        }

        /// <summary>
        /// Creates a new state-machine runtime with the specified
        /// <see cref="PSharp.Configuration"/>.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>Runtime</returns>
        public static PSharpRuntime Create(Configuration configuration)
        {
            return new ProductionRuntime(configuration);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpRuntime"/> class.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        protected PSharpRuntime(Configuration configuration)
        {
            this.Configuration = configuration;
            this.MachineMap = new ConcurrentDictionary<MachineId, Machine>();
            this.MachineIdCounter = 0;
            this.NetworkProvider = new LocalNetworkProvider(this);
            this.SetLogger(new ConsoleLogger());
            this.IsRunning = true;
        }

        /// <summary>
        /// Creates a fresh machine id that has not yet been bound to any machine.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <returns>MachineId</returns>
        public MachineId CreateMachineId(Type type, string friendlyName = null) => new MachineId(type, friendlyName, this);

        /// <summary>
        /// Creates a machine id that is uniquely tied to the specified unique name. The
        /// returned machine id can either be a fresh id (not yet bound to any machine),
        /// or it can be bound to a previously created machine. In the second case, this
        /// machine id can be directly used to communicate with the corresponding machine.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="uniqueName">Unique name used to create the machine id</param>
        /// <returns>MachineId</returns>
        public abstract MachineId CreateMachineIdFromName(Type type, string uniqueName);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <returns>MachineId</returns>
        public abstract MachineId CreateMachine(Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        public abstract void CreateMachine(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <returns>MachineId</returns>
        public abstract MachineId CreateMachine(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <returns>MachineId</returns>
        public abstract Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        public abstract Task CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <returns>MachineId</returns>
        public abstract Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new remote machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <returns>MachineId</returns>
        public abstract MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new remote machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <returns>MachineId</returns>
        public abstract MachineId RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        public abstract void SendEvent(MachineId target, Event e, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately
        /// if the target machine was already running. Otherwise blocks until the machine handles
        /// the event and reaches quiescense again.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>True if event was handled, false if the event was only enqueued</returns>
        public abstract Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        public abstract void RemoteSendEvent(MachineId target, Event e, SendOptions options = null);

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        public abstract void RegisterMonitor(Type type);

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        public abstract void InvokeMonitor<T>(Event e);

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="e">Event</param>
        public abstract void InvokeMonitor(Type type, Event e);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>The controlled nondeterministic choice.</returns>
        public bool Random()
        {
            return this.GetNondeterministicBooleanChoice(null, 2);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerMemberName">CallerMemberName</param>
        /// <param name="callerFilePath">CallerFilePath</param>
        /// <param name="callerLineNumber">CallerLineNumber</param>
        /// <returns>The controlled nondeterministic choice.</returns>
        public bool FairRandom(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var havocId = string.Format(
                "Runtime_{0}_{1}_{2}",
                callerMemberName, callerFilePath, callerLineNumber);
            return this.GetFairNondeterministicBooleanChoice(null, havocId);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic choice.</returns>
        public bool Random(int maxValue)
        {
            return this.GetNondeterministicBooleanChoice(null, maxValue);
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic integer.</returns>
        public int RandomInteger(int maxValue)
        {
            return this.GetNondeterministicIntegerChoice(null, maxValue);
        }

        /// <summary>
        /// Returns the operation group id of the specified machine. During testing,
        /// the runtime asserts that the specified machine is currently executing.
        /// </summary>
        /// <param name="currentMachine">MachineId of the currently executing machine.</param>
        /// <returns>Guid</returns>
        public abstract Guid GetCurrentOperationGroupId(MachineId currentMachine);

        /// <summary>
        /// Notifies each active machine to halt execution to allow the runtime
        /// to reach quiescence. This is an experimental feature, which should
        /// be used only for testing purposes.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Gets the target machine for an event; if not found, logs a halted-machine entry.
        /// </summary>
        /// <param name="targetMachineId">The id of target machine.</param>
        /// <param name="e">The event that will be sent.</param>
        /// <param name="sender">The machine that is sending the event.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="targetMachine">Receives the target machine, if found.</param>
        protected bool GetTargetMachine(MachineId targetMachineId, Event e, BaseMachine sender,
            Guid operationGroupId, out Machine targetMachine)
        {
            if (!this.MachineMap.TryGetValue(targetMachineId, out targetMachine))
            {
                var senderState = (sender as Machine)?.CurrentStateName ?? string.Empty;
                this.Logger.OnSend(targetMachineId, sender?.Id, senderState,
                    e.GetType().FullName, operationGroupId, isTargetHalted: true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <returns>MachineId</returns>
        internal abstract MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <returns>MachineId</returns>
        internal abstract Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Creates a new remote <see cref="Machine"/> of the specified <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <returns>MachineId</returns>
        internal abstract MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        internal abstract void SendEvent(MachineId mid, Event e, BaseMachine sender, SendOptions options);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine. Returns immediately
        /// if the target machine was already running. Otherwise blocks until the machine handles
        /// the event and reaches quiescense again.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>True if event was handled, false if the event was only enqueued</returns>
        internal abstract Task<bool> SendEventAndExecute(MachineId mid, Event e, BaseMachine sender, SendOptions options);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        internal abstract void SendEventRemotely(MachineId mid, Event e, BaseMachine sender, SendOptions options);

        /// <summary>
        /// Checks that a machine can start its event handler. Returns false if the event
        /// handler should not be started.
        /// </summary>
        /// <param name="machine">The caller machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual bool CheckStartEventHandler(Machine machine)
        {
            return true;
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner machine.
        /// </summary>
        /// <param name="info">Stores information about the timer.</param>
        /// <param name="owner">The owner machine.</param>
        /// <returns>The machine timer.</returns>
        internal abstract IMachineTimer CreateMachineTimer(TimerInfo info, Machine owner);

        /// <summary>
        /// Returns the timer machine type.
        /// </summary>
        internal abstract Type GetTimerMachineType();

        /// <summary>
        /// Tries to create a new <see cref="PSharp.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal abstract void TryCreateMonitor(Type type);

        /// <summary>
        /// Invokes the specified <see cref="PSharp.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="e">Event</param>
        internal abstract void Monitor(Type type, BaseMachine sender, Event e);

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an
        /// <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public virtual void Assert(bool predicate)
        {
            if (!predicate)
            {
                throw new AssertionFailureException("Detected an assertion failure.");
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an
        /// <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public virtual void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(IO.Utilities.Format(s, args));
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The caller machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>Boolean</returns>
        internal abstract bool GetNondeterministicBooleanChoice(BaseMachine machine, int maxValue);

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The caller machine.</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal abstract bool GetFairNondeterministicBooleanChoice(BaseMachine machine, string uniqueId);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The caller machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>Integer</returns>
        internal abstract int GetNondeterministicIntegerChoice(BaseMachine machine, int maxValue);

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyEnteredState(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">The monitor that triggered the notification.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyEnteredState(Monitor monitor)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyExitedState(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">The monitor that triggered the notification.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyExitedState(Monitor monitor)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyInvokedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine completed invoking an action.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyCompletedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        /// <param name="monitor">The monitor that triggered the notification.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="monitor">The monitor that triggered the notification.</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine invoked pop.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyPop(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceiveCalled(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is handling a raised <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHandleRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        internal virtual void NotifyWaitEvents(Machine machine, EventInfo eventInfoInInbox)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        /// <param name="inbox">Machine inbox.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHalted(Machine machine, LinkedList<EventInfo> inbox)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that the inbox of the specified machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultEventHandlerCheck(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that the default handler of the specified machine has been fired.
        /// </summary>
        /// <param name="machine">The machine that triggered the notification.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultHandlerFired(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Logs the specified text.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        protected internal virtual void Log(string format, params object[] args)
        {
            if (this.Configuration.Verbose > 1)
            {
                this.Logger.WriteLine(format, args);
            }
        }

        /// <summary>
        /// Installs the specified <see cref="ILogger"/>.
        /// </summary>
        /// <param name="logger">ILogger</param>
        public void SetLogger(ILogger logger)
        {
            this.Logger = logger ?? throw new InvalidOperationException("Cannot install a null logger.");
            this.Logger.Configuration = this.Configuration;
        }

        /// <summary>
        /// Removes the currently installed <see cref="ILogger"/>, and replaces
        /// it with the default <see cref="ILogger"/>.
        /// </summary>
        public void RemoveLogger()
        {
            this.Logger = new ConsoleLogger();
        }

        /// <summary>
        /// Gets the new operation group id to propagate.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <returns>Operation group Id</returns>
        internal Guid GetNewOperationGroupId(BaseMachine sender, Guid? operationGroupId)
        {
            if (operationGroupId.HasValue)
            {
                return operationGroupId.Value;
            }
            else if (sender != null)
            {
                return sender.Info.OperationGroupId;
            }
            else
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Sets the operation group id for the specified machine.
        /// </summary>
        /// <param name="created">Machine created</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        internal void SetOperationGroupIdForMachine(Machine created, BaseMachine sender, Guid? operationGroupId)
        {
            if (operationGroupId.HasValue)
            {
                created.Info.OperationGroupId = operationGroupId.Value;
            }
            else if (sender != null)
            {
                created.Info.OperationGroupId = sender.Info.OperationGroupId;
            }
            else
            {
                created.Info.OperationGroupId = Guid.Empty;
            }
        }

        /// <summary>
        /// Installs the specified <see cref="INetworkProvider"/>.
        /// </summary>
        /// <param name="networkProvider">INetworkProvider</param>
        public void SetNetworkProvider(INetworkProvider networkProvider)
        {
            if (networkProvider == null)
            {
                throw new InvalidOperationException("Cannot install a null network provider.");
            }

            this.NetworkProvider.Dispose();
            this.NetworkProvider = networkProvider;
        }

        /// <summary>
        /// Replaces the currently installed <see cref="INetworkProvider"/>
        /// with the default <see cref="INetworkProvider"/>.
        /// </summary>
        public void RemoveNetworkProvider()
        {
            this.NetworkProvider.Dispose();
            this.NetworkProvider = new LocalNetworkProvider(this);
        }

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        /// <param name="exception">Exception</param>
        protected internal void RaiseOnFailureEvent(Exception exception)
        {
            if (this.Configuration.AttachDebugger && exception is MachineActionExceptionFilterException &&
                !((exception as MachineActionExceptionFilterException).InnerException is RuntimeException))
            {
                System.Diagnostics.Debugger.Break();
                this.Configuration.AttachDebugger = false;
            }

            this.OnFailure?.Invoke(exception);
        }

        /// <summary>
        /// Checks if an <see cref="OnEventDropped"/> handler has been registered by the user.
        /// </summary>
        /// <returns>True if an <see cref="OnEventDropped"/> handler is registered.</returns>
        protected internal bool IsOnEventDroppedHandlerRegistered() => this.OnEventDropped != null;

        /// <summary>
        /// Tries to handle the specified dropped <see cref="Event"/>.
        /// </summary>
        /// <param name="e">Event being dropped.</param>
        /// <param name="mid">Target machine id.</param>
        protected internal void TryHandleDroppedEvent(Event e, MachineId mid)
        {
            this.OnEventDropped?.Invoke(e, mid);
        }

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        internal virtual void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            throw (exception is AssertionFailureException)
                ? exception
                : new AssertionFailureException(IO.Utilities.Format(s, args), exception);
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.MachineIdCounter = 0;
                this.NetworkProvider.Dispose();
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public virtual void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
