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
    public interface IRsmHost
    {
        #region Properties
        /// <summary>
        /// Unique RSM Id
        /// </summary>
        IRsmId Id { get;  }

        /// <summary>
        /// Currently executing transaction
        /// </summary>
        ITransaction CurrentTransaction { get;  }

        #endregion

        #region Reliable communication API
        /// <summary>
        /// Creates an RSM in the local partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startingEvent">Starting event for the machine</param>
        /// <returns>Unique identifier for the machine</returns>
        Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent) where T : ReliableStateMachine;

        /// <summary>
        /// Creates an RSM in the specified partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startingEvent">Starting event for the machine</param>
        /// <returns>Unique identifier for the machine</returns>
        Task<IRsmId> ReliableCreateMachine<T>(Event startingEvent, string partitionName) where T : ReliableStateMachine;

        /// <summary>
        /// Sends an event to an RSM
        /// </summary>
        /// <param name="target">Target RSM identifier</param>
        /// <param name="e">Event</param>
        /// <returns></returns>
        Task ReliableSend(IRsmId target, Event e);

        #endregion

        #region Reliable data API

        /// <summary>
        /// Creates reliable state using the underlying Service Fabric StateManger
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<T> GetOrAddAsync<T>(string name) where T : IReliableState;

        #endregion

        #region Notifications

        /// <summary>
        /// Notifies the host of an exception inside the RSM
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <param name="methodName">Method that threw the exception</param>
        void NotifyTxFailure(Exception ex, string methodName);

        /// <summary>
        /// Notifies the host that the machine has halted
        /// </summary>
        void NotifyHalt();

        #endregion

    }
}
