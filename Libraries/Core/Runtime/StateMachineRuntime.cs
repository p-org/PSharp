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

        /// <summary>
        /// Map from task ids to machines.
        /// </summary>
        private ConcurrentDictionary<int, Machine> TaskMap;

        /// <summary>
        /// Collection of machine tasks.
        /// </summary>
        private ConcurrentBag<Task> MachineTasks;

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
            this.TaskMap = new ConcurrentDictionary<int, Machine>();
            this.MachineTasks = new ConcurrentBag<Task>();
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
            return this.TryCreateMachine(type, null, e, null, false);
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
            return this.TryCreateMachine(type, friendlyName, e, null, false);
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
        public override MachineId CreateMachineAndExecute(Type type, Event e = null)
        {
            return this.TryCreateMachine(type, null, e, null, true);
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
        public override MachineId CreateMachineAndExecute(Type type, string friendlyName, Event e = null)
        {
            return this.TryCreateMachine(type, friendlyName, e, null, true);
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
            return this.TryCreateRemoteMachine(type, null, endpoint, e, null);
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
            return this.TryCreateRemoteMachine(type, friendlyName, endpoint, e, null);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void SendEvent(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.Send(target, e, null, false, false);
        }

        /// <summary>
        /// Synchronously delivers an <see cref="Event"/> to a machine
        /// and executes it if the machine is available.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void SendEventAndExecute(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.Send(target, e, null, true, false);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void RemoteSendEvent(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.SendRemotely(target, e, null, false);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        /// <returns>Received event</returns>
        public override Event Receive(params Type[] eventTypes)
        {
            this.Assert(Task.CurrentId != null, "Only machines can " +
                "wait to receive an event.");
            this.Assert(this.TaskMap.ContainsKey((int)Task.CurrentId),
                "Only machines can wait to receive an event; task " +
                $"{(int)Task.CurrentId} does not correspond to a machine.");

            Machine machine = this.TaskMap[(int)Task.CurrentId];
            return machine.Receive(eventTypes);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified type
        /// that satisfies the specified predicate.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="predicate">Predicate</param>
        /// <returns>Received event</returns>
        public override Event Receive(Type eventType, Func<Event, bool> predicate)
        {
            this.Assert(Task.CurrentId != null, "Only machines can " +
                "wait to receive an event.");
            this.Assert(this.TaskMap.ContainsKey((int)Task.CurrentId),
                "Only machines can wait to receive an event; task " +
                $"{(int)Task.CurrentId} does not belong to a machine.");

            Machine machine = this.TaskMap[(int)Task.CurrentId];
            return machine.Receive(eventType, predicate);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types
        /// that satisfy the specified predicates.
        /// </summary>
        /// <param name="events">Event types and predicates</param>
        /// <returns>Received event</returns>
        public override Event Receive(params Tuple<Type, Func<Event, bool>>[] events)
        {
            this.Assert(Task.CurrentId != null, "Only machines can " +
                "wait to receive an event.");
            this.Assert(this.TaskMap.ContainsKey((int)Task.CurrentId),
                "Only machines can wait to receive an event; task " +
                $"{(int)Task.CurrentId} does not belong to a machine.");

            Machine machine = this.TaskMap[(int)Task.CurrentId];
            return machine.Receive(events);
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
            this.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor<T>(null, e);
        }

        /// <summary>
        /// Gets the id of the currently executing <see cref="Machine"/>.
        /// <returns>MachineId</returns>
        /// </summary>
        public override MachineId GetCurrentMachineId()
        {
            if (Task.CurrentId == null || !this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                return null;
            }
            Machine machine = this.TaskMap[(int)Task.CurrentId];
            return machine.Id;
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        public override void Wait()
        {
            Task[] taskArray = null;

            while (true)
            {
                taskArray = this.MachineTasks.ToArray();

                try
                {
                    Task.WaitAll(taskArray);
                }
                catch (AggregateException)
                {
                    this.MachineTasks = new ConcurrentBag<Task>(
                        this.MachineTasks.Where(val => !val.IsCompleted));

                    continue;
                }

                if (taskArray.Length == this.MachineTasks.Count)
                {
                    break;
                }
            }
        }

        #endregion

        #region state-machine execution

        /// <summary>
        /// Tries to create a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <param name="executeSynchronously">If true, this operation executes synchronously</param> 
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateMachine(Type type, string friendlyName, Event e,
            Machine creator, bool executeSynchronously)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), $"Type '{type.Name}' is not a machine.");

            MachineId mid = new MachineId(type, friendlyName, this);
            Machine machine = MachineFactory.Create(type);

            machine.SetMachineId(mid);
            machine.InitializeStateInformation();
            
            bool result = this.MachineMap.TryAdd(mid.Value, machine);
            this.Assert(result, $"Machine '{mid}' was already created.");

            this.Log($"<CreateLog> Machine '{mid}' is created.");

            if (executeSynchronously)
            {
                this.RunMachineEventHandler(machine, e, true);
            }
            else
            {
                this.RunMachineEventHandlerAsync(machine, e, true);
            }

            return mid;
        }

        /// <summary>
        /// Tries to create a new remote <see cref="Machine"/> of the specified <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)),
                $"Type '{type.Name}' is not a machine.");
            return base.NetworkProvider.RemoteCreateMachine(type, friendlyName, endpoint, e);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="executeSynchronously">If true, this operation executes synchronously</param> 
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void Send(MachineId mid, Event e, AbstractMachine sender, bool executeSynchronously, bool isStarter)
        {
            EventInfo eventInfo = new EventInfo(e, null);

            Machine machine = null;
            if (!this.MachineMap.TryGetValue(mid.Value, out machine))
            {
                return;
            }

            if (sender != null)
            {
                this.Log($"<SendLog> Machine '{sender.Id}' sent event " +
                    $"'{eventInfo.EventName}' to '{mid}'.");
            }
            else
            {
                this.Log($"<SendLog> Event '{eventInfo.EventName}' was sent to '{mid}'.");
            }

            bool runNewHandler = false;
            machine.Enqueue(eventInfo, ref runNewHandler);
            if (runNewHandler)
            {
                if (executeSynchronously)
                {
                    this.RunMachineEventHandler(machine, null, false);
                }
                else
                {
                    this.RunMachineEventHandlerAsync(machine, null, false);
                }
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">If true, the send is starting a new operation</param>
        internal override void SendRemotely(MachineId mid, Event e, AbstractMachine sender, bool isStarter)
        {
            base.NetworkProvider.RemoteSend(mid, e);
        }

        /// <summary>
        /// Runs a new synchronous machine event handler.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="isFresh">Is a new machine</param>
        private void RunMachineEventHandler(Machine machine, Event e, bool isFresh)
        {
            try
            {
                if (isFresh)
                {
                    machine.GotoStartState(e);
                }

                machine.RunEventHandler();
            }
            catch (Exception ex)
            {
                base.IsFaulted = true;
                base.RaiseOnFailureEvent(ex);
            }
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="isFresh">Is a new machine</param>
        private void RunMachineEventHandlerAsync(Machine machine, Event e, bool isFresh)
        {

            Task task = new Task(() =>
            {
                try
                {
                    if (isFresh)
                    {
                        machine.GotoStartState(e);
                    }

                    machine.RunEventHandler();
                }
                catch (Exception ex)
                {
                    base.IsFaulted = true;
                    base.RaiseOnFailureEvent(ex);
                }
                finally
                {
                    Machine m;
                    this.TaskMap.TryRemove(Task.CurrentId.Value, out m);
                }
            });

            this.MachineTasks.Add(task);
            this.TaskMap.TryAdd(task.Id, machine);

            task.Start();
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

            this.Assert(type.IsSubclassOf(typeof(Monitor)), $"Type '{type.Name}' " +
                "is not a subclass of Monitor.\n");

            MachineId mid = new MachineId(type, null, this);
            Object monitor = Activator.CreateInstance(type);
            (monitor as Monitor).SetMachineId(mid);
            (monitor as Monitor).InitializeStateInformation();

            lock (this.Monitors)
            {
                this.Monitors.Add(monitor as Monitor);
            }

            this.Log($"<CreateLog> Monitor '{type.Name}' is created.");

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
                this.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{result}'.");
            }
            else
            {
                this.Log($"<RandomLog> Runtime nondeterministically chose '{result}'.");
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
                this.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{result}'.");
            }
            else
            {
                this.Log($"<RandomLog> Runtime nondeterministically chose '{result}'.");
            }

            return result;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal override void NotifyEnteredState(AbstractMachine machine)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;

                this.Log($"<StateLog> Machine '{machine.Id}' enters " +
                    $"state '{machineState}'.");
            }
            else if (machine is Monitor)
            {
                string liveness = "";
                string monitorState = (machine as Monitor).CurrentStateNameWithTemperature;

                if ((machine as Monitor).IsInHotState())
                {
                    liveness = "'hot' ";
                }
                else if ((machine as Monitor).IsInColdState())
                {
                    liveness = "'cold' ";
                }

                this.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' " +
                    $"enters {liveness}state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal override void NotifyExitedState(AbstractMachine machine)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;

                this.Log($"<StateLog> Machine '{machine.Id}' exits " +
                    $"state '{machineState}'.");
            }
            else if (machine is Monitor)
            {
                string liveness = "";
                string monitorState = (machine as Monitor).CurrentStateName;

                if ((machine as Monitor).IsInHotState())
                {
                    liveness = "'hot' ";
                    monitorState += "[hot]";
                }
                else if ((machine as Monitor).IsInColdState())
                {
                    liveness = "'cold' ";
                    monitorState += "[cold]";
                }

                this.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' " +
                    $"exits {liveness}state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        internal override void NotifyInvokedAction(AbstractMachine machine, MethodInfo action, Event receivedEvent)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;

                this.Log($"<ActionLog> Machine '{machine.Id}' invoked action " +
                    $"'{action.Name}' in state '{machineState}'.");
            }
            else if (machine is Monitor)
            {
                string monitorState = (machine as Monitor).CurrentStateName;

                this.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' executed " +
                    $"action '{action.Name}' in state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            this.Log($"<DequeueLog> Machine '{machine.Id}' dequeued " +
                $"event '{eventInfo.EventName}'.");
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void NotifyRaisedEvent(AbstractMachine machine, EventInfo eventInfo, bool isStarter)
        {
            if (base.Configuration.Verbose <= 1)
            {
                return;
            }

            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;
                this.Log($"<RaiseLog> Machine '{machine.Id}' raised " +
                    $"event '{eventInfo.EventName}'.");
            }
            else if (machine is Monitor)
            {
                string monitorState = (machine as Monitor).CurrentStateName;

                this.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' raised " +
                    $"event '{eventInfo.EventName}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="events">Events</param>
        internal override void NotifyWaitEvents(Machine machine, string events)
        {
            this.Log($"<ReceiveLog> Machine '{machine.Id}' " +
                $"is waiting on events:{events}.");

            lock (machine)
            {
                while (machine.IsWaitingToReceive)
                {
                    System.Threading.Monitor.Wait(machine);
                }
            }
        }

        /// <summary>
        /// Notifies that a machine received an <see cref="Event"/> that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            this.Log($"<ReceiveLog> Machine '{machine.Id}' received " +
                $"event '{eventInfo.EventName}' and unblocked.");

            lock (machine)
            {
                System.Threading.Monitor.Pulse(machine);
                machine.IsWaitingToReceive = false;
            }
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyHalted(Machine machine)
        {
            this.Log($"<HaltLog> Machine '{machine.Id}' halted.");
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
            this.TaskMap.Clear();
            base.Dispose();
        }

        #endregion
    }
}
