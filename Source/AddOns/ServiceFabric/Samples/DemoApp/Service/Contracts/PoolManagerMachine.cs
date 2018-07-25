namespace PoolServicesContract
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PSharp;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    public class PoolManagerMachine : ReliableMachine
    {
        private const string VMMachineIdDictionary = "VMMachineIdDictionary";

        private IReliableDictionary<MachineId, List<MachineId>> vMMachineIdTable;

        public PoolManagerMachine(IReliableStateManager stateManager) : base(stateManager)
        {
        }

        protected override async Task OnActivate()
        {
            vMMachineIdTable = await this.StateManager.GetOrAddAsync<IReliableDictionary<MachineId, List<MachineId>>>(VMMachineIdDictionary);
        }

        [Start]
        [OnEntry(nameof(ResizePool))]
        [OnEventDoAction(typeof(ePoolResizeRequestEvent), nameof(ResizePool))]
        [OnEventGotoState(typeof(ePoolDeletionRequestEvent), typeof(Deleting))]
        class Resizing : MachineState
        {
        }

        [OnEntry(nameof(DeletePool))]
        class Deleting : MachineState
        {
        }

        private async Task ResizePool()
        {
            ePoolResizeRequestEvent resizeRequest = this.ReceivedEvent as ePoolResizeRequestEvent;
            this.Logger.WriteLine($"PoolManagerMachine Resize requested for pool {this.Id} and size {resizeRequest.Size}");
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                bool commit = false;
                ConditionalValue<List<MachineId>> createdVMList = await vMMachineIdTable.TryGetValueAsync(tx, this.Id);
                int difference;
                List<MachineId> newVMList = null;
                if (createdVMList.HasValue)
                {
                    difference = resizeRequest.Size - createdVMList.Value.Count;
                    newVMList = new List<MachineId>(createdVMList.Value);
                }
                else
                {
                    difference = resizeRequest.Size;
                    newVMList = new List<MachineId>();
                }

                this.Logger.WriteLine($"PoolManagerMachine Required VMs for pool {this.Id} is {difference}");

                if(difference < 0)
                {
                    while (difference++ < 0)
                    {
                        MachineId machineId = newVMList[0];
                        newVMList.Remove(machineId);
                        this.Send(machineId, new eVMDeleteRequestEvent(this.Id));
                    }
                }
                else
                {
                    while (difference-- < 0)
                    {
                        commit = true;
                        newVMList.Add(this.CreateMachine(typeof(VMManagerMachine), new eVMCreateRequestEvent(this.Id)));
                    }
                }

                if(commit)
                {
                    await vMMachineIdTable.AddOrUpdateAsync(tx, this.Id, newVMList, (key, oldvalue) => newVMList);
                    await tx.CommitAsync();
                }
            }
        }

        private async Task DeletePool()
        {
            this.Logger.WriteLine($"PoolManagerMachine Deletion request of pool {this.Id}");
            ePoolDeletionRequestEvent deleteEvent = this.ReceivedEvent as ePoolDeletionRequestEvent;
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                bool commit = false;
                ConditionalValue<List<MachineId>> createdVMList = await vMMachineIdTable.TryGetValueAsync(tx, this.Id);
                if (createdVMList.HasValue)
                {
                    commit = true;
                    foreach(MachineId machineId in createdVMList.Value)
                    {
                        this.Send(machineId, new eVMDeleteRequestEvent(this.Id));
                    }
                }
                else
                {
                    this.Logger.WriteLine($"PoolManagerMachine Delete Request Pool Not Found {this.Id}");
                }

                if (commit)
                {
                    await vMMachineIdTable.TryRemoveAsync(tx, this.Id);
                    await tx.CommitAsync();
                }
            }

            this.Send(this.Id, new Halt());
        }
    }
}
