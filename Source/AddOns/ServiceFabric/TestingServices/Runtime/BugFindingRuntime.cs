using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric.TestingServices
{
    internal class BugFindingRuntime : PSharp.TestingServices.BugFindingRuntime
    {
        IReliableStateManager StateManager;

        Dictionary<string, Tuple<MachineId, Type, Event>> CreatedMachines;
        Dictionary<MachineId, List<Tuple<MachineId, Type, string, Event, Guid?>>> PendingMachineCreations;

        internal BugFindingRuntime(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration, strategy, reporter)
        {
            this.StateManager = new StateManagerMock(null).DisallowFailures();
            this.CreatedMachines = new Dictionary<string, Tuple<MachineId, Type, Event>>();
            this.PendingMachineCreations = new Dictionary<MachineId, List<Tuple<MachineId, Type, string, Event, Guid?>>>();
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <returns>MachineId</returns>
        protected internal override MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            Console.WriteLine("===========TEST CreateMachine=============");
            this.AssertCorrectCallerMachine(creator, "CreateMachine");
            if (creator != null)
            {
                this.Assert(creator is ReliableMachine, "Type '{0}' is not a reliable machine.", creator.GetType().Name);
                this.AssertNoPendingTransitionStatement(creator, "CreateMachine");
            }

            if (creator == null)
            {
                // Using ulong.MaxValue because a 'Create' operation cannot specify
                // the id of its target, because the id does not exist yet.
                this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

                Machine machine = this.CreateMachine(mid, type, friendlyName, e, (ReliableMachine)creator);
                this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);

                this.BugTrace.AddCreateMachineStep(creator, machine.Id, e == null ? null : new EventInfo(e));
                this.RunMachineEventHandler(machine, e, true, null, null);

                mid = machine.Id;
            }
            else
            {
                mid = this.ScheduleMachineCreation(mid, type, friendlyName, e, (ReliableMachine)creator, operationGroupId);
            }

            Console.WriteLine("===========TEST CreateMachine (DONE)=============");
            return mid;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">The id of the machine that created the returned machine.</param>
        /// <returns>Machine</returns>
        private Machine CreateMachine(MachineId mid, Type type, string friendlyName, Event e, ReliableMachine creator)
        {
            Console.WriteLine("===========TEST CreateMachine (non-buffering)=============");
            this.Assert(type.IsSubclassOf(typeof(ReliableMachine)), "Type '{0}' is not a reliable machine.", type.Name);

            if (mid == null)
            {
                mid = new MachineId(type, friendlyName, this);
            }
            else
            {
                this.Assert(mid.Runtime == null || mid.Runtime == this, "Unbound machine id '{0}' was created by another runtime.", mid.Value);
                this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                    mid.Value, mid.Type, type.FullName);
                mid.Bind(this);
            }

            Machine machine = ReliableMachineFactory.Create(type, StateManager);

            machine.Initialize(this, mid, new SchedulableInfo(mid));
            machine.InitializeStateInformation();

            this.CreatedMachines.Add(mid.ToString(), Tuple.Create(mid, type, e));

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "MachineId {0} = This typically occurs " +
                "either if the machine id was created by another runtime instance, or if a machine id from a previous " +
                "runtime generation was deserialized, but the current runtime has not increased its generation value.",
                mid.Name);

            this.Logger.OnCreateMachine(machine.Id, creator?.Id);
            
            return machine;
        }

        /// <summary>
        /// Schedules the creation of a new <see cref="Machine"/> of the specified <see cref="Type"/>
        /// and returns its machine id. Machine creation is postponed until the creator has commited
        /// its current transaction.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">The id of the machine that created the returned machine.</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <returns>MachineId</returns>
        private MachineId ScheduleMachineCreation(MachineId mid, Type type, string friendlyName, Event e, ReliableMachine creator, Guid? operationGroupId)
        {
            Console.WriteLine("===========TEST CreateMachine (buffering)=============");
            this.Assert(type.IsSubclassOf(typeof(ReliableMachine)), "Type '{0}' is not a reliable machine.", type.Name);

            if (mid == null)
            {
                mid = this.CreateMachineId(Type.GetType(type.AssemblyQualifiedName), friendlyName);
            }

            // TODO: needed in bugfinding?
            // Idempotence check.
            if (MachineMap.ContainsKey(mid))
            {
                // Machine already created.
                return mid;
            }

            this.Assert(creator.CurrentTransaction != null, "Creator's transaction cannot be null");

            if (!PendingMachineCreations.ContainsKey(creator.Id))
            {
                PendingMachineCreations[creator.Id] = new List<Tuple<MachineId, Type, string, Event, Guid?>>();
            }

            this.PendingMachineCreations[creator.Id].Add(Tuple.Create(mid, type, friendlyName, e, operationGroupId));

            return mid;
        }

        /// <summary>
        /// Restarts created machines on failover.
        /// </summary>
        private void RestartFailedMachines()
        {
            foreach (var kvp in this.CreatedMachines)
            {
                this.CreateMachine(kvp.Value.Item1, kvp.Value.Item2, null, kvp.Value.Item3, null);
            }
        }

        protected internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            // TODO: Make async
            SendEventAsync(mid, e, sender, options).Wait();
        }

        protected internal async Task SendEventAsync(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            var senderState = (sender as Machine)?.CurrentStateName ?? string.Empty;
            this.Logger.OnSend(mid, sender?.Id, senderState, 
                e.GetType().FullName, options?.OperationGroupId, isTargetHalted: false);

            var reliableSender = sender as ReliableMachine;

            var targetQueue = await StateManager.GetMachineInputQueue(mid);
            if (reliableSender == null || reliableSender.CurrentTransaction == null)
            {
                // Environment sending to a local machine
                using (var tx = this.StateManager.CreateTransaction())
                {
                    if (e is TaggedRemoteEvent)
                    {
                        var ReceiveCounters = await StateManager.GetMachineReceiveCounters(mid);
                        var tg = (e as TaggedRemoteEvent);
                        var currentCounter = await ReceiveCounters.GetOrAddAsync(tx, tg.mid.Name, 0);
                        if (currentCounter == tg.tag - 1)
                        {
                            await targetQueue.EnqueueAsync(tx, new EventInfo(tg.ev));
                            await ReceiveCounters.AddOrUpdateAsync(tx, tg.mid.Name, 0, (k, v) => tg.tag);
                            await tx.CommitAsync();
                        }
                    }
                    else
                    {
                        await targetQueue.EnqueueAsync(tx, new EventInfo(e));
                        await tx.CommitAsync();
                    }
                }
            }
            else
            {
                // Machine to machine
                await targetQueue.EnqueueAsync(reliableSender.CurrentTransaction, new EventInfo(e));
            }
        }

        /// <summary>
        /// Notifies that a machine has progressed. This method can be used to
        /// implement custom notifications based on the specified arguments.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="args">Arguments</param>
        protected internal override void NotifyProgress(Machine machine, params object[] args)
        {
            if (args.Length > 0)
            {
                // Notifies that a reliable machine has committed its current transaction.
                ITransaction tx = args[0] as ITransaction;
                if (tx != null && this.PendingMachineCreations.ContainsKey(machine.Id))
                {
                    foreach (var tup in this.PendingMachineCreations[machine.Id])
                    {
                        if (this.Logger.Configuration.Verbose >= this.Logger.LoggingVerbosity)
                        {
                            this.Logger.WriteLine("<CommitLog> Machine '{0}' committed transaction '{1}'.", machine.Id, tx.TransactionId);
                        }

                        // Using ulong.MaxValue because a 'Create' operation cannot specify
                        // the id of its target, because the id does not exist yet.
                        this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

                        MachineId mid = tup.Item1;
                        Type type = tup.Item2;
                        string friendlyName = tup.Item3;
                        Event e = tup.Item4;
                        Guid? operationGroupId = tup.Item5;

                        Machine m = this.CreateMachine(mid, type, friendlyName, e, (ReliableMachine)machine);
                        this.SetOperationGroupIdForMachine(m, machine, operationGroupId);

                        this.BugTrace.AddCreateMachineStep(machine, m.Id, e == null ? null : new EventInfo(e));
                        this.RunMachineEventHandler(m, e, true, null, null);
                    }

                    this.PendingMachineCreations.Remove(machine.Id);
                }
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public override void Dispose()
        {
            this.CreatedMachines.Clear();
            this.PendingMachineCreations.Clear();
            base.Dispose();
        }

        #region Unsupported

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
