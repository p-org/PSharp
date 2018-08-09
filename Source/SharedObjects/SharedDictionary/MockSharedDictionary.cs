//-----------------------------------------------------------------------
// <copyright file="MockSharedDictionary.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared dictionary modeled using a state-machine for testing.
    /// </summary>
    internal sealed class MockSharedDictionary<TKey, TValue> : ISharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// The testing runtime hosting this shared dictionary.
        /// </summary>
        private readonly ITestingRuntime Runtime;

        /// <summary>
        /// Machine modeling the shared dictionary.
        /// </summary>
        private MachineId DictionaryMachine;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public TValue this[TKey key]
        {
            get
            {
                var currentMachine = this.Runtime.GetCurrentMachine();
                this.Runtime.SendEventAsync(this.DictionaryMachine, SharedDictionaryEvent.GetEvent(key, currentMachine.Id)).Wait();
                var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<TValue>)).Result as SharedDictionaryResponseEvent<TValue>;
                return e.Value;
            }
            set
            {
                this.Runtime.SendEventAsync(this.DictionaryMachine, SharedDictionaryEvent.SetEvent(key, value)).Wait();
            }
        }

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        /// <returns>Size</returns>
        public int Count
        {
            get
            {
                var currentMachine = this.Runtime.GetCurrentMachine();
                this.Runtime.SendEventAsync(this.DictionaryMachine, SharedDictionaryEvent.CountEvent(currentMachine.Id)).Wait();
                var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<int>)).Result as SharedDictionaryResponseEvent<int>;
                return e.Value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">The P# runtime instance.</param>
        public MockSharedDictionary(ITestingRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Initializes the shared dictionary.
        /// </summary>
        /// <param name="comparer">Comparer for keys.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal async Task InitializeAsync(IEqualityComparer<TKey> comparer)
        {
            if (comparer != null)
            {
                this.DictionaryMachine = await this.Runtime.CreateMachineAsync(typeof(SharedDictionaryMachine<TKey, TValue>),
                    SharedDictionaryEvent.InitEvent(comparer));
            }
            else
            {
                this.DictionaryMachine = await this.Runtime.CreateMachineAsync(typeof(SharedDictionaryMachine<TKey, TValue>));
            }
        }

        /// <summary>
        /// Adds a new key to the dictionary, if it doesn’t already exist in the dictionary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>The result is true or false depending on whether the new key/value pair was added.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            return this.TryAddAsync(key, value).Result;
        }

        /// <summary>
        /// Adds a new key to the dictionary, if it doesn’t already exist in the dictionary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is
        /// true or false depending on whether the new key/value pair was added.
        /// </returns>
        public async Task<bool> TryAddAsync(TKey key, TValue value)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.DictionaryMachine, SharedDictionaryEvent.TryAddEvent(key, value, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
            return e.Value;
        }

        /// <summary>
        /// Updates the value for an existing key in the dictionary, if that key has a specific value.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="comparisonValue">The old value.</param>
        /// <returns>
        /// The result is true if the value with key was equal to comparisonValue and was replaced with newValue; otherwise, false.
        /// </returns>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            return this.TryUpdateAsync(key, newValue, comparisonValue).Result;
        }

        /// <summary>
        /// Updates the value for an existing key in the dictionary, if that key has a specific value.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="comparisonValue">The old value.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is true if the value with key
        /// was equal to comparisonValue and was replaced with newValue; otherwise, false.
        /// </returns>
        public async Task<bool> TryUpdateAsync(TKey key, TValue newValue, TValue comparisonValue)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.DictionaryMachine, SharedDictionaryEvent.TryUpdateEvent(
                key, newValue, comparisonValue, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
            return e.Value;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <param name="value">The value associated with the key, or the default value if the key does not exist.</param>
        /// <returns>The result is true if the key was found; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var tup = this.TryGetValueAsync(key).Result;
            value = tup.value;
            return tup.result;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is a tuple that
        /// contains a result that is true if the key was found, otherwise, false, and the
        /// value associated with the key, or the default value if the key does not exist.
        /// </returns>
        public async Task<(bool result, TValue value)> TryGetValueAsync(TKey key)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.DictionaryMachine, SharedDictionaryEvent.TryGetEvent(key, currentMachine.Id));
            var e = await currentMachine.Receive(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>))
                as SharedDictionaryResponseEvent<Tuple<bool, TValue>>;
            return (result: e.Value.Item1, value: e.Value.Item2);
        }

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="value">The value associated with the key if present, or the default value otherwise.</param>
        /// <returns>The result is true if the element is successfully removed; otherwise, false.</returns>
        public bool TryRemove(TKey key, out TValue value)
        {
            var tup = this.TryRemoveAsync(key).Result;
            value = tup.value;
            return tup.result;
        }

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is a tuple that contains a
        /// result that is true if the key was successfully removed, otherwise, false, and the value
        /// associated with the key, or the default value if the key does not exist.
        /// </returns>
        public async Task<(bool result, TValue value)> TryRemoveAsync(TKey key)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.DictionaryMachine, SharedDictionaryEvent.TryRemoveEvent(key, currentMachine.Id));
            var e = await currentMachine.Receive(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>))
                as SharedDictionaryResponseEvent<Tuple<bool, TValue>>;
            return (result: e.Value.Item1, value: e.Value.Item2);
        }
    }
}
