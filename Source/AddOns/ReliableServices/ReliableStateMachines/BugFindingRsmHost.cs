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
    sealed class BugFindingRsmHost : RsmHost
    {
        /// <summary>
        /// Persistent current state (stack)
        /// </summary>
        public IReliableDictionary<int, string> StateStackStore;

        /// <summary>
        /// Machines that haven't been started yet
        /// </summary>
        public Dictionary<IRsmId, Tuple<string, RsmInitEvent>> PendingMachineCreations;

        /// <summary>
        /// Pending Sends
        /// </summary>
        public List<Tuple<MachineId, Event>> PendingSends;

        /// <summary>
        /// Has the machine halted
        /// </summary>
        public bool MachineHalted;

        /// <summary>
        /// Machine failed with an exception
        /// </summary>
        public Exception MachineFailureException;

        /// <summary>
        /// Type of hosted machine
        /// </summary>
        Type MachineType;

        /// <summary>
        /// Initial event of the hosted machine
        /// </summary>
        RsmInitEvent StartingEvent;

        private BugFindingRsmHost(IReliableStateManager stateManager, BugFindingRsmId id, PSharpRuntime runtime)
            : base(stateManager)
        {
            this.Id = id;
            this.PendingMachineCreations = new Dictionary<IRsmId, Tuple<string, RsmInitEvent>>();
            this.PendingSends = new List<Tuple<MachineId, Event>>();
            this.Runtime = runtime;
            MachineHalted = false;
            MachineFailureException = null;
        }

        public void SetTransaction(ITransaction tx)
        {
            this.CurrentTransaction = tx;
        }

        private async Task Initialize(Type machineType, RsmInitEvent ev)
        {
            StateStackStore = await StateManager.GetOrAddAsync<IReliableDictionary<int, string>>(string.Format("StateStackStore.{0}", this.Id.Name));
            this.MachineType = machineType;
            this.StartingEvent = ev;

            var mid = (this.Id as BugFindingRsmId).Mid;
            Runtime.CreateMachine(mid, typeof(BugFindingRsmHostMachine),
                new BugFindingRsmHostMachineInitEvent(this, machineType, ev));
        }

        internal async Task InitializationTransaction(Type machineType, RsmInitEvent ev)
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

            if (MachineFailureException != null &&
                (MachineFailureException is TimeoutException || MachineFailureException is System.Fabric.TransactionFaultedException))
            {
                throw MachineFailureException;
            }

        }

        internal async Task<bool> PersistStateStack()
        {
            if (StackChanges.PopDepth == 0 && StackChanges.PushedSuffix.Count == 0)
            {
                return false;
            }

            var cnt = (int)await StateStackStore.GetCountAsync(CurrentTransaction);
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

        internal async Task EventHandlerLoop(bool machineRestartRequired, Event ev)
        {
            while (true)
            {
                try
                {
                    using (CurrentTransaction = StateManager.CreateTransaction())
                    {
                        SetReliableRegisterTx();

                        if (machineRestartRequired)
                        {
                            machineRestartRequired = false;
                            await InitializationTransaction(MachineType, StartingEvent);
                            await PersistStateStack();
                        }
                        else
                        {
                            await EventHandler(ev);
                            var stackChanged = await PersistStateStack();
                        }

                        await CurrentTransaction.CommitAsync();
                    }

                    StackChanges = new StackDelta();
                    await ExecutePendingWork();
                    break;
                }
                catch (Exception ex) when (ex is TimeoutException || ex is System.Fabric.TransactionFaultedException)
                {
                    MachineFailureException = null;
                    MachineHalted = false;
                    machineRestartRequired = true;

                    StackChanges = new StackDelta();
                    PendingMachineCreations.Clear();
                    PendingSends.Clear();
                }
            }

        }

        /// <summary>
        /// Returns true if dequeued
        /// </summary>
        /// <returns></returns>
        private async Task EventHandler(Event ev)
        {
            await Runtime.SendEventAndExecute(Mid, ev);

            if (MachineFailureException != null &&
                (MachineFailureException is TimeoutException || MachineFailureException is System.Fabric.TransactionFaultedException))
            {
                throw MachineFailureException;
            }
        }

        internal async Task ExecutePendingWork()
        {
            // machine creations
            foreach (var tup in PendingMachineCreations)
            {
                var host = new BugFindingRsmHost(this.StateManager, tup.Key as BugFindingRsmId, Runtime);
                await host.Initialize(Type.GetType(tup.Value.Item1), tup.Value.Item2);
            }

            // send
            foreach(var tup in PendingSends)
            {
                Runtime.SendEvent(tup.Item1, tup.Item2);
            }

            PendingMachineCreations.Clear();
            PendingSends.Clear();
        }

        public override async Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent)
        {
            if (startingEvent == null)
            {
                //TODO: Specific exception
                throw new Exception("StartingEvent cannot be null");
            }

            var mid = Runtime.CreateMachineId(typeof(BugFindingRsmHostMachine));
            var rid = new BugFindingRsmId(mid, this.Id.PartitionName);

            PendingMachineCreations.Add(rid, Tuple.Create(typeof(T).AssemblyQualifiedName, startingEvent));

            if (this.Mid != null)
            { 
                await ExecutePendingWork();
            }

            return rid;
        }

        public override Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent, string partitionName)
        {
            throw new NotImplementedException();
        }

        public override async Task ReliableSend(IRsmId target, Event e)
        {
            if (target.PartitionName != this.Id.PartitionName)
            {
                throw new NotImplementedException();
            }

            PendingSends.Add(Tuple.Create((target as BugFindingRsmId).Mid, e));

            if (this.Mid != null)
            {
                await ExecutePendingWork();
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
    }

}
