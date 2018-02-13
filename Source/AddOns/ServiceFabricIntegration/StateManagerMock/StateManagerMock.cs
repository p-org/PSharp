using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;
using System.Threading;

namespace Microsoft.PSharp.ReliableServices
{
    /// <summary>
    /// Mock of IReliableStateManager
    /// </summary>
    public class StateManagerMock : IReliableStateManager
    {
        /// <summary>
        /// The PSharp runtime
        /// </summary>
        PSharpRuntime Runtime;

        long TransactionCounter;

        private ConcurrentDictionary<Uri, IReliableState> store = new ConcurrentDictionary<Uri, IReliableState>();

        private Dictionary<Type, Type> dependencyMap = new Dictionary<Type, Type>()
        {
            {typeof(IReliableDictionary<,>), typeof(ReliableDictionaryMock<,>)},
            { typeof(IReliableConcurrentQueue<>), typeof(ReliableConcurrentQueueMock<>)}
        };

        public StateManagerMock(PSharpRuntime runtime)
        {
            this.Runtime = runtime;
            this.TransactionCounter = 0;
        }

        // IReliableStateManager interface

#pragma warning disable 67
        public event EventHandler<NotifyTransactionChangedEventArgs> TransactionChanged;
        public event EventHandler<NotifyStateManagerChangedEventArgs> StateManagerChanged;
#pragma warning restore 67

        public ITransaction CreateTransaction()
        {
            return new TransactionMock(Runtime, Interlocked.Increment(ref TransactionCounter));
        }

        public IAsyncEnumerator<IReliableState> GetAsyncEnumerator()
        {
            throw new NotImplementedException();
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T)this.store.GetOrAdd(name, this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name) where T : IReliableState
        {
            return Task.FromResult((T)this.store.GetOrAdd(name, this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(Uri name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T)this.store.GetOrAdd(name, this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(Uri name) where T : IReliableState
        {
            return Task.FromResult((T)this.store.GetOrAdd(name, this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, string name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T)this.store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, string name) where T : IReliableState
        {
            return Task.FromResult((T)this.store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(string name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T)this.store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(string name) where T : IReliableState
        {
            return Task.FromResult((T)this.store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T))));
        }

        public Task RemoveAsync(ITransaction tx, Uri name, TimeSpan timeout)
        {
            IReliableState result;
            this.store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, Uri name)
        {
            IReliableState result;
            this.store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(Uri name, TimeSpan timeout)
        {
            IReliableState result;
            this.store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(Uri name)
        {
            IReliableState result;
            this.store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, string name, TimeSpan timeout)
        {
            IReliableState result;
            this.store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, string name)
        {
            IReliableState result;
            this.store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(string name, TimeSpan timeout)
        {
            IReliableState result;
            this.store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(string name)
        {
            IReliableState result;
            this.store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public bool TryAddStateSerializer<T>(IStateSerializer<T> stateSerializer)
        {
            throw new NotImplementedException();
        }

        public Task<ConditionalValue<T>> TryGetAsync<T>(Uri name) where T : IReliableState
        {
            throw new NotImplementedException();
        }

        public Task<ConditionalValue<T>> TryGetAsync<T>(string name) where T : IReliableState
        {
            IReliableState item;
            bool result = this.store.TryGetValue(this.ToUri(name), out item);

            return Task.FromResult(new ConditionalValue<T>(result, (T)item));
        }

        private IReliableState GetDependency(Type t)
        {
            Type mockType = this.dependencyMap[t.GetGenericTypeDefinition()];

            return (IReliableState)Activator.CreateInstance(mockType.MakeGenericType(t.GetGenericArguments()));
        }

        private Uri ToUri(string name)
        {
            return new Uri("mock://" + name.Replace('(','_').Replace(')','_'), UriKind.Absolute);
        }
    }
}
