using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Microsoft.PSharp.ServiceFabric.Net
{
    public class RsmNetworkProvider : Microsoft.PSharp.ServiceFabric.Net.IRsmNetworkProvider
    {
        private const string NetworkTable = "RSMNetworkProviderTable";
        private readonly IReliableStateManager stateManager;
        private CancellationToken token;
        readonly IServiceRemotingMessageSerializationProvider serializationProvider;
        private IReliableDictionary2<string, bool> endpointList;

        // cache
        ConcurrentDictionary<string, ServicePartitionMessageSender> partitionToService;

        IRemoteMachineManager RemoteMachineManager;
        private IPSharpEventSourceLogger pSharpLogger;

        public RsmNetworkProvider(IReliableStateManager stateManager, IRemoteMachineManager remoteMachineManager, IServiceRemotingMessageSerializationProvider serializationProvider, IPSharpEventSourceLogger pSharpLogger)
        {
            this.stateManager = stateManager;
            this.serializationProvider = serializationProvider;
            this.RemoteMachineManager = remoteMachineManager;
            this.partitionToService = new ConcurrentDictionary<string, ServicePartitionMessageSender>();
            this.pSharpLogger = pSharpLogger;
        }
        

        public async Task Initialize(CancellationToken token)
        {
            this.token = token;
            this.endpointList = await this.stateManager.GetOrAddAsync<IReliableDictionary2<string, bool>>(NetworkTable);
            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                var enumerable = await endpointList.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(this.token))
                {
                    token.ThrowIfCancellationRequested();
                    GetOrAdd(enumerator.Current.Key);
                }
            }
        }

        private ServicePartitionMessageSender GetOrAdd(string endpoint)
        {
            string serviceName, partitionName;
            this.RemoteMachineManager.ParseMachineIdEndpoint(endpoint, out serviceName, out partitionName);
            string newKey = serviceName + "|||" + partitionName;
            this.pSharpLogger.Message($"KEY {newKey} For ENDPOINT = {endpoint}");
            return this.partitionToService.GetOrAdd(newKey, (key) => 
            {
                var sender = new ServicePartitionMessageSender(serviceName, partitionName, this.stateManager, this.serializationProvider);
                sender.Initialize(this.token);
                return sender;
            });
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
            var service = GetOrAdd(endpoint);
            return service.RemoteCreateMachineId(machineType, friendlyName, endpoint);
        }

        /// <summary>
        /// Creates a machine with the given ID
        /// </summary>
        /// <param name="tx">The transaction</param>
        /// <param name="machineType">Type of machine to create</param>
        /// <param name="mid">ID for the machine</param>
        /// <param name="e">Starting event</param>
        /// <returns></returns>
        public Task RemoteCreateMachine(ITransaction tx, Type machineType, MachineId mid, Event e)
        {
            var service = GetOrAdd(mid.Endpoint);
            return service.RemoteCreateMachine(tx, machineType, mid, e);
        }

        /// <summary>
        /// Sends an event to a machine
        /// </summary>
        /// <param name="tx">The transaction</param>
        /// <param name="target">Target machine</param>
        /// <param name="e">Event</param>
        /// <returns></returns>
        public Task RemoteSend(ITransaction tx, MachineId target, Event e)
        {
            var service = GetOrAdd(target.Endpoint);
            return service.RemoteSend(tx, target, e);
        }
    }
}
