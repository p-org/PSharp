//-----------------------------------------------------------------------
// <copyright file="BaseRuntime.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// The base P# runtime.
    /// </summary>
    internal abstract class BaseRuntime : IPSharpRuntime, IRuntimeMachineManager
    {
        /// <summary>
        /// Map of unique machine id values to machines.
        /// </summary>
        private protected readonly ConcurrentDictionary<ulong, IMachine> MachineMap;

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        internal long MachineIdCounter;

        /// <summary>
        /// True if the runtime is currently running, else false.
        /// </summary>
        protected volatile bool IsRunning;

        /// <summary>
        /// The type of the timer machine.
        /// </summary>
        private Type TimerMachineType = null;

        /// <summary>
        /// The P# runtime.
        /// </summary>
        public IPSharpRuntime Runtime => this;

        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        public Configuration Configuration { get; private set; }

        /// <summary>
        /// The installed logger.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// True if the runtime is currently running, else false.
        /// </summary>
        bool IRuntimeMachineManager.IsRunning => this.IsRunning;

        /// <summary>
        /// True if testing mode is enabled, else false.
        /// </summary>
        public abstract bool IsTestingModeEnabled { get; }

        /// <summary>
        /// Event that is fired when the P# program throws an exception.
        /// </summary>
        public event OnFailureHandler OnFailure;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">The logger to install.</param>
        /// <param name="configuration">The configuration to use during runtime.</param>
        private protected BaseRuntime(ILogger logger, Configuration configuration)
        {
            this.Configuration = configuration;
            this.MachineIdCounter = 0;
            this.MachineMap = new ConcurrentDictionary<ulong, IMachine>();
            this.IsRunning = true;
            this.SetLogger(logger);
        }

        #region machine creation and execution

        /// <summary>
        /// Creates a fresh machine id that has not yet been bound to any machine.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>

        public MachineId CreateMachineId(Type type, string friendlyName = null)
        {
            // Atomically increments and safely wraps into an unsigned long.
            ulong value = (ulong)Interlocked.Increment(ref this.MachineIdCounter) - 1;
            // Checks for overflow.
            this.Assert(value != ulong.MaxValue, "Detected machine id overflow.");
            return new MachineId(this, type, value, friendlyName);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        public MachineId CreateMachine(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAsync(type, e, operationGroupId).Result;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        public MachineId CreateMachine(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAsync(type, friendlyName, e, operationGroupId).Result;
        }

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        public MachineId CreateMachine(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAsync(mid, type, e, operationGroupId).Result;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public abstract Task<MachineId> CreateMachineAsync(Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public abstract Task<MachineId> CreateMachineAsync(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public abstract Task<MachineId> CreateMachineAsync(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecuteAsync(type, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecuteAsync(type, friendlyName, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecuteAsync(mid, type, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public abstract Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public abstract Task<MachineId> CreateMachineAndExecuteAsync(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public abstract Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="e">Event passed during machine construction.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="creatorId">The id of the creator machine, if any.</param>
        /// <param name="creatorInfo">The metadata of the creator machine.</param>
        /// <param name="creatorStateName">The state name of the creator machine.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public abstract Task<MachineId> CreateMachineAsync(MachineId mid, Type type, string friendlyName, Event e, Guid? operationGroupId,
            IMachineId creatorId, MachineInfo creatorInfo, string creatorStateName);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        public void SendEvent(MachineId target, Event e, SendOptions options = null)
        {
            this.SendEventAsync(target, e, options).Wait();
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public abstract Task SendEventAsync(MachineId target, Event e, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense again.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        public Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null)
        {
            return this.SendEventAndExecuteAsync(target, e, options);
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense again.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        public abstract Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, SendOptions options = null);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <param name="sender">The sender machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public abstract Task SendEventAsync(MachineId mid, Event e, SendOptions options, IMachineId senderId, MachineInfo senderInfo,
            Type senderState, string senderStateName);

        /// <summary>
        /// Gets the machine with the specified id, or null if such machine does not exist.
        /// </summary>
        /// <param name="mid">The id of machine.</param>
        /// <param name="targetMachine">Receives the target machine, if found.</param>
        protected bool GetMachineFromId(MachineId mid, out IMachine targetMachine)
        {
            if (!this.MachineMap.TryGetValue(mid.Value, out targetMachine))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the specified type is a machine that can execute on this runtime.
        /// </summary>
        /// <returns>True if the type is supported, else false.</returns>
        protected abstract bool IsSupportedMachineType(Type type);

        /// <summary>
        /// Checks if the constructor of the machine constructor for the
        /// specified machine type exists in the cache.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Boolean</returns>
        protected abstract bool IsMachineConstructorCached(Type type);

        /// <summary>
        /// Notifies each active machine to halt execution to allow the runtime
        /// to reach quiescence. This is an experimental feature, which should
        /// be used only for testing purposes.
        /// </summary>
        public virtual void Stop()
        {
            this.IsRunning = false;
        }

        #endregion

        #region specifications and error checking

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
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="callerState">The state of the caller machine.</param>
        /// <param name="e">Event sent to the monitor.</param>
        public abstract void Monitor(Type type, IMachineId callerId, MachineInfo callerInfo, Type callerState, Event e);

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

        #endregion

        #region nondeterministic choices

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>The nondeterministic boolean choice.</returns>
        public bool Random() => this.GetNondeterministicBooleanChoice(null, null, String.Empty, 2);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        public bool Random(int maxValue) => this.GetNondeterministicBooleanChoice(null, null, String.Empty, maxValue);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="callerStateName">The state name of the caller machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        public abstract bool GetNondeterministicBooleanChoice(IMachineId callerId, MachineInfo callerInfo, string callerStateName, int maxValue);

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="callerStateName">The state name of the caller machine.</param>
        /// <param name="uniqueId">Unique id.</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        public abstract bool GetFairNondeterministicBooleanChoice(IMachineId callerId, MachineInfo callerInfo, string callerStateName, string uniqueId);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate an integer in the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic integer choice.</returns>
        public int RandomInteger(int maxValue) => this.GetNondeterministicIntegerChoice(null, null, String.Empty, maxValue);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="callerStateName">The state name of the caller machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic integer choice.</returns>
        public abstract int GetNondeterministicIntegerChoice(IMachineId callerId, MachineInfo callerInfo, string callerStateName, int maxValue);

        #endregion

        #region timers

        /// <summary>
        /// Overrides the default machine type for instantiating timers.
        /// </summary>
        /// <param name="type">Type of the timer.</param>
        public void SetTimerMachineType(Type type)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), "Timer must be a subclass of Machine.");
            this.TimerMachineType = type;
        }

        /// <summary>
        /// Returns the timer machine type.
        /// </summary>
        /// <returns>The timer machine type.</returns>
        public virtual Type GetTimerMachineType()
        {
            return this.TimerMachineType;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyEnteredState(IMachine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyEnteredState(Monitor monitor)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyExitedState(IMachine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyExitedState(Monitor monitor)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is performing a 'goto' transition to the specified state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="newStateName">The target state.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyGotoState(IMachine machine, string currStateName, string newStateName)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is performing a 'push' transition to the specified state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="newStateName">The target state.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyPushState(IMachine machine, string currStateName, string newStateName)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is performing a 'pop' transition from the current state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="restoredStateName">The name of the state being restored, if any.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyPopState(IMachine machine, string currStateName, string restoredStateName)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine popped its state because it cannot handle the current event.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyPopUnhandledEvent(IMachine machine, string currStateName, string eventName)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine invoked the 'pop' state action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyPopAction(IMachine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyInvokedAction(IMachine machine, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine completed an action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyCompletedAction(IMachine machine, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyRaisedEvent(IMachine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="eventInfo">The event metadata.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is handling a raised <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyHandleRaisedEvent(IMachine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyDequeuedEvent(IMachine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">The machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyReceiveCalled(IMachine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        /// <param name="eventNames">The names of the events that the machine is waiting for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyWaitEvents(IMachine machine, EventInfo eventInfoInInbox, string eventNames)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyReceivedEvent(IMachine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that the inbox of the specified machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyDefaultEventHandlerCheck(IMachine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that the default handler of the specified machine has been fired.
        /// </summary>
        /// <param name="machine">The machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyDefaultHandlerFired(IMachine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Task NotifyHaltedAsync(IMachine machine)
        {
            // Override to implement the notification.
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Notifies that a machine is throwing an exception.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyMachineExceptionThrown(IMachine machine, string currStateName, string actionName, Exception ex)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is using 'OnException' to handle a thrown exception.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void NotifyMachineExceptionHandled(IMachine machine, string currStateName, string actionName, Exception ex)
        {
            // Override to implement the notification.
        }

        #endregion

        #region logging

        /// <summary>
        /// Logs the specified text.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public virtual void Log(string format, params object[] args)
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

        #endregion

        #region operation group id

        /// <summary>
        /// Returns the operation group id of the specified machine. During testing,
        /// the runtime asserts that the specified machine is currently executing.
        /// </summary>
        /// <param name="currentMachineId">The id of the currently executing machine.</param>
        /// <returns>Guid</returns>
        public abstract Guid GetCurrentOperationGroupId(IMachineId currentMachineId);

        /// <summary>
        /// Gets the new operation group id to propagate.
        /// </summary>
        /// <param name="senderInfo">The metadata of the sender machine.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <returns>Operation group Id</returns>
        internal Guid GetNewOperationGroupId(MachineInfo senderInfo, Guid? operationGroupId)
        {
            if (operationGroupId.HasValue)
            {
                return operationGroupId.Value;
            }
            else if (senderInfo != null)
            {
                return senderInfo.OperationGroupId;
            }
            else
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Sets the operation group id for the destination machine.
        /// </summary>
        /// <param name="srcInfo">The metadata of the source machine.</param>
        /// <param name="destInfo">The metadata of the destination machine.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        internal void SetOperationGroupIdForMachine(MachineInfo srcInfo, MachineInfo destInfo, Guid? operationGroupId)
        {
            if (operationGroupId.HasValue)
            {
                destInfo.OperationGroupId = operationGroupId.Value;
            }
            else if (srcInfo != null)
            {
                destInfo.OperationGroupId = srcInfo.OperationGroupId;
            }
            else
            {
                destInfo.OperationGroupId = Guid.Empty;
            }
        }

        #endregion

        #region exceptions

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        /// <param name="exception">Exception</param>
        public void RaiseOnFailureEvent(Exception exception)
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
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public virtual void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            throw (exception is AssertionFailureException)
                ? exception
                : new AssertionFailureException(IO.Utilities.Format(s, args), exception);
        }

        #endregion

        #region cleanup

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public virtual void Dispose()
        {
            this.MachineIdCounter = 0;
            this.MachineMap.Clear();
        }

        #endregion
    }
}
