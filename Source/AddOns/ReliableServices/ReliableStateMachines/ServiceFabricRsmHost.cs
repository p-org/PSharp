using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace Microsoft.PSharp.ReliableServices
{
    /// <summary>
    /// Object hosting an RSM
    /// </summary>
    public sealed class ServiceFabricRsmHost : RsmHost
    {
        /// <summary>
        /// Persistent current state (stack)
        /// </summary>
        private IReliableDictionary<int, string> StateStackStore;

        /// <summary>
        /// Inbox
        /// </summary>
        private IReliableConcurrentQueue<Event> InputQueue;

        /// <summary>
        /// RSMs created by this host
        /// TODO: Remove entries on HALT
        /// </summary>
        private IReliableDictionary<IRsmId, Tuple<string, RsmInitEvent>> CreatedMachines;

        /// <summary>
        /// Remote RSMs created by this host
        /// </summary>
        private IReliableDictionary<IRsmId, Tuple<string, RsmInitEvent>> RemoteCreatedMachines;

        /// <summary>
        /// Remote messages to be sent
        /// </summary>
        private IReliableQueue<Tuple<IRsmId, Event>> RemoteMessages;

        /// <summary>
        /// Machines that haven't been started yet
        /// </summary>
        private Dictionary<IRsmId, Tuple<string, RsmInitEvent>> PendingMachineCreations;

        /// <summary>
        /// Machines that haven't been started yet
        /// </summary>
        private Queue<Tuple<IRsmId, Event>> PendingMessages;

        /// <summary>
        /// Counters for reliable remote send
        /// </summary>
        private IReliableDictionary<string, int> SendCounters;

        /// <summary>
        /// Counters for reliable remote receive
        /// </summary>
        private IReliableDictionary<string, int> ReceiveCounters;

        /// <summary>
        /// Queue of timeouts
        /// </summary>
        private LinkedList<string> TimeoutQueue;

        /// <summary>
        /// For creating unique RsmIds
        /// </summary>
        private ServiceFabricRsmIdFactory IdFactory;

        /// <summary>
        /// Has the machine halted
        /// </summary>
        private bool MachineHalted;

        /// <summary>
        /// Machine failed with an exception
        /// </summary>
        private Exception MachineFailureException;

        /// <summary>
        /// P# runtime config
        /// </summary>
        private Configuration PSharpRuntimeConfiguration;

        private ServiceFabricRsmHost(IReliableStateManager stateManager, ServiceFabricRsmId id, ServiceFabricRsmIdFactory factory, Net.IRsmNetworkProvider netProvider, Configuration config)
            : base(stateManager)
        {
            this.Id = id;
            this.IdFactory = factory;
            this.CreatedMachines = null;
            this.PendingMachineCreations = new Dictionary<IRsmId, Tuple<string, RsmInitEvent>>();
            this.PSharpRuntimeConfiguration = config;
            this.PendingMessages = new Queue<Tuple<IRsmId, Event>>();

            MachineHalted = false;
            MachineFailureException = null;

            this.TimeoutQueue = new LinkedList<string>();

            this.NetworkProvider = netProvider;
        }

        internal static RsmHost Create(IReliableStateManager stateManager, ServiceFabricRsmIdFactory factory, Net.IRsmNetworkProvider netProvider, Configuration config)
        {
            var id = factory.Generate("Root");
            return new ServiceFabricRsmHost(stateManager, id, factory, netProvider, config);
        }

        /// <summary>
        /// Create a host with the specified partition name
        /// </summary>
        /// <param name="partition">Partition Name</param>
        /// <returns>Host</returns>
        internal override RsmHost CreateHost(string partition)
        {
            var factory = new ServiceFabricRsmIdFactory(0, partition);
            return new ServiceFabricRsmHost(this.StateManager, factory.Generate("Root"), factory, NetworkProvider, this.Runtime.Configuration);
        }

        private async Task Initialize(Type machineType, RsmInitEvent ev)
        {
            InputQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Event>>(GetInputQueueName(this.Id));
            StateStackStore = await StateManager.GetOrAddAsync<IReliableDictionary<int, string>>(string.Format("StateStackStore.{0}", this.Id.Name));
            Timers = await StateManager.GetOrAddAsync<IReliableDictionary<string, Timers.ReliableTimerConfig>>(string.Format("Timers.{0}", this.Id.Name));
            SendCounters = await StateManager.GetOrAddAsync<IReliableDictionary<string, int>>(string.Format("SendCounters.{0}", this.Id.Name));
            ReceiveCounters = await StateManager.GetOrAddAsync<IReliableDictionary<string, int>>(string.Format("ReceiveCounters.{0}", this.Id.Name));
            RemoteMessages = await StateManager.GetOrAddAsync<IReliableQueue<Tuple<IRsmId, Event>>>(string.Format("RemoteMessages.{0}", this.Id.Name));

            Runtime = PSharpRuntime.Create(PSharpRuntimeConfiguration);
            MachineHosted = true;

            RunMachine(machineType, ev);
        }

        private void RunMachine(Type machineType, RsmInitEvent ev)
        {
            // Attached child task: propagates exception to
            // its parent
            Task.Factory.StartNew(async () =>
            {
                bool firstExecution = true;

                while (true)
                {

                    if(!Runtime.IsRunning)
                    {
                        // TODO: Replace with a custom exception
                        throw new Exception("Unexpected failure of the P# runtime");
                    }

                    await EventHandlerLoop(machineType, ev, firstExecution);
                    firstExecution = false;

                    if (MachineHalted)
                    {
                        return;
                    }

                    // Inbox empty, wait
                    await Task.Delay(100);
                }
            }, TaskCreationOptions.AttachedToParent);
        }

        private async Task InitializationTransaction(Type machineType, RsmInitEvent ev)
        {
            var stack = new List<string>();

            var cnt = await StateStackStore.GetCountAsync(CurrentTransaction);
            if (cnt != 0)
            {
                for (int i = 0; i < cnt; i++)
                {
                    var s = await StateStackStore.TryGetValueAsync(CurrentTransaction, i);
                    stack.Add(s.Value);
                }

                this.Mid = await Runtime.CreateMachineAndExecute(machineType, new ResumeEvent(stack, new RsmInitEvent(this)));
            }
            else
            {
                ev.Host = this;
                this.Mid = await Runtime.CreateMachineAndExecute(machineType, ev);
            }

            if (MachineFailureException != null)
            {
                throw MachineFailureException;
            }

        }

        private async Task EventHandlerLoop(Type machineType, RsmInitEvent ev, bool firstExecution)
        {
            var machineRestartRequired = firstExecution;

            // TODO: retry policy
            while (!MachineHalted && Runtime.IsRunning)
            {
                try
                {
                    var writeTx = false;
                    var dequeued = true;

                    using (CurrentTransaction = StateManager.CreateTransaction())
                    {
                        SetReliableRegisterTx();

                        if(firstExecution)
                        {
                            await ReadPendingWorkOnStart();
                            writeTx = false;
                        }
                        else if (machineRestartRequired)
                        {
                            machineRestartRequired = false;
                            await InitializationTransaction(machineType, ev);
                            await PersistStateStack();
                            writeTx = true;
                        }
                        else
                        {
                            dequeued = await EventHandler();
                            var stackChanged = await PersistStateStack();
                            writeTx = (dequeued || stackChanged);
                        }

                        if (writeTx)
                        {
                            await CurrentTransaction.CommitAsync();
                        }
                    }

                    StackChanges = new StackDelta();
                    await ExecutePendingWork();
                    firstExecution = false;

                    if (!dequeued)
                    {
                        return;
                    }
                }
                catch (Exception ex) when (ex is TimeoutException || ex is System.Fabric.TransactionFaultedException)
                {
                    MachineFailureException = null;
                    MachineHalted = false;
                    machineRestartRequired = true;

                    StackChanges = new StackDelta();
                    PendingMachineCreations.Clear();
                    PendingTimerCreations.Clear();
                    PendingTimerRemovals.Clear();
                    PendingMessages.Clear();

                    // retry
                    await Task.Delay(100);
                    continue;
                }
            }
        }

        /// <summary>
        /// Returns true if dequeued
        /// </summary>
        /// <returns></returns>
        private async Task<bool> EventHandler()
        {
            Event ev = null;
            string lastTimer = null;

            // TODO: wakeup host (which sleeps when there is nothing to do) when
            // a timeout is delivered
            lock(TimeoutQueue)
            {
                if(TimeoutQueue.Count != 0)
                {
                    lastTimer = TimeoutQueue.Last();
                    ev = new Timers.TimeoutEvent(lastTimer);
                    TimeoutQueue.RemoveLast();
                }
            }

            if (ev == null)
            {
                var cv = await InputQueue.TryDequeueAsync(CurrentTransaction);
                if (!cv.HasValue)
                {
                    return false;
                }

                ev = cv.Value;

                if (ev is TaggedRemoteEvent)
                {
                    var tg = (ev as TaggedRemoteEvent);
                    var currentCounter = await ReceiveCounters.GetOrAddAsync(CurrentTransaction, tg.mid.Name, 0);
                    if (currentCounter == tg.tag - 1)
                    {
                        ev = tg.ev;
                        await ReceiveCounters.AddOrUpdateAsync(CurrentTransaction, tg.mid.Name, 0, (k, v) => tg.tag);
                    }
                    else
                    {
                        // drop the message and return
                        return true;
                    }
                }
            }

            await Runtime.SendEventAndExecute(Mid, ev);

            if (MachineFailureException != null)
            {
                if (lastTimer != null)
                {
                    lock (TimeoutQueue)
                    {
                        TimeoutQueue.AddLast(lastTimer);
                    }
                }

                throw MachineFailureException;
            }

            if(lastTimer != null && !PendingTimerRemovals.Contains(lastTimer))
            {
                // restart timer
                PendingTimerCreations.Add(lastTimer,
                    new Timers.ReliableTimerConfig(TimerObjects[lastTimer].Name, TimerObjects[lastTimer].TimePeriod));
                TimerObjects.Remove(lastTimer);
            }

            return true;
        }

        private async Task<bool> PersistStateStack()
        {
            if (StackChanges.PopDepth == 0 && StackChanges.PushedSuffix.Count == 0)
            {
                return false;
            }

            var cnt = (int) await StateStackStore.GetCountAsync(CurrentTransaction);
            for (int i = cnt - 1; i > cnt - 1 - StackChanges.PopDepth; i--)
            {
                await StateStackStore.TryRemoveAsync(CurrentTransaction, i);
            }

            for (int i = 0; i < StackChanges.PushedSuffix.Count; i++)
            {
                await StateStackStore.AddAsync(CurrentTransaction, i + (cnt - StackChanges.PopDepth), 
                    StackChanges.PushedSuffix[i]);
            }

            return true;
        }

        // TODO: This can probably be done in the background
        private async Task ExecutePendingWork()
        {
            var remoteMachines = new HashSet<IRsmId>();

            // machine creations
            foreach(var tup in PendingMachineCreations)
            {
                if (tup.Key.PartitionName == this.Id.PartitionName)
                {
                    var host = new ServiceFabricRsmHost(this.StateManager, tup.Key as ServiceFabricRsmId, this.IdFactory, NetworkProvider, PSharpRuntimeConfiguration);
                    await host.Initialize(Type.GetType(tup.Value.Item1), tup.Value.Item2);
                }
                else
                {
                    remoteMachines.Add(tup.Key);
                    await this.NetworkProvider.RemoteCreateMachine(Type.GetType(tup.Value.Item1), tup.Key, tup.Value.Item2);
                }
            }

            PendingMachineCreations.Clear();

            // remote send
            foreach(var tup in PendingMessages)
            {
                await this.NetworkProvider.RemoteSend(tup.Item1, tup.Item2);
            }

            PendingMessages.Clear();

            // timers
            foreach(var tup in PendingTimerCreations)
            {
                var timer = new Timers.ReliableTimerProd(this.TimeoutQueue, tup.Value.Period, tup.Value.Name);
                timer.StartTimer();
                TimerObjects.Add(tup.Key, timer);
            }

            PendingTimerCreations.Clear();

            foreach(var name in PendingTimerRemovals)
            {
                if(!TimerObjects.ContainsKey(name))
                {
                    continue;
                }

                TimerObjects[name].StopTimer();

                TimerObjects.Remove(name);
            }

            PendingTimerRemovals.Clear();

            await ClearOutBuffers(remoteMachines);
        }

        private async Task ClearOutBuffers(HashSet<IRsmId> remoteMachines)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                foreach (var id in remoteMachines)
                {
                    await RemoteCreatedMachines.TryRemoveAsync(tx, id);
                }

                while(await RemoteMessages.GetCountAsync(tx) > 0)
                {
                    await RemoteMessages.TryDequeueAsync(tx);
                }

                await tx.CommitAsync();
            }
        }

        private async Task ReadPendingWorkOnStart()
        {
            // Created machines
            CreatedMachines = await StateManager.GetOrAddAsync<IReliableDictionary<IRsmId, Tuple<string, RsmInitEvent>>>(
                GetCreatedMachineMapName(this.Id));

            var enumerable = await CreatedMachines.CreateEnumerableAsync(CurrentTransaction);
            var enumerator = enumerable.GetAsyncEnumerator();

            // TODO: Add cancellation token
            var ct = new System.Threading.CancellationToken();

            while (await enumerator.MoveNextAsync(ct))
            {
                PendingMachineCreations.Add(enumerator.Current.Key, enumerator.Current.Value);
            }

            // Pending remote creations
            RemoteCreatedMachines = await StateManager.GetOrAddAsync<IReliableDictionary<IRsmId, Tuple<string, RsmInitEvent>>>(
                GetRemoteCreatedMachineMapName(this.Id));

            enumerable = await RemoteCreatedMachines.CreateEnumerableAsync(CurrentTransaction);
            enumerator = enumerable.GetAsyncEnumerator();

            // TODO: Add cancellation token
            ct = new System.Threading.CancellationToken();

            while (await enumerator.MoveNextAsync(ct))
            {
                PendingMachineCreations.Add(enumerator.Current.Key, enumerator.Current.Value);
            }

            // Pending remote messages
            var enumerable2 = await RemoteMessages.CreateEnumerableAsync(CurrentTransaction);
            var enumerator2 = enumerable2.GetAsyncEnumerator();

            // TODO: Add cancellation token
            var ct2 = new System.Threading.CancellationToken();

            while (await enumerator2.MoveNextAsync(ct2))
            {
                PendingMessages.Enqueue(enumerator2.Current);
            }

            var enumerable3 = await Timers.CreateEnumerableAsync(CurrentTransaction);
            var enumerator3 = enumerable3.GetAsyncEnumerator();

            // TODO: Add cancellation token
            var ct3 = new System.Threading.CancellationToken();

            while (await enumerator3.MoveNextAsync(ct3))
            {
                PendingTimerCreations.Add(enumerator3.Current.Key, enumerator3.Current.Value);
            }

        }

        /// <summary>
        /// Creates a fresh Id
        /// </summary>
        /// <typeparam name="T">Machine Type</typeparam>
        /// <returns>Unique Id</returns>
        public override Task<IRsmId> ReliableCreateMachineId<T>() 
        {
            return Task.FromResult(IdFactory.Generate(typeof(T).Name) as IRsmId);
        }

        public override async Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent)
        {
            var id = IdFactory.Generate(typeof(T).Name);
            await ReliableCreateMachine(typeof(T), id, startingEvent);
            return id;
        }

        public override async Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent, string partitionName)
        {
            if(partitionName == this.Id.PartitionName)
            {
                return await ReliableCreateMachine<T>(startingEvent);
            }

            var id = await this.NetworkProvider.RemoteCreateMachineId<T>(partitionName);
            await ReliableCreateMachine(typeof(T), id, startingEvent);
            return id;
        }

        /// <summary>
        /// Creates an RSM in the specified partition with the given ID
        /// </summary>
        /// <typeparam name="T">Machine Type</typeparam>
        /// <param name="id">ID to attach to the machine</param>
        /// <param name="startingEvent">Starting event for the machine</param>
        public override async Task ReliableCreateMachine(Type machineType, IRsmId id, RsmInitEvent startingEvent)
        {
            if (startingEvent == null)
            {
                //TODO: Specific exception
                throw new Exception("StartingEvent cannot be null");
            }

            if (!machineType.IsSubclassOf(typeof(ReliableStateMachine)))
            {
                //TODO: Specific exception
                throw new Exception($"Type {machineType.Name} is not an instance of ReliableStateMachine");
            }

            if(id.PartitionName == this.Id.PartitionName)
            {
                await ReliableCreateMachineLocal(machineType, id, startingEvent);
            }
            else
            {
                await ReliableCreateMachineRemote(machineType, id, startingEvent);
            }
        }

        private async Task ReliableCreateMachineLocal(Type machineType, IRsmId id, RsmInitEvent startingEvent)
        {
            if (CreatedMachines == null)
            {
                CreatedMachines = await StateManager.GetOrAddAsync<IReliableDictionary<IRsmId, Tuple<string, RsmInitEvent>>>(
                    GetCreatedMachineMapName(this.Id));
            }

            if (MachineHosted)
            {
                await CreatedMachines.AddAsync(CurrentTransaction, id,
                    Tuple.Create(machineType.AssemblyQualifiedName, startingEvent));

                // delay creation until machine commits its current transaction
                PendingMachineCreations.Add(id, Tuple.Create(machineType.AssemblyQualifiedName, startingEvent));
            }
            else
            {
                using (var tx = StateManager.CreateTransaction())
                {
                    await CreatedMachines.AddAsync(tx, id,
                        Tuple.Create(machineType.AssemblyQualifiedName, startingEvent));
                    await tx.CommitAsync();
                }

                PendingMachineCreations.Add(id, Tuple.Create(machineType.AssemblyQualifiedName, startingEvent));

                await ExecutePendingWork();
            }
        }

        private async Task ReliableCreateMachineRemote(Type machineType, IRsmId id, RsmInitEvent startingEvent)
        {
            if (RemoteCreatedMachines == null)
            {
                RemoteCreatedMachines = await StateManager.GetOrAddAsync<IReliableDictionary<IRsmId, Tuple<string, RsmInitEvent>>>(
                    GetRemoteCreatedMachineMapName(this.Id));
            }

            if (MachineHosted)
            {
                await RemoteCreatedMachines.AddAsync(CurrentTransaction, id,
                    Tuple.Create(machineType.AssemblyQualifiedName, startingEvent));

                // delay creation until machine commits its current transaction
                PendingMachineCreations.Add(id, Tuple.Create(machineType.AssemblyQualifiedName, startingEvent));
            }
            else
            {
                await this.NetworkProvider.RemoteCreateMachine(machineType, id, startingEvent);
            }
        }

        public override async Task ReliableSend(IRsmId target, Event e)
        {
            if (target.PartitionName != this.Id.PartitionName)
            {
                await ReliableSendRemote(target, e);
                return;
            }

            var targetQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Event>>(GetInputQueueName(target));

            if (MachineHosted)
            {
                await targetQueue.EnqueueAsync(CurrentTransaction, e);
            }
            else
            {
                using (var tx = StateManager.CreateTransaction())
                {
                    await targetQueue.EnqueueAsync(tx, e);
                    await tx.CommitAsync();
                }
            }

        }

        private async Task ReliableSendRemote(IRsmId target, Event e)
        {
            if (MachineHosted)
            {
                var tag = await SendCounters.AddOrUpdateAsync(CurrentTransaction, target.Name, 1, (key, oldValue) => oldValue + 1);
                var tev = new TaggedRemoteEvent(this.Id, e, tag);
                await RemoteMessages.EnqueueAsync(CurrentTransaction, Tuple.Create(target, tev as Event));
                PendingMessages.Enqueue(Tuple.Create(target, tev as Event));
            }
            else
            {
                await this.NetworkProvider.RemoteSend(target, e);
            }
        }

        internal override void NotifyFailure(Exception ex, string methodName)
        {
            MachineFailureException = ex;
        }

        internal override void NotifyHalt()
        {
            MachineHalted = true;
        }

        static string GetInputQueueName(IRsmId id)
        {
            return string.Format("InputQueue.{0}", id.Name);
        }

        static string GetCreatedMachineMapName(IRsmId id)
        {
            return string.Format("CreatedMachines.{0}", id.Name);
        }

        static string GetRemoteCreatedMachineMapName(IRsmId id)
        {
            return string.Format("RemoteCreatedMachines.{0}", id.Name);
        }
    }
}
