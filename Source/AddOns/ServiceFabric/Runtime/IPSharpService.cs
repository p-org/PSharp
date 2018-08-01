namespace Microsoft.PSharp.ServiceFabric
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting;

    [ServiceContract]
    public interface IPSharpService : IService
    {
        [OperationContract]
        Task<MachineId> CreateMachineId(string machineType, string friendlyName);
        [OperationContract]
        Task CreateMachine(MachineId machineId, string machineType, Event e);
        [OperationContract]
        Task BulkCreateMachine(List<Tuple<MachineId, string, Event>> sendEvents);
        [OperationContract]
        Task SendEvent(MachineId machineId, Event e);
        [OperationContract]
        Task BulkSendEvent(List<Tuple<MachineId, Event>> sendEvents);

        // For other runtimes to learn
        [OperationContract]
        Task<List<ResourceTypesResponse>> ListResourceTypesAsync();
        [OperationContract]
        Task<List<ResourceDetailsResponse>> ListResourcesAsync();
    }
}
