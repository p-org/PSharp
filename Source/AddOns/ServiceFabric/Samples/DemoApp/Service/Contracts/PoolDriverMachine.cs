using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PoolServicesContract
{
    public class PoolDriverMachine : ReliableMachine
    {
        private const string TablePrefix = "POOLDRIVER-";
        // Pool name to machine id
        private IReliableDictionary<long, MachineId> currentPoolTable;

        public PoolDriverMachine(IReliableStateManager stateManager) : base(stateManager)
        {
        }

        protected override async Task OnActivate()
        {
            this.currentPoolTable = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, MachineId>>(TablePrefix + "CurrentPool");
        }

        [Start]
        [OnEntry(nameof(Evaluate))]
        [OnEventDoAction(typeof(ePoolDriverConfigChangeEvent), nameof(Evaluate))]
        class Running : MachineState
        {
        }

        
        private async Task Evaluate()
        {
            CancellationToken token = CancellationToken.None;
            long count = await this.currentPoolTable.GetCountAsync(this.CurrentTransaction);
            try
            {
                ePoolDriverConfigChangeEvent configChange = this.ReceivedEvent as ePoolDriverConfigChangeEvent;
                if (configChange == null)
                {
                    return;
                }

                if (configChange.Configuration.PoolData.Count > count)
                {
                    for (long i = count; i < configChange.Configuration.PoolData.Count; i++)
                    {
                        MachineId machineId = this.CreateMachine(typeof(PoolManagerMachine), Guid.NewGuid().ToString(),
                            //new ePoolResizeRequestEvent() { Size = configChange.Configuration.PoolData[$"Pool{i + 1}"] });
                        new ePoolResizeRequestEvent() { Size = 10 });
                        await this.currentPoolTable.AddAsync(this.CurrentTransaction, i, machineId);
                    }
                }
                else if (configChange.Configuration.PoolData.Count < count)
                {
                    for (long i = configChange.Configuration.PoolData.Count; i < count; i++)
                    {
                        var cv = await this.currentPoolTable.TryGetValueAsync(this.CurrentTransaction, i);
                        this.Send(cv.Value, new ePoolDeletionRequestEvent());
                        await this.currentPoolTable.TryRemoveAsync(this.CurrentTransaction, i);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine($"Exception in Evaluate {ex}");
            }
        }
    }
}
