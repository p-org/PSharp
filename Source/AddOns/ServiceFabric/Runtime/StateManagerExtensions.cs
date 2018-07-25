using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ServiceFabric
{
    public static class StateManagerExtensions
    {
        private const string InputQueuePrefix = "InputQueue_";
        private const string SendCountersPrefix = "SendCounters_";
        private const string ReceiveCountersPrefix = "ReceiveCounters_";
        private const string StateStackStorePrefix = "StateStackStore_";

        public static Task<IReliableConcurrentQueue<Event>> GetMachineInputQueue(this IReliableStateManager manager, MachineId id)
        {
            return manager.GetOrAddAsync<IReliableConcurrentQueue<Event>>(InputQueuePrefix + id.ToString());
        }

        public static Task DeleteMachineInputQueue(this IReliableStateManager manager, MachineId id)
        {
            return manager.RemoveAsync(InputQueuePrefix + id.ToString());
        }

        public static Task<IReliableDictionary<string, int>> GetMachineSendCounters(this IReliableStateManager manager, MachineId id)
        {
            return manager.GetOrAddAsync<IReliableDictionary<string, int>>(SendCountersPrefix + id.ToString());
        }

        public static Task DeleteMachineSendCounters(this IReliableStateManager manager, MachineId id)
        {
            return manager.RemoveAsync(SendCountersPrefix + id.ToString());
        }

        public static Task<IReliableDictionary<string, int>> GetMachineReceiveCounters(this IReliableStateManager manager, MachineId id)
        {
            return manager.GetOrAddAsync<IReliableDictionary<string, int>>(ReceiveCountersPrefix + id.ToString());
        }

        public static Task DeleteMachineReceiveCounters(this IReliableStateManager manager, MachineId id)
        {
            return manager.RemoveAsync(ReceiveCountersPrefix + id.ToString());
        }

        public static Task<IReliableDictionary<int, string>> GetMachineStackStore(this IReliableStateManager manager, MachineId id)
        {
            return manager.GetOrAddAsync<IReliableDictionary<int, string>>(StateStackStorePrefix + id.ToString());
        }

        public static Task DeleteMachineStackStore(this IReliableStateManager manager, MachineId id)
        {
            return manager.RemoveAsync(StateStackStorePrefix + id.ToString());
        }
    }
}
