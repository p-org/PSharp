#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ResourceManager.SF
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class ResourceManagerService : StatefulService, IResourceManager
    {
        public ResourceManagerService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext)
        {
            this.Logger = logger;
        }

        public ResourceManagerService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext, reliableStateManagerReplica)
        {
            this.Logger = logger;
        }

        public ILogger Logger { get; }

        public Task<CreateResourceResponse> CreateResourceAsync(CreateResourceRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<DeleteResourceResponse> DeleteResourceAsync(DeleteResourceRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<ResourceDetailsResponse>> ListResourcesAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<List<ResourceTypesResponse>> ListResourceTypesAsync()
        {
            throw new System.NotImplementedException();
        }
        
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            List<ServiceReplicaListener> listeners = new List<ServiceReplicaListener>();
            KeyedCollection<string, EndpointResourceDescription> endpoints = this.Context.CodePackageActivationContext.GetEndpoints();
            ServiceReplicaListener listener = new ServiceReplicaListener((context) =>
            {
                return new FabricTransportServiceRemotingListener(context, 
                    this, 
                    new FabricTransportRemotingListenerSettings()
                    {
                        EndpointResourceName = "ResourceManagerServiceEndpoint",
                    });
            });

            listeners.Add(listener);
            return listeners;
        }
    }
}
