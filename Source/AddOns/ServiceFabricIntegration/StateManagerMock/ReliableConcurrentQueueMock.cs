using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;

namespace Microsoft.PSharp.ReliableServices
{
    public class ReliableConcurrentQueueMock<T> : IReliableConcurrentQueue<T>, ITxState
    {
        private Queue<T> persisted_queue = new Queue<T>();
        private Queue<T> curr_queue = new Queue<T>();
        private TransactionMock curr_tx = null;

        public long Count => curr_queue.Count;

        public Uri Name { get; set; }

        Uri IReliableState.Name => throw new NotImplementedException();

        public Task EnqueueAsync(ITransaction tx, T value, CancellationToken cancellationToken = default(CancellationToken), TimeSpan? timeout = null)
        {
            var mt = tx as TransactionMock;

            if (curr_tx == null)
            {
                curr_tx = mt;
                mt.RegisterStateObject(this);
            }
            else
            {
                curr_tx.Runtime.Assert(curr_tx == mt, "ReliableCollection: multiple concurrent transaction detected");
            }

            mt.CheckTimeout(timeout);

            this.curr_queue.Enqueue(value);
            return Task.FromResult(true);
        }

        public Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx, CancellationToken cancellationToken = default(CancellationToken), TimeSpan? timeout = null)
        {
            var mt = tx as TransactionMock;

            if (curr_tx == null)
            {
                curr_tx = mt;
                mt.RegisterStateObject(this);
            }
            else
            {
                curr_tx.Runtime.Assert(curr_tx == mt, "ReliableCollection: multiple concurrent transaction detected");
            }

            mt.CheckTimeout(timeout);

            T item = default(T);

            if (curr_queue.Count == 0)
            {
                return Task.FromResult(new ConditionalValue<T>(false, item));
            }

            return Task.FromResult(new ConditionalValue<T>(false, curr_queue.Dequeue()));
        }

        void ITxState.Abort()
        {
            curr_queue = new Queue<T>(persisted_queue);
            curr_tx = null;
        }

        void ITxState.Commit()
        {
            persisted_queue = new Queue<T>(curr_queue);
            curr_tx = null;
        }
    }
}
