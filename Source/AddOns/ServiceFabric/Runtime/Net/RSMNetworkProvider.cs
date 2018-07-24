using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace Microsoft.PSharp.ServiceFabric.Net
{
    public class RsmNetworkProvider : Microsoft.PSharp.ServiceFabric.Net.IRsmNetworkProvider
    {
        readonly IServiceRemotingMessageSerializationProvider serializationProvider;

        // cache
        ConcurrentDictionary<string, IPSharpService> partitionToService;

        IRemoteMachineManager RemoteMachineManager;

        public RsmNetworkProvider(IRemoteMachineManager remoteMachineManager,
            IServiceRemotingMessageSerializationProvider serializationProvider)
        {
            this.serializationProvider = serializationProvider;
            this.RemoteMachineManager = remoteMachineManager;
            this.partitionToService = new ConcurrentDictionary<string, IPSharpService>();
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
            var service = GetService(endpoint);
            return service.CreateMachineId(machineType, friendlyName);
        }

        /// <summary>
        /// Creates a machine with the given ID
        /// </summary>
        /// <param name="machineType">Type of machine to create</param>
        /// <param name="mid">ID for the machine</param>
        /// <param name="e">Starting event</param>
        /// <returns></returns>
        public Task RemoteCreateMachine(Type machineType, MachineId mid, Event e)
        {
            var service = GetService(mid.Endpoint);
            return service.CreateMachine(mid, e);
        }

        /// <summary>
        /// Sends an event to a machine
        /// </summary>
        /// <param name="target">Target machine</param>
        /// <param name="e">Event</param>
        /// <returns></returns>
        public Task RemoteSend(MachineId target, Event e)
        {
            var service = GetService(target.Endpoint);
            return service.SendEvent(target, e);
        }

        private IPSharpService GetService(string endpoint)
        {
            IPSharpService service;

            if (partitionToService.TryGetValue(endpoint, out service))
            {
                return service;
            }

            var proxyFactory = new ServiceProxyFactory((c) =>
            {
                return new FabricTransportServiceRemotingClientFactory(
                    serializationProvider: serializationProvider
                    );
            });

            string serviceName, partitionName;
            RemoteMachineManager.ParseMachineIdEndpoint(endpoint, out serviceName, out partitionName);

            service = proxyFactory.CreateServiceProxy<IPSharpService>(
                new Uri(serviceName),
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partitionName));

            partitionToService.TryAdd(endpoint, service);
            return service;
        }
    }
}
