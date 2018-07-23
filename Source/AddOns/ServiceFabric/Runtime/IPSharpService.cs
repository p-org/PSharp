namespace Microsoft.PSharp.ServiceFabric
{
    using System.ServiceModel;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting;

    [ServiceContract]
    public interface IPSharpService : IService
    {
        [OperationContract]
        Task<MachineId> CreateMachineId(string machineType, string friendlyName);
        [OperationContract]
        Task CreateMachine(MachineId machineId, Event e);
        [OperationContract]
        Task SendEvent(MachineId machineId, Event e);
    }
}
