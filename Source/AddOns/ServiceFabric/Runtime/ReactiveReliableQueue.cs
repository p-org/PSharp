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
            ServiceEventSource.Current.Message($"[FRAMEWORK] Waiting for signal {_queue.Name}");
            await _signal.WaitAsync(cancellationToken);
            ServiceEventSource.Current.Message($"[FRAMEWORK] Waiting to dequeue from {_queue.Name}");
            var result = await _queue.TryDequeueAsync(tx, TimeSpan.FromMinutes(1), cancellationToken);
            ServiceEventSource.Current.Message($"[FRAMEWORK] Dequeue from {_queue.Name} and has value = {result.HasValue}");
            var countDiff = await GetCountDiff(tx);

            if (countDiff > 0)
            {
                ServiceEventSource.Current.Message($"[FRAMEWORK] Signal release for {_queue.Name} with count {countDiff}");
                _signal.Release(countDiff);
            }

            return result;
        }

        private async Task<int> GetCountDiff(ITransaction tx)
        {
            return (int)await _queue.GetCountAsync(tx) - _signal.CurrentCount;
        }
    }
}
