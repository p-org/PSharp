﻿namespace Microsoft.PSharp.ServiceFabric
{
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
        Task SendEvent(MachineId machineId, Event e);

        // For other runtimes to learn
        [OperationContract]
        Task<List<ResourceTypesResponse>> ListResourceTypesAsync();
        [OperationContract]
        Task<List<ResourceDetailsResponse>> ListResourcesAsync();
    }
}
