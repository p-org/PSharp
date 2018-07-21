using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    internal class ServiceFabricPSharpRuntime : StateMachineRuntime
    {
        private const string CreatedMachinesDictionaryName = "CreatedMachines";

        /// <summary>
        /// State Manager
        /// </summary>
        IReliableStateManager StateManager;

        /// <summary>
        /// Pending machine creations
        /// </summary>
        Dictionary<ITransaction, List<Tuple<MachineId, Type, string, Event, MachineId>>> PendingMachineCreations;

        /// <summary>
        /// The remote machine manager used for creating/sending messages
        /// </summary>
        public IRemoteMachineManager RemoteMachineManager { get; }

        internal ServiceFabricPSharpRuntime(IReliableStateManager stateManager, IRemoteMachineManager manager)
            : base()
        {
            this.StateManager = stateManager;
            this.RemoteMachineManager = manager;
            this.PendingMachineCreations = new Dictionary<ITransaction, List<Tuple<MachineId, Type, string, Event, MachineId>>>();
        }

        internal ServiceFabricPSharpRuntime(IReliableStateManager stateManager, IRemoteMachineManager manager, Configuration configuration) 
            : base(configuration)
        {
            this.StateManager = stateManager;
            this.RemoteMachineManager = manager;
            this.PendingMachineCreations = new Dictionary<ITransaction, List<Tuple<MachineId, Type, string, Event, MachineId>>>();
        }

        #region Monitor

        public override void InvokeMonitor<T>(Event e)
        {
            // no-op
        }

        public override void InvokeMonitor(Type type, Event e)
        {
            // no-op
        }

        public override void RegisterMonitor(Type type)
        {
            // no-op
        }

        #endregion

        #region CreateMachine

        public Task<MachineId> CreateMachine(string type, Guid requestId, Machine creator)
        { 
            return this.RemoteMachineManager.CreateMachine(requestId, type, creator, CancellationToken.None);
        }

        protected internal override MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            mid = CreateMachineAsync(mid, type, friendlyName, e, creator, operationGroupId).Result;
            return mid;
        }

        internal async Task<MachineId> CreateMachineAsync(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            if (mid == null)
            {
                mid = new MachineId(type, friendlyName, this);
            }

            this.Assert(type.IsSubclassOf(typeof(ReliableMachine)), "Type '{0}' is not a reliable machine.", type.Name);
            this.Assert(creator == null || creator is ReliableMachine, "Type '{0}' is not a reliable machine.", creator != null ? creator.GetType().Name : "");

            var reliableCreator = creator as ReliableMachine;
            var createdMachineMap = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Tuple<MachineId, string, Event>>>(CreatedMachinesDictionaryName);

            if (reliableCreator == null)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    await createdMachineMap.AddAsync(tx, mid.ToString(), Tuple.Create(mid, type.AssemblyQualifiedName, e));
                    await tx.CommitAsync();
                }
                StartMachine(mid, type, friendlyName, e, creator?.Id);
            }
            else
            {
                this.Assert(reliableCreator.CurrentTransaction != null, "Creator's transaction cannot be null");
                await createdMachineMap.AddAsync(reliableCreator.CurrentTransaction, mid.ToString(), Tuple.Create(mid, type.AssemblyQualifiedName, e));
                
                if(!PendingMachineCreations.ContainsKey(reliableCreator.CurrentTransaction))
                {
                    PendingMachineCreations[reliableCreator.CurrentTransaction] = new List<Tuple<MachineId, Type, string, Event, MachineId>>();
                }
                PendingMachineCreations[reliableCreator.CurrentTransaction].Add(
                    Tuple.Create(mid, type, friendlyName, e, reliableCreator.Id));
            }

            return mid;

        }

        private void StartMachine(MachineId mid, Type type, string friendlyName, Event e, MachineId creator)
        {
            this.Assert(mid.Runtime == null || mid.Runtime == this, "Unbound machine id '{0}' was created by another runtime.", mid.Name);
            this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                mid.Name, mid.Type, type.FullName);
            mid.Bind(this);

            Machine machine = ReliableMachineFactory.Create(type, StateManager);

            machine.Initialize(this, mid, new MachineInfo(mid));
            machine.InitializeStateInformation();

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "MachineId {0} = This typically occurs " +
                "either if the machine id was created by another runtime instance, or if a machine id from a previous " +
                "runtime generation was deserialized, but the current runtime has not increased its generation value.",
                mid.Name);

            base.Logger.OnCreateMachine(machine.Id, creator);
            this.RunMachineEventHandler(machine, e, true);
        }

        #endregion

        #region Send

        protected internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            // TODO: Make async
            SendEventAsync(mid, e, sender, options).Wait();
        }

        protected internal Task SendEventAsync(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            // TODO: Propogate the cancellation token.
            return this.RemoteMachineManager.SendEvent(mid, e, sender, options, CancellationToken.None);
        }

        #endregion

        #region Internal

        internal void NotifyTransactionCommit(ITransaction tx)
        {
            if (PendingMachineCreations.ContainsKey(tx))
            {
                foreach (var tup in PendingMachineCreations[tx])
                {
                    StartMachine(tup.Item1, tup.Item2, tup.Item3, tup.Item4, tup.Item5);
                }
                PendingMachineCreations.Remove(tx);
            }

        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        private void RunMachineEventHandler(Machine machine, Event initialEvent, bool isFresh)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await machine.GotoStartState(initialEvent);
                    }

                    await machine.RunEventHandler();
                }
                catch (Exception ex)
                {
                    if(machine is ReliableMachine && (machine as ReliableMachine).CurrentTransaction != null)
                    {
                        (machine as ReliableMachine).CurrentTransaction.Dispose();
                    }

                    base.IsRunning = false;
                    base.RaiseOnFailureEvent(ex);
                }
            });
        }

        internal override ulong GenerateTestId()
        {
            return 0;
        }
        #endregion


        #region Unsupported

        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override Task CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override MachineId RemoteCreateMachine(Type type, string friendlyName, string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override void RemoteSendEvent(MachineId target, Event e, SendOptions options = null)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        protected internal override MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint, Event e, Machine creator, Guid? operationGroupId)
        {
            throw new NotImplementedException();
        }

        protected internal override void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            throw new NotImplementedException();
        }

        protected internal override void TryCreateMonitor(Type type)
        {
            throw new NotImplementedException();
        }

        protected internal override Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            throw new NotImplementedException();
        }


        protected internal override Task<bool> SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
