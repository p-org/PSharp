namespace Microsoft.PSharp.ServiceFabric
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

        public TaskCompletionSource<PSharpRuntime> RuntimeTcs { get; private set; }

        protected PSharpService(StatefulServiceContext serviceContext, IEnumerable<Type> knownTypes) : base(serviceContext)
        {
            var localKnownTypes = new List<Type> { typeof(Event), typeof(TaggedRemoteEvent) };
            localKnownTypes.AddRange(knownTypes);
            
            this.knownTypes = localKnownTypes;
            this.RuntimeTcs = new TaskCompletionSource<PSharpRuntime>();

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
                new EventDataSeralizer<Tuple<MachineId, string, Event>>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<Event>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if(!this.StateManager.TryAddStateSerializer(new EventDataSeralizer<Tuple<string, MachineId, Event>>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }
        }

        protected PSharpService(StatefulServiceContext serviceContext, IEnumerable<Type> knownTypes, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
        {
            var localKnownTypes = new List<Type> { typeof(Event), typeof(TaggedRemoteEvent) };
            localKnownTypes.AddRange(knownTypes);

            this.knownTypes = localKnownTypes;
            this.RuntimeTcs = new TaskCompletionSource<PSharpRuntime>();
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
                new EventDataSeralizer<Tuple<MachineId, string, Event>>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<Event>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if (!this.StateManager.TryAddStateSerializer(new EventDataSeralizer<Tuple<string, MachineId, Event>>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }
        }

        protected virtual IRemoteMachineManager GetMachineManager()
        {
            return new SingleProcessMachineManager();
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            IRemoteMachineManager machineManager = this.GetMachineManager();
            var runtime =
            ServiceFabricRuntimeFactory.Create(this.StateManager, this.GetRuntimeConfiguration(),
                machineManager,
                new Func<PSharpRuntime, Net.IRsmNetworkProvider>(r => new Net.RsmNetworkProvider(machineManager, eventSerializationProvider)));

            this.RuntimeTcs.SetResult(runtime);

            var logger = this.GetPSharpRuntimeLogger();
            if (logger != null)
            {
                runtime.SetLogger(new EventSourcePSharpLogger(logger));
            }

            // Lets have a runtime.Initialize(cancellationToken);
            // that should ideally be used for rehydration and restarting the machines

            await Task.Yield();
        }

        protected virtual IPSharpEventSourceLogger GetPSharpRuntimeLogger()
        {
            return null;
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

        public Task<GetServicePartitionResponse> GetServicePartitionAsync(GetServicePartitionRequest request)
        {
            throw new InvalidOperationException("Cannot create using resource manager API");
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

        public virtual async Task<MachineId> CreateMachineId(string machineType, string friendlyName)
        {
            var runtime = await RuntimeTcs.Task;
            return runtime.CreateMachineId(Type.GetType(machineType), friendlyName);
        }

        public virtual async Task CreateMachine(MachineId machineId, string machineType, Event e)
        {
            var runtime = await RuntimeTcs.Task;
            runtime.CreateMachine(machineId, Type.GetType(machineType), e);
        }

        public virtual async Task SendEvent(MachineId machineId, Event e)
        {
            var runtime = await RuntimeTcs.Task;
            runtime.SendEvent(machineId, e);
        }
    }
}
