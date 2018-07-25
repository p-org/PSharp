using System;

namespace Microsoft.PSharp.ServiceFabric.Net
{
    internal class NullNetworkProvider : PSharp.Net.INetworkProvider
    {
        string Endpoint;

        public NullNetworkProvider(string endpoint)
        {
            this.Endpoint = endpoint;
        }

        public void Dispose() { }

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
