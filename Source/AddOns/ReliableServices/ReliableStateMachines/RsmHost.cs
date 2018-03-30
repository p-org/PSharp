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

        #endregion

        internal RsmHost(IReliableStateManager StateManager)
        {
            this.StateManager = StateManager;

            this.CurrentTransaction = null;
            this.Id = null;
            this.Runtime = null;
            this.Mid = null;
            
            StackChanges = new StackDelta();
        }

        public static RsmHost Create(IReliableStateManager stateManager, string partitionName)
        {
            var factory = new ServiceFabricRsmIdFactory(0, partitionName);
            return ServiceFabricRsmHost.Create(stateManager, factory);
        }

        #region Reliable communication API

        /// <summary>
        /// Creates an RSM in the local partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startingEvent">Starting event for the machine</param>
        /// <returns>Unique identifier for the machine</returns>
        public abstract Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent) where T : ReliableStateMachine;

        /// <summary>
        /// Creates an RSM in the specified partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startingEvent">Starting event for the machine</param>
        /// <returns>Unique identifier for the machine</returns>
        public abstract Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent, string partitionName) where T : ReliableStateMachine;

        /// <summary>
        /// Sends an event to an RSM
        /// </summary>
        /// <param name="target">Target RSM identifier</param>
        /// <param name="e">Event</param>
        /// <returns></returns>
        public abstract Task ReliableSend(IRsmId target, Event e);

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

        #endregion
    }
}
