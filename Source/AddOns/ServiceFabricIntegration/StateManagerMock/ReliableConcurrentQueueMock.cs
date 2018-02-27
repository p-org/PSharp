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
        private List<T> persisted_queue = new List<T>();

        private object lck = new object();
        private Dictionary<ITransaction, Queue<T>> pending_enq = new Dictionary<ITransaction, Queue<T>>();

        private ITransaction pending_deq = null;
        private List<T> pending_deq_values = new List<T>();
 
        public long Count
        {
            get
            {
                lock(lck)
                {
                    return persisted_queue.Count;
                }
            }
        }


        public Uri Name { get; set; }

        Uri IReliableState.Name => throw new NotImplementedException();

        public Task EnqueueAsync(ITransaction tx, T value, CancellationToken cancellationToken = default(CancellationToken), TimeSpan? timeout = null)
        {
            var mt = tx as TransactionMock;

            lock (lck)
            {
                mt.RegisterStateObject(this);
                mt.CheckTimeout(timeout);

                if (!pending_enq.ContainsKey(mt))
                {
                    pending_enq.Add(mt, new Queue<T>());
                }
                pending_enq[mt].Enqueue(value);

            }
            return Task.FromResult(true);
        }

        public Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx, CancellationToken cancellationToken = default(CancellationToken), TimeSpan? timeout = null)
        {
            var mt = tx as TransactionMock;

            lock (lck)
            {
                mt.RegisterStateObject(this);
                mt.CheckTimeout(timeout);
                mt.Runtime.Assert(pending_deq == null || pending_deq == tx, "ReliableConcurrentQueueMock: Concurrent deq transactions detected");
                pending_deq = tx;

                T item = default(T);

                if (persisted_queue.Count == 0)
                {
                    if (pending_enq.ContainsKey(tx) && pending_enq[tx].Count > 0)
                    {
                        item = pending_enq[tx].Dequeue();
                        return Task.FromResult(new ConditionalValue<T>(true, item));
                    }
                    else
                    {
                        return Task.FromResult(new ConditionalValue<T>(false, item));
                    }
                }

                item = persisted_queue[persisted_queue.Count - 1];
                persisted_queue.RemoveAt(persisted_queue.Count - 1);
                pending_deq_values.Insert(0, item);

                return Task.FromResult(new ConditionalValue<T>(true, item));
            }
        }

        void ITxState.Abort(ITransaction tx)
        {
            lock(lck)
            {
                pending_enq.Remove(tx);
                if (pending_deq == tx)
                {
                    for (int i = 0; i < pending_deq_values.Count; i++)
                    {
                        persisted_queue.Add(pending_deq_values[i]);
                    }
                    pending_deq = null;
                }
            }
        }

        void ITxState.Commit(ITransaction tx)
        {
            lock(lck)
            {
                if(pending_enq.ContainsKey(tx))
                {
                    while(pending_enq[tx].Count > 0)
                    {
                        persisted_queue.Insert(0, pending_enq[tx].Dequeue());
                    }
                    pending_enq.Remove(tx);
                }

                if(pending_deq == tx)
                {
                    pending_deq = null;
                    pending_deq_values.Clear();
                }

            }
        }
    }
}
