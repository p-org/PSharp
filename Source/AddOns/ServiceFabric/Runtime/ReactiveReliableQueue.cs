namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
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

        public async Task EnqueueAsync(ITransaction tx, T item, CancellationToken cancellationToken)
        {
            await _queue.EnqueueAsync(tx, item, TimeSpan.FromMinutes(1), cancellationToken);
            _signal.Release();
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            return _queue.GetCountAsync(tx);
        }

        public async Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx, CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            // Setting up 1 minute timeout - when enqueue lock takes more time, we might run into TimeoutException
            var result = await _queue.TryDequeueAsync(tx, TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
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
