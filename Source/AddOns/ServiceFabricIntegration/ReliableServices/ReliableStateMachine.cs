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
        /// Current transaction (if any)
        /// </summary>
        private ITransaction cTransaction;

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

            using (var tx = StateManager.CreateTransaction())
            {
                var cnt = await StateStackStore.GetCountAsync(tx);
                if (cnt != 0)
                {
                    // re-hydrate
                    DoStatePop();

                    for (int i = 0; i < cnt; i++)
                    {
                        var s = await StateStackStore.TryGetValueAsync(tx, i);
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
                    await StateStackStore.AddAsync(tx, 0, CurrentState.FullName);

                    await OnActivate();

                    this.cTransaction = tx;
                    this.ReceivedEvent = e;
                    await this.ExecuteCurrentStateOnEntry();
                    await tx.CommitAsync();
                    this.cTransaction = null;
                }
            }
        }

        #region inbox accessing

        /// <summary>
        /// Enqueues the specified <see cref="EventInfo"/>.
        /// </summary>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="runNewHandler">Run a new handler</param>
        internal override void Enqueue(EventInfo eventInfo, ref bool runNewHandler)
        { }

        #endregion
    }
}
