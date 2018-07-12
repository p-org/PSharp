using System;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    public class StateMachineRuntime : PSharpRuntime
    {
        protected StateMachineRuntime()
        {
        }

        protected StateMachineRuntime(Configuration configuration) : base(configuration)
        {
        }

        public override MachineId CreateMachine(Type type, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override void CreateMachine(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override MachineId CreateMachine(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override Task CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override Guid GetCurrentOperationGroupId(MachineId currentMachine)
        {
            throw new NotImplementedException();
        }

        public override void InvokeMonitor<T>(Event e)
        {
            throw new NotImplementedException();
        }

        public override void InvokeMonitor(Type type, Event e)
        {
            throw new NotImplementedException();
        }

        public override void RegisterMonitor(Type type)
        {
            throw new NotImplementedException();
        }

        public override MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override MachineId RemoteCreateMachine(Type type, string friendlyName, string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            throw new NotImplementedException();
        }

        public override void RemoteSendEvent(MachineId target, Event e, SendOptions options = null)
        {
            throw new NotImplementedException();
        }

        public override void SendEvent(MachineId target, Event e, SendOptions options = null)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        protected internal override MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            throw new NotImplementedException();
        }

        protected internal override Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            throw new NotImplementedException();
        }

        protected internal override MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint, Event e, Machine creator, Guid? operationGroupId)
        {
            throw new NotImplementedException();
        }

        protected internal override bool GetFairNondeterministicBooleanChoice(AbstractMachine machine, string uniqueId)
        {
            throw new NotImplementedException();
        }

        protected internal override bool GetNondeterministicBooleanChoice(AbstractMachine machine, int maxValue)
        {
            throw new NotImplementedException();
        }

        protected internal override int GetNondeterministicIntegerChoice(AbstractMachine machine, int maxValue)
        {
            throw new NotImplementedException();
        }

        protected internal override void Monitor(Type type, AbstractMachine sender, Event e)
        {
            throw new NotImplementedException();
        }

        protected internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            throw new NotImplementedException();
        }

        protected internal override Task<bool> SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            throw new NotImplementedException();
        }

        protected internal override void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            throw new NotImplementedException();
        }

        protected internal override void TryCreateMonitor(Type type)
        {
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
