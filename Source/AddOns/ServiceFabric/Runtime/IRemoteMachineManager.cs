#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.PSharp;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// All creates and sends goes via this abstraction
    /// This layer is responsible for talking to "Resource manager" to determine the location of where this machine needs to be created.
    /// In some cases, the machine could also be a local instance (for which we might not have to go through Resource manager)
    /// This layer is also responsible for translating MachineId to a Service/Partition/ResourceId and vice versa.
    /// </summary>
    public interface IRemoteMachineManager
    {
        /// <summary>
        /// Returns true if the MachineId is local
        /// </summary>
        /// <param name="mid"></param>
        /// <returns></returns>
        bool IsLocalMachine(MachineId mid);

        /// <summary>
        /// Returns a fresh MachineId (perhaps remote)
        /// </summary>
        /// <param name="machineType"></param>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        Task<MachineId> CreateMachineId(Type machineType, string friendlyName);

        /// <summary>
        /// Returns the service and partition hosting the given MachineId
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="serviceName"></param>
        /// <param name="partitionName"></param>
        void ParseMachineIdEndpoint(MachineId mid, out string serviceName, out string partitionName);
    }
}
