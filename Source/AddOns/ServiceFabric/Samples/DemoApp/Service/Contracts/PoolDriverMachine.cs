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
        // Pool name to machine id
        private IReliableDictionary<string, MachineId> CreatedPools;
        private IReliableDictionary<string, MachineId> CreatingPools;

        public PoolDriverMachine(IReliableStateManager stateManager) : base(stateManager)
        {
        }

        protected override Task OnActivate()
        {
            return Task.FromResult(true);
        }

        [Start]
        [OnEntry(nameof(Evaluate))]
        [OnEventDoAction(typeof(ePoolDriverConfigChange), nameof(UpdateConfig))]
        class Running : MachineState
        {
        }

        private async Task Evaluate()
        {
            throw new NotImplementedException();
        }

        private async Task UpdateConfig()
        {
            Dictionary<string, MachineId> currentCreatedPools = new Dictionary<string, MachineId>();
            Dictionary<string, MachineId> currentCreatingPools = new Dictionary<string, MachineId>();
            CancellationTokenSource source = new CancellationTokenSource();
            var token = source.Token;
            ePoolDriverConfigChange e = this.ReceivedEvent as ePoolDriverConfigChange;

            IAsyncEnumerable<KeyValuePair<string, MachineId>> enumerable = await this.CreatedPools.CreateEnumerableAsync(this.CurrentTransaction);
            using (IAsyncEnumerator<KeyValuePair<string, MachineId>> dictEnumerator = enumerable.GetAsyncEnumerator())
            {
                while (await dictEnumerator.MoveNextAsync(token))
                {
                    token.ThrowIfCancellationRequested();
                    currentCreatedPools.Add(dictEnumerator.Current.Key, dictEnumerator.Current.Value);
                }
            }

            // Calculate CreatingPool
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                bool hasChanged = false;
                foreach (var item in e.Configuration.PoolData)
                {
                    if(!currentCreatedPools.ContainsKey(item.Key))
                    {
                        var data = await this.CreatingPools.AddOrUpdateAsync(tx, item.Key, (key) =>
                        {
                            hasChanged = true;
                            // TODO - talk to resource manager for partition details at this given moment and determing a machine ID
                            return null;
                        }, (key, current) => { return current; });
                        currentCreatingPools.Add(item.Key, data);
                    }
                    else
                    {
                        hasChanged = true;
                        await this.CreatingPools.TryRemoveAsync(tx, item.Key);
                    }
                }

                if(hasChanged)
                {
                    await tx.CommitAsync();
                }
            }

            

            IAsyncEnumerable<KeyValuePair<string, MachineId>> enumerable = await this.CreatedPools.CreateEnumerableAsync(this.CurrentTransaction);
            using (IAsyncEnumerator<KeyValuePair<string, MachineId>> dictEnumerator = enumerable.GetAsyncEnumerator())
            {
                while (await dictEnumerator.MoveNextAsync(token))
                {
                    token.ThrowIfCancellationRequested();
                    currentCreatedPools.Add(dictEnumerator.Current.Key, dictEnumerator.Current.Value);
                }
            }

            // Diff does not contain machine - issue deletes
            foreach (var item in currentCreatedPools)
            {
                if(!e.Configuration.PoolData.ContainsKey(item.Key))
                {
                    this.Send(item.Value, new ePoolDeletionRequest());
                }
            }

            // TODO: LEAK?? - What happens if creates worked but we did not persist the transaction?????
            foreach (var item in e.Configuration.PoolData)
            {
                await this.CreatedPools.AddOrUpdateAsync(this.CurrentTransaction, item.Key, (key) => 
                {
                    // TODO: Use string types!!!!!
                    return this.CreateRemoteMachine(typeof(PoolManagerMachine), key, new ePoolResizeRequest() { Size = item.Value });
                }, (key, current) => 
                {
                    this.Send(current, new ePoolResizeRequest() { Size = item.Value });
                    return current;
                }
                );
            }

            // At the end of it re-evaulate
            await this.Evaluate();

            //1. Check if machine ID exists - No?
            //2. Check if pending machine IDs - Yes?
        }
    }
}
