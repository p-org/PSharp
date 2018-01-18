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
    public abstract class ReliableStateMachine : Machine
    {
        #region fields

        /// <summary>
        /// StatefulService State Manager
        /// </summary>
        private IReliableStateManager StateManager;

        /// <summary>
        /// Persistent current state (stack)
        /// </summary>
        private IReliableDictionary<int, string> StateStackStore;

        /// <summary>
        /// Inbox
        /// </summary>
        private IReliableConcurrentQueue<EventInfo> InputQueue;

        /// <summary>
        /// Current transaction
        /// </summary>
        public ITransaction CurrentTransaction { get; internal set; }

        /// <summary>
        /// Pending machine creations
        /// </summary>
        private List<TaskCompletionSource<bool>> PendingMachineCreations;

        /// <summary>
        /// State changes (inverted, for undo)
        /// </summary>
        private List<MachineStateChangeOp> PendingStateChangesInverted;

        /// <summary>
        /// State changes
        /// </summary>
        private List<MachineStateChangeOp> PendingStateChanges;

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        protected ReliableStateMachine(IReliableStateManager stateManager)
            : base()
        {
            this.StateManager = stateManager;
            this.PendingMachineCreations = new List<TaskCompletionSource<bool>>();
            this.PendingStateChangesInverted = new List<MachineStateChangeOp>();
            this.PendingStateChanges = new List<MachineStateChangeOp>();
        }

        /// <summary>
        /// User-supplied initialization. Called when the machine is
        /// created for the first time or resurrected on failure.
        /// </summary>
        public abstract Task OnActivate();

        /// <summary>
        /// Initializes the reliable structures, calls OnActivate,
        /// and transitions to the previously known state
        /// </summary>
        /// <param name="e">Initial event</param>
        internal override async Task GotoStartState(Event e)
        {
            StateStackStore = await StateManager.GetOrAddAsync<IReliableDictionary<int, string>>("StateStackStore_" + Id.ToString());
            InputQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<EventInfo>>("InputQueue_" + Id.ToString());

            using (CurrentTransaction = this.StateManager.CreateTransaction())
            {
                var cnt = await StateStackStore.GetCountAsync(CurrentTransaction);
                if (cnt != 0)
                {
                    // re-hydrate
                    DoStatePop();

                    for (int i = 0; i < cnt; i++)
                    {
                        var s = await StateStackStore.TryGetValueAsync(CurrentTransaction, i);
                        this.Assert(s.HasValue, "Error reading store for the state stack");

                        var nextState = StateMap[this.GetType()].First(val
                            => val.GetType().Equals(s.Value));

                        this.DoStatePush(nextState);
                    }

                    this.Assert(e == null, "Unexpected event passed on failover");

                    await OnActivate();
                }
                else
                {
                    // fresh start
                    await StateStackStore.AddAsync(CurrentTransaction, 0, CurrentState.FullName);

                    await OnActivate();

                    this.ReceivedEvent = e;
                    await this.ExecuteCurrentStateOnEntry();
                }

                await CurrentTransaction.CommitAsync();
            }

            this.CurrentTransaction = null;
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

            await createdMachineMap.AddAsync(CurrentTransaction, mid.ToString(), Tuple.Create(mid, type.FullName, e));
            var tcs = SpawnMachineCreationTask(this.Runtime, mid, type, e);
            PendingMachineCreations.Add(tcs);
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
                await createdMachineMap.AddAsync(tx, mid.ToString(), Tuple.Create(mid, type.FullName, e));
            }

            var tcs = SpawnMachineCreationTask(runtime, mid, type, e);
            tcs.SetResult(true);
            return mid;
        }

        private static TaskCompletionSource<bool> SpawnMachineCreationTask(PSharpRuntime runtime, MachineId mid, Type type, Event e)
        {
            var tcs = new TaskCompletionSource<bool>();
            Task.Run(async () =>
            {
                var r = await tcs.Task;
                if(r)
                {
                    runtime.CreateMachine(mid, type, e);
                }
            });
            return tcs;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        /// <param name="returnEarly">Returns after handling just one event</param>
        internal override async Task<bool> RunEventHandler(bool returnEarly = false)
        {
            if (this.Info.IsHalted)
            {
                return true;
            }

            while (!this.Info.IsHalted && base.Runtime.IsRunning)
            {
                var dequeued = false;

                // Try to get the raised event, if there is one. Raised events
                // have priority over the events in the inbox.
                EventInfo nextEventInfo = this.TryGetRaisedEvent();

                if (nextEventInfo == null)
                {
                    lock (base.Inbox)
                    {
                        // Try to dequeue the next event, if there is one.
                        nextEventInfo = this.TryDequeueEvent();
                        
                    }

                    if (nextEventInfo == null)
                    {
                        // commit previous transaction
                        if (CurrentTransaction != null)
                        {
                            try
                            {
                                // record state changes (if any)
                                if (PendingStateChanges.Count > 0)
                                {
                                    var size = await StateStackStore.GetCountAsync(CurrentTransaction);
                                    for (int i = 0; i < PendingStateChanges.Count; i++)
                                    {
                                        if(PendingStateChanges[i] is PopStateChangeOp)
                                        {
                                            await StateStackStore.TryRemoveAsync(CurrentTransaction, (int)(size - 1));
                                            size--;
                                        }
                                        else
                                        {
                                            var toPush = (PendingStateChanges[i] as PushStateChangeOp).state.GetType().FullName;
                                            await StateStackStore.AddAsync(CurrentTransaction, (int)size, toPush);
                                            size++;
                                        }
                                    }
                                }

                                await CurrentTransaction.CommitAsync();
                                CurrentTransaction.Dispose();

                                PendingMachineCreations.ForEach(tcs => tcs.SetResult(true));
                            }
                            catch (Exception)
                            {
                                PendingMachineCreations.ForEach(tcs => tcs.SetResult(false));
                                
                                // restore state stack
                                for(int i = PendingStateChangesInverted.Count - 1; i >= 0; i--)
                                {
                                    if(PendingStateChangesInverted[i] is PopStateChangeOp)
                                    {
                                        base.DoStatePop();
                                    }
                                    else
                                    {
                                        base.DoStatePush((PendingStateChangesInverted[i] as PushStateChangeOp).state);
                                    }
                                }

                            }
                        }

                        PendingMachineCreations.Clear();
                        PendingStateChanges.Clear();
                        PendingStateChangesInverted.Clear();
                        CurrentTransaction = StateManager.CreateTransaction();

                        nextEventInfo = await ReliableDequeue();

                        if(nextEventInfo == null)
                        {
                            CurrentTransaction.Dispose();
                            CurrentTransaction = null;
                        }
                    }

                    dequeued = nextEventInfo != null;
                }

                if(nextEventInfo == null)
                {
                    // retry
                    await Task.Delay(10);
                    continue; 
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

                // Handles next event.
                await this.HandleEvent(nextEventInfo.Event);

                // Return after handling the first event?
                if (returnEarly)
                {
                    return false;
                }
            }

            return false;
        }

        private async Task<EventInfo> ReliableDequeue()
        {
            var cv = await InputQueue.TryDequeueAsync(CurrentTransaction);
            if (cv.HasValue)
            {
                return cv.Value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is pushed on to the stack.
        /// </summary>
        /// <param name="state">State that is to be pushed on to the top of the stack</param>
        internal protected override void DoStatePush(MachineState state)
        {             
            PendingStateChanges.Add(new PushStateChangeOp(state));
            PendingStateChangesInverted.Add(new PopStateChangeOp());
            base.DoStatePush(state);
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is popped.
        /// </summary>
        internal protected override void DoStatePop()
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
        /// <param name="options">Optional parameters</param>
        protected async Task ReliableSend(MachineId mid, Event e)
        {
            var targetQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<EventInfo>>("InputQueue_" + mid.ToString());
            await targetQueue.EnqueueAsync(CurrentTransaction, new EventInfo(e));
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters</param>
        public static async Task ReliableSend(IReliableStateManager stateManager, MachineId mid, Event e)
        {
            using (var tx = stateManager.CreateTransaction())
            {
                var targetQueue = await stateManager.GetOrAddAsync<IReliableConcurrentQueue<EventInfo>>("InputQueue_" + mid.ToString());
                await targetQueue.EnqueueAsync(tx, new EventInfo(e));
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
