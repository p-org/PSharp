#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.PSharp;
    using Microsoft.ServiceFabric.Data;
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public class SingleProcessMachineManager : IRemoteMachineManager
    {
        public SingleProcessMachineManager() 
        {
            
        }

        public bool IsLocalMachine(MachineId id)
        {
            return true;
        }

        public Task<MachineId> CreateMachineId(Type machineType, string friendlyName)
        {
            return Task.FromResult(new MachineId(machineType, friendlyName, null));
        }

        public void ParseMachineIdEndpoint(MachineId mid, out string serviceName, out string partitionName)
        {
            serviceName = "";
            partitionName = "";
        }
    }
}
