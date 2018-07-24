using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Query = System.Fabric.Query;

namespace Microsoft.PSharp.ServiceFabric
{
    class ResourceTypeLearnerBackgroundTask : BackgroundTask
    {
        private const string ResourceToPartitionDictionary = "ResourceToPartitionDictionary";
        private const string PartitionInformationDictionary = "PartitionInformationDictionary";
        private const string PSharpServiceEndpoint = "PSharpServiceEndpoint";
        private PSharpService service;

        //
        // Summary:
        // Gets this replica's Microsoft.ServiceFabric.Data.IReliableStateManager.
        public IReliableStateManager stateManager { get; }

        private ServiceProxyFactory proxyFactory;

        public TimeSpan waitTime { get;  }

        private IPSharpEventSourceLogger logger { get; set; }

        public ResourceTypeLearnerBackgroundTask(PSharpService service, TimeSpan waitTime, IPSharpEventSourceLogger logger)
        {
            this.service = service;
            this.stateManager = service.StateManager;
            this.waitTime = waitTime;
            this.logger = logger;
            this.proxyFactory = new ServiceProxyFactory((c) =>
            {
                return new FabricTransportServiceRemotingClientFactory(
                    serializationProvider: service.EventSerializationProvider
                    );
            });
        }

        protected override bool IsEnabled()
        {
            return true;
        }

        protected override async Task Run(CancellationToken token)
        {
            while (IsEnabled())
            {
                FabricClient fabricClient = new FabricClient();
                Query.PagedList<Query.Application> applicationList = null;
                try
                {
                    this.logger.Message("GetApplicationListAsync");
                    applicationList = await fabricClient.QueryManager.GetApplicationListAsync();
                }
                catch (Exception e)
                {
                    this.logger.Message(string.Format("Exception occurred in GetApplicationListAsync {0}", e.Message));
                }

                if (applicationList != null)
                {
                    foreach (Query.Application application in applicationList)
                    {
                        Query.PagedList<Query.Service> serviceList = null;
                        try
                        {
                            this.logger.Message(string.Format("GetServiceListAsync for application {0}", application.ApplicationName));
                            serviceList = await fabricClient.QueryManager.GetServiceListAsync(application.ApplicationName);
                        }
                        catch (Exception e)
                        {
                            this.logger.Message(string.Format("Exception occurred in GetServiceListAsync for application {0}, Exception {1}", application.ApplicationName, e));
                        }

                        if (serviceList != null)
                        {
                            foreach (Query.Service service in serviceList)
                            {
                                Query.PagedList<Query.Partition> partitionList = null;
                                try
                                {
                                    this.logger.Message(string.Format("GetPartitionListAsync for service {0}", service.ServiceName));
                                    partitionList = await fabricClient.QueryManager.GetPartitionListAsync(service.ServiceName);
                                }
                                catch (Exception e)
                                {
                                    this.logger.Message(string.Format("Exception occurred in GetPartitionListAsync for service {0}, Exception {1}", service.ServiceName, e));
                                }

                                if (partitionList != null)
                                {
                                    foreach (Query.Partition partition in partitionList)
                                    {
                                        try
                                        {
                                            await ProcessPartitionAsync(service, partition);
                                        }
                                        catch (Exception e)
                                        {
                                            this.logger.Message(string.Format("Exception occurred in ProcessPartitionAsync for partition {0} service {1} Exception {2}",
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

                await Task.Delay(WaitTime(), token);
            }
        }

        private async Task ProcessPartitionAsync(Query.Service service, Query.Partition partition)
        {
            this.logger.Message(string.Format("ProcessPartitionAsync for partition {0}", partition.PartitionInformation.Id));
            ServicePartitionResolver resolver = ServicePartitionResolver.GetDefault();
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
                this.logger.Message(
                    string.Format("ResolvedServicePartition or Endpoints are null for partition {0} service {1}", partition.PartitionInformation.Id, service.ServiceName));
                return;
            }

            foreach (ResolvedServiceEndpoint endpoint in resolved.Endpoints)
            {
                if (endpoint != null && !string.IsNullOrWhiteSpace(endpoint.Address) && endpoint.Address.Contains(PSharpServiceEndpoint))
                {
                    IPSharpService resourceManagerClient = this.proxyFactory.CreateServiceProxy<IPSharpService>(service.ServiceName, key,
                        listenerName: PSharpServiceEndpoint);
                    List<ResourceTypesResponse> responseList = await resourceManagerClient.ListResourceTypesAsync();
                    foreach (ResourceTypesResponse response in responseList)
                    {
                        try
                        {
                            await SaveResourceTypeInformation(response, service, partition);
                            this.logger.Message(string.Format("Successful SaveResourceTypeInformation for partition {0} resource type {1}, service {2}",
                                partition.PartitionInformation.Id,
                                response.ResourceType,
                                service.ServiceName));
                        }
                        catch (Exception e)
                        {
                            this.logger.Message(string.Format("Exception occurred in SaveResourceTypeInformation for partition {0} resource type {1}, service {2} Exception {3}",
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
            IReliableDictionary<string, HashSet<Guid>> resourceTypeToPartitionIds = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, HashSet<Guid>>>(ResourceToPartitionDictionary);
            IReliableDictionary<Guid, Tuple<Uri, double>> partitionInformation = await this.stateManager.GetOrAddAsync<IReliableDictionary<Guid, Tuple<Uri, double>>>(PartitionInformationDictionary);
            using (ITransaction transaction = this.stateManager.CreateTransaction())
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
                    Tuple.Create(service.ServiceName, ((double)response.Count) / response.MaxCapacity),
                    (key, oldvalue) => Tuple.Create(service.ServiceName, ((double)response.Count) / response.MaxCapacity));
                await transaction.CommitAsync();
            }
        }

        protected override TimeSpan WaitTime()
        {
            return this.waitTime;
        }
    }
}
