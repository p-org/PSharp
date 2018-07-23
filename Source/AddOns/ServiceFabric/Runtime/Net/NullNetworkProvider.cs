using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ServiceFabric.Net
{
    class NullNetworkProvider : Microsoft.PSharp.Net.INetworkProvider
    {
        string Endpoint;

        public NullNetworkProvider(string endpoint)
        {
            this.Endpoint = endpoint;
        }

        public void Dispose()
        {
            
        }

        public string GetLocalEndpoint()
        {
            return Endpoint;
        }

        public MachineId RemoteCreateMachine(Type type, string friendlyName, string endpoint, Event e)
        {
            throw new NotImplementedException();
        }

        public void RemoteSend(MachineId target, Event e)
        {
            throw new NotImplementedException();
        }
    }
}
