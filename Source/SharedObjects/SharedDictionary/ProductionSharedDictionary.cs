//-----------------------------------------------------------------------
// <copyright file="ProductionSharedDictionary.cs">
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

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Implements a shared dictionary to be used in production.
    /// </summary>
    internal sealed class ProductionSharedDictionary<TKey, TValue> : ISharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// The internal dictionary.
        /// </summary>
        private readonly ConcurrentDictionary<TKey, TValue> InternalDictionary;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                return this.InternalDictionary[key];
            }

            set
            {
                this.InternalDictionary[key] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        public int Count => this.InternalDictionary.Count;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ProductionSharedDictionary()
        {
            this.InternalDictionary = new ConcurrentDictionary<TKey, TValue>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="comparer">Comparer for keys.</param>
        public ProductionSharedDictionary(IEqualityComparer<TKey> comparer)
        {
            this.InternalDictionary = new ConcurrentDictionary<TKey, TValue>(comparer);
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
        public Task<bool> TryAddAsync(TKey key, TValue value)
        {
            return Task.FromResult(this.InternalDictionary.TryAdd(key, value));
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
        public Task<bool> TryUpdateAsync(TKey key, TValue newValue, TValue comparisonValue)
        {
            return Task.FromResult(this.InternalDictionary.TryUpdate(key, newValue, comparisonValue));
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
        public Task<(bool result, TValue value)> TryGetValueAsync(TKey key)
        {
            var result = this.InternalDictionary.TryGetValue(key, out TValue value);
            return Task.FromResult((result: result, value: value));
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
        public Task<(bool result, TValue value)> TryRemoveAsync(TKey key)
        {
            var result = this.InternalDictionary.TryRemove(key, out TValue value);
            return Task.FromResult((result: result, value: value));
        }
    }
}
