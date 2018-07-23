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

namespace RSM
{
    public class RsmNetworkProvider : Microsoft.PSharp.ServiceFabric.Net.IRsmNetworkProvider
    {
        // partition name
        readonly string currentPartitionName;
        readonly Uri currentService;
        readonly IServiceRemotingMessageSerializationProvider serializationProvider;

        // cache
        ConcurrentDictionary<string, IPSharpService> partitionToService;

        IRemoteMachineManager RemoteMachineManager;

        public RsmNetworkProvider(string currentPartitionName, Uri currentService,
            IRemoteMachineManager remoteMachineManager,
            IServiceRemotingMessageSerializationProvider serializationProvider)
        {
            this.currentPartitionName = currentPartitionName;
            this.currentService = currentService;
            this.serializationProvider = serializationProvider;
            this.RemoteMachineManager = remoteMachineManager;
            this.partitionToService = new ConcurrentDictionary<string, IPSharpService>();
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
            var service = GetService(mid);
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
            var service = GetService(target);
            return service.SendEvent(target, e);
        }

        private IPSharpService GetService(MachineId mid)
        {
            IPSharpService service;

            if (partitionToService.TryGetValue(mid.Endpoint, out service))
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
            RemoteMachineManager.ParseMachineIdEndpoint(mid, out serviceName, out partitionName);

            service = proxyFactory.CreateServiceProxy<IPSharpService>(
                new Uri(serviceName),
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partitionName));

            partitionToService.TryAdd(mid.Endpoint, service);
            return service;
        }
    }
}
