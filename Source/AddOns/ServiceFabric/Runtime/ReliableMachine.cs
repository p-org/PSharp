#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace Microsoft.PSharp.ServiceFabric
{
    public abstract class ReliableMachine : Machine
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
        /// Current transaction
        /// </summary>
        public ITransaction CurrentTransaction { get; internal set; }

        /// <summary>
        /// ReliableRegisters created by the machine
        /// </summary>
        List<Utilities.RsmRegister> CreatedRegisters;

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
        protected ReliableMachine(IReliableStateManager stateManager)
            : base()
        {
            this.StateManager = stateManager;
            this.PendingStateChangesInverted = new List<MachineStateChangeOp>();
            this.PendingStateChanges = new List<MachineStateChangeOp>();

            this.InTestMode = testMode;
            this.TestModeOutBuffer = new Queue<Tuple<MachineId, Event>>();
            this.TestModeCreateBuffer = new Queue<Tuple<MachineId, Type, Event>>();
            this.LastDequeuedEvent = null;

            this.CreatedRegisters = new List<Utilities.RsmRegister>();
        }

        /// <summary>
        /// User-supplied initialization. Called when the machine is
        /// created for the first time or resurrected on failure.
        /// </summary>
        protected abstract Task OnActivate();


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
        /// Creates a reliable register
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="name">Name of the register</param>
        /// <param name="initialValue">Initial value of the register</param>
        /// <returns></returns>
        public Utilities.ReliableRegister<T> GetOrAddRegister<T>(string name, T initialValue = default(T))
        {
            var reg = new Utilities.ReliableRegister<T>(name + this.Id.ToString(), this.StateManager, initialValue);
            reg.SetTransaction(this.CurrentTransaction);
            CreatedRegisters.Add(reg);
            return reg;
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

            var startState = this.StateStack.Peek();

            this.StateStack.Clear();
            this.PendingStateChanges.Clear();
            this.PendingStateChangesInverted.Clear();

            CurrentTransaction = this.StateManager.CreateTransaction();
            SetReliableRegisterTx();

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
        }

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

            (this.Runtime as ServiceFabricPSharpRuntime).NotifyTransactionCommit(CurrentTransaction);

            if (InTestMode)
            {
                // disable R/G/P check
                this.Info.CurrentActionCalledTransitionStatement = false;

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

            CurrentTransaction.Dispose();
            PendingStateChanges.Clear();
            PendingStateChangesInverted.Clear();
            CurrentTransaction = null;
        }

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        internal override async Task<bool> RunEventHandler()
        {
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
                    if(CurrentTransaction != null)
                    {
                        await CommitCurrentTransaction();
                        CurrentTransaction = this.StateManager.CreateTransaction();
                        SetReliableRegisterTx();
                    }

                    var reliableDequeue = false;

                    lock (base.Inbox)
                    {
                        // Try to dequeue the next event, if there is one.
                        nextEventInfo = this.TryDequeueEvent();
                        LastDequeuedEvent = nextEventInfo;
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

                if (nextEventInfo == null)
                {
                    if (InTestMode)
                    {
                        CurrentTransaction.Dispose();
                        CurrentTransaction = null;

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

                // Handles next event.
                await this.HandleEvent(nextEventInfo.Event);
            }

            return false;
        }

        private async Task<EventInfo> ReliableDequeue()
        {
            EventInfo ret;

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

        internal void SetReliableRegisterTx()
        {
            foreach (var reg in CreatedRegisters)
            {
                reg.SetTransaction(this.CurrentTransaction);
            }
        }

        #region Unsupported        

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
