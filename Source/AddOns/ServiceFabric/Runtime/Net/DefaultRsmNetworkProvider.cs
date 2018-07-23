using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ServiceFabric.Net
{
    class DefaultRsmNetworkProvider : IRsmNetworkProvider
    {
        ServiceFabricPSharpRuntime Runtime;

        public DefaultRsmNetworkProvider(ServiceFabricPSharpRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Creates a new ID for a specified machine type and partition
        /// </summary>
        /// <param name="friendlyName">Friendly name associated with the machine</param>
        /// <param name="endpoint">Partition where to create the ID</param>
        /// <param name="machineType">Type of the machine to bind to the ID</param>
        /// <returns></returns>
        public Task<MachineId> RemoteCreateMachineId(string machineType, string friendlyName, string endpoint)
        {
            return Task.FromResult(Runtime.CreateMachineId(Type.GetType(machineType), friendlyName));
        }

        public Task RemoteCreateMachine(Type machineType, MachineId mid, Event e)
        {
            Runtime.CreateMachine(mid, machineType, e);
            return Task.CompletedTask;
        }

        public Task RemoteSend(MachineId target, Event e)
        {
            Runtime.SendEvent(target, e);
            return Task.CompletedTask;
        }

    }

}
