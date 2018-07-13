namespace ResourceManager.SF
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using Grpc.Core;
    using Microsoft.Extensions.Logging;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Grpc;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class ResourceManagerService : StatefulService
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

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            List<ServiceReplicaListener> listeners = new List<ServiceReplicaListener>();
            KeyedCollection<string, EndpointResourceDescription> endpoints = this.Context.CodePackageActivationContext.GetEndpoints();
            ServiceReplicaListener listener = new ServiceReplicaListener((context) =>
            {
                return new GrpcServiceListener(context, this.Logger, new List<ServerServiceDefinition>()
                {
                    Contracts.ResourceManagerService.BindService(new RmRpc(context, this.Logger, this.StateManager))
                }, 
                "ResourceManagerEndpoint");
            });

            listeners.Add(listener);
            return listeners;
        }
    }
}
