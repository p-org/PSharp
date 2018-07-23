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

        public string GetLocalEndpoint()
        {
            return "";
        }

        public Task<string> CreateMachineIdEndpoint(Type machineType)
        {
            return Task.FromResult("");
        }

        public void ParseMachineIdEndpoint(string endpoint, out string serviceName, out string partitionName)
        {
            serviceName = "";
            partitionName = "";
        }
    }
}
