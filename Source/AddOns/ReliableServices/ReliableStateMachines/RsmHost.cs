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
    public abstract class RsmHost
    {
        #region fields
        /// <summary>
        /// Unique RSM Id
        /// </summary>
        public IRsmId Id { get; protected set; }

        /// <summary>
        /// Currently executing transaction
        /// </summary>
        public ITransaction CurrentTransaction { get; protected set; }

        /// <summary>
        /// Hosted P# runtime
        /// </summary>
        internal PSharpRuntime Runtime;

        /// <summary>
        /// Hosted machine ID
        /// </summary>
        internal MachineId Mid;

        /// <summary>
        /// Changes made to the machine stack
        /// </summary>
        internal StackDelta StackChanges;

        /// <summary>
        /// State Manager
        /// </summary>
        internal IReliableStateManager StateManager;

        /// <summary>
        /// ReliableRegisters created by the machine
        /// </summary>
        List<Utilities.RsmRegister> CreatedRegisters;

        /// <summary>
        /// Does this RSM host a machine
        /// </summary>
        internal bool MachineHosted;

        /// <summary>
        /// Set of active timers
        /// </summary>
        protected IReliableDictionary<string, Timers.ReliableTimerConfig> Timers;

        /// <summary>
        /// Pending set of timers to be started
        /// </summary>
        protected Dictionary<string, Timers.ReliableTimerConfig> PendingTimerCreations;

        /// <summary>
        /// Pending set of timers to be stopped
        /// </summary>
        protected HashSet<string> PendingTimerRemovals;

        /// <summary>
        /// Active timers
        /// </summary>
        internal Dictionary<string, Timers.ISingleTimer> TimerObjects;


        #endregion

        internal RsmHost(IReliableStateManager StateManager)
        {
            this.StateManager = StateManager;

            this.CurrentTransaction = null;
            this.Id = null;
            this.Runtime = null;
            this.Mid = null;
            this.MachineHosted = false;

            StackChanges = new StackDelta();
            CreatedRegisters = new List<Utilities.RsmRegister>();

            this.TimerObjects = new Dictionary<string, Timers.ISingleTimer>();
            this.PendingTimerCreations = new Dictionary<string, Timers.ReliableTimerConfig>();
            this.PendingTimerRemovals = new HashSet<string>();
        }

        public static RsmHost Create(IReliableStateManager stateManager, string partitionName, Configuration psharpConfig)
        {
            var factory = new ServiceFabricRsmIdFactory(0, partitionName);
            return ServiceFabricRsmHost.Create(stateManager, factory, psharpConfig);
        }

        public static RsmHost CreateForTesting(IReliableStateManager stateManager, string partitionName, PSharpRuntime runtime)
        {
            return BugFindingRsmHost.Create(stateManager, partitionName, runtime);
        }

        /// <summary>
        /// Create a host with the specified partition name
        /// </summary>
        /// <param name="partition">Partition Name</param>
        /// <returns>Host</returns>
        internal abstract RsmHost CreateHost(string partition);

        #region Reliable communication API

        /// <summary>
        /// Creates a fresh Id
        /// </summary>
        /// <typeparam name="T">Machine Type</typeparam>
        /// <returns>Unique Id</returns>
        public abstract Task<IRsmId> ReliableCreateMachineId<T>() where T : ReliableStateMachine;

        /// <summary>
        /// Creates an RSM in the local partition
        /// </summary>
        /// <typeparam name="T">Machine Type</typeparam>
        /// <param name="startingEvent">Starting event for the machine</param>
        /// <returns>Unique identifier for the machine</returns>
        public abstract Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent) where T : ReliableStateMachine;

        /// <summary>
        /// Creates an RSM in the specified partition
        /// </summary>
        /// <typeparam name="T">Machine Type</typeparam>
        /// <param name="startingEvent">Starting event for the machine</param>
        public abstract Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent, string partitionName) where T : ReliableStateMachine;

        /// <summary>
        /// Creates an RSM in the specified partition with the given ID
        /// </summary>
        /// <typeparam name="T">Machine Type</typeparam>
        /// <param name="id">ID to attach to the machine</param>
        /// <param name="startingEvent">Starting event for the machine</param>
        /// <param name="partitionName">Partition where the machine will be created</param>
        public abstract Task ReliableCreateMachine<T>(IRsmId id, RsmInitEvent startingEvent, string partitionName) where T : ReliableStateMachine;

        /// <summary>
        /// Sends an event to an RSM
        /// </summary>
        /// <param name="target">Target RSM identifier</param>
        /// <param name="e">Event</param>
        /// <returns></returns>
        public abstract Task ReliableSend(IRsmId target, Event e);

        /// <summary>
        /// Starts a periodic timer
        /// </summary>
        /// <param name="name">Name of the timer</param>
        /// <param name="period">Periodic interval (ms)</param>
        /// <returns></returns>
        public async Task StartTimer(string name, int period)
        {
            var config = new Timers.ReliableTimerConfig(name, period);
            var success = await Timers.TryAddAsync(CurrentTransaction, name, config);
            if (!success)
            {
                // TODO: Specific exception
                throw new Exception($"Timer {name} already started");
            }

            PendingTimerCreations.Add(name, config);
            PendingTimerRemovals.Remove(name);
        }

        /// <summary>
        /// Stops a timer
        /// </summary>
        /// <param name="name">Name of the timer</param>
        /// <returns></returns>
        public async Task StopTimer(string name)
        {
            var cv = await Timers.TryRemoveAsync(CurrentTransaction, name);
            if (!cv.HasValue)
            {
                // TODO: Specific exception
                throw new Exception($"Attempt to stop a timer {name} that was not started (or stopped already)");
            }

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

        #endregion

        #region Reliable data API

        /// <summary>
        /// Creates reliable state using the underlying Service Fabric StateManger
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public Task<T> GetOrAddAsync<T>(string name) where T : IReliableState
        {
            return StateManager.GetOrAddAsync<T>(RsmQualifliedName(this.Id, name));
        }

        /// <summary>
        /// Creates reliable state using the underlying Service Fabric StateManger
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public Utilities.ReliableRegister<T> GetOrAddRegister<T>(string name, T initialValue = default(T)) 
        {
            var reg = new Utilities.ReliableRegister<T>(RsmQualifliedName(this.Id, name), this.StateManager, initialValue);
            reg.SetTransaction(this.CurrentTransaction);
            CreatedRegisters.Add(reg);
            return reg;
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Notifies the host of an exception inside the RSM
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <param name="methodName">Method that threw the exception</param>
        internal abstract void NotifyFailure(Exception ex, string methodName);

        /// <summary>
        /// Notifies the host that the machine has halted
        /// </summary>
        internal abstract void NotifyHalt();

        /// <summary>
        /// Notifies the host that the machine popped a state
        /// </summary>
        internal void NotifyStatePop()
        {
            StackChanges.Pop();
        }

        /// <summary>
        /// Notifies the host that the machine pushed a state
        /// </summary>
        internal void NotifyStatePush(string state)
        {
            StackChanges.Push(state);
        }

        #endregion

        #region Helpers

        internal static string RsmQualifliedName(IRsmId id, string name)
        {
            return string.Format("{0}.{1}", name, id.Name);
        }

        internal void SetReliableRegisterTx()
        {
            foreach(var reg in CreatedRegisters)
            {
                reg.SetTransaction(this.CurrentTransaction);
            }
        }
        #endregion
    }
}
