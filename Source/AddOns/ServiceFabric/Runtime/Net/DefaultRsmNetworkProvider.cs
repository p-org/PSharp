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
