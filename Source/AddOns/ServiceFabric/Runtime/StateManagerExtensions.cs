using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ServiceFabric
{
    public static class StateManagerExtensions
    {
        private const string InputQueuePrefix = "InputQueue_";

        public static Task<IReliableConcurrentQueue<EventInfo>> GetLocalMachineQueue(this IReliableStateManager manager, MachineId id)
        {
            return manager.GetOrAddAsync<IReliableConcurrentQueue<EventInfo>>(InputQueuePrefix + id.ToString());
        }
    }
}
