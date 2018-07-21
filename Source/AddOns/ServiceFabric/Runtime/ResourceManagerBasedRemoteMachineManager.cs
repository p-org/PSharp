#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.PSharp;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    using System;
    using System.Collections.Concurrent;
    using System.Fabric;
    using System.Fabric.Health;
    using System.Fabric.Query;
    using System.Threading;
    using System.Threading.Tasks;

    public class ResourceManagerBasedRemoteMachineManager : AbstractRemoteMachineManager
    {
        private const string Delimiter = "|||";
        private readonly ConcurrentDictionary<Uri, IResourceManager> serviceProxyMap;

        public ResourceManagerBasedRemoteMachineManager(StatefulServiceContext context, IReliableStateManager manager, string resourceManagerService) : base(manager)
        {
            this.ServiceContext = context;
            this.ResourceManagerServiceLocation = new Uri(resourceManagerService);
            this.serviceProxyMap = new ConcurrentDictionary<Uri, IResourceManager>();
        }

        public StatefulServiceContext ServiceContext { get; }
        public Uri ResourceManagerServiceLocation { get; }

        public override async Task<MachineId> CreateMachine(Guid requestId, string resourceType, Machine sender, CancellationToken token)
        {
            IResourceManager manager = this.serviceProxyMap.GetOrAdd(this.ResourceManagerServiceLocation, GetResourceManagerProxy);
            CreateResourceRequest request = new CreateResourceRequest();
            request.OwningService = this.ServiceContext.ServiceName;
            request.OwningPartition = this.ServiceContext.PartitionId;
            request.OwningResource = sender == null ? "RUNTIME" : sender.Id.ToString();
            request.RequestId = requestId;
            request.ResourceType = resourceType;
            CreateResourceResponse response = await manager.CreateResourceAsync(request);
            string friendlyName = response.Service + Delimiter + response.Partition + Delimiter + response.ResourceId;
            MachineId id = new MachineId(resourceType, friendlyName, ServiceFabricRuntimeFactory.Current);
            return id;
        }

        private IResourceManager GetResourceManagerProxy(Uri arg)
        {
            return ServiceProxy.Create<IResourceManager>(this.ResourceManagerServiceLocation);
        }

        protected internal override bool IsLocalMachine(MachineId id)
        {
            string[] parts = id.FriendlyName.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length != 3)
            {
                throw new InvalidOperationException($"Parts mismatch = Unable to find a machine with id = {id}");
            }

            if(new Uri(parts[0]) == this.ServiceContext.ServiceName && Guid.Parse(parts[1]) == this.ServiceContext.PartitionId)
            {
                return true;
            }

            return false;
        }

        protected internal override async Task RemoteSend(MachineId id, Event e, AbstractMachine sender, SendOptions options, CancellationToken token)
        {
            string[] parts = id.FriendlyName.Split(new[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
            Uri serviceName = new Uri(parts[0]);
            Guid partitionId = Guid.Parse(parts[1]);
            // todo - create a service proxy for a known type on this partition
            throw new NotImplementedException();
        }
    }
}
