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
    public class ReliableDictionaryMock<TKey, TValue> : ITxState,
        IReliableDictionary2<TKey, TValue>,
        IReliableDictionary<TKey, TValue> where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        private ConcurrentDictionary<TKey, TValue> persisted_dictionary = new ConcurrentDictionary<TKey, TValue>();
        private ConcurrentDictionary<TKey, TValue> curr_dictionary = new ConcurrentDictionary<TKey, TValue>();
        private MockTransaction curr_tx = null;

        public Func<IReliableDictionary<TKey, TValue>, NotifyDictionaryRebuildEventArgs<TKey, TValue>, Task> RebuildNotificationAsyncCallback { set => throw new NotImplementedException(); }

        public Uri Name { get; set; }

        Uri IReliableState.Name => throw new NotImplementedException();

        public long Count
        {
            get
            {
                return (long)persisted_dictionary.Count();
            }
        }

#pragma warning disable 67
        public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> DictionaryChanged;
#pragma warning restore 67

        public Task AddAsync(ITransaction tx, TKey key, TValue value)
        {
            CheckTx(tx);

            if(!curr_dictionary.TryAdd(key, value))
            {
                throw new InvalidOperationException("key already exists: " + key.ToString());
            }

            return Task.FromResult(true);
        }

        public Task AddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx);

            if (!curr_dictionary.TryAdd(key, value))
            {
                throw new InvalidOperationException("key already exists: " + key.ToString());
            }

            return Task.FromResult(true);
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            CheckTx(tx);
            return Task.FromResult(this.curr_dictionary.AddOrUpdate(key, addValueFactory, updateValueFactory));
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            CheckTx(tx);
            return Task.FromResult(this.curr_dictionary.AddOrUpdate(key, addValue, updateValueFactory));
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            return Task.FromResult(this.curr_dictionary.AddOrUpdate(key, addValueFactory, updateValueFactory));
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            return Task.FromResult(this.curr_dictionary.AddOrUpdate(key, addValue, updateValueFactory));
        }

        public Task ClearAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            curr_tx = null;
            curr_dictionary.Clear();
            persisted_dictionary.Clear();
            return Task.FromResult(true);
        }

        public Task ClearAsync()
        {
            curr_tx = null;
            curr_dictionary.Clear();
            persisted_dictionary.Clear();
            return Task.FromResult(true);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key)
        {
            CheckTx(tx);
            return Task.FromResult(this.curr_dictionary.ContainsKey(key));
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            CheckTx(tx);
            return Task.FromResult(this.curr_dictionary.ContainsKey(key));
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            return Task.FromResult(this.curr_dictionary.ContainsKey(key));
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            return Task.FromResult(this.curr_dictionary.ContainsKey(key));
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn)
        {
            CheckTx(txn);
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(this.curr_dictionary));
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn, EnumerationMode enumerationMode)
        {
            CheckTx(txn);
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(
                enumerationMode == EnumerationMode.Unordered
                    ? (IEnumerable<KeyValuePair<TKey, TValue>>)this.curr_dictionary
                    : this.curr_dictionary.OrderBy(x => x.Key)));
        }

        public Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn, Func<TKey, bool> filter, EnumerationMode enumerationMode)
        {
            CheckTx(txn);
            return Task.FromResult<IAsyncEnumerable<KeyValuePair<TKey, TValue>>>(new MockAsyncEnumerable<KeyValuePair<TKey, TValue>>(
                enumerationMode == EnumerationMode.Unordered
                    ? this.curr_dictionary.Where(x => filter(x.Key))
                    : this.curr_dictionary.Where(x => filter(x.Key)).OrderBy(x => x.Key)));
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            CheckTx(tx);
            return Task.FromResult((long)this.curr_dictionary.Count);
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory)
        {
            CheckTx(tx);
            return Task.FromResult(this.curr_dictionary.GetOrAdd(key, valueFactory));
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value)
        {
            CheckTx(tx);
            return Task.FromResult(this.curr_dictionary.GetOrAdd(key, value));
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            return Task.FromResult(this.curr_dictionary.GetOrAdd(key, valueFactory));
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            return Task.FromResult(this.curr_dictionary.GetOrAdd(key, value));
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value)
        {
            CheckTx(tx);
            this.curr_dictionary[key] = value;
            return Task.FromResult(true);
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            this.curr_dictionary[key] = value;
            return Task.FromResult(true);
        }

        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value)
        {
            CheckTx(tx);
            return Task.FromResult(this.curr_dictionary.TryAdd(key, value));
        }

        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            return Task.FromResult(this.curr_dictionary.TryAdd(key, value));
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key)
        {
            CheckTx(tx);

            TValue value;
            bool result = this.curr_dictionary.TryGetValue(key, out value);
            return Task.FromResult(new ConditionalValue<TValue>(result, value));
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            CheckTx(tx);

            TValue value;
            bool result = this.curr_dictionary.TryGetValue(key, out value);
            return Task.FromResult(new ConditionalValue<TValue>(result, value));
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            TValue value;
            bool result = this.curr_dictionary.TryGetValue(key, out value);
            return Task.FromResult(new ConditionalValue<TValue>(result, value));
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            TValue value;
            bool result = this.curr_dictionary.TryGetValue(key, out value);
            return Task.FromResult(new ConditionalValue<TValue>(result, value));
        }

        public Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key)
        {
            CheckTx(tx);

            TValue outValue;
            return Task.FromResult(new ConditionalValue<TValue>(this.curr_dictionary.TryRemove(key, out outValue), outValue));

        }

        public Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);

            TValue outValue;
            return Task.FromResult(new ConditionalValue<TValue>(this.curr_dictionary.TryRemove(key, out outValue), outValue));
        }

        public Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue)
        {
            CheckTx(tx);
            return Task.FromResult(this.curr_dictionary.TryUpdate(key, newValue, comparisonValue));
        }

        public Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue, TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckTx(tx, timeout);
            return Task.FromResult(this.curr_dictionary.TryUpdate(key, newValue, comparisonValue));
        }

        private MockTransaction CheckTx(ITransaction tx, TimeSpan? timeout = null)
        {
            var mt = tx as MockTransaction;

            if (curr_tx == null)
            {
                curr_tx = mt;
                mt.RegisterStateObject(this);
            }
            else
            {
                curr_tx.Runtime.Assert(curr_tx == mt, "ReliableCollection: multiple concurrent transactions detected");
            }

            mt.CheckTimeout(timeout);
            
            return mt;
        }

        void ITxState.Abort(ITransaction tx)
        {
            curr_dictionary = new ConcurrentDictionary<TKey, TValue>(persisted_dictionary);
            curr_tx = null;
        }

        void ITxState.Commit(ITransaction tx)
        {
            persisted_dictionary = new ConcurrentDictionary<TKey, TValue>(curr_dictionary);
            curr_tx = null;
        }

        public Task<IAsyncEnumerable<TKey>> CreateKeyEnumerableAsync(ITransaction txn)
        {
            return CreateKeyEnumerableAsync(txn, EnumerationMode.Unordered);
        }

        public Task<IAsyncEnumerable<TKey>> CreateKeyEnumerableAsync(ITransaction txn, EnumerationMode enumerationMode)
        {
            CheckTx(txn);
            return Task.FromResult<IAsyncEnumerable<TKey>>(new MockAsyncEnumerable<TKey>(
                enumerationMode == EnumerationMode.Unordered
                    ? (IEnumerable<TKey>)this.curr_dictionary.Keys
                    : this.curr_dictionary.Keys.OrderBy(x => x)));
        }

        public Task<IAsyncEnumerable<TKey>> CreateKeyEnumerableAsync(ITransaction txn, EnumerationMode enumerationMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return CreateKeyEnumerableAsync(txn, enumerationMode);
        }
    }
}
