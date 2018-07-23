﻿namespace Microsoft.PSharp.ServiceFabric
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    public abstract class PSharpService : StatefulService, IResourceManager, IPSharpService
    {
        private List<Type> knownTypes;
        private EventSerializationProvider eventSerializationProvider;

        protected PSharpService(StatefulServiceContext serviceContext, IEnumerable<Type> knownTypes) : base(serviceContext)
        {
            this.knownTypes = new List<Type>(knownTypes);
            //TODO: add framework known types here.

            this.eventSerializationProvider = new EventSerializationProvider(this.knownTypes);

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<MachineId>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }
            
            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<Tuple<MachineId, Event>>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<Event>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }
        }

        protected PSharpService(StatefulServiceContext serviceContext, IEnumerable<Type> knownTypes, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
        {
            this.knownTypes = new List<Type>(knownTypes);
            //TODO: add framework known types here.

            this.eventSerializationProvider = new EventSerializationProvider(this.knownTypes);

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<MachineId>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<Tuple<MachineId, Event>>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<Event>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ServiceFabricRuntimeFactory.Create(this.StateManager, this.GetRuntimeConfiguration());

            // Lets have a runtime.Initialize(cancellationToken);
            // that should ideally be used for rehydration and restarting the machines

            await Task.Yield();
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            List<ServiceReplicaListener> listeners = new List<ServiceReplicaListener>();
            KeyedCollection<string, EndpointResourceDescription> endpoints = this.Context.CodePackageActivationContext.GetEndpoints();
            ServiceReplicaListener listener1 = new ServiceReplicaListener((context) =>
            {
                return new FabricTransportServiceRemotingListener(context,
                    (IResourceManager)this,
                    new FabricTransportRemotingListenerSettings()
                    {
                        EndpointResourceName = "ResourceManagerServiceEndpoint",
                    });
            }, "ResourceManagerServiceEndpoint");

            ServiceReplicaListener listener2 = new ServiceReplicaListener((context) =>
            {
                return new FabricTransportServiceRemotingListener(context,
                    (IPSharpService)this,
                    new FabricTransportRemotingListenerSettings()
                    {
                        EndpointResourceName = "PSharpServiceEndpoint",
                    },
                    this.eventSerializationProvider);
            }, "PSharpServiceEndpoint");

            listeners.Add(listener1);
            listeners.Add(listener2);
            return listeners;
        }

        public Task<CreateResourceResponse> CreateResourceAsync(CreateResourceRequest request)
        {
            throw new InvalidOperationException("Cannot create using resource manager API");
        }

        public Task<DeleteResourceResponse> DeleteResourceAsync(DeleteResourceRequest request)
        {
            throw new InvalidOperationException("Cannot delete using resource manager API");
        }

        protected virtual Configuration GetRuntimeConfiguration()
        {
            // TODO: Setup something common
            return Configuration.Create().WithVerbosityEnabled(4);
        }

        public virtual Task<List<ResourceTypesResponse>> ListResourceTypesAsync()
        {
            //TODO: Implement
            return Task.FromResult(new List<ResourceTypesResponse>());
        }

        public virtual Task<List<ResourceDetailsResponse>> ListResourcesAsync()
        {
            //TODO: Implement
            return Task.FromResult(new List<ResourceDetailsResponse>());
        }
        
        public abstract Task<MachineId> CreateMachineId(string machineType, string friendlyName);

        public virtual Task CreateMachine(MachineId machineId, Event e)
        {
            //TODO: Implement
            return Task.FromResult(true);
        }

        public virtual Task SendEvent(MachineId machineId, Event e)
        {
            //TODO: Implement
            return Task.FromResult(true);
        }
    }
}
