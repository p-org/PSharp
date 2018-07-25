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
        ///  Call initialize before using the manager
        /// </summary>
        /// <param name="token">The cancellation token</param>
        /// <returns>A task</returns>
        Task Initialize(CancellationToken token);

        /// <summary>
        /// Returns true if the MachineId is local
        /// </summary>
        /// <param name="mid"></param>
        /// <returns></returns>
        bool IsLocalMachine(MachineId mid);

        /// <summary>
        /// Returns local endpoint
        /// </summary>
        /// <returns></returns>
        string GetLocalEndpoint();

        /// <summary>
        /// Returns a fresh MachineId (perhaps remote)
        /// </summary>
        /// <param name="machineType"></param>
        /// <returns>Endpoint</returns>
        Task<string> CreateMachineIdEndpoint(Type machineType);

        /// <summary>
        /// Returns the service and partition hosting the given MachineId
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="serviceName"></param>
        /// <param name="partitionName"></param>
        void ParseMachineIdEndpoint(string endpoint, out string serviceName, out string partitionName);
    }
}
