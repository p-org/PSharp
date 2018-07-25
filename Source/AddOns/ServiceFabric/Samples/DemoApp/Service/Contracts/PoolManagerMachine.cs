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
        private const string VMCreatingDictionary = "VMCreatingDictionary";
        private const string VMCreatedDictionary = "VMCreatedDictionary";
        private const string VMDeletingDictionary = "VMDeletingDictionary";

        private IReliableDictionary<MachineId, List<MachineId>> VMCreatingTable;
        private IReliableDictionary<MachineId, List<MachineId>> VMCreatedTable;
        private IReliableDictionary<MachineId, List<MachineId>> VMDeletingTable;

        public PoolManagerMachine(IReliableStateManager stateManager) : base(stateManager)
        {
        }

        protected override async Task OnActivate()
        {
            VMCreatingTable = await this.StateManager.GetOrAddAsync<IReliableDictionary<MachineId, List<MachineId>>>(VMCreatingDictionary);
            VMCreatedTable = await this.StateManager.GetOrAddAsync<IReliableDictionary<MachineId, List<MachineId>>>(VMCreatedDictionary);
            VMDeletingTable = await this.StateManager.GetOrAddAsync<IReliableDictionary<MachineId, List<MachineId>>>(VMDeletingDictionary);
        }

        [Start]
        [OnEntry(nameof(ResizePool))]
        [OnEventDoAction(typeof(ePoolResizeRequestEvent), nameof(ResizePool))]
        [OnEventGotoState(typeof(ePoolDeletionRequestEvent), typeof(Deleting))]
        [OnEventGotoState(typeof(eVMCreateFailureRequestEvent), typeof(VMCreateFailing))]
        [OnEventGotoState(typeof(eVMCreateSuccessRequestEvent), typeof(VMCreated))]
        [OnEventGotoState(typeof(eVMDeleteFailureRequestEvent), typeof(VMDeleteFailing))]
        [OnEventGotoState(typeof(eVMDeleteSuccessRequestEvent), typeof(VMDeleted))]
        class Resizing : MachineState
        {
        }

        [OnEntry(nameof(RetryCreateVM))]
        [OnEventDoAction(typeof(eVMCreateFailureRequestEvent), nameof(RetryCreateVM))]
        [OnEventGotoState(typeof(ePoolDeletionRequestEvent), typeof(Deleting))]
        [OnEventGotoState(typeof(eVMCreateSuccessRequestEvent), typeof(VMCreated))]
        [OnEventGotoState(typeof(eVMDeleteFailureRequestEvent), typeof(VMDeleteFailing))]
        [OnEventGotoState(typeof(ePoolResizeRequestEvent), typeof(Resizing))]
        class VMCreateFailing : MachineState
        {
        }

        [OnEntry(nameof(RetryDeleteVM))]
        [OnEventDoAction(typeof(eVMDeleteFailureRequestEvent), nameof(RetryDeleteVM))]
        [OnEventGotoState(typeof(ePoolDeletionRequestEvent), typeof(Deleting))]
        [OnEventGotoState(typeof(eVMDeleteSuccessRequestEvent), typeof(VMDeleted))]
        [OnEventGotoState(typeof(ePoolResizeRequestEvent), typeof(Resizing))]
        class VMDeleteFailing : MachineState
        {
        }

        [OnEntry(nameof(DeletePool))]
        [OnEventGotoState(typeof(eVMDeleteSuccessRequestEvent), typeof(VMDeleted))]
        [OnEventGotoState(typeof(eVMDeleteFailureRequestEvent), typeof(VMDeleteFailing))]
        class Deleting : MachineState
        {
        }

        [OnEntry(nameof(OnVMCreated))]
        [OnEventGotoState(typeof(ePoolResizeRequestEvent), typeof(Resizing))]
        [OnEventGotoState(typeof(ePoolDeletionRequestEvent), typeof(Deleting))]
        class VMCreated : MachineState
        {
        }

        [OnEntry(nameof(OnVMDeleted))]
        [OnEventGotoState(typeof(ePoolResizeRequestEvent), typeof(Resizing))]
        [OnEventGotoState(typeof(ePoolDeletionRequestEvent), typeof(Deleting))]
        class VMDeleted : MachineState
        {
        }

        private async Task OnVMCreated()
        {
            eVMCreateSuccessRequestEvent request = this.ReceivedEvent as eVMCreateSuccessRequestEvent;
            this.Logger.WriteLine($"PoolManagerMachine for {this.Id} received VM Create Success for {request.senderId}");
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await VMCreatedTable.AddOrUpdateAsync(
                           tx,
                           this.Id,
                           new List<MachineId>() { request.senderId },
                           (key, oldvalue) =>
                           {
                               oldvalue.Add(request.senderId);
                               return oldvalue;
                           });
                await VMCreatingTable.AddOrUpdateAsync(
                           tx,
                           this.Id,
                           new List<MachineId>(),
                           (key, oldvalue) =>
                           {
                               oldvalue.Remove(request.senderId);
                               return oldvalue;
                           });
                await tx.CommitAsync();
            }
        }

        private async Task OnVMDeleted()
        {
            eVMDeleteSuccessRequestEvent request = this.ReceivedEvent as eVMDeleteSuccessRequestEvent;
            this.Logger.WriteLine($"PoolManagerMachine for {this.Id} received VM Delete Success for {request.senderId}");
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                bool createEmpty = false;
                bool deleteEmpty = false;
                await VMCreatedTable.AddOrUpdateAsync(
                          tx,
                          this.Id,
                          new List<MachineId>(),
                          (key, oldvalue) =>
                          {
                              oldvalue.Remove(request.senderId);
                              createEmpty = oldvalue.Count == 0;
                              return oldvalue;
                          });
                await VMCreatingTable.AddOrUpdateAsync(
                           tx,
                           this.Id,
                           new List<MachineId>(),
                           (key, oldvalue) =>
                           {
                               oldvalue.Remove(request.senderId);
                               createEmpty = createEmpty && oldvalue.Count == 0;
                               return oldvalue;
                           });
                await VMDeletingTable.AddOrUpdateAsync(
                           tx,
                           this.Id,
                           new List<MachineId>(),
                           (key, oldvalue) =>
                           {
                               oldvalue.Remove(request.senderId);
                               deleteEmpty = oldvalue.Count == 0;
                               return oldvalue;
                           });

                if(createEmpty && deleteEmpty)
                {
                    this.Send(this.Id, new Halt());
                }

                await tx.CommitAsync();
            }
        }

        private void RetryCreateVM()
        {
            eVMCreateFailureRequestEvent request = this.ReceivedEvent as eVMCreateFailureRequestEvent;
            this.Logger.WriteLine($"PoolManagerMachine for {this.Id} received VM Create Failure for {request.senderId}");
            this.Logger.WriteLine($"PoolManagerMachine for {this.Id} Deleting VM for {request.senderId} and Retrying create");
            this.Send(request.senderId, new eVMDeleteRequestEvent(this.Id));
            this.CreateMachine(typeof(VMManagerMachine), new eVMRetryCreateRequestEvent(this.Id));
        }

        private void RetryDeleteVM()
        {
            eVMDeleteFailureRequestEvent request = this.ReceivedEvent as eVMDeleteFailureRequestEvent;
            this.Logger.WriteLine($"PoolManagerMachine for {this.Id} received VM Create Failure for {request.senderId}");
            this.Send(request.senderId, new eVMRetryDeleteRequestEvent(this.Id));
        }

        private async Task ResizePool()
        {
            ePoolResizeRequestEvent resizeRequest = this.ReceivedEvent as ePoolResizeRequestEvent;
            this.Logger.WriteLine($"PoolManagerMachine Resize requested for pool {this.Id} and size {resizeRequest.Size}");
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<List<MachineId>> createdVMList = await VMCreatedTable.TryGetValueAsync(tx, this.Id);
                int difference;
                if (createdVMList.HasValue)
                {
                    difference = resizeRequest.Size - createdVMList.Value.Count;
                }
                else
                {
                    difference = resizeRequest.Size;
                }

                this.Logger.WriteLine($"PoolManagerMachine Required VMs for pool {this.Id} is {difference}");

                if(difference < 0)
                {
                    this.Logger.WriteLine($"PoolManagerMachine - Scale down Deleting VMs for pool {this.Id}");
                    int index = 0;
                    while (difference++ < 0)
                    {
                        MachineId machineId = createdVMList.Value[index++];
                        this.Send(machineId, new eVMDeleteRequestEvent(this.Id));
                        await VMDeletingTable.AddOrUpdateAsync(
                            tx,
                            this.Id,
                            new List<MachineId>() { machineId },
                            (key, oldvalue) =>
                            {
                                oldvalue.Add(machineId);
                                return oldvalue;
                            });
                    }
                }
                else
                {
                    this.Logger.WriteLine($"PoolManagerMachine - Scale up Creating VMs for pool {this.Id}");
                    while (difference-- > 0)
                    {
                        MachineId machineId = this.CreateMachine(typeof(VMManagerMachine), new eVMCreateRequestEvent(this.Id));
                        await VMCreatingTable.AddOrUpdateAsync(
                            tx,
                            this.Id,
                            new List<MachineId>() { machineId },
                            (key, oldvalue) =>
                            {
                                oldvalue.Add(machineId);
                                return oldvalue;
                            });
                    }
                }

                await tx.CommitAsync();
            }
        }

        private async Task DeletePool()
        {
            this.Logger.WriteLine($"PoolManagerMachine Deletion request of pool {this.Id}");
            ePoolDeletionRequestEvent deleteEvent = this.ReceivedEvent as ePoolDeletionRequestEvent;
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<List<MachineId>> createdVMList = await VMCreatedTable.TryGetValueAsync(tx, this.Id);
                ConditionalValue<List<MachineId>> creatingVMList = await VMCreatingTable.TryGetValueAsync(tx, this.Id);
                if (createdVMList.HasValue)
                {
                    foreach(MachineId machineId in createdVMList.Value)
                    {
                        this.Send(machineId, new eVMDeleteRequestEvent(this.Id));
                        await VMDeletingTable.AddOrUpdateAsync(
                            tx,
                            this.Id,
                            new List<MachineId>() { machineId },
                            (key, oldvalue) =>
                            {
                                oldvalue.Add(machineId);
                                return oldvalue;
                            });
                    }
                }

                if (creatingVMList.HasValue)
                {
                    foreach (MachineId machineId in creatingVMList.Value)
                    {
                        this.Send(machineId, new eVMDeleteRequestEvent(this.Id));
                        await VMDeletingTable.AddOrUpdateAsync(
                            tx,
                            this.Id,
                            new List<MachineId>() { machineId },
                            (key, oldvalue) =>
                            {
                                oldvalue.Add(machineId);
                                return oldvalue;
                            });
                    }
                }

                await tx.CommitAsync();
            }
        }
    }
}
