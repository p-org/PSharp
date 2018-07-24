namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Services.Remoting;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.Threading.Tasks;

    [ServiceContract]
    public interface IResourceManager : IService
    {
        [OperationContract]
        Task<GetServicePartitionResponse> GetServicePartitionAsync(GetServicePartitionRequest request);
        [OperationContract]
        Task<List<ResourceTypesResponse>> ListResourceTypesAsync();
        [OperationContract]
        Task<List<ResourceDetailsResponse>> ListResourcesAsync();
    }

    [DataContract]
    public class GetServicePartitionRequest
    {
        // The request ID to help with idempotency
        [DataMember]
        public Guid RequestId;

        // The type of resource of which service and partition details are required
        [DataMember]
        public string ResourceType;

        // The service requesting the information
        [DataMember]
        public Uri OwningService;

        // The service partition requesting the information
        [DataMember]
        public Guid OwningPartition;
    }

    [DataContract]
    public class GetServicePartitionResponse
    {
        // Result of the creation request
        [DataMember]
        public string Result;

        // The URI of the service of the resource type requested
        [DataMember]
        public Uri Service;

        // The partition ID of the service of the resource type requested
        [DataMember]
        public Guid Partition;
    }

    [DataContract]
    public class ResourceTypesResponse
    {
        // The resource type
        [DataMember]
        public string ResourceType;

        // The total number of resource managed in this service
        [DataMember]
        public ulong Count;

        [DataMember]
        public ulong MaxCapacity;
    }

    [DataContract]
    public class ResourceDetailsResponse
    {
        // The resource type
        [DataMember]
        public string ResourceType;

        [DataMember]
        public string ResourceId;
    }
}
