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
        Task<CreateResourceResponse> CreateResourceAsync(CreateResourceRequest request);
        [OperationContract]
        Task<DeleteResourceResponse> DeleteResourceAsync(DeleteResourceRequest request);
        [OperationContract]
        Task<List<ResourceTypesResponse>> ListResourceTypesAsync();
        [OperationContract]
        Task<List<ResourceDetailsResponse>> ListResourcesAsync();
    }

    [DataContract]
    public class CreateResourceRequest
    {
        // The request ID to help with idempotency
        [DataMember]
        public Guid RequestId;

        // The type of resource to create in resource manager
        [DataMember]
        public string ResourceType;

        // The service requesting the resource creation
        [DataMember]
        public Uri OwningService;

        // The service partition requesting the resource creation
        [DataMember]
        public Guid OwningPartition;

        // The resource in the service requesting the resource creation
        [DataMember]
        public string OwningResource;
    }

    [DataContract]
    public class CreateResourceResponse
    {
        // Result of the creation request
        [DataMember]
        public string Result;

        // The URI of the service which owns the resource
        [DataMember]
        public Uri Service;

        // The partition ID of the service which owns the resource
        [DataMember]
        public Guid Partition;

        // The resource ID of the resource created
        [DataMember]
        public string ResourceId;
    }

    [DataContract]
    public class DeleteResourceRequest
    {
        // The request ID to help with idempotency
        [DataMember]
        public Guid RequestId;

        // The URI of the service which owns the resource
        [DataMember]
        public Uri Service;

        // The URI of the service which owns the resource
        [DataMember]
        public Guid Partition;

        // The resource ID of the resource created
        [DataMember]
        public string ResourceId;
    }

    [DataContract]
    public class DeleteResourceResponse
    {
        // Result of the deletion request
        [DataMember]
        public string Result;

        // The URI of the service which owns the resource
        [DataMember]
        public Uri Service;

        // The URI of the service which owns the resource
        [DataMember]
        public Guid Partition;

        // The resource ID of the resource created
        [DataMember]
        public string ResourceId;
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
