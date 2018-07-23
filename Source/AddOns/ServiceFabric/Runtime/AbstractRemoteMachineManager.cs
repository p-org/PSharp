#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.PSharp;
    using Microsoft.ServiceFabric.Data;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    // Not needed
    public abstract class AbstractRemoteMachineManager 
    {
        protected AbstractRemoteMachineManager(IReliableStateManager manager)
        {
            this.StateManager = manager;
        }

        protected IReliableStateManager StateManager { get; }
        public abstract bool IsLocalMachine(MachineId id);
        public abstract MachineId CreateMachineId(Type machineType, string friendlyName);
    }
}
