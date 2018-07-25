using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;

namespace Microsoft.PSharp.ServiceFabric.TestingServices
{
    public class ReliableDictionaryMock<TKey, TValue> : ITxState, IReliableDictionary<TKey, TValue> where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        private Dictionary<TKey, ReliableDictionaryMockValue<TValue>> persisted_dictionary = new Dictionary<TKey, ReliableDictionaryMockValue<TValue>>();

        public Func<IReliableDictionary<TKey, TValue>, NotifyDictionaryRebuildEventArgs<TKey, TValue>, Task> RebuildNotificationAsyncCallback { set => throw new NotImplementedException(); }

        public Uri Name { get; set; }

        Uri IReliableState.Name => throw new NotImplementedException();

#pragma warning disable 67
        public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> DictionaryChanged;
#pragma warning restore 67

        public Task AddAsync(ITransaction tx, TKey key, TValue value)
        {
            lock(this)
            {
                var rv = CheckTx(key, tx);
                if(rv.GetSnapshot(tx).HasValue)
                {
                    throw new InvalidOperationException("key already exists: " + key.ToString());
                }

                rv.CurrValue = value;
                rv.CurrOp = OperationType.Update;
            }

            return Task.FromResult(true);
        }

        public Task AddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            lock (this)
            {
                var rv = CheckTx(key, tx);
                if (rv.GetSnapshot(tx).HasValue)
                {
                    throw new InvalidOperationException("key already exists: " + key.ToString());
                }

                rv.CurrValue = value;
                rv.CurrOp = OperationType.Update;
            }

