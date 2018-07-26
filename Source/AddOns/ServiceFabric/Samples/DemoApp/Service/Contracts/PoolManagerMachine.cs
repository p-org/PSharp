namespace PoolServicesContract
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PSharp;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    public class PoolManagerMachine : ReliableMachine
    {
        private const string VMCreatedDictionary = "VMCreatedDictionary";
        private const string VMCreatingDictionary = "VMCreatingDictionary";
        private const string VMDeletingDictionary = "VMDeletingDictionary";

        private IReliableDictionary2<MachineId, bool> VMCreatedTable;
        private IReliableDictionary2<MachineId, bool> VMCreatingTable;
        private IReliableDictionary2<MachineId, bool> VMDeletingTable;

        public PoolManagerMachine(IReliableStateManager stateManager) : base(stateManager)
        {
        }

        protected override async Task OnActivate()
        {
            VMCreatedTable = await this.StateManager.GetOrAddAsync<IReliableDictionary2<MachineId, bool>>(VMCreatedDictionary + this.Id.ToString());
            VMCreatingTable = await this.StateManager.GetOrAddAsync<IReliableDictionary2<MachineId, bool>>(VMCreatingDictionary + this.Id.ToString());
            VMDeletingTable = await this.StateManager.GetOrAddAsync<IReliableDictionary2<MachineId, bool>>(VMDeletingDictionary + this.Id.ToString());
        }

        [Start]
        [OnEntry(nameof(ResizePool))]
        [OnEventDoAction(typeof(ePoolResizeRequestEvent), nameof(ResizePool))]
        [OnEventGotoState(typeof(ePoolDeletionRequestEvent), typeof(DeletingPool))]
        [OnEventDoAction(typeof(eVMCreateFailureRequestEvent), nameof(RetryCreateVM))]
        [OnEventDoAction(typeof(eVMCreateSuccessRequestEvent), nameof(OnVMCreated))]
        [OnEventDoAction(typeof(eVMDeleteFailureRequestEvent), nameof(RetryDeleteVM))]
        [OnEventDoAction(typeof(eVMDeleteSuccessRequestEvent), nameof(OnVMDeleted))]
        class ResizingPool : MachineState
        {
        }

        [OnEntry(nameof(DeletePool))]
        [OnEventDoAction(typeof(ePoolDeletionRequestEvent), nameof(DeletePool))]
        [OnEventDoAction(typeof(eVMDeleteSuccessRequestEvent), nameof(OnVMDeleted))]
        [OnEventDoAction(typeof(eVMDeleteFailureRequestEvent), nameof(RetryDeleteVM))]
        class DeletingPool : MachineState
        {
        }

        private async Task OnVMCreated()
        {
            eVMCreateSuccessRequestEvent request = this.ReceivedEvent as eVMCreateSuccessRequestEvent;
            this.Logger.WriteLine($"PM- {this.Id} received VM Create Success for {request.senderId}");
            await VMCreatedTable.AddOrUpdateAsync(
                this.CurrentTransaction,
                request.senderId,
                true,
                (key, oldvalue) => true);
            await VMCreatingTable.TryRemoveAsync(
                this.CurrentTransaction,
                request.senderId);
        }

        private async Task OnVMDeleted()
        {
            eVMDeleteSuccessRequestEvent request = this.ReceivedEvent as eVMDeleteSuccessRequestEvent;
            this.Logger.WriteLine($"PM- {this.Id} received VM Delete Success for {request.senderId}");
            await VMDeletingTable.TryRemoveAsync(
                this.CurrentTransaction,
                request.senderId);
            long count = await VMCreatedTable.GetCountAsync(this.CurrentTransaction)
                + await VMCreatingTable.GetCountAsync(this.CurrentTransaction)
                + await VMDeletingTable.GetCountAsync(this.CurrentTransaction);
            if (count == 0)
            {
                this.Logger.WriteLine($"PM- {this.Id} Deleting pool");
                this.Send(this.Id, new Halt());
            }
            else
            {
                this.Logger.WriteLine($"PM- {this.Id} remaining count = {count}");
            }
        }

        private async Task RetryCreateVM()
        {
            eVMCreateFailureRequestEvent request = this.ReceivedEvent as eVMCreateFailureRequestEvent;
            this.Logger.WriteLine($"PM- {this.Id} received VM Create Failure for {request.senderId}");
            this.Logger.WriteLine($"PM- {this.Id} Deleting VM for {request.senderId} and Retrying create");
            await SendDeleteRequest(request.senderId);
            await SendCreateVMRequest();
        }

        private async Task SendCreateVMRequest()
        {
            MachineId machineId = this.CreateMachine(typeof(VMManagerMachine), Guid.NewGuid().ToString(), new eVMCreateRequestEvent(this.Id));
            await VMCreatingTable.AddOrUpdateAsync(
                this.CurrentTransaction,
                machineId,
                true,
                (key, oldvalue) => true);
        }

        private async Task SendDeleteRequest(MachineId machineId)
        {
            this.Send(machineId, new eVMDeleteRequestEvent(this.Id));
            await VMCreatedTable.TryRemoveAsync(
                this.CurrentTransaction,
                machineId);
            await VMCreatingTable.TryRemoveAsync(
                this.CurrentTransaction,
                machineId);
            await VMDeletingTable.AddOrUpdateAsync(
                this.CurrentTransaction,
                machineId,
                true,
                (key, oldvalue) => true);
        }

        private async Task RetryDeleteVM()
        {
            eVMDeleteFailureRequestEvent request = this.ReceivedEvent as eVMDeleteFailureRequestEvent;
            this.Logger.WriteLine($"PM- {this.Id} received VM Create Failure for {request.senderId}");
            await SendDeleteRequest(request.senderId);
        }

        private async Task ResizePool()
        {
            ePoolResizeRequestEvent resizeRequest = this.ReceivedEvent as ePoolResizeRequestEvent;
            if (resizeRequest == null) return;
            this.Logger.WriteLine($"PM- {this.Id} Resize requested- size {resizeRequest.Size}");
            long count = await VMCreatedTable.GetCountAsync(this.CurrentTransaction);
            long difference = resizeRequest.Size - count;

            this.Logger.WriteLine($"PM- {this.Id} Required VMs for pool is {difference}");
          
            if (difference < 0)
            {
                IAsyncEnumerable<MachineId> enumerable = await VMCreatedTable.CreateKeyEnumerableAsync(this.CurrentTransaction);
                IAsyncEnumerator<MachineId> enumerator = enumerable.GetAsyncEnumerator();
                CancellationToken token = new CancellationToken();
                this.Logger.WriteLine($"PM- {this.Id} - Scale down Deleting VMs for pool {this.Id}");
                List<MachineId> ids = new List<MachineId>();
                while (difference++ < 0L)
                {
                    await enumerator.MoveNextAsync(token);
                    ids.Add(enumerator.Current);
                }
                foreach (var id in ids)
                {
                    await SendDeleteRequest(id);
                }
            }
            else
            {
                this.Logger.WriteLine($"PM- {this.Id} - Scale up Creating VMs for pool {this.Id}");
                while (difference-- > 0L)
                {
                    await SendCreateVMRequest();
                }
            }
        }

        private async Task DeletePool()
        {
            this.Logger.WriteLine($"PM- {this.Id} Deletion request of pool");
            ePoolDeletionRequestEvent deleteEvent = this.ReceivedEvent as ePoolDeletionRequestEvent;
            if (deleteEvent == null) return;

            IAsyncEnumerable<MachineId> enumerable = await VMCreatedTable.CreateKeyEnumerableAsync(this.CurrentTransaction);
            IAsyncEnumerator<MachineId> enumerator = enumerable.GetAsyncEnumerator();
            CancellationToken token = new CancellationToken();
            List<MachineId> ids = new List<MachineId>();
            while (await enumerator.MoveNextAsync(token))
            {
                ids.Add(enumerator.Current);
            }

            enumerable = await VMCreatingTable.CreateKeyEnumerableAsync(this.CurrentTransaction);
            enumerator = enumerable.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync(token))
            {
                ids.Add(enumerator.Current);
            }

            foreach (var id in ids)
            {
                await SendDeleteRequest(id);
            }
        }
    }
}
