#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System.Threading;
    using System.Threading.Tasks;

    public class ReactiveReliableQueue<T> : IReactiveReliableQueue<T>
    {
        private readonly IReliableQueue<T> _queue;
        private readonly SemaphoreSlim _signal;

        public ReactiveReliableQueue(IReliableQueue<T> queue)
        {
            _queue = queue;
            _signal = new SemaphoreSlim(1);
        }

        public async Task EnqueueAsync(ITransaction tx, T item)
        {
            await _queue.EnqueueAsync(tx, item);
            _signal.Release();
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            return _queue.GetCountAsync(tx);
        }

        public async Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx, CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);

            var result = await _queue.TryDequeueAsync(tx).ConfigureAwait(false);

            var countDiff = await GetCountDiff(tx);

            if (countDiff > 0)
            {
                _signal.Release(countDiff);
            }

            return result;
        }

        private async Task<int> GetCountDiff(ITransaction tx)
        {
            return (int)await _queue.GetCountAsync(tx).ConfigureAwait(false) - _signal.CurrentCount;
        }
    }
}
