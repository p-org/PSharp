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
        Task<MachineId> CreateMachine(Guid requestId, string resourceType, Machine sender, CancellationToken token);

        Task SendEvent(MachineId id, Event e, AbstractMachine sender, SendOptions options, CancellationToken token);
    }
}
