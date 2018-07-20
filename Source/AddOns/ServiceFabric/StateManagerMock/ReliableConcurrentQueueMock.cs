﻿using System;
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
    public class ReliableConcurrentQueueMock<T> : IReliableConcurrentQueue<T>, IReliableQueue<T>, ITxState
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

        Uri IReliableState.Name => Name;

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IAsyncEnumerable<T>> CreateEnumerableAsync(ITransaction tx)
        {
            var mt = tx as TransactionMock;

            List<T> ls;
            lock (lck)
            {
                mt.RegisterStateObject(this);
                mt.CheckTimeout(null);

                ls = new List<T>(persisted_queue);
                if (pending_enq.ContainsKey(tx))
                {
                    foreach (var v in pending_enq[tx])
                    {
                        ls.Insert(0, v);
                    }
                }
                if (tx != pending_deq)
                {
                    for (int i = 0; i < pending_deq_values.Count; i++)
                    {
                        ls.Add(pending_deq_values[i]);
                    }
                }
                ls.Reverse();
            }

            return Task.FromResult<IAsyncEnumerable<T>>(new MockAsyncEnumerable<T>(ls));
        }

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

        public Task EnqueueAsync(ITransaction tx, T item)
        {
            return EnqueueAsync(tx, item, default(CancellationToken), null);
        }

        public Task EnqueueAsync(ITransaction tx, T item, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return EnqueueAsync(tx, item, cancellationToken, timeout);
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            var mt = tx as TransactionMock;
            var cnt = 0;

            lock (lck)
            {
                mt.RegisterStateObject(this);
                mt.CheckTimeout();

                cnt = persisted_queue.Count;
                if(pending_enq.ContainsKey(mt))
                {
                    cnt += pending_enq[mt].Count;
                }
                if(tx != pending_deq)
                {
                    cnt += pending_deq_values.Count;
                }
            }
            return Task.FromResult((long)cnt);
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

        public Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx)
        {
            return TryDequeueAsync(tx, default(CancellationToken), null);
        }

        public Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return TryDequeueAsync(tx, cancellationToken, timeout);
        }

        public Task<ConditionalValue<T>> TryPeekAsync(ITransaction tx)
        {
            throw new NotImplementedException();
        }

        public Task<ConditionalValue<T>> TryPeekAsync(ITransaction tx, TimeSpan timeout, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ConditionalValue<T>> TryPeekAsync(ITransaction tx, LockMode lockMode)
        {
            throw new NotImplementedException();
        }

        public Task<ConditionalValue<T>> TryPeekAsync(ITransaction tx, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
