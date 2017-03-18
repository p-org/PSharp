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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Net;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class implementing the P# runtime.
    /// </summary>
    public class PSharpRuntime
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        internal Configuration Configuration;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        protected List<Monitor> Monitors;

        /// <summary>
        /// Map from unique machine ids to machines.
        /// </summary>
        protected ConcurrentDictionary<ulong, Machine> MachineMap;

        /// <summary>
        /// Map from task ids to machines.
        /// </summary>
        protected ConcurrentDictionary<int, Machine> TaskMap;

        /// <summary>
        /// Collection of machine tasks.
        /// </summary>
        protected ConcurrentBag<Task> MachineTasks;

        /// <summary>
        /// Network provider for remote communication.
        /// </summary>
        internal INetworkProvider NetworkProvider;

        /// <summary>
        /// Records if the P# program has faulted.
        /// </summary>
        internal volatile bool IsFaulted;

        #endregion

        #region properties

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

        #region public methods

        /// <summary>
        /// Creates a new P# runtime.
        /// </summary>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create()
        {
            return new PSharpRuntime();
        }

        /// <summary>
        /// Creates a new P# runtime.
        /// </summary>
        /// <param name="netProvider">NetworkProvider</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(INetworkProvider netProvider)
        {
            return new PSharpRuntime(netProvider);
        }

        /// <summary>
        /// Creates a new P# runtime.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(Configuration configuration)
        {
            return new PSharpRuntime(configuration);
        }

        /// <summary>
        /// Creates a new P# runtime.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="netProvider">NetworkProvider</param>
        /// <returns>PSharpRuntime</returns>
        public static PSharpRuntime Create(Configuration configuration, INetworkProvider netProvider)
        {
            return new PSharpRuntime(configuration, netProvider);
        }

        /// <summary>
        /// Creates a new machine of the specified type and with
        /// the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public virtual MachineId CreateMachine(Type type, Event e = null)
        {
            return this.TryCreateMachine(null, type, null, e);
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and
        /// with the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public virtual MachineId CreateMachine(Type type, string friendlyName, Event e = null)
        {
            return this.TryCreateMachine(null, type, friendlyName, e);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and with
        /// the specified optional event. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public virtual MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null)
        {
            return this.TryCreateRemoteMachine(null, type, null, endpoint, e);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and name, and
        /// with the specified optional event. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public virtual MachineId RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e = null)
        {
            return this.TryCreateRemoteMachine(null, type, friendlyName, endpoint, e);
        }

        /// <summary>
        /// Gets the id of the currently executing machine. Returns null if none.
        /// <returns>MachineId</returns>
        /// </summary>
        public virtual MachineId GetCurrentMachineId()
        {
            if(Task.CurrentId == null || !this.TaskMap.ContainsKey((int) Task.CurrentId))
            {
                return null;
            }
            Machine machine = this.TaskMap[(int)Task.CurrentId];
            return machine.Id;
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public virtual void SendEvent(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.Send(null, target, e, false);
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public virtual void RemoteSendEvent(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.SendRemotely(null, target, e, false);
        }

        /// <summary>
        /// Blocks and waits to receive an event of the specified types.
        /// Returns the received event.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        /// <returns>Received event</returns>
        public virtual Event Receive(params Type[] eventTypes)
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
        /// Blocks and waits to receive an event of the specified types that satisfies
        /// the specified predicate. Returns the received event.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="predicate">Predicate</param>
        /// <returns>Received event</returns>
        public virtual Event Receive(Type eventType, Func<Event, bool> predicate)
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
        /// Blocks and waits to receive an event of the specified types that satisfy
        /// the specified predicates. Returns the received event.
        /// </summary>
        /// <param name="events">Event types and predicates</param>
        /// <returns>Received event</returns>
        public virtual Event Receive(params Tuple<Type, Func<Event, bool>>[] events)
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
        /// Registers a new specification monitor of the specified type.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        public virtual void RegisterMonitor(Type type)
        {
            this.TryCreateMonitor(type);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        public virtual void InvokeMonitor<T>(Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor<T>(null, e);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        public virtual bool Random()
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
        public virtual bool Random(int maxValue)
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
        public virtual int RandomInteger(int maxValue)
        {
            return this.GetNondeterministicIntegerChoice(null, maxValue);
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        public void Wait()
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

        #region initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        protected PSharpRuntime()
        {
            this.Configuration = Configuration.Create();
            this.NetworkProvider = new DefaultNetworkProvider(this);
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="netProvider">NetworkProvider</param>
        protected PSharpRuntime(INetworkProvider netProvider)
        {
            this.Configuration = Configuration.Create();
            this.NetworkProvider = netProvider;
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        protected PSharpRuntime(Configuration configuration)
        {
            this.Configuration = configuration;
            this.NetworkProvider = new DefaultNetworkProvider(this);
            this.Initialize();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="netProvider">NetworkProvider</param>
        protected PSharpRuntime(Configuration configuration, INetworkProvider netProvider)
        {
            this.Configuration = configuration;
            this.NetworkProvider = netProvider;
            this.Initialize();
        }

        /// <summary>
        /// Initializes various components of the runtime.
        /// </summary>
        private void Initialize()
        {
            this.Logger = new DefaultLogger();
            this.MachineMap = new ConcurrentDictionary<ulong, Machine>();
            this.TaskMap = new ConcurrentDictionary<int, Machine>();
            this.MachineTasks = new ConcurrentBag<Task>();
            this.Monitors = new List<Monitor>();
            this.IsFaulted = false;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Gets the currently executing machine.
        /// </summary>
        /// <returns>Machine or null, if not present</returns>
        internal virtual Machine GetCurrentMachine()
        {
            //  The current task does not correspond to a machine.
            if (Task.CurrentId == null)
            {
                return null;
            }

            // The current task does not correspond to a machine.
            if (!this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                return null;
            }

            return this.TaskMap[(int)Task.CurrentId];
        }

        /// <summary>
        /// Tries to create a new machine of the specified type.
        /// </summary>
        /// <param name="creator">Creator machine</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        internal virtual MachineId TryCreateMachine(Machine creator, Type type, string friendlyName, Event e)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), $"Type '{type.Name}' is not a machine.");

            MachineId mid = new MachineId(type, friendlyName, this);
            Machine machine = MachineFactory.Create(type);

            machine.SetMachineId(mid);
            machine.InitializeStateInformation();
            
            bool result = this.MachineMap.TryAdd(mid.Value, machine);
            this.Assert(result, $"Machine '{mid}' was already created.");

            this.Log($"<CreateLog> Machine '{mid}' is created.");

            this.RunMachineEventHandler(machine, e, true);

            return mid;
        }

        /// <summary>
        /// Tries to create a new remote machine of the specified type.
        /// </summary>
        /// <param name="creator">Creator machine</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        internal virtual MachineId TryCreateRemoteMachine(Machine creator, Type type,
            string friendlyName, string endpoint, Event e)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)),
                $"Type '{type.Name}' is not a machine.");
            return this.NetworkProvider.RemoteCreateMachine(type, friendlyName, endpoint, e);
        }

        /// <summary>
        /// Tries to create a new monitor of the specified type.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal virtual void TryCreateMonitor(Type type)
        {
            if(!this.Configuration.EnableMonitorsInProduction)
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
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal virtual void Send(AbstractMachine sender, MachineId mid, Event e, bool isStarter)
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
                this.RunMachineEventHandler(machine);
            }
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal virtual void SendRemotely(AbstractMachine sender, MachineId mid, Event e, bool isStarter)
        {
            this.NetworkProvider.RemoteSend(mid, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal virtual void Monitor<T>(AbstractMachine sender, Event e)
        {
            if (!this.Configuration.EnableMonitorsInProduction)
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

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        internal virtual bool GetNondeterministicBooleanChoice(
            AbstractMachine machine, int maxValue)
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
        internal virtual bool GetFairNondeterministicBooleanChoice(
            AbstractMachine machine, string uniqueId)
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
        internal virtual int GetNondeterministicIntegerChoice(
            AbstractMachine machine, int maxValue)
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

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal virtual void NotifyEnteredState(AbstractMachine machine)
        {
            // No-op in production, except for logging.
            if (this.Configuration.Verbose <= 1)
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
        internal virtual void NotifyExitedState(AbstractMachine machine)
        {
            // No-op in production, except for logging.
            if (this.Configuration.Verbose <= 1)
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
        internal virtual void NotifyInvokedAction(AbstractMachine machine, MethodInfo action, Event receivedEvent)
        {
            // No-op in production, except for logging.
            if (this.Configuration.Verbose <= 1)
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
        /// Notifies that a machine dequeued an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal virtual void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            // No-op in production, except for logging.
            this.Log($"<DequeueLog> Machine '{machine.Id}' dequeued " +
                $"event '{eventInfo.EventName}'.");
        }

        /// <summary>
        /// Notifies that a machine called Pop.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="fromState">Top of the stack state</param>
        /// <param name="toState">Next to top state of the stack</param>
        internal virtual void NotifyPop(Machine machine, Type fromState, Type toState)
        {
            // No-op in production.
        }

        /// <summary>
        /// Notifies that a machine raised an event.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal virtual void NotifyRaisedEvent(AbstractMachine machine, EventInfo eventInfo, bool isStarter)
        {
            // No-op in production, except for logging.
            if (this.Configuration.Verbose <= 1)
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
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal virtual void NotifyReceiveCalled(AbstractMachine machine)
        {
            // No-op in production.
        }

        /// <summary>
        /// Notifies that a machine handles a raised event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal virtual void NotifyHandleRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            // No-op in production.
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one
        /// or more events.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="events">Events</param>
        internal virtual void NotifyWaitEvents(Machine machine, string events)
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
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal virtual void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
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
        internal virtual void NotifyHalted(Machine machine)
        {
            this.Log($"<HaltLog> Machine '{machine.Id}' halted.");
            this.MachineMap.TryRemove(machine.Id.Value, out machine);
        }

        /// <summary>
        /// Notifies that a default handler has been used.
        /// </summary>
        internal virtual void NotifyDefaultHandlerFired()
        {
            // No-op in production.
        }

        #endregion

        #region error checking

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
                string message = IO.Utilities.Format(s, args);
                throw new AssertionFailureException(message);
            }
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
            string message = IO.Utilities.Format(s, args);
            throw new AssertionFailureException(message, exception);
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
        /// Installs the specified logger.
        /// </summary>
        /// <param name="logger">TextWriter</param>
        public void SetLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new InvalidOperationException("Cannot install a null logger.");
            }

            this.Logger.Dispose();
            this.Logger = logger;
        }

        /// <summary>
        /// Replaces the currently installed logger with the default logger.
        /// </summary>
        public void RemoveLogger()
        {
            this.Logger.Dispose();
            this.Logger = new DefaultLogger();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="isFresh">Is a new machine</param>
        private void RunMachineEventHandler(Machine machine, Event e = null, bool isFresh = false)
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
                    this.IsFaulted = true;
                    this.OnFailure?.Invoke(ex);
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
    }
}
