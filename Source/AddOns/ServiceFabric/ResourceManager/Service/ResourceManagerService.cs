#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ResourceManager.SF
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using Query = System.Fabric.Query;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Data.Collections;

    public class ResourceManagerService : StatefulService, IResourceManager
    {
        private const string ResourceToPartitionDictionary = "ResourceToPartitionDictionary";
        private const string PartitionInformationDictionary = "PartitionInformationDictionary";
        private const string ResourceManagerServiceEndpoint = "ResourceManagerServiceEndpoint";

        public ResourceManagerService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext)
        {
            this.Logger = logger;
        }

        public ResourceManagerService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext, reliableStateManagerReplica)
        {
            this.Logger = logger;
        }

        public ILogger Logger { get; }

        public async Task<GetServicePartitionResponse> GetServicePartitionAsync(GetServicePartitionRequest request)
        {
            GetServicePartitionResponse response = null;
            string resourceType = request.ResourceType;
            ServiceEventSource.Current.Message(string.Format("Received GetServicePartitionAsync request for resource type {0}", resourceType));
            IReliableDictionary<string, HashSet<Guid>> resourceTypeToPartitionIds = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, HashSet<Guid>>>(ResourceToPartitionDictionary);
            IReliableDictionary<Guid, Tuple<Uri, double>> partitionInformation = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, Tuple<Uri, double>>>(PartitionInformationDictionary);
            using (ITransaction transaction = this.StateManager.CreateTransaction())
            {
                ConditionalValue<HashSet<Guid>> partitionList = await resourceTypeToPartitionIds.TryGetValueAsync(transaction, resourceType);
                if (partitionList.HasValue)
                {
                    double currentUtilization = 1.1;
                    foreach (Guid partitionId in partitionList.Value)
                    {
                        ConditionalValue<Tuple<Uri, double>> partitionsDetails = await partitionInformation.TryGetValueAsync(transaction, partitionId);
                        if(partitionsDetails.HasValue)
                        {
                            Tuple<Uri, double> detail = partitionsDetails.Value;
                            if(response == null)
                            {
                                response = new GetServicePartitionResponse();
                                response.Result = "Success";
                            }

                            if(currentUtilization > detail.Item2)
                            {
                                response.Partition = partitionId;
                                response.Service = detail.Item1;
                                currentUtilization = detail.Item2;
                            }
                        }
                        else
                        {
                            throw new ArgumentException("Details for Partition {0} not found", partitionId.ToString());
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Resource type of {0} not found", resourceType);
                }
                
                if(response == null)
                {
                    response = new GetServicePartitionResponse { Result = "Failure" };
                    ServiceEventSource.Current.Message(string.Format("Failed to get result GetServicePartitionAsync request for resource type {0}", resourceType));
                }
                else
                {
                    ServiceEventSource.Current.Message(string.Format("Sending response for resource type {0}, with service {1}, partition {2}", resourceType, response.Service, response.Partition));
                }

                return response;
            }
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
                        EndpointResourceName = ResourceManagerServiceEndpoint,
                    });
            }, ResourceManagerServiceEndpoint);

            listeners.Add(listener);
            return listeners;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                FabricClient fabricClient = new FabricClient();
                Query.PagedList<Query.Application> applicationList = null;
                try
                {
                    ServiceEventSource.Current.Message("GetApplicationListAsync");
                    applicationList = await fabricClient.QueryManager.GetApplicationListAsync();
                }
                catch (Exception e)
                {
                    ServiceEventSource.Current.Message(string.Format("Exception occurred in GetApplicationListAsync {0}", e.Message));
                }

                if (applicationList != null)
                {
                    foreach (Query.Application application in applicationList)
                    {
                        Query.PagedList<Query.Service> serviceList = null;
                        try
                        {
                            ServiceEventSource.Current.Message(string.Format("GetServiceListAsync for application {0}", application.ApplicationName));
                            serviceList = await fabricClient.QueryManager.GetServiceListAsync(application.ApplicationName);
                        }
                        catch (Exception e)
                        {
                            ServiceEventSource.Current.Message(string.Format("Exception occurred in GetServiceListAsync for application {0}, Exception {1}", application.ApplicationName, e));
                        }

                        if(serviceList != null)
                        {
                            foreach (Query.Service service in serviceList)
                            {
                                if(service.ServiceName == this.Context.ServiceName)
                                {
                                    continue;
                                }

                                Query.PagedList<Query.Partition> partitionList = null;
                                try
                                {
                                    ServiceEventSource.Current.Message(string.Format("GetPartitionListAsync for service {0}", service.ServiceName));
                                    partitionList = await fabricClient.QueryManager.GetPartitionListAsync(service.ServiceName);
                                }
                                catch (Exception e)
                                {
                                    ServiceEventSource.Current.Message(string.Format("Exception occurred in GetPartitionListAsync for service {0}, Exception {1}", service.ServiceName, e));
                                }

                                if(partitionList != null)
                                {
                                    foreach (Query.Partition partition in partitionList)
                                    {
                                        try
                                        {
                                            await ProcessPartitionAsync(service, partition);
                                        }
                                        catch (Exception e)
                                        {
                                            ServiceEventSource.Current.Message(string.Format("Exception occurred in ProcessPartitionAsync for partition {0} service {1} Exception {2}",
                                                partition.PartitionInformation.Id,
                                                service.ServiceName,
                                                e));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                await Task.Delay(10 * 1000, cancellationToken);
            }
        }

        private async Task ProcessPartitionAsync(Query.Service service, Query.Partition partition)
        {
            ServiceEventSource.Current.Message(string.Format("ProcessPartitionAsync for partition {0}", partition.PartitionInformation.Id));
            ServicePartitionResolver resolver =  ServicePartitionResolver.GetDefault();
            ServicePartitionKey key;
            switch (partition.PartitionInformation.Kind)
            {
                case ServicePartitionKind.Singleton:
                    key = ServicePartitionKey.Singleton;
                    break;
                case ServicePartitionKind.Int64Range:
                    var longKey = (Int64RangePartitionInformation)partition.PartitionInformation;
                    key = new ServicePartitionKey(longKey.LowKey);
                    break;
                case ServicePartitionKind.Named:
                    var namedKey = (NamedPartitionInformation)partition.PartitionInformation;
                    key = new ServicePartitionKey(namedKey.Name);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("partition.PartitionInformation.Kind");
            }

            ResolvedServicePartition resolved = await resolver.ResolveAsync(service.ServiceName, key, CancellationToken.None);
            if (resolved == null || resolved.Endpoints == null)
            {
                ServiceEventSource.Current.Message(
                    string.Format("ResolvedServicePartition or Endpoints are null for partition {0} service {1}", partition.PartitionInformation.Id, service.ServiceName));
                return;
            }

            foreach (ResolvedServiceEndpoint endpoint in resolved.Endpoints)
            {
                if(endpoint != null && !string.IsNullOrWhiteSpace(endpoint.Address) && endpoint.Address.Contains(ResourceManagerServiceEndpoint))
                {
                    IResourceManager resourceManagerClient = ServiceProxy.Create<IResourceManager>(service.ServiceName, key,
                        listenerName: ResourceManagerServiceEndpoint);
                    List<ResourceTypesResponse> responseList = await resourceManagerClient.ListResourceTypesAsync();
                    foreach(ResourceTypesResponse response in responseList)
                    {
                        try
                        {
                            await SaveResourceTypeInformation(response, service, partition);
                            ServiceEventSource.Current.Message(string.Format("Successful SaveResourceTypeInformation for partition {0} resource type {1}, service {2}",
                                partition.PartitionInformation.Id,
                                response.ResourceType,
                                service.ServiceName));
                        }
                        catch (Exception e)
                        {
                            ServiceEventSource.Current.Message(string.Format("Exception occurred in SaveResourceTypeInformation for partition {0} resource type {1}, service {2} Exception {3}",
                                partition.PartitionInformation.Id,
                                response.ResourceType,
                                service.ServiceName,
                                e));
                        }
                    }
                }
            }
        }

        private async Task SaveResourceTypeInformation(ResourceTypesResponse response, Query.Service service, Query.Partition partition)
        {
            IReliableDictionary<string, HashSet<Guid>> resourceTypeToPartitionIds = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, HashSet<Guid>>>(ResourceToPartitionDictionary);
            IReliableDictionary<Guid, Tuple<Uri, double>> partitionInformation = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, Tuple<Uri, double>>>(PartitionInformationDictionary);
            using (ITransaction transaction = this.StateManager.CreateTransaction())
            {

                await resourceTypeToPartitionIds.AddOrUpdateAsync(
                    transaction,
                    response.ResourceType,
                    new HashSet<Guid> { partition.PartitionInformation.Id },
                    (key, oldvalue) =>
                        {
                            oldvalue.Add(partition.PartitionInformation.Id);
                            return oldvalue;
                        });

                await partitionInformation.AddOrUpdateAsync(
                    transaction,
                    partition.PartitionInformation.Id,
                    Tuple.Create(service.ServiceName, ((double)response.Count)/response.MaxCapacity),
                    (key,oldvalue) => Tuple.Create(service.ServiceName, ((double)response.Count) / response.MaxCapacity));
                await transaction.CommitAsync();
            }
        }
    }
}
