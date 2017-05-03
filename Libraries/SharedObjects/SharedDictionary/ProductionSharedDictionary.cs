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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Implements a shared dictionary
    /// </summary>
    internal sealed class ProductionSharedDictionary<TKey, TValue> : ISharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// The dictionary
        /// </summary>
        ConcurrentDictionary<TKey, TValue> dictionary;

        /// <summary>
        /// Initializes the dictionary
        /// </summary>
        public ProductionSharedDictionary()
        {
            dictionary = new ConcurrentDictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes the dictionary
        /// </summary>
        public ProductionSharedDictionary(IEqualityComparer<TKey> comparer)
        {
            dictionary = new ConcurrentDictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// Add a new key to the dictionary, if it doesn’t already exist in the dictionary
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>true or false depending on whether the new key/value pair was added.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            return dictionary.TryAdd(key, value);
        }

        /// <summary>
        /// Update the value for an existing key in the dictionary, if that key has a specific value
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="newValue">New value</param>
        /// <param name="comparisonValue">Old value</param>
        /// <returns>true if the value with key was equal to comparisonValue and was replaced with newValue; otherwise, false.</returns>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            return dictionary.TryUpdate(key, newValue, comparisonValue);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        public TValue this[TKey key]
        {
            get
            {
                return dictionary[key];
            }
            set
            {
                dictionary[key] = value;
            }
        }

        /// <summary>
        /// Removes the specified key from the dictionary
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value associated with the key if present, or the default value otherwise.</param>
        /// <returns>true if the element is successfully removed; otherwise, false.</returns>
        public bool TryRemove(TKey key, out TValue value)
        {
            return dictionary.TryRemove(key, out value);
        }

        /// <summary>
        /// Gets the number of elements in the dictionary
        /// </summary>
        /// <returns>Size</returns>
        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }
    }
}
