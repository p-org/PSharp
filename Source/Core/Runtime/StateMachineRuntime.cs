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
using System.Linq;
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

        /// <summary>
        /// Map from unique machine ids to machines.
        /// </summary>
        private ConcurrentDictionary<ulong, Machine> MachineMap;

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
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, Event e = null)
        {
            return this.CreateMachine(type, null, e, null);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, string friendlyName, Event e = null)
        {
            return this.CreateMachine(type, friendlyName, e, null);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null)
        {
            return this.CreateMachineAndExecute(type, null, e, null);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null)
        {
            return this.CreateMachineAndExecute(type, friendlyName, e, null);
        }

        /// <summary>
        /// Creates a new remote machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null)
        {
            return this.CreateRemoteMachine(type, null, endpoint, e, null);
        }

        /// <summary>
        /// Creates a new remote machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e = null)
        {
            return this.CreateRemoteMachine(type, friendlyName, endpoint, e, null);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void SendEvent(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            base.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            base.Assert(e != null, "Cannot send a null event.");
            this.SendEvent(target, e, null);
        }

        /// <summary>
        /// Synchronously delivers an <see cref="Event"/> to a machine
        /// and executes it if the machine is available.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override Task SendEventAndExecute(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            base.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            base.Assert(e != null, "Cannot send a null event.");
            return this.SendEventAndExecute(target, e, null);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void RemoteSendEvent(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            base.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            base.Assert(e != null, "Cannot send a null event.");
            this.SendEventRemotely(target, e, null);
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
            // If the event is null then report an error and exit.
            base.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor<T>(null, e);
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
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId CreateMachine(Type type, string friendlyName, Event e, Machine creator)
        {
            Machine machine = this.CreateMachine(type, friendlyName);
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
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override async Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e, Machine creator)
        {
            Machine machine = this.CreateMachine(type, friendlyName);
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
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator)
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

            base.Log($"<CreateLog> Machine '{mid}' is created.");

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender)
        {
            Machine machine = null;
            if (!this.MachineMap.TryGetValue(mid.Value, out machine))
            {
                if (sender != null)
                {
                    base.Log($"<SendLog> Machine '{sender.Id}' sent event '{e.GetType().FullName}' to a halted machine '{mid}'.");
                }
                else
                {
                    base.Log($"<SendLog> The event '{e.GetType().FullName}' was sent to a halted machine '{mid}'.");
                }

                return;
            }

            bool runNewHandler = false;
            this.EnqueueEvent(machine, e, sender, ref runNewHandler);
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
        internal override async Task SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender)
        {
            Machine machine = null;
            if (!this.MachineMap.TryGetValue(mid.Value, out machine))
            {
                if (sender != null)
                {
                    base.Log($"<SendLog> Machine '{sender.Id}' sent event '{e.GetType().FullName}' to a halted machine '{mid}'.");
                }
                else
                {
                    base.Log($"<SendLog> The event '{e.GetType().FullName}' was sent to a halted machine '{mid}'.");
                }

                return;
            }

            bool runNewHandler = false;
            this.EnqueueEvent(machine, e, sender, ref runNewHandler);
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
        internal override void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender)
        {
            base.NetworkProvider.RemoteSend(mid, e);
        }

        /// <summary>
        /// Enqueues an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="runNewHandler">Run a new handler</param>
        private void EnqueueEvent(Machine machine, Event e, AbstractMachine sender, ref bool runNewHandler)
        {
            EventInfo eventInfo = new EventInfo(e, null);

            if (sender != null)
            {
                base.Log($"<SendLog> Machine '{sender.Id}' sent event '{eventInfo.EventName}' to '{machine.Id}'.");
            }
            else
            {
                base.Log($"<SendLog> Event '{eventInfo.EventName}' was sent to '{machine.Id}'.");
            }

            machine.Enqueue(eventInfo, ref runNewHandler);
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="isFresh">Is a new machine</param>
        private void RunMachineEventHandler(Machine machine, Event e, bool isFresh)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await machine.GotoStartState(e);
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
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="isFresh">Is a new machine</param>
        private async Task RunMachineEventHandlerAsync(Machine machine, Event e, bool isFresh)
        {
            try
            {
                if (isFresh)
                {
                    await machine.GotoStartState(e);
                }

                await machine.RunEventHandler(true);
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

            RunMachineEventHandler(machine, null, false);
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
            Object monitor = Activator.CreateInstance(type);

            (monitor as Monitor).Initialize(mid);
            (monitor as Monitor).InitializeStateInformation();

            lock (this.Monitors)
            {
                this.Monitors.Add(monitor as Monitor);
            }

            base.Log($"<CreateLog> Monitor '{type.Name}' is created.");

            (monitor as Monitor).GotoStartState();
        }

        /// <summary>
        /// Invokes the specified <see cref="PSharp.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal override void Monitor<T>(AbstractMachine sender, Event e)
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
                    if (m.GetType() == typeof(T))
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

            if (machine != null)
            {
                base.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{result}'.");
            }
            else
            {
                base.Log($"<RandomLog> Runtime nondeterministically chose '{result}'.");
            }

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

            if (machine != null)
            {
                base.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{result}'.");
            }
            else
            {
                base.Log($"<RandomLog> Runtime nondeterministically chose '{result}'.");
            }

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

            base.Log($"<StateLog> Machine '{machine.Id}' enters state '{machine.CurrentStateName}'.");
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

            string liveness = "";
            string monitorState = monitor.CurrentStateNameWithTemperature;

            if (monitor.IsInHotState())
            {
                liveness = "'hot' ";
            }
            else if (monitor.IsInColdState())
            {
                liveness = "'cold' ";
            }

            base.Log($"<MonitorLog> Monitor '{monitor.GetType().Name}' " +
                $"enters {liveness}state '{monitorState}'.");
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

            base.Log($"<StateLog> Machine '{machine.Id}' exits state '{machine.CurrentStateName}'.");
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

            string liveness = "";
            string monitorState = monitor.CurrentStateName;

            if (monitor.IsInHotState())
            {
                liveness = "'hot' ";
                monitorState += "[hot]";
            }
            else if (monitor.IsInColdState())
            {
                liveness = "'cold' ";
                monitorState += "[cold]";
            }

            base.Log($"<MonitorLog> Monitor '{monitor.GetType().Name}' " +
                $"exits {liveness}state '{monitorState}'.");
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

            base.Log($"<ActionLog> Machine '{machine.Id}' invoked action " +
                $"'{action.Name}' in state '{machine.CurrentStateName}'.");
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

            base.Log($"<MonitorLog> Monitor '{monitor.GetType().Name}' executed " +
                $"action '{action.Name}' in state '{monitor.CurrentStateName}'.");
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            base.Log($"<RaiseLog> Machine '{machine.Id}' raised event '{eventInfo.EventName}'.");
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

            base.Log($"<MonitorLog> Monitor '{monitor.GetType().Name}' raised " +
                $"event '{eventInfo.EventName}'.");
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            base.Log($"<DequeueLog> Machine '{machine.Id}' dequeued " +
                $"event '{eventInfo.EventName}'.");
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyWaitEvents(Machine machine)
        {
            base.Log($"<ReceiveLog> Machine '{machine.Id}' is waiting to receive an event.");
            machine.Info.IsWaitingToReceive = true;
        }

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            base.Log($"<ReceiveLog> Machine '{machine.Id}' received " +
                $"event '{eventInfo.EventName}' and unblocked.");

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
        internal override void NotifyHalted(Machine machine)
        {
            base.Log($"<HaltLog> Machine '{machine.Id}' halted.");
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
