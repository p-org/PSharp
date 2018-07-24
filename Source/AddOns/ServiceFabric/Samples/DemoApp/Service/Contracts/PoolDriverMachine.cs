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
        private IReliableDictionary<string, Guid> currentPoolTable;
        private IReliableDictionary<Guid, string> currentPoolReverseTable;
        private IReliableDictionary<Guid, MachineId> currentMachineIdTable;

        public PoolDriverMachine(IReliableStateManager stateManager) : base(stateManager)
        {
        }

        protected override async Task OnActivate()
        {
            this.currentPoolTable = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Guid>>(TablePrefix + "CurrentPool");
            this.currentPoolReverseTable = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, string>>(TablePrefix + "CurrentPoolReverse");
            this.currentMachineIdTable = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, MachineId>>(TablePrefix + "CurrentPoolMachineId");
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
            try
            {
                ePoolDriverConfigChangeEvent configChange = this.ReceivedEvent as ePoolDriverConfigChangeEvent;
                if (configChange == null)
                {
                    return;
                }

                await this.CreatePoolsInStorage(configChange.Configuration);
                await IssueCreates(configChange.Configuration, token);
                var poolsToRemove = await GetPoolsToRemove(configChange.Configuration, token);
                var deletedPools = await SubmitDeletionRequests(poolsToRemove);
                await ClearDeletedItems(deletedPools);
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine($"Exception in Evaluate {ex}");
            }
        }


        private async Task CreatePoolsInStorage(PoolDriverConfig config)
        {
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                bool commit = false;
                foreach (KeyValuePair<string, int> pair in config.PoolData)
                {
                    commit = await this.GetOrCreateReliablePool(tx, pair.Key) || commit;
                }

                if (commit)
                {
                    await tx.CommitAsync();
                }
            }
        }

        private async Task<bool> GetOrCreateReliablePool(ITransaction tx, string poolName)
        {
            Guid newValue = Guid.NewGuid();
            Guid oldValue = await this.currentPoolTable.AddOrUpdateAsync(tx, poolName, newValue, (key, current) => current);
            return oldValue == newValue;
        }

        private async Task IssueCreates(PoolDriverConfig config, CancellationToken token)
        {
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                IAsyncEnumerable<KeyValuePair<string, Guid>> enumerable = await this.currentPoolTable.CreateEnumerableAsync(tx);
                using (IAsyncEnumerator<KeyValuePair<string, Guid>> dictEnumerator = enumerable.GetAsyncEnumerator())
                {
                    while (await dictEnumerator.MoveNextAsync(token))
                    {
                        token.ThrowIfCancellationRequested();

                        // Issue creates only when the changes are part of the current config
                        if (config.PoolData.ContainsKey(dictEnumerator.Current.Key))
                        {
                            /*****************************************************************
                             * NOTE: This mechanism needs to change
                             *****************************************************************/
                            // persist the machine id
                            MachineId macId = this.CreateMachine(typeof(PoolManagerMachine),
                                // NOTE: This is my key for the CreateMachine!!!
                                dictEnumerator.Current.Value.ToString(),
                                new ePoolResizeRequestEvent() { Size = config.PoolData[dictEnumerator.Current.Key] });

                            await this.currentMachineIdTable.AddOrUpdateAsync(tx, dictEnumerator.Current.Value, macId, (guid, machineId) => machineId);
                        }
                    }
                }

                // TODO: Make this smarter
                await tx.CommitAsync();
            }
        }

        private async Task<Dictionary<string, Guid>> GetPoolsToRemove(PoolDriverConfig config, CancellationToken token)
        {
            Dictionary<string, Guid> dict = new Dictionary<string, Guid>();
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                IAsyncEnumerable<KeyValuePair<string, Guid>> enumerable = await this.currentPoolTable.CreateEnumerableAsync(tx);
                using (IAsyncEnumerator<KeyValuePair<string, Guid>> dictEnumerator = enumerable.GetAsyncEnumerator())
                {
                    while (await dictEnumerator.MoveNextAsync(token))
                    {
                        token.ThrowIfCancellationRequested();

                        // Issue creates only when the changes are part of the current config
                        if (!config.PoolData.ContainsKey(dictEnumerator.Current.Key))
                        {
                            dict.Add(dictEnumerator.Current.Key, dictEnumerator.Current.Value);
                        }
                    }
                }
            }

            return dict;
        }

        private async Task<Dictionary<string, Guid>> SubmitDeletionRequests(Dictionary<string, Guid> deletionQueue)
        {
            Dictionary<string, Guid> successfulDeletion = new Dictionary<string, Guid>();
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                foreach (var item in deletionQueue)
                {
                    ConditionalValue<MachineId> val = await this.currentMachineIdTable.TryGetValueAsync(tx, item.Value);
                    if(val.HasValue)
                    {
                        try
                        {
                            this.Send(val.Value, new ePoolDeletionRequestEvent());
                            successfulDeletion.Add(item.Key, item.Value);
                        }
                        catch (Exception)
                        {
                            // Swallow for the time being
                        }
                    }
                }
            }

            return successfulDeletion;
        }

        private async Task ClearDeletedItems(Dictionary<string, Guid> deletedItems)
        {
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                foreach (var item in deletedItems)
                {
                    ConditionalValue<MachineId> val1 = await this.currentMachineIdTable.TryRemoveAsync(tx, item.Value);
                    ConditionalValue<Guid> val2 = await this.currentPoolTable.TryRemoveAsync(tx, item.Key);
                    ConditionalValue<string> val3 = await this.currentPoolReverseTable.TryRemoveAsync(tx, item.Value);
                }

                await tx.CommitAsync();
            }
        }
    }
}
