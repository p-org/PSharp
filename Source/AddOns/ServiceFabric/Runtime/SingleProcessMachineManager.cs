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

    public class SingleProcessMachineManager : AbstractRemoteMachineManager
    {
        ConcurrentDictionary<string, Type> typeMap;

        public SingleProcessMachineManager(IReliableStateManager manager) : base(manager)
        {
            this.typeMap = new ConcurrentDictionary<string, Type>();
        }

        public override Task<MachineId> CreateMachine(Guid requestId, string resourceType, Machine creator, CancellationToken token)
        {
            Type type = this.typeMap.GetOrAdd(resourceType, CreateType);
            return ServiceFabricRuntimeFactory.Current.CreateMachineAsync(null, type, requestId.ToString(), null, creator, null);
        }

        private Type CreateType(string typeFullName)
        {
            foreach (Assembly item in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in item.GetTypes())
                {
                    if(type.FullName == typeFullName)
                    {
                        return type;
                    }
                }
            }

            throw new InvalidOperationException($"Unable to find type {typeFullName}");
        }

        protected internal override bool IsLocalMachine(MachineId id)
        {
            return true;
        }

        protected internal override Task RemoteSend(MachineId id, Event e, AbstractMachine sender, SendOptions options, CancellationToken token)
        {
            throw new InvalidOperationException($"RemoteSend unexpected for {id} as all machines are supposed to be local machines");
        }
    }
}
