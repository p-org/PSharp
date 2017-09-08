//-----------------------------------------------------------------------
// <copyright file="StateMachineRuntime.cs">
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
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Runtime for executing state-machines in production.
    /// </summary>
    internal sealed class StateMachineRuntime : PSharpRuntime
    {
        #region fields

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private List<Monitor> Monitors;

        #endregion

        #region initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        internal StateMachineRuntime()
            : base()
        {
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal StateMachineRuntime(Configuration configuration)
            : base(configuration)
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes various components of the runtime.
        /// </summary>
        private void Initialize()
        {
            this.Monitors = new List<Monitor>();
            this.MachineMap = new ConcurrentDictionary<ulong, Machine>();
        }

        #endregion

        #region runtime interface

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachine(type, null, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachine(type, friendlyName, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecute(type, null, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecute(type, friendlyName, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new remote machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateRemoteMachine(type, null, endpoint, e, null, operationGroupId);
        }

        /// <summary>
        /// Creates a new remote machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateRemoteMachine(type, friendlyName, endpoint, e, null, operationGroupId);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        public override void SendEvent(MachineId target, Event e, Guid? operationGroupId = null)
        {
            // If the target machine is null then report an error and exit.
            base.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            base.Assert(e != null, "Cannot send a null event.");
            this.SendEvent(target, e, null, operationGroupId);
        }

        /// <summary>
        /// Synchronously delivers an <see cref="Event"/> to a machine
        /// and executes it if the machine is available.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        public override Task SendEventAndExecute(MachineId target, Event e, Guid? operationGroupId = null)
        {
            // If the target machine is null then report an error and exit.
            base.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            base.Assert(e != null, "Cannot send a null event.");
            return this.SendEventAndExecute(target, e, null, operationGroupId);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        public override void RemoteSendEvent(MachineId target, Event e, Guid? operationGroupId = null)
        {
            // If the target machine is null then report an error and exit.
            base.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            base.Assert(e != null, "Cannot send a null event.");
            this.SendEventRemotely(target, e, null, operationGroupId);
        }

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        public override void RegisterMonitor(Type type)
        {
            this.TryCreateMonitor(type);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        public override void InvokeMonitor<T>(Event e)
        {
            this.InvokeMonitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="e">Event</param>
        public override void InvokeMonitor(Type type, Event e)
        {
            // If the event is null then report an error and exit.
            base.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor(type, null, e);
        }

        /// <summary>
        /// Returns the operation group id of the specified machine. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="MachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        /// <param name="currentMachine">MachineId of the currently executing machine.</param>
        /// <returns>Guid</returns>
        public override Guid GetCurrentOperationGroupId(MachineId currentMachine)
        {
            if (!this.MachineMap.TryGetValue(currentMachine.Value, out Machine machine))
            {
                return Guid.Empty;
            }

            return machine.Info.OperationGroupId;
        }

        /// <summary>
        /// Notifies each active machine to halt execution to allow the runtime
        /// to reach quiescence. This is an experimental feature, which should
        /// be used only for testing purposes.
        /// </summary>
        public override void Stop()
        {
            base.IsRunning = false;
        }

        #endregion

        #region state-machine execution

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId CreateMachine(Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            Machine machine = this.CreateMachine(type, friendlyName);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);

            this.RunMachineEventHandler(machine, e, true);
            return machine.Id;
        }

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
        internal override async Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            Machine machine = this.CreateMachine(type, friendlyName);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);
            await this.RunMachineEventHandlerAsync(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new remote <see cref="Machine"/> of the specified <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator, Guid? operationGroupId)
        {
            base.Assert(type.IsSubclassOf(typeof(Machine)),
                $"Type '{type.Name}' is not a machine.");
            return base.NetworkProvider.RemoteCreateMachine(type, friendlyName, endpoint, e);
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <returns>Machine</returns>
        private Machine CreateMachine(Type type, string friendlyName)
        {
            base.Assert(type.IsSubclassOf(typeof(Machine)), $"Type '{type.Name}' is not a machine.");

            MachineId mid = new MachineId(type, friendlyName, this);
            Machine machine = MachineFactory.Create(type);

            machine.Initialize(this, mid, new MachineInfo(mid));
            machine.InitializeStateInformation();

            bool result = this.MachineMap.TryAdd(mid.Value, machine);
            base.Assert(result, $"Machine '{mid}' was already created.");

            base.Logger.OnCreateMachine(mid);

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender, Guid? operationGroupId)
        {
            if (!base.GetTargetMachine(mid, e, sender, operationGroupId, out Machine machine))
            {
                return;
            }

            bool runNewHandler = false;
            this.EnqueueEvent(machine, e, sender, operationGroupId, ref runNewHandler);
            if (runNewHandler)
            {
                this.RunMachineEventHandler(machine, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine and
        /// executes the event handler if the machine is available.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        internal override async Task SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, Guid? operationGroupId)
        {
            if (!base.GetTargetMachine(mid, e, sender, operationGroupId, out Machine machine))
            {
                return;
            }

            bool runNewHandler = false;
            this.EnqueueEvent(machine, e, sender, operationGroupId, ref runNewHandler);
            if (runNewHandler)
            {
                await this.RunMachineEventHandlerAsync(machine, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        internal override void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, Guid? operationGroupId)
        {
            base.NetworkProvider.RemoteSend(mid, e);
        }

        /// <summary>
        /// Enqueues an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="runNewHandler">Run a new handler</param>
        private void EnqueueEvent(Machine machine, Event e, AbstractMachine sender, Guid? operationGroupId, ref bool runNewHandler)
        {
            EventInfo eventInfo = new EventInfo(e, null);
            this.SetOperationGroupIdForEvent(eventInfo, sender, ref operationGroupId);

            var senderState = (sender as Machine)?.CurrentStateName ?? string.Empty;
            base.Logger.OnSend(machine.Id, machine.CurrentStateName, sender?.Id, senderState,
                e.GetType().FullName, operationGroupId, isTargetHalted: false);

            machine.Enqueue(eventInfo, ref runNewHandler);
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        private void RunMachineEventHandler(Machine machine, Event initialEvent, bool isFresh)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await machine.GotoStartState(initialEvent);
                    }

                    await machine.RunEventHandler();
                }
                catch (AssertionFailureException)
                {
                    base.IsRunning = false;
                }
                catch (Exception ex)
                {
                    base.IsRunning = false;
                    base.RaiseOnFailureEvent(ex);
                }
            });
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// </summary>
        /// <param name="machine">Machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        private async Task RunMachineEventHandlerAsync(Machine machine, Event initialEvent, bool isFresh)
        {
            bool completed;

            try
            {
                if (isFresh)
                {
                    await machine.GotoStartState(initialEvent);
                }

                completed = await machine.RunEventHandler(true);
            }
            catch (AssertionFailureException)
            {
                base.IsRunning = false;
                return;
            }
            catch (Exception ex)
            {
                base.IsRunning = false;
                base.RaiseOnFailureEvent(ex);
                return;
            }

            if (!completed)
            {
                RunMachineEventHandler(machine, null, false);
            }
        }

        #endregion

        #region specifications and error checking

        /// <summary>
        /// Tries to create a new <see cref="PSharp.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal override void TryCreateMonitor(Type type)
        {
            if (!base.Configuration.EnableMonitorsInProduction)
            {
                // No-op in production.
                return;
            }

            base.Assert(type.IsSubclassOf(typeof(Monitor)), $"Type '{type.Name}' " +
                "is not a subclass of Monitor.\n");

            MachineId mid = new MachineId(type, null, this);
            Monitor monitor = (Monitor)Activator.CreateInstance(type);

            monitor.Initialize(mid);
            monitor.InitializeStateInformation();

            lock (this.Monitors)
            {
                this.Monitors.Add(monitor);
            }

            base.Logger.OnCreateMonitor(type.Name, monitor.Id);

            monitor.GotoStartState();
        }

        /// <summary>
        /// Invokes the specified <see cref="PSharp.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="type">Type of the monitor</param>
        /// <param name="e">Event</param>
        internal override void Monitor(Type type, AbstractMachine sender, Event e)
        {
            if (!base.Configuration.EnableMonitorsInProduction)
            {
                // No-op in production.
                return;
            }

            Monitor monitor = null;

            lock (this.Monitors)
            {
                foreach (var m in this.Monitors)
                {
                    if (m.GetType() == type)
                    {
                        monitor = m;
                        break;
                    }
                }
            }

            if (monitor != null)
            {
                lock (monitor)
                {
                    monitor.MonitorEvent(e);
                }
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
        internal override bool GetNondeterministicBooleanChoice(AbstractMachine machine, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);

            bool result = false;
            if (random.Next(maxValue) == 0)
            {
                result = true;
            }

            base.Logger.OnRandom(machine?.Id, result);

            return result;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal override bool GetFairNondeterministicBooleanChoice(AbstractMachine machine, string uniqueId)
        {
            return this.GetNondeterministicBooleanChoice(machine, 2);
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
        internal override int GetNondeterministicIntegerChoice(AbstractMachine machine, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var result = random.Next(maxValue);

            base.Logger.OnRandom(machine?.Id, result);

            return result;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyEnteredState(Machine machine)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            base.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry:true);
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            string monitorState = monitor.CurrentStateNameWithTemperature;
            base.Logger.OnMonitorState(monitor.GetType().Name, monitor.Id, monitorState, true, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyExitedState(Machine machine)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            base.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: false);
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        internal override void NotifyExitedState(Monitor monitor)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            string monitorState = monitor.CurrentStateNameWithTemperature;
            base.Logger.OnMonitorState(monitor.GetType().Name,monitor.Id, monitorState, false, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        internal override void NotifyInvokedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            base.Logger.OnMachineAction(machine.Id, machine.CurrentStateName, action.Name);
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            base.Logger.OnMonitorAction(monitor.GetType().Name, monitor.Id, action.Name, monitor.CurrentStateName);
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="operationGroupId">Operation group id</param>
        internal override void NotifyRaisedEvent(Machine machine, EventInfo eventInfo, Guid? operationGroupId)
        {
            this.SetOperationGroupIdForEvent(eventInfo, machine, ref operationGroupId);

            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            base.Logger.OnMachineEvent(machine.Id, machine.CurrentStateName, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            base.Logger.OnMonitorEvent(monitor.GetType().Name, monitor.Id, monitor.CurrentStateName,
                eventInfo.EventName, isProcessing:false);
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            // The machine inherits the operation group id of the dequeued event.
            machine.Info.OperationGroupId = eventInfo.OperationGroupId;

            base.Logger.OnDequeue(machine.Id, machine.CurrentStateName, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        internal override void NotifyWaitEvents(Machine machine, EventInfo eventInfoInInbox)
        {
            if (eventInfoInInbox == null)
            {
                base.Logger.OnWait(machine.Id, machine.CurrentStateName, string.Empty);
                machine.Info.IsWaitingToReceive = true;
            }
        }

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            base.Logger.OnReceive(machine.Id, machine.CurrentStateName, eventInfo.EventName, wasBlocked:true);

            lock (machine)
            {
                System.Threading.Monitor.Pulse(machine);
                machine.Info.IsWaitingToReceive = false;
            }
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="inboxSize">Current size of the machine inbox.</param>
        internal override void NotifyHalted(Machine machine, int inboxSize)
        {
            base.Logger.OnHalt(machine.Id, inboxSize);
            this.MachineMap.TryRemove(machine.Id.Value, out machine);
        }

        #endregion

        #region cleanup

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public override void Dispose()
        {
            this.Monitors.Clear();
            this.MachineMap.Clear();
            base.Dispose();
        }

        #endregion
    }
}
