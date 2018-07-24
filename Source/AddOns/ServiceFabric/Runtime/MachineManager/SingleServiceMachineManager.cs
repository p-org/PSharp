#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.PSharp;
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public class SingleServiceMachineManager : IRemoteMachineManager
    {
        string ServiceName;
        string PartitionName;
        Dictionary<Type, string> TypeToPartitionMap;
        
        public SingleServiceMachineManager(string localServiceName, string localPartitionName, Dictionary<Type, string> typeToPartitionMap)
        {
            this.ServiceName = localServiceName;
            this.PartitionName = localPartitionName;
            this.TypeToPartitionMap = typeToPartitionMap;
        }

        public bool IsLocalMachine(MachineId id)
        {
            return (id.Endpoint == PartitionName);
        }

        public string GetLocalEndpoint()
        {
            return PartitionName;
        }

        public Task<string> CreateMachineIdEndpoint(Type machineType)
        {
            if (TypeToPartitionMap.ContainsKey(machineType))
            {
                return Task.FromResult(TypeToPartitionMap[machineType]);
            }
            else
            {
                return Task.FromResult(PartitionName);
            }
        }

        public void ParseMachineIdEndpoint(string endpoint, out string serviceName, out string partitionName)
        {
            serviceName = this.ServiceName;
            partitionName = endpoint;
        }

        public Task Initialize(CancellationToken token)
        {
            return Task.FromResult(true);
        }
    }
}
