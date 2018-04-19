using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices.Net
{
    class DefaultRsmNetworkProvider : IRsmNetworkProvider
    {
        RsmHost StartingHost;
        Dictionary<string, RsmHost> PartitionToHost;

        public DefaultRsmNetworkProvider(RsmHost startingHost)
        {
            this.StartingHost = startingHost;
            PartitionToHost = new Dictionary<string, RsmHost>();
            PartitionToHost.Add(startingHost.Id.PartitionName, startingHost);
        }

        public Task RemoteCreateMachine<T>(IRsmId mid, RsmInitEvent e) where T : ReliableStateMachine
        {
            var host = GetPartitionHost(mid.PartitionName);
            return host.ReliableCreateMachine<T>(mid, e, mid.PartitionName);
        }

        public Task<IRsmId> RemoteCreateMachineId<T>(string endpoint) where T : ReliableStateMachine
        {
            var host = GetPartitionHost(endpoint);
            return host.ReliableCreateMachineId<T>();
        }

        public Task RemoteSend(IRsmId target, Event e)
        {
            var host = GetPartitionHost(target.PartitionName);
            return host.ReliableSend(target, e);
        }

        private RsmHost GetPartitionHost(string partition)
        {
            lock(PartitionToHost)
            {
                if(PartitionToHost.ContainsKey(partition))
                {
                    return PartitionToHost[partition];
                }

                var host = StartingHost.CreateHost(partition);

                PartitionToHost.Add(partition, host);

                return host;
            }
        }
    }
}
