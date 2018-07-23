using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ServiceFabric.Net
{
    /// <summary>
    /// Provides inter-partition communication
    /// </summary>
    public interface IRsmNetworkProvider
    {
        /// <summary>
        /// Creates a machine with the given ID
        /// </summary>
        /// <param name="machineType">Type of machine to create</param>
        /// <param name="mid">ID for the machine</param>
        /// <param name="e">Starting event</param>
        /// <returns></returns>
        Task RemoteCreateMachine(Type machineType, MachineId mid, Event e);

        /// <summary>
        /// Sends an event to a machine
        /// </summary>
        /// <param name="target">Target machine</param>
        /// <param name="e">Event</param>
        /// <returns></returns>
        Task RemoteSend(MachineId target, Event e);

    }
}
