using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace Microsoft.PSharp.ReliableServices
{
    public abstract class ReliableStateMachine : Machine
    {
        #region fields

        /// <summary>
        /// StatefulService State Manager
        /// </summary>
        protected IReliableStateManager StateManager;

        /// <summary>
        /// Persistent current state (stack)
        /// </summary>
        private IReliableDictionary<int, string> StateStackStore;

        /// <summary>
        /// Inbox
        /// </summary>
        private IReliableConcurrentQueue<EventInfo> InputQueue;

        /// <summary>
        /// Counters for reliable remote send
        /// </summary>
        protected IReliableDictionary<string, int> SendCounters;

        /// <summary>
        /// Counters for reliable remote receive
        /// </summary>
        protected IReliableDictionary<string, int> ReceiveCounters;

        /// <summary>
        /// Set of active reliable timers
        /// </summary>
        private IReliableDictionary<string, Timers.ReliableTimerConfig> Timers;

        /// <summary>
        /// Pending set of timers to be started
        /// </summary>
        private Dictionary<string, Timers.ReliableTimerConfig> PendingTimerCreations;

        /// <summary>
        /// Pending set of timers to be stopped
        /// </summary>
        private HashSet<string> PendingTimerRemovals;


        /// <summary>
        /// Active timers
        /// </summary>
        private Dictionary<string, Timers.ReliableTimer> TimerObjects;

        /// <summary>
        /// Current transaction
        /// </summary>
        public ITransaction CurrentTransaction { get; internal set; }

        /// <summary>
        /// Current transaction
        /// </summary>
        private bool LastTxThrewException;

        /// <summary>
        /// Pending machine creations
        /// </summary>
        private ConcurrentBag<TaskCompletionSource<bool>> PendingMachineCreations;

        /// <summary>
        /// State changes (inverted, for undo)
        /// </summary>
        private List<MachineStateChangeOp> PendingStateChangesInverted;

        /// <summary>
        /// State changes
        /// </summary>
        private List<MachineStateChangeOp> PendingStateChanges;

        /// <summary>
        /// Machine is executing under test mode
        /// </summary>
        private bool InTestMode;

        /// <summary>
        /// Out-buffer for send operations (test mode)
        /// </summary>
        private Queue<Tuple<MachineId, Event>> TestModeOutBuffer;

        /// <summary>
        /// Out-buffer for create-machine operations (test mode)
        /// </summary>
        private Queue<Tuple<MachineId, Type, Event>> TestModeCreateBuffer;

        /// <summary>
        /// Last dequeued event (test mode)
        /// </summary>
        private EventInfo LastDequeuedEvent;

        /// <summary>
        /// Create the machine in testing mode
        /// </summary>
        internal static bool testMode = false;

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        protected ReliableStateMachine(IReliableStateManager stateManager)
            : base()
        {
            this.StateManager = stateManager;
            this.PendingMachineCreations = new ConcurrentBag<TaskCompletionSource<bool>>();
            this.PendingStateChangesInverted = new List<MachineStateChangeOp>();
            this.PendingStateChanges = new List<MachineStateChangeOp>();

            this.InTestMode = testMode;
            this.TestModeOutBuffer = new Queue<Tuple<MachineId, Event>>();
            this.TestModeCreateBuffer = new Queue<Tuple<MachineId, Type, Event>>();
            this.LastDequeuedEvent = null;
            this.LastTxThrewException = false;

            this.TimerObjects = new Dictionary<string, ReliableServices.Timers.ReliableTimer>();
            this.PendingTimerCreations = new Dictionary<string, ReliableServices.Timers.ReliableTimerConfig>();
            this.PendingTimerRemovals = new HashSet<string>();
        }

        /// <summary>
        /// User-supplied initialization. Called when the machine is
        /// created for the first time or resurrected on failure.
        /// </summary>
        public abstract Task OnActivate();


        /// <summary>
        /// User-supplied routine for clearing volatile fields of
        /// the machine. Used only for testing purposes.
        /// </summary>
        /// <returns></returns>
        public virtual Task ClearVolatileState()
        {
            // TODO: Automate this method using reflection
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called when a transaction aborts but before the
        /// machine retries the operation. CurrentTransaction
        /// is null when this method is called.
        /// </summary>
        /// <returns></returns>
        public virtual void OnTxAbort()
        {
            // TODO: Pass additional information about the transaction
            // that just aborted
        }

        /// <summary>
        /// Initializes the reliable structures, calls OnActivate,
        /// and transitions to the previously known state
        /// </summary>
        /// <param name="e">Initial event</param>
        internal override async Task GotoStartState(Event e)
        {
            StateStackStore = await StateManager.GetOrAddAsync<IReliableDictionary<int, string>>("StateStackStore_" + Id.ToString());
            InputQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<EventInfo>>("InputQueue_" + Id.ToString());
            SendCounters =
                await StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("SendCounters_" + Id.ToString());
            ReceiveCounters =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("ReceiveCounters_" + Id.ToString());
            Timers =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Timers.ReliableTimerConfig>>("ReliableTimers_" + Id.ToString());

            var startState = this.StateStack.Peek();

            // TODO: include a retry policy
            while (true)
            {
                CurrentTransaction = this.StateManager.CreateTransaction();
                this.StateStack.Clear();
                this.PendingStateChanges.Clear();
                this.PendingStateChangesInverted.Clear();

                try
                {
                    var cnt = await StateStackStore.GetCountAsync(CurrentTransaction);
                    if (cnt != 0)
                    {
                        for (int i = 0; i < cnt; i++)
                        {
                            var s = await StateStackStore.TryGetValueAsync(CurrentTransaction, i);
                            this.Assert(s.HasValue, "Error reading store for the state stack");

                            var nextState = StateMap[this.GetType()].First(val
                                => val.GetType().AssemblyQualifiedName.Equals(s.Value));

                            base.DoStatePush(nextState);
                        }

                        this.Assert(e == null, "Unexpected event passed on failover");

                        await OnActivate();

                        // start timers
                        var enumerator = (await Timers.CreateEnumerableAsync(CurrentTransaction))
                            .GetAsyncEnumerator();
                        var ct = new System.Threading.CancellationToken();

                        while (await enumerator.MoveNextAsync(ct))
                        {
                            var config = enumerator.Current.Value;

                            var timer = new Timers.ReliableTimer(this.Id, config.Period, config.Name);
                            timer.StartTimer();
                            TimerObjects.Add(config.Name, timer);
                        }
                    }
                    else
                    {
                        // fresh start
                        base.DoStatePush(startState);
                        await StateStackStore.AddAsync(CurrentTransaction, 0, CurrentState.AssemblyQualifiedName);

                        await OnActivate();

                        this.ReceivedEvent = e;
                        await this.ExecuteCurrentStateOnEntry();
                    }

                    await CommitCurrentTransaction();
                    break;
                }
                catch (Exception ex) when (ex is System.Fabric.TransactionFaultedException || ex is TimeoutException)
                {
                    this.Logger.WriteLine("ReliableStateMachine::GotoStartState encountered an exception trying to commit a transaction: {0}", ex.ToString());
                    OnTxAbort();
                    CleanupOnAbortedTransaction();
                }
            }
        }

        #region CreateMachine

        /// <summary>
        /// Creates a new Reliable State Machine of the specified <see cref="Type"/> and name, 
        /// and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        protected async Task<MachineId> ReliableCreateMachine(Type type, string friendlyName, Event e = null)
        {
            var createdMachineMap = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>("CreatedMachines");
            var mid = this.Runtime.CreateMachineId(type, friendlyName);

            await createdMachineMap.AddAsync(CurrentTransaction, mid.ToString(), Tuple.Create<MachineId, string, Event>(mid, type.AssemblyQualifiedName, e));
            if (!InTestMode)
            {
                var tcs = SpawnMachineCreationTask(this.Runtime, mid, type, e);
                PendingMachineCreations.Add(tcs);
            }
            else
            {
                TestModeCreateBuffer.Enqueue(Tuple.Create(mid, type, e));
            }
            return mid;
        }

        /// <summary>
        /// Creates a new Reliable State Machine of the specified <see cref="Type"/> and name, 
        /// and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint where to create the machine</param>
        /// <param name="e">Event</param>
        protected async Task<MachineId> ReliableRemoteCreateMachine(Type type, string friendlyName, string endpoint, Event e = null)
        {
            if (InTestMode)
            {
                return await ReliableCreateMachine(type, friendlyName, e);
            }

            var remoteCreationMachineMap = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>("remoteCreationMachineMap");
            var mid = await this.Runtime.NetworkProvider.RemoteCreateMachineId(type, friendlyName, endpoint);

            await remoteCreationMachineMap.AddAsync(CurrentTransaction, mid.ToString(), Tuple.Create<MachineId, string, Event>(mid, type.AssemblyQualifiedName, e));
            return mid;
        }

        /// <summary>
        /// Creates a new Reliable State Machine of the specified <see cref="Type"/> and name, 
        /// and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        public static async Task<MachineId> ReliableCreateMachine(IReliableStateManager stateManager, PSharpRuntime runtime, Type type, string friendlyName, Event e = null)
        {
            var createdMachineMap = await stateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>("CreatedMachines");
            var mid = runtime.CreateMachineId(type, friendlyName);

            using (var tx = stateManager.CreateTransaction())
            {
                await createdMachineMap.AddAsync(tx, mid.ToString(), Tuple.Create(mid, type.AssemblyQualifiedName, e));
                await tx.CommitAsync();
            }

            var tcs = SpawnMachineCreationTask(runtime, mid, type, e);
            tcs.SetResult(true);
            return mid;
        }

        /// <summary>
        /// Creates a new Reliable State Machine of the specified <see cref="Type"/> and name, 
        /// and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="mid">MachineId of the machine to be created</param>
        /// <param name="e">Event</param>
        /// <returns>False, if machine was already created</returns>
        public static async Task<bool> ReliableCreateMachine(IReliableStateManager stateManager, PSharpRuntime runtime, MachineId mid, Type type, Event e = null)
        {
            var createdMachineMap = await stateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>("CreatedMachines");
            var alreadyCreated = false;

            using (var tx = stateManager.CreateTransaction())
            {
                var cv = await createdMachineMap.TryGetValueAsync(tx, mid.ToString());
                if (cv.HasValue)
                {
                    alreadyCreated = true;
                }
                else
                {
                    await createdMachineMap.AddAsync(tx, mid.ToString(), Tuple.Create(mid, type.AssemblyQualifiedName, e));
                    await tx.CommitAsync();
                }
            }

            if (!alreadyCreated)
            {
                var tcs = SpawnMachineCreationTask(runtime, mid, type, e);
                tcs.SetResult(true);
            }

            return !alreadyCreated;
        }

        private static TaskCompletionSource<bool> SpawnMachineCreationTask(PSharpRuntime runtime, MachineId mid, Type type, Event e)
        {
            var tcs = new TaskCompletionSource<bool>();
            Task.Run(async () =>
            {
                var r = await tcs.Task;
                if (r)
                {
                    runtime.CreateMachine(mid, type, e);
                }
            });
            return tcs;
        }

        #endregion

        #region Timers

        /// <summary>
        /// Starts a period timer
        /// </summary>
        /// <param name="name">Name of the timer</param>
        /// <param name="period">Periodic interval (ms)</param>
        protected async void StartTimer(string name, int period)
        {
            var config = new Timers.ReliableTimerConfig(name, period);
            var success = await Timers.TryAddAsync(CurrentTransaction, name, config);
            this.Assert(success, "Timer {0} already started", name);

            PendingTimerCreations.Add(name, config);
        }

        /// <summary>
        /// Stops a timer
        /// </summary>
        /// <param name="name"></param>
        protected async void StopTimer(string name)
        {
            var cv = await Timers.TryRemoveAsync(CurrentTransaction, name);
            this.Assert(cv.HasValue, "Attempt to stop a timer {0} that was not started", name);

            if (PendingTimerCreations.ContainsKey(name))
            {
                // timer was pending, so just remove it
                PendingTimerCreations.Remove(name);
            }
            else
            {
                PendingTimerRemovals.Add(name);
            }

        }

        /// <summary>
        /// Actually start/stop the timers marked pending
        /// </summary>
        private async Task ProcessTimers()
        {
            foreach(var tup in PendingTimerCreations)
            {
                var timer = new Timers.ReliableTimer(this.Id, tup.Value.Period, tup.Key);
                timer.StartTimer();
                TimerObjects.Add(tup.Key, timer);
            }

            PendingTimerCreations.Clear();

            foreach (var name in PendingTimerRemovals)
            {
                if (!TimerObjects.ContainsKey(name)) continue;

                var success = TimerObjects[name].StopTimer();
                if (!success)
                {
                    // wait for, and remove, the timeout
                    await base.Receive(typeof(Timers.TimeoutEvent), ev => (ev as Timers.TimeoutEvent).Name == name);
                }

                TimerObjects.Remove(name);
            }

            PendingTimerRemovals.Clear();
        }

        #endregion

        #region internal methods

        private async Task CommitCurrentTransaction()
        {
            // record state changes (if any)
            if (PendingStateChanges.Count > 0)
            {
                var size = await StateStackStore.GetCountAsync(CurrentTransaction);
                for (int i = 0; i < PendingStateChanges.Count; i++)
                {
                    if (PendingStateChanges[i] is PopStateChangeOp)
                    {
                        await StateStackStore.TryRemoveAsync(CurrentTransaction, (int)(size - 1));
                        size--;
                    }
                    else
                    {
                        var toPush = (PendingStateChanges[i] as PushStateChangeOp).state.GetType().AssemblyQualifiedName;
                        await StateStackStore.AddAsync(CurrentTransaction, (int)size, toPush);
                        size++;
                    }
                }
            }

            await CurrentTransaction.CommitAsync();

            if (this.Logger.Configuration.Verbose >= this.Logger.LoggingVerbosity)
            {
                this.Logger.WriteLine("<CommitLog> Successfully committed transaction {0}", CurrentTransaction.TransactionId);
            }

            if (InTestMode)
            {
                while (TestModeCreateBuffer.Count > 0)
                {
                    var tup = TestModeCreateBuffer.Dequeue();
                    this.Runtime.CreateMachine(tup.Item1, tup.Item2, tup.Item3);
                }

                while (TestModeOutBuffer.Count > 0)
                {
                    var tup = TestModeOutBuffer.Dequeue();
                    this.Runtime.SendEvent(tup.Item1, tup.Item2);
                }
            }

            // remote machine creations
            var remoteCreationMachineMap = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>("remoteCreationMachineMap");

            // TODO: include retry policy
            while (true)
            {
                try
                {
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        var keys = new List<string>();

                        var enumerable = await remoteCreationMachineMap.CreateEnumerableAsync(tx);
                        var enumerator = enumerable.GetAsyncEnumerator();

                        // TODO: Add cancellation token
                        var ct = new System.Threading.CancellationToken();

                        while (await enumerator.MoveNextAsync(ct))
                        {
                            keys.Add(enumerator.Current.Key);
                            var tup = enumerator.Current.Value;
                            // idempotent, so this can happen multiple times
                            await this.Runtime.NetworkProvider.RemoteCreateMachine(tup.Item1, Type.GetType(tup.Item2), tup.Item3);
                        }

                        // TODO: ClearAsync doesn't work
                        foreach (var k in keys)
                        {
                            await remoteCreationMachineMap.TryRemoveAsync(tx, k);
                        }

                        await tx.CommitAsync();
                    }
                    break;
                }
                catch (Exception ex) when (ex is System.Fabric.TransactionFaultedException || ex is TimeoutException)
                {
                    // retry
                }
            }
       
            // local machine creations
            foreach (var tcs in PendingMachineCreations.AsEnumerable())
            {
                tcs.SetResult(true);
            }

            await ProcessTimers();

            CurrentTransaction.Dispose();
            PendingMachineCreations = new ConcurrentBag<TaskCompletionSource<bool>>();
            PendingStateChanges.Clear();
            PendingStateChangesInverted.Clear();
            CurrentTransaction = null;
        }

        private void CleanupOnAbortedTransaction()
        {
            foreach (var tcs in PendingMachineCreations.AsEnumerable())
            {
                tcs.SetResult(false);
            }

            // restore state stack
            for (int i = PendingStateChangesInverted.Count - 1; i >= 0; i--)
            {
                if (PendingStateChangesInverted[i] is PopStateChangeOp)
                {
                    base.DoStatePop();
                }
                else
                {
                    base.DoStatePush((PendingStateChangesInverted[i] as PushStateChangeOp).state);
                }
            }

            if (CurrentTransaction != null)
            {
                CurrentTransaction.Dispose();
            }

            PendingMachineCreations = new ConcurrentBag<TaskCompletionSource<bool>>();
            PendingStateChanges.Clear();
            PendingStateChangesInverted.Clear();
            PendingTimerCreations.Clear();
            PendingTimerRemovals.Clear();
            CurrentTransaction = null;
            RaisedEvent = null;

            TestModeOutBuffer.Clear();
            TestModeCreateBuffer.Clear();
        }

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        internal override async Task<bool> RunEventHandler()
        {
            Timers.ReliableTimer lastTimer = null;

            if (this.Info.IsHalted)
            {
                return true;
            }

            while (!this.Info.IsHalted && base.Runtime.IsRunning)
            {
                var dequeued = false;
                EventInfo nextEventInfo = null;

                // Try to get the raised event, if there is one. Raised events
                // have priority over the events in the inbox.
                nextEventInfo = this.TryGetRaisedEvent();

                if (nextEventInfo == null)
                {
                    var lastTimerStopped = false;
                    if(lastTimer != null && PendingTimerRemovals.Contains(lastTimer.Name))
                    {
                        lastTimerStopped = true;
                    }

                    // commit previous transaction
                    if (CurrentTransaction != null)
                    {
                        try
                        {
                            await CommitCurrentTransaction();
                        }
                        catch (Exception ex) when (ex is System.Fabric.TransactionFaultedException || ex is TimeoutException)
                        {
                            this.Logger.WriteLine("ReliableStateMachine encountered an exception trying to commit a transaction: {0}", ex.ToString());
                            LastTxThrewException = true;
                            CleanupOnAbortedTransaction();
                            await TxAbortHandler();
                        }
                    }

                    if (!LastTxThrewException && !lastTimerStopped && lastTimer != null)
                    {
                        var newTimer = new Timers.ReliableTimer(this.Id, lastTimer.Period, lastTimer.Name);
                        TimerObjects[lastTimer.Name] = newTimer;
                        newTimer.StartTimer();
                    }

                    CurrentTransaction = StateManager.CreateTransaction();
                    var reliableDequeue = false;

                    lock (base.Inbox)
                    {
                        // Try to dequeue the next event, if there is one.
                        nextEventInfo = (LastTxThrewException) ? LastDequeuedEvent
                            : this.TryDequeueEvent();
                        LastDequeuedEvent = nextEventInfo;
                    }

                    if (!LastTxThrewException)
                    {
                        lastTimer = null;
                        if (nextEventInfo != null && nextEventInfo.Event is Timers.TimeoutEvent)
                        {
                            lastTimer = TimerObjects[(nextEventInfo.Event as Timers.TimeoutEvent).Name];
                            TimerObjects.Remove(lastTimer.Name);
                        }
                    }

                    if (nextEventInfo == null && !InTestMode)
                    {
                        nextEventInfo = await ReliableDequeue();
                        reliableDequeue = (nextEventInfo != null);
                    }

                    if (nextEventInfo == null && !InTestMode)
                    {
                        CurrentTransaction.Dispose();
                        CurrentTransaction = null;
                    }

                    dequeued = nextEventInfo != null;
                }

                if(nextEventInfo == null)
                {
                    if (InTestMode)
                    {
                        this.IsRunning = false;
                        break;
                    }
                    else
                    {
                        // retry
                        await Task.Delay(10);
                        continue;
                    }
                }

                if (dequeued)
                {
                    // Notifies the runtime for a new event to handle. This is only used
                    // during bug-finding and operation bounding, because the runtime has
                    // to schedule a machine when a new operation is dequeued.
                    base.Runtime.NotifyDequeuedEvent(this, nextEventInfo);
                }
                else
                {
                    base.Runtime.NotifyHandleRaisedEvent(this, nextEventInfo);
                }

                // Assigns the received event.
                this.ReceivedEvent = nextEventInfo.Event;

                this.LastDequeuedEvent = nextEventInfo;
                this.LastTxThrewException = false;

                // Handles next event.
                await this.HandleEvent(nextEventInfo.Event);

                if (this.LastTxThrewException)
                {
                    CleanupOnAbortedTransaction();
                    await TxAbortHandler();
                }
            }

            return false;
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <param name="cachedAction">The cached methodInfo and corresponding delegate</param>
        internal override async Task ExecuteAction(CachedAction cachedAction)
        {
            if (!LastTxThrewException)
            {
                await base.ExecuteAction(cachedAction);
            }
        }

        private async Task TxAbortHandler()
        {
            if (InTestMode)
            {
                if (this.Random())
                {
                    // simulating a failure
                    
                    this.Assert(CurrentTransaction == null);

                    // TODO: Bound number of iterations
                    // of this retry loop
                    while (true)
                    {
                        try
                        {
                            await ClearVolatileState();
                            CurrentTransaction = StateManager.CreateTransaction();
                            await OnActivate();
                            await CurrentTransaction.CommitAsync();
                            break;
                        }
                        catch (Exception ex) when (ex is System.Fabric.TransactionFaultedException || ex is TimeoutException)
                        {
                            CurrentTransaction.Dispose();
                        }
                    }
                    CurrentTransaction = null;
                }
                else
                {
                    // or a normal abort
                    OnTxAbort();
                }
            }
            else
            {
                OnTxAbort();
            }
        }

        private async Task<EventInfo> ReliableDequeue()
        {
            EventInfo ret;

            // TODO: retry policy
            while (true)
            {
                try
                {

                    while (true)
                    {
                        var cv = await InputQueue.TryDequeueAsync(CurrentTransaction);
                        if (cv.HasValue)
                        {
                            if (cv.Value.Event is TaggedRemoteEvent)
                            {
                                var tg = (cv.Value.Event as TaggedRemoteEvent);
                                var currentCounter = await ReceiveCounters.GetOrAddAsync(CurrentTransaction, tg.mid.Name, 0);
                                if (currentCounter == tg.tag - 1)
                                {
                                    ret = new EventInfo(tg.ev, cv.Value.OriginInfo);
                                    await ReceiveCounters.AddOrUpdateAsync(CurrentTransaction, tg.mid.Name, 0, (k, v) => tg.tag);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                ret = cv.Value;
                            }

                            return ret;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex) when (ex is System.Fabric.TransactionFaultedException || ex is TimeoutException)
                {
                    //retry
                    await Task.Delay(10);
                }
            }

        }

        /// <summary>
        /// Invokes user callback when a machine throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if it was handled in this method</returns>
        internal override bool OnExceptionHandler(string methodName, Exception ex)
        {
            if(ex is System.Fabric.TransactionFaultedException || ex is TimeoutException)
            {
                this.LastTxThrewException = true;
                this.Logger.OnMachineExceptionThrown(this.Id, CurrentStateName, methodName, ex);
                this.Logger.OnMachineExceptionHandled(this.Id, CurrentStateName, methodName, ex);
                // TODO: We have to ensure that if a handler throws this exception (i.e., tx aborts)
                // then subsequent exit or entry handlers are not invoked. This requires a change in
                // Machine.HandleEvent
                return true;
            }

            return base.OnExceptionHandler(methodName, ex);
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is pushed on to the stack.
        /// </summary>
        /// <param name="state">State that is to be pushed on to the top of the stack</param>
        internal override void DoStatePush(MachineState state)
        {             
            PendingStateChanges.Add(new PushStateChangeOp(state));
            PendingStateChangesInverted.Add(new PopStateChangeOp());
            base.DoStatePush(state);
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is popped.
        /// </summary>
        internal override void DoStatePop()
        {
            PendingStateChanges.Add(new PopStateChangeOp());
            PendingStateChangesInverted.Add(new PushStateChangeOp(StateStack.Peek()));
            base.DoStatePop();
        }
        #endregion

        #region Send

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        protected async Task ReliableSend(MachineId mid, Event e)
        {
            if (!InTestMode)
            {
                var targetQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<EventInfo>>("InputQueue_" + mid.ToString());
                await targetQueue.EnqueueAsync(CurrentTransaction, new EventInfo(e));
            }
            else
            {
                TestModeOutBuffer.Enqueue(Tuple.Create(mid, e));
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine executing on a different StateManager
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        protected async Task ReliableRemoteSend(MachineId mid, Event e)
        {
            if (!InTestMode)
            {
                var tag = await SendCounters.AddOrUpdateAsync(CurrentTransaction, mid.Name, 1, (key, oldValue) => oldValue + 1);
                await this.RemoteSend(mid, new TaggedRemoteEvent(this.Id, e, tag));
            }
            else
            {
                TestModeOutBuffer.Enqueue(Tuple.Create(mid, e));
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters</param>
        public static async Task ReliableSend(IReliableStateManager stateManager, MachineId mid, Event e)
        {
            // TODO: retry policy
            while (true)
            {
                try
                {
                    using (var tx = stateManager.CreateTransaction())
                    {
                        var targetQueue = await stateManager.GetOrAddAsync<IReliableConcurrentQueue<EventInfo>>("InputQueue_" + mid.ToString());
                        await targetQueue.EnqueueAsync(tx, new EventInfo(e));
                        await tx.CommitAsync();
                    }
                    break;
                }
                catch (Exception ex) when (ex is System.Fabric.TransactionFaultedException || ex is TimeoutException)
                {
                    // retry
                }
            }
        }




        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters</param>
        protected override void Send(MachineId mid, Event e, SendOptions options = null)
        {
            this.Assert(false, $"ReliableStateMachines ('{base.Id}') don't support Send. Use ReliableSend instead.");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        /// <returns>Event received</returns>
        protected internal override Task<Event> Receive(params Type[] eventTypes)
        {
            this.Assert(false, $"ReliableStateMachines ('{base.Id}') don't support the Receive operation.");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified type
        /// that satisfies the specified predicate.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="predicate">Predicate</param>
        /// <returns>Event received</returns>
        protected internal override Task<Event> Receive(Type eventType, Func<Event, bool> predicate)
        {
            this.Assert(false, $"ReliableStateMachines ('{base.Id}') don't support the Receive operation.");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types
        /// that satisfy the specified predicates.
        /// </summary>
        /// <param name="events">Event types and predicates</param>
        /// <returns>Event received</returns>
        protected internal override Task<Event> Receive(params Tuple<Type, Func<Event, bool>>[] events)
        {
            this.Assert(false, $"ReliableStateMachines ('{base.Id}') don't support the Receive operation.");
            throw new NotImplementedException();
        }

        #endregion
    }
}
