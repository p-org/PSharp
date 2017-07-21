//-----------------------------------------------------------------------
// <copyright file="Runtime.cs">
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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Net;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Runtime for executing state-machines.
    /// </summary>
    public abstract class PSharpRuntime : IDisposable
    {
        #region fields

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
        protected ConcurrentDictionary<ulong, Machine> MachineMap;

        #endregion

        #region properties

        /// <summary>
        /// Network provider used for remote communication.
        /// </summary>
        public INetworkProvider NetworkProvider { get; private set; }

        /// <summary>
        /// The installed logger.
        /// </summary>
        public ILogger Logger { get; private set; }

        #endregion

        #region events

        /// <summary>
        /// Event that is fired when the P# program throws an exception.
        /// </summary>
        public event OnFailureHandler OnFailure;

        /// <summary>
        /// Handles the <see cref="OnFailure"/> event.
        /// </summary>
        public delegate void OnFailureHandler(Exception ex);

        #endregion

        #region factory methods

        /// <summary>
        /// Creates a new state-machine runtime.
        /// </summary>
        /// <returns>Runtime</returns>
        public static PSharpRuntime Create()
        {
            return new StateMachineRuntime();
        }

        /// <summary>
        /// Creates a new state-machine runtime with the specified
        /// <see cref="PSharp.Configuration"/>.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>Runtime</returns>
        public static PSharpRuntime Create(Configuration configuration)
        {
            return new StateMachineRuntime(configuration);
        }

        #endregion

        #region initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        protected PSharpRuntime()
        {
            this.Configuration = Configuration.Create();
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        protected PSharpRuntime(Configuration configuration)
        {
            this.Configuration = configuration;
            this.Initialize();
        }

        /// <summary>
        /// Initializes various components of the runtime.
        /// </summary>
        private void Initialize()
        {
            this.MachineIdCounter = 0;
            this.NetworkProvider = new LocalNetworkProvider(this);
            this.SetLogger(new ConsoleLogger());
            this.IsRunning = true;
        }

        #endregion

        #region runtime interface

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
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
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
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public abstract Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Creates a new remote machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
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
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public abstract MachineId RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e = null, Guid? operationGroupId = null);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        public abstract void SendEvent(MachineId target, Event e, Guid? operationGroupId = null);

        /// <summary>
        /// Synchronously delivers an <see cref="Event"/> to a machine and
        /// executes the event handler if the machine is available.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        public abstract Task SendEventAndExecute(MachineId target, Event e, Guid? operationGroupId = null);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        public abstract void RemoteSendEvent(MachineId target, Event e, Guid? operationGroupId = null);

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
        /// <returns>Boolean</returns>
        public bool Random()
        {
            return this.GetNondeterministicBooleanChoice(null, 2);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        public bool Random(int maxValue)
        {
            return this.GetNondeterministicBooleanChoice(null, maxValue);
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate an integer in the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
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

        #endregion

        #region protected methods

        /// <summary>
        /// Gets the target machine for an event; if not found, logs a halted-machine entry.
        /// </summary>
        /// <param name="targetMachineId">The id of target machine.</param>
        /// <param name="e">The event that will be sent.</param>
        /// <param name="sender">The machine that is sending the event.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="targetMachine">Receives the target machine, if found.</param>
        protected bool GetTargetMachine(MachineId targetMachineId, Event e, AbstractMachine sender,
            Guid? operationGroupId, out Machine targetMachine)
        {
            if (!this.MachineMap.TryGetValue(targetMachineId.Value, out targetMachine))
            {
                var senderState = (sender as Machine)?.CurrentStateName ?? string.Empty;
                this.Logger.OnSend(targetMachineId, sender?.Id, senderState,
                    e.GetType().FullName, operationGroupId, isTargetHalted: true);
                return false;
            }

            return true;
        }

        #endregion

        #region state-machine execution

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal abstract MachineId CreateMachine(Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal abstract Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Creates a new remote <see cref="Machine"/> of the specified <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal abstract MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator, Guid? operationGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        internal abstract void SendEvent(MachineId mid, Event e, AbstractMachine sender, Guid? operationGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine and
        /// executes the event handler if the machine is available.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        internal abstract Task SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, Guid? operationGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        internal abstract void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, Guid? operationGroupId);

        /// <summary>
        /// Checks that a machine can start its event handler. Returns false if the event
        /// handler should not be started.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <returns>Boolean</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual bool CheckStartEventHandler(Machine machine)
        {
            return true;
        }

        #endregion

        #region specifications and error checking

        /// <summary>
        /// Tries to create a new <see cref="PSharp.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal abstract void TryCreateMonitor(Type type);

        /// <summary>
        /// Invokes the specified <see cref="PSharp.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="type">Type of the monitor</param>
        /// <param name="e">Event</param>
        internal abstract void Monitor(Type type, AbstractMachine sender, Event e);

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an
        /// <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public virtual void Assert(bool predicate)
        {
            if (!predicate)
            {
                var exception = new AssertionFailureException("Detected an assertion failure.");
                this.RaiseOnFailureEvent(exception);
                throw exception;
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
                var exception = new AssertionFailureException(IO.Utilities.Format(s, args));
                this.RaiseOnFailureEvent(exception);
                throw exception;
            }
        }

        #endregion

        #region nondeterministic choices

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        internal abstract bool GetNondeterministicBooleanChoice(AbstractMachine machine, int maxValue);

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal abstract bool GetFairNondeterministicBooleanChoice(AbstractMachine machine, string uniqueId);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
        internal abstract int GetNondeterministicIntegerChoice(AbstractMachine machine, int maxValue);

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">Machine</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyEnteredState(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyEnteredState(Monitor monitor)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">Machine</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyExitedState(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyExitedState(Monitor monitor)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">Machine</param>
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
        /// <param name="machine">Machine</param>
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
        /// <param name="monitor">Monitor</param>
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
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="operationGroupId">Operation group id</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyRaisedEvent(Machine machine, EventInfo eventInfo, Guid? operationGroupId)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine invoked pop.
        /// </summary>
        /// <param name="machine">Machine</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyPop(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">Machine</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceiveCalled(Machine machine)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is handling a raised <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHandleRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        internal virtual void NotifyWaitEvents(Machine machine, EventInfo eventInfoInInbox)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            // Override to implement the notification.
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="inboxSize">Current size of the machine inbox.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHalted(Machine machine, int inboxSize)
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
        /// <param name="machine">Machine</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultHandlerFired(Machine machine)
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

        #endregion

        #region Operation Group Id
        /// <summary>
        /// Sets the operation group id for the specified event.
        /// </summary>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        internal void SetOperationGroupIdForEvent(EventInfo eventInfo, AbstractMachine sender, ref Guid? operationGroupId)
        {
            if (operationGroupId.HasValue)
            {
                eventInfo.SetOperationGroupId(operationGroupId.Value);
            }
            else if (sender != null)
            {
                operationGroupId = sender.Info.OperationGroupId;
                eventInfo.SetOperationGroupId(sender.Info.OperationGroupId);
            }
            else
            {
                operationGroupId = Guid.Empty;
                eventInfo.SetOperationGroupId(Guid.Empty);
            }
        }

        /// <summary>
        /// Sets the operation group id for the specified machine.
        /// </summary>
        /// <param name="created">Machine created</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        internal void SetOperationGroupIdForMachine(Machine created, AbstractMachine sender, Guid? operationGroupId)
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

        #endregion

        #region networking

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

        #endregion

        #region exceptions

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
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        internal virtual void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            if (exception is AssertionFailureException)
            {
                throw exception;
            }
            else
            {
                this.RaiseOnFailureEvent(exception);
                throw new AssertionFailureException(IO.Utilities.Format(s, args), exception);
            }
        }

        #endregion

        #region cleanup

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public virtual void Dispose()
        {
            this.MachineIdCounter = 0;
            this.NetworkProvider.Dispose();
        }

        #endregion

    }
}
