#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.PSharp;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    using System;
    using System.Collections.Concurrent;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Threading;
    using System.Threading.Tasks;

    public class ResourceManagerBasedRemoteMachineManager : IRemoteMachineManager
    {
        private const string Delimiter = "|||";
        private readonly ConcurrentDictionary<Uri, IResourceManager> serviceProxyMap;
        private readonly FabricClient fabricClient;
        private IReliableStateManager StateManager;

        public ResourceManagerBasedRemoteMachineManager(StatefulServiceContext context, IReliableStateManager manager, string resourceManagerService)
        {
            this.ServiceContext = context;
            this.ResourceManagerServiceLocation = new Uri(resourceManagerService);
            this.serviceProxyMap = new ConcurrentDictionary<Uri, IResourceManager>();
            this.fabricClient = new FabricClient();
            this.StateManager = manager;
        }

        public StatefulServiceContext ServiceContext { get; }
        public Uri ResourceManagerServiceLocation { get; }

        public string GetLocalEndpoint()
        {
            return this.ServiceContext.ServiceName + Delimiter + this.ServiceContext.PartitionId;
        }

        public async Task<string> CreateMachineIdEndpoint(Type machineType)
        {
            IResourceManager manager = this.serviceProxyMap.GetOrAdd(this.ResourceManagerServiceLocation, GetResourceManagerProxy);
            GetServicePartitionRequest request = new GetServicePartitionRequest();
            request.OwningService = this.ServiceContext.ServiceName;
            request.OwningPartition = this.ServiceContext.PartitionId;
            request.RequestId = Guid.Empty;  // not needed
            request.ResourceType = machineType.AssemblyQualifiedName;
            GetServicePartitionResponse response = await manager.GetServicePartitionAsync(request);
            return response.Service + Delimiter + response.Partition;
        }

        private IResourceManager GetResourceManagerProxy(Uri arg)
        {
            return ServiceProxy.Create<IResourceManager>(this.ResourceManagerServiceLocation);
        }

        public bool IsLocalMachine(MachineId id)
        {
            string[] parts = id.FriendlyName.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length != 2)
            {
                throw new InvalidOperationException($"Parts mismatch = Unable to find a machine with id = {id}");
            }

            if(new Uri(parts[0]) == this.ServiceContext.ServiceName && Guid.Parse(parts[1]) == this.ServiceContext.PartitionId)
            {
                return true;
            }

            return false;
        }

        public void ParseMachineIdEndpoint(string endpoint, out string serviceName, out string partitionName)
        {
            string[] parts = endpoint.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                throw new InvalidOperationException($"Parts mismatch = Unable to find a machine with id = {endpoint}");
            }

            serviceName = parts[0];
            partitionName = GetPartitionName(new Uri(serviceName), Guid.Parse(parts[1]));
        }

        private string GetPartitionName(Uri serviceName, Guid partitionId)
        {
            ServicePartitionList partitionList = fabricClient.QueryManager.GetPartitionListAsync(serviceName, partitionId).Result;
            foreach (Partition partition in partitionList)
            {
                NamedPartitionInformation namedPartitionInfo = partition.PartitionInformation as NamedPartitionInformation;
                return namedPartitionInfo.Name;
            }

            throw new InvalidOperationException($"Did not find Service {serviceName} with partition {partitionId}");
        }
    }
}
