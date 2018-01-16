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

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        protected ReliableStateMachine(IReliableStateManager stateManager)
            : base()
        {
            this.StateManager = stateManager;
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

            // CurrentTransaction started by the MachineFactory 
            // (Machine object creation already added to the tx)

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
                // don't commit, the sender will do so
            }
            else
            {
                // fresh start
                await StateStackStore.AddAsync(CurrentTransaction, 0, CurrentState.FullName);

                await OnActivate();

                this.ReceivedEvent = e;
                await this.ExecuteCurrentStateOnEntry();

                // finish machine creation
                await CurrentTransaction.CommitAsync();                
            }

            this.CurrentTransaction = null;
        }

        #region inbox accessing

        /// <summary>
        /// Enqueues the specified <see cref="EventInfo"/>.
        /// </summary>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="runNewHandler">Run a new handler</param>
        /// <param name="sender">Sender machine</param>
        internal override void Enqueue(EventInfo eventInfo, ref bool runNewHandler, AbstractMachine sender)
        {
            var senderRSM = sender as ReliableStateMachine;

            if(senderRSM != null)
            {
                this.Assert(senderRSM.StateManager == this.StateManager, "Multiple different state managers detected");
            }

            if(senderRSM == null || senderRSM.CurrentTransaction == null)
            {
                using(var tx = this.StateManager.CreateTransaction())
                {
                    this.InputQueue.EnqueueAsync(tx, eventInfo);
                }
            }

        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters</param>
        protected override async Task SendAsync(MachineId mid, Event e, SendOptions options = null)
        {
            // If the target machine is null, then report an error and exit.
            this.Assert(mid != null, $"Machine '{base.Id}' is sending to a null machine.");
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{base.Id}' is sending a null event.");
            await base.Runtime.SendEventAndExecute(mid, e, this, options);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters</param>
        protected override void Send(MachineId mid, Event e, SendOptions options = null)
        {
            this.Assert(false, $"ReliableStateMachines ('{base.Id}') don't support Send. Use SendAsync instead.");
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
