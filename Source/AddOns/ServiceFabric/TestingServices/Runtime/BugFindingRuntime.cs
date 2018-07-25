//-----------------------------------------------------------------------
// <copyright file="BugFindingRuntime.cs">
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
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

using Microsoft.ServiceFabric.Data;

namespace Microsoft.PSharp.ServiceFabric.TestingServices
{
    internal class BugFindingRuntime : PSharp.TestingServices.BugFindingRuntime
    {
        /// <summary>
        /// The Service Fabric state manager used by the reliable machines.
        /// </summary>
        IReliableStateManager StateManager;

        /// <summary>
        /// Map of the created machines.
        /// </summary>
        Dictionary<string, Tuple<MachineId, Type, Event>> CreatedMachines;

        /// <summary>
        /// Map containing pending machine creations.
        /// </summary>
        Dictionary<MachineId, List<Tuple<MachineId, Type, string, Event, Guid?>>> PendingMachineCreations;

        /// <summary>
        /// Map containing pending event sends.
        /// </summary>
        Dictionary<MachineId, List<Tuple<MachineId, Event, SendOptions>>> PendingEventSends;

        /// <summary>
        /// Constructor.
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <param name="reporter">Reporter to register runtime operations.</param>
        /// </summary>
        internal BugFindingRuntime(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration, strategy, reporter)
        {
            ReliableMachine.IsTestingModeEnabled = true;
            this.StateManager = new StateManagerMock(null).DisallowFailures();
            this.CreatedMachines = new Dictionary<string, Tuple<MachineId, Type, Event>>();
            this.PendingMachineCreations = new Dictionary<MachineId, List<Tuple<MachineId, Type, string, Event, Guid?>>>();
            this.PendingEventSends = new Dictionary<MachineId, List<Tuple<MachineId, Event, SendOptions>>>();
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
            if (creator == null)
            {
                mid = base.CreateMachine(mid, type, friendlyName, e, creator, operationGroupId);
                this.CreatedMachines.Add(mid.ToString(), Tuple.Create(mid, type, e));
            }
            else
            {
                mid = this.ScheduleCreateMachine(mid, type, friendlyName, e, (ReliableMachine)creator, operationGroupId);
            }

            return mid;
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
        private MachineId ScheduleCreateMachine(MachineId mid, Type type, string friendlyName, Event e, ReliableMachine creator, Guid? operationGroupId)
        {
            this.AssertCorrectCallerMachine(creator, "CreateMachine");
            this.Assert(creator is ReliableMachine, "Type '{0}' is not a reliable machine.", creator.GetType().Name);
            this.Assert(creator.CurrentTransaction != null, "Machine '{0}' tried to create another machine using a null transaction.", creator.Id);
            this.Assert(type.IsSubclassOf(typeof(ReliableMachine)), "Type '{0}' is not a reliable machine.", type.Name);
            this.AssertNoPendingTransitionStatement(creator, "CreateMachine");

            if (mid == null)
            {
                mid = this.CreateMachineId(Type.GetType(type.AssemblyQualifiedName), friendlyName);
            }

            if (!PendingMachineCreations.ContainsKey(creator.Id))
            {
                PendingMachineCreations[creator.Id] = new List<Tuple<MachineId, Type, string, Event, Guid?>>();
            }

            this.PendingMachineCreations[creator.Id].Add(Tuple.Create(mid, type, friendlyName, e, operationGroupId));

            return mid;
        }

        /// <summary>
        /// Creates a new P# machine of the specified type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Machine</returns>
        protected override Machine CreateMachine(Type type)
        {
            return ReliableMachineFactory.Create(type, this.StateManager);
        }

        /// <summary>
        /// Checks if the constructor of the specified machine type exists in the cache.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Boolean</returns>
        protected override bool IsMachineCached(Type type)
        {
            return ReliableMachineFactory.IsCached(type);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        protected internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            var reliableSender = sender as ReliableMachine;
            if (reliableSender == null || reliableSender.CurrentTransaction == null)
            {
                base.SendEvent(mid, e, sender, options);
            }
            else
            {
                this.ScheduleSendEvent(mid, e, reliableSender, options);
            }
        }

        /// <summary>
        /// Schedules the sending of an asynchronous <see cref="Event"/> to a machine. Sending
        /// of the event is postponed until the sender has commited its current transaction.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        private void ScheduleSendEvent(MachineId mid, Event e, ReliableMachine sender, SendOptions options)
        {
            this.AssertCorrectCallerMachine(sender as Machine, "SendEvent");
            this.Assert(this.CreatedMachineIds.Contains(mid), "Cannot Send event {0} to a MachineId '{1}' that was never " +
                "previously bound to a machine of type {2}", e.GetType().FullName, mid.Value, mid);
            this.Assert(sender.CurrentTransaction != null, "Machine '{0}' tried to send an event using a null transaction.", sender.Id);

            if (!PendingEventSends.ContainsKey(sender.Id))
            {
                PendingEventSends[sender.Id] = new List<Tuple<MachineId, Event, SendOptions>>();
            }

            this.PendingEventSends[sender.Id].Add(Tuple.Create(mid, e, options));
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
                if (tx != null)
                {
                    if (this.Logger.Configuration.Verbose >= this.Logger.LoggingVerbosity)
                    {
                        this.Logger.WriteLine("<CommitLog> Machine '{0}' committed transaction '{1}'.", machine.Id, tx.TransactionId);
                    }
                    
                    if (this.PendingMachineCreations.ContainsKey(machine.Id))
                    {
                        // Create any pending machines.
                        foreach (var tup in this.PendingMachineCreations[machine.Id])
                        {
                            MachineId mid = tup.Item1;
                            Type type = tup.Item2;
                            string friendlyName = tup.Item3;
                            Event e = tup.Item4;
                            Guid? operationGroupId = tup.Item5;

                            mid = base.CreateMachine(mid, type, friendlyName, e, machine, operationGroupId);
                            this.CreatedMachines.Add(mid.ToString(), Tuple.Create(mid, type, e));
                        }

                        this.PendingMachineCreations.Remove(machine.Id);
                    }

                    if (this.PendingEventSends.ContainsKey(machine.Id))
                    {
                        // Send any pending events.
                        foreach (var tup in this.PendingEventSends[machine.Id])
                        {
                            MachineId mid = tup.Item1;
                            Event e = tup.Item2;
                            SendOptions options = tup.Item3;

                            base.SendEvent(mid, e, machine, options);
                        }

                        this.PendingEventSends.Remove(machine.Id);
                    }
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
        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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

        public override Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null)
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
        #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        #endregion
    }
}

