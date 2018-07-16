namespace Microsoft.PSharp.ServiceFabric
{
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
    using System.Collections.Concurrent;

    public class ReactiveReliableQueueManager : IReactiveReliableQueueManager
    {
        private readonly ConcurrentDictionary<Uri, object> _reactiveReliableQueues
            = new ConcurrentDictionary<Uri, object>();

        public IReactiveReliableQueue<T> GetOrCreateAsync<T>(IReliableQueue<T> queue)
        {
            var wrappedQueue = _reactiveReliableQueues.GetOrAdd(queue.Name, x => new ReactiveReliableQueue<T>(queue));

            return (IReactiveReliableQueue<T>)wrappedQueue;
        }
    }
}
