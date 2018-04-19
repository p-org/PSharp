using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices.Net
{
    /// <summary>
    /// Provides inter-partition communication
    /// </summary>
    public interface IRsmNetworkProvider
    {
        /// <summary>
        /// Creates a new ID for a specified machine type and partition
        /// </summary>
        /// <typeparam name="T">Machine Type</typeparam>
        /// <param name="endpoint">Partition where to create the ID</param>
        /// <returns></returns>
        Task<IRsmId> RemoteCreateMachineId<T>(string endpoint) where T : ReliableStateMachine;

        /// <summary>
        /// Creates a machine with the given ID
        /// </summary>
        /// <typeparam name="T">Machine Type</typeparam>
        /// <param name="mid">ID for the machine</param>
        /// <param name="e">Starting event</param>
        /// <returns></returns>
        Task RemoteCreateMachine<T>(IRsmId mid, RsmInitEvent e) where T : ReliableStateMachine;

        /// <summary>
        /// Sends an event to a machine
        /// </summary>
        /// <param name="target">Target machine</param>
        /// <param name="e">Event</param>
        /// <returns></returns>
        Task RemoteSend(IRsmId target, Event e);

    }
}
