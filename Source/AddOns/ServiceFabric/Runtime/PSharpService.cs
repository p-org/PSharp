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

    public abstract class PSharpService : StatefulService, IPSharpService
    {
        private static List<Type> KnownTypes = new List<Type>
        {
            typeof(Event),
            typeof(Halt),
            typeof(TaggedRemoteEvent),
            typeof(CreationRequest),
            typeof(MessageRequest),
            typeof(ResourceTypesResponse),
            typeof(ResourceDetailsResponse),
            // The below 2 types needs additional work
            typeof(List<ResourceTypesResponse>),
            typeof(List<ResourceDetailsResponse>)
        };

        private List<Type> knownTypes;
        internal EventSerializationProvider EventSerializationProvider;

        public TaskCompletionSource<PSharpRuntime> RuntimeTcs { get; private set; }
        protected IPSharpEventSourceLogger PSharpLogger { get; private set; }

        protected PSharpService(StatefulServiceContext serviceContext, IEnumerable<Type> knownTypes) : base(serviceContext)
        {
            var localKnownTypes = new List<Type>(KnownTypes);
            localKnownTypes.AddRange(knownTypes);
            
            this.knownTypes = localKnownTypes;
            this.RuntimeTcs = new TaskCompletionSource<PSharpRuntime>();

            this.EventSerializationProvider = new EventSerializationProvider(this.knownTypes);

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<MachineId>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<OutboxElement>(this.knownTypes)))
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

        protected PSharpService(StatefulServiceContext serviceContext, IEnumerable<Type> knownTypes, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
        {
            var localKnownTypes = new List<Type>(KnownTypes);
            localKnownTypes.AddRange(knownTypes);

            this.knownTypes = localKnownTypes;
            this.RuntimeTcs = new TaskCompletionSource<PSharpRuntime>();
            this.EventSerializationProvider = new EventSerializationProvider(this.knownTypes);

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<MachineId>(this.knownTypes)))
            {
                throw new InvalidOperationException("Failed to set custom Event serializer");
            }

            if (!this.StateManager.TryAddStateSerializer(
                new EventDataSeralizer<OutboxElement>(this.knownTypes)))
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
            this.PSharpLogger = this.GetPSharpRuntimeLogger();

            IRemoteMachineManager machineManager = this.GetMachineManager();
            await machineManager.Initialize(cancellationToken);

            var runtime = ServiceFabricRuntimeFactory.Create(this.StateManager, machineManager, this.GetRuntimeConfiguration(), cancellationToken,
                new Func<PSharpRuntime, Net.IRsmNetworkProvider>(r => new Net.RsmNetworkProvider(machineManager, EventSerializationProvider)));

            if (this.PSharpLogger != null)
            {
                runtime.SetLogger(new EventSourcePSharpLogger(this.PSharpLogger));
            }

            this.RuntimeTcs.SetResult(runtime);

            RuntimeLoadReporter reporter = new RuntimeLoadReporter(this, this.PSharpLogger, this.Partition);

            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            reporter.Start(cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

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
            
            ServiceReplicaListener listener = new ServiceReplicaListener((context) =>
            {
                return new FabricTransportServiceRemotingListener(context,
                    (IPSharpService)this,
                    new FabricTransportRemotingListenerSettings()
                    {
                        EndpointResourceName = "PSharpServiceEndpoint",
                    },
                    this.EventSerializationProvider);
            }, "PSharpServiceEndpoint");

            listeners.Add(listener);
            return listeners;
        }

        protected virtual Configuration GetRuntimeConfiguration()
        {
            // TODO: Setup something common
            return Configuration.Create().WithVerbosityEnabled(4);
        }

        public virtual async Task<List<ResourceTypesResponse>> ListResourceTypesAsync()
        {
            Dictionary<string, ulong> typeMapping = new Dictionary<string, ulong>();
            var details = new List<ResourceTypesResponse>();
            var types = this.GetMachineTypesWithMaxLoad();
            var runtime = await this.RuntimeTcs.Task;
            HashSet<MachineId> list = runtime.GetCreatedMachines();
            foreach (MachineId id in list)
            {
                if (!typeMapping.ContainsKey(id.Type))
                {
                    typeMapping[id.Type] = 1;
                }
                else
                {
                    typeMapping[id.Type] = typeMapping[id.Type]  + 1;
                }
            }

            foreach (var type in types)
            {
                ResourceTypesResponse response = new ResourceTypesResponse();
                response.ResourceType = type.Key.FullName;
                response.MaxCapacity = type.Value;
                response.Count = typeMapping.ContainsKey(type.Key.FullName) ? typeMapping[type.Key.FullName] : 0;

                details.Add(response);
            }

            return details;
        }

        public virtual Task<List<ResourceDetailsResponse>> ListResourcesAsync()
        {
            //TODO: Implement - contact runtime and report
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

        protected virtual Dictionary<Type, ulong> GetMachineTypesWithMaxLoad()
        {
            return new Dictionary<Type, ulong>();
        }
    }
}
