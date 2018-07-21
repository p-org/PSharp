namespace Microsoft.PSharp.ServiceFabric
{
    using System.ServiceModel;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting;

    [ServiceContract]
    public interface IPSharpService : IService
    {
        [OperationContract]
        Task CreateMachine(MachineId machineId, MachineId creator, Event e);
        [OperationContract]
        Task SendEvent(MachineId machineId, MachineId sender, Event e);
    }
}
