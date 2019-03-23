// ------------------------------------------------------------------------------------------------
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
using Microsoft.PSharp.Timers;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Runtime for executing machines.
    /// </summary>
    internal abstract class BaseRuntime : IMachineRuntime
    {
        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        internal readonly Configuration Configuration;

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
        protected readonly ConcurrentDictionary<MachineId, Machine> MachineMap;

        /// <summary>
        /// The installed logger.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Callback that is fired when the P# program throws an exception.
        /// </summary>
        public event OnFailureHandler OnFailure;

        /// <summary>
        /// Callback that is fired when a P# event is dropped.
        /// </summary>
        public event OnEventDroppedHandler OnEventDropped;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRuntime"/> class.
        /// </summary>
        protected BaseRuntime(Configuration configuration)
        {
            this.Configuration = configuration;
            this.MachineMap = new ConcurrentDictionary<MachineId, Machine>();
            this.MachineIdCounter = 0;
            this.SetLogger(new ConsoleLogger());
            this.IsRunning = true;
        }

        /// <summary>
        /// Creates a fresh machine id that has not yet been bound to any machine.
        /// </summary>
        public MachineId CreateMachineId(Type type, string machineName = null) => new MachineId(type, machineName, this);

        /// <summary>
        /// Creates a machine id that is uniquely tied to the specified unique name. The
        /// returned machine id can either be a fresh id (not yet bound to any machine),
        /// or it can be bound to a previously created machine. In the second case, this
        /// machine id can be directly used to communicate with the corresponding machine.
        /// </summary>
        public abstract MachineId CreateMachineIdFromName(Type type, string machineName);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public abstract MachineId CreateMachine(Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public abstract MachineId CreateMachine(Type type, string machineName, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public abstract MachineId CreateMachine(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public abstract Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public abstract Task<MachineId> CreateMachineAndExecuteAsync(Type type, string machineName, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public abstract Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public abstract Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public abstract Task<MachineId> CreateMachineAndExecute(Type type, string machineName, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public abstract Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        public abstract void SendEvent(MachineId target, Event e, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense again.
        /// </summary>
        public abstract Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense again.
        /// </summary>
        public abstract Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null);

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        public void RegisterMonitor(Type type)
        {
            this.TryCreateMonitor(type);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        public void InvokeMonitor<T>(Event e)
        {
            this.InvokeMonitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        public void InvokeMonitor(Type type, Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor(type, null, e);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        public bool Random()
        {
            return this.GetNondeterministicBooleanChoice(null, 2);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
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
        public bool Random(int maxValue)
        {
            return this.GetNondeterministicBooleanChoice(null, maxValue);
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        public int RandomInteger(int maxValue)
        {
            return this.GetNondeterministicIntegerChoice(null, maxValue);
        }

        /// <summary>
        /// Returns the operation group id of the specified machine. During testing,
        /// the runtime asserts that the specified machine is currently executing.
        /// </summary>
        public abstract Guid GetCurrentOperationGroupId(MachineId currentMachine);

        /// <summary>
        /// Notifies each active machine to halt execution to allow the runtime
        /// to reach quiescence. This is an experimental feature, which should
        /// be used only for testing purposes.
        /// </summary>
        public void Stop()
        {
            this.IsRunning = false;
        }

        /// <summary>
        /// Gets the target machine for an event; if not found, logs a halted-machine entry.
        /// </summary>
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
        /// <returns>MachineId</returns>
        internal abstract MachineId CreateMachine(MachineId mid, Type type, string machineName, Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        /// <returns>MachineId</returns>
        internal abstract Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string machineName,
            Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        internal abstract void SendEvent(MachineId target, Event e, BaseMachine sender, SendOptions options);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine. Returns immediately
        /// if the target machine was already running. Otherwise blocks until the machine handles
        /// the event and reaches quiescense again.
        /// </summary>
        internal abstract Task<bool> SendEventAndExecute(MachineId target, Event e, BaseMachine sender, SendOptions options);

        /// <summary>
        /// Checks that a machine can start its event handler. Returns false if the event
        /// handler should not be started.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual bool CheckStartEventHandler(Machine machine)
        {
            return true;
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner machine.
        /// </summary>
        internal abstract IMachineTimer CreateMachineTimer(TimerInfo info, Machine owner);

        /// <summary>
        /// Returns the timer machine type.
        /// </summary>
        internal abstract Type GetTimerMachineType();

        /// <summary>
        /// Tries to create a new <see cref="PSharp.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal abstract void TryCreateMonitor(Type type);

        /// <summary>
        /// Invokes the specified <see cref="PSharp.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal abstract void Monitor(Type type, BaseMachine sender, Event e);

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an
        /// <see cref="AssertionFailureException"/> exception.
        /// </summary>
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
        internal abstract bool GetNondeterministicBooleanChoice(BaseMachine machine, int maxValue);

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal abstract bool GetFairNondeterministicBooleanChoice(BaseMachine machine, string uniqueId);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal abstract int GetNondeterministicIntegerChoice(BaseMachine machine, int maxValue);

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyEnteredState(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyEnteredState(Monitor monitor)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyExitedState(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyExitedState(Monitor monitor)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyInvokedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine completed invoking an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyCompletedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine invoked pop.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyPop(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceiveCalled(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is handling a raised <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHandleRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        internal virtual void NotifyWaitEvents(Machine machine, EventInfo eventInfoInInbox)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHalted(Machine machine, LinkedList<EventInfo> inbox)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that the inbox of the specified machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultEventHandlerCheck(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that the default handler of the specified machine has been fired.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultHandlerFired(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Logs the specified text.
        /// </summary>
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
        public void SetLogger(ILogger logger)
        {
            this.Logger = logger ?? throw new InvalidOperationException("Cannot install a null logger.");
            this.Logger.Configuration = this.Configuration;
        }

        /// <summary>
        /// Gets the new operation group id to propagate.
        /// </summary>
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
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
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
        protected internal bool IsOnEventDroppedHandlerRegistered() => this.OnEventDropped != null;

        /// <summary>
        /// Tries to handle the specified dropped <see cref="Event"/>.
        /// </summary>
        protected internal void TryHandleDroppedEvent(Event e, MachineId mid)
        {
            this.OnEventDropped?.Invoke(e, mid);
        }

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
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
