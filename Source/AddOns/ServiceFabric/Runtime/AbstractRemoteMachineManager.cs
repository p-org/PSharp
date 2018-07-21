#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.PSharp;
    using Microsoft.ServiceFabric.Data;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class AbstractRemoteMachineManager : IRemoteMachineManager
    {
        protected AbstractRemoteMachineManager(IReliableStateManager manager)
        {
            this.StateManager = manager;
        }

        protected IReliableStateManager StateManager { get; }
        public abstract Task<MachineId> CreateMachine(Guid requestId, string resourceType, Machine sender, CancellationToken token);

        public async Task SendEvent(MachineId id, Event e, AbstractMachine sender, SendOptions options, CancellationToken token)
        {
            var reliableSender = sender as ReliableMachine;
            if (this.IsLocalMachine(id))
            {
                // QUESTION: For sending to local machines this is fine. For a remote machine this seems slightly confusing.
                var targetQueue = await StateManager.GetLocalMachineQueue(id);

                if (reliableSender == null || reliableSender.CurrentTransaction == null)
                {
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        await targetQueue.EnqueueAsync(tx, new EventInfo(e), token);
                        await tx.CommitAsync();
                    }
                }
                else
                {
                    await targetQueue.EnqueueAsync(reliableSender.CurrentTransaction, new EventInfo(e));
                }
            }
            else
            {
                await this.RemoteSend(id, e, sender, options, token);
            }
        }

        protected internal abstract Task RemoteSend(MachineId id, Event e, AbstractMachine sender, SendOptions options, CancellationToken token);
        public abstract bool IsLocalMachine(MachineId id);
    }
}