            return Task.FromResult(true);
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            lock (this)
            {
                var rv = CheckTx(key, tx);
                if (rv.GetSnapshot(tx).HasValue)
                {
                    rv.CurrValue = updateValueFactory(key, rv.GetSnapshot(tx).Value);
                    rv.CurrOp = OperationType.Update;
                }
                else
                {
                    rv.CurrValue = addValueFactory(key);
                    rv.CurrOp = OperationType.Update;
                }
                return Task.FromResult(rv.CurrValue);
            }
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return AddOrUpdateAsync(tx, key, k => addValue, updateValueFactory);
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return AddOrUpdateAsync(tx, key, addValueFactory, updateValueFactory);
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return AddOrUpdateAsync(tx, key, k => addValue, updateValueFactory);
        }

        public Task ClearAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            persisted_dictionary.Clear();
            return Task.FromResult(true);
        }

        public Task ClearAsync()
        {
            persisted_dictionary.Clear();
            return Task.FromResult(true);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key)
        {
            var rv = CheckTx(key, tx);
            return Task.FromResult(rv.GetSnapshot(tx).HasValue);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            return ContainsKeyAsync(tx, key);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return ContainsKeyAsync(tx, key);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return ContainsKeyAsync(tx, key);
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn)
        {
            var ret = GetSnapshot(txn);
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(ret));
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn, EnumerationMode enumerationMode)
        {
            var ret = GetSnapshot(txn);
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(
                enumerationMode == EnumerationMode.Unordered
                    ? (IEnumerable<KeyValuePair<TKey, TValue>>)ret
                    : ret.OrderBy(x => x.Key)));
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn, Func<TKey, bool> filter, EnumerationMode enumerationMode)
        {
            var ret = GetSnapshot(txn);
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(
                enumerationMode == EnumerationMode.Unordered
                    ? ret.Where(x => filter(x.Key))
                    : ret.Where(x => filter(x.Key)).OrderBy(x => x.Key)));
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            var ret = GetSnapshot(tx);
            return Task.FromResult((long)ret.Count);
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory)
        {
            lock(this)
            {
                var rv = CheckTx(key, tx); 
                if(rv.HasValue(tx))
                {
                    return Task.FromResult(rv.GetValue(tx));
                }
                else
                {
                    rv.CurrValue = valueFactory(key);
                    rv.CurrOp = OperationType.Update;
                    return Task.FromResult(rv.CurrValue);
                }

            }
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value)
        {
            return GetOrAddAsync(tx, key, k => value);
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return GetOrAddAsync(tx, key, valueFactory);
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return GetOrAddAsync(tx, key, k => value);
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value)
        {
            lock(this)
            {
                var rv = CheckTx(key, tx);
                rv.CurrValue = value;
                rv.CurrOp = OperationType.Update;
                return Task.FromResult(true);
            }
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return SetAsync(tx, key, value);
        }

        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value)
        {
            lock(this)
            {
                var rv = CheckTx(key, tx);
                if(rv.HasValue(tx))
                {
                    return Task.FromResult(false);
                }
                else
                {
                    rv.CurrValue = value;
                    rv.CurrOp = OperationType.Update;
                    return Task.FromResult(true);
                }
            }
        }

        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return TryAddAsync(tx, key, value);
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key)
        {
            lock (this)
            {
                var rv = CheckTx(key, tx);
                return Task.FromResult(rv.GetSnapshot(tx));
            }
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            return TryGetValueAsync(tx, key);
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return TryGetValueAsync(tx, key);
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return TryGetValueAsync(tx, key);
        }

        public Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key)
        {
            CheckTx(key, tx);

            lock (this)
            {
                var rv = CheckTx(key, tx);
                var ret = rv.GetSnapshot(tx);
                if(rv.HasValue(tx))
                {
                    rv.CurrValue = default(TValue);
                    rv.CurrOp = OperationType.Delete;
                }
                return Task.FromResult(ret);
            }
        }

        public Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return TryRemoveAsync(tx, key);
        }

        public Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue)
        {
            lock (this)
            {
                var rv = CheckTx(key, tx);
                if(rv.HasValue(tx))
                {
                    if(comparisonValue.Equals(rv.GetValue(tx)))
                    {
                        rv.CurrValue = newValue;
                        rv.CurrOp = OperationType.Update;
                        return Task.FromResult(true);
                    }
                }
                return Task.FromResult(false);
            }

        }

        public Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return TryUpdateAsync(tx, key, newValue, comparisonValue);
        }

        private Dictionary<TKey, TValue> GetSnapshot(ITransaction txn)
        {
            var ret = new Dictionary<TKey, TValue>();
            foreach (var tup in persisted_dictionary)
            {
                var cv = tup.Value.GetSnapshot(txn);
                if (cv.HasValue)
                {
                    ret.Add(tup.Key, cv.Value);
                }
            }
            return ret;
        }

        private ReliableDictionaryMockValue<TValue> CheckTx(TKey key, ITransaction tx, TimeSpan? timeout = null)
        {
            var mt = tx as TransactionMock;
            mt.RegisterStateObject(this);

            if (persisted_dictionary.ContainsKey(key))
            {
                var cv = persisted_dictionary[key];
                if (cv.CurrTx == null)
                {
                    cv.CurrTx = mt;
                    cv.CurrOp = OperationType.Read;
                }
                else
                {
                    mt.Runtime.Assert(cv.CurrTx == mt,
                        $"ReliableCollection: multiple concurrent transactions detected on the same key {key}");
                }
            }
            else
            {
                persisted_dictionary.Add(key, new ReliableDictionaryMockValue<TValue>(false, default(TValue), tx));
            }


            mt.CheckTimeout(timeout);

            return persisted_dictionary[key];
        }

        void ITxState.Abort(ITransaction tx)
        {
            lock (this)
            {
                foreach (var tup in persisted_dictionary)
                {
                    if (tup.Value.CurrTx == tx)
                    {
                        tup.Value.CurrTx = null;
                    }
                }
            }
        }

        void ITxState.Commit(ITransaction tx)
        {
            lock(this)
            {
                foreach(var tup in persisted_dictionary)
                {
                    if(tup.Value.CurrTx == tx)
                    {
                        var cv = tup.Value.GetSnapshot(tx);
                        tup.Value.PersistedValuePresent = cv.HasValue;
                        tup.Value.PersistedValue = cv.Value;
                        tup.Value.CurrTx = null; 
                    }
                }
            }
        }
    }

    enum OperationType { Read, Delete, Update };
    class ReliableDictionaryMockValue<TValue>
    {
        public bool PersistedValuePresent;
        public TValue PersistedValue;

        public TransactionMock CurrTx;
        public TValue CurrValue;
        public OperationType CurrOp;

        public ReliableDictionaryMockValue(bool keyPresent, TValue persistedValue, ITransaction currTx)
        {
            this.PersistedValuePresent = keyPresent;
            this.PersistedValue = persistedValue;
            this.CurrTx = currTx as TransactionMock;
            CurrValue = default(TValue);
            CurrOp = OperationType.Read;
        }

        public ReliableDictionaryMockValue(bool keyPresent, TValue persistedValue, ITransaction currTx, TValue value)
        {
            this.PersistedValuePresent = keyPresent;
            this.PersistedValue = persistedValue;
            this.CurrTx = currTx as TransactionMock;
            CurrValue = value;
            CurrOp = OperationType.Update;
        }

        public ConditionalValue<TValue> GetPersistedValue()
        {
            var notPresent = new ConditionalValue<TValue>(false, default(TValue));

            if (PersistedValuePresent)
            {
                return new ConditionalValue<TValue>(true, PersistedValue);
            }
            else
            {
                return notPresent;
            }

        }

        public ConditionalValue<TValue> GetSnapshot(ITransaction tx)
        {
            var notPresent = new ConditionalValue<TValue>(false, default(TValue));

            if (CurrTx == tx)
            {
                if(CurrOp == OperationType.Read)
                {
                    return GetPersistedValue();
                }
                else if(CurrOp == OperationType.Delete)
                {
                    return notPresent;
                }
                else
                {
                    return new ConditionalValue<TValue>(true, CurrValue);
                }
            }
            else
            {
                return GetPersistedValue();
            }
        }

        public bool HasValue(ITransaction tx)
        {
            return GetSnapshot(tx).HasValue;
        }

        public TValue GetValue(ITransaction tx)
        {
            return GetSnapshot(tx).Value;
        }

        public static ReliableDictionaryMockValue<TValue> FromValue(ITransaction tx, TValue value)
        {
            return new ReliableDictionaryMockValue<TValue>(false, default(TValue), tx as TransactionMock, value);
        }
    }
}
