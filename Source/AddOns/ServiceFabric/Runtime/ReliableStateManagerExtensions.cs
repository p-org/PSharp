#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System.Threading.Tasks;

    public static class ReliableStateManagerExtensions
    {
        private static readonly IReactiveReliableQueueManager _reactiveReliableQueueManager = new ReactiveReliableQueueManager();

        public static async Task<IReactiveReliableQueue<T>> GetOrAddReactiveReliableQueue<T>(this IReliableStateManager reliableStateManager, string name)
        {
            var queue = await reliableStateManager.GetOrAddAsync<IReliableQueue<T>>(name)
                .ConfigureAwait(false);

            return _reactiveReliableQueueManager.GetOrCreateAsync(queue);
        }
    }
}
