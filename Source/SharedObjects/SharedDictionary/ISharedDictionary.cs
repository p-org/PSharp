//-----------------------------------------------------------------------
// <copyright file="ISharedDictionary.cs">
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
using System.Threading.Tasks;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Interface of a shared dictionary.
    /// </summary>
    public interface ISharedDictionary<TKey, TValue> 
    {
        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        TValue this[TKey key] { get; set; }

        /// <summary>
        /// The number of elements in the dictionary.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a new key to the dictionary, if it doesn’t already exist in the dictionary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>The result is true or false depending on whether the new key/value pair was added.</returns>
        [Obsolete("Please use ISharedDictionary.TryAdd(...) instead.")]
        bool TryAdd(TKey key, TValue value);

        /// <summary>
        /// Adds a new key to the dictionary, if it doesn’t already exist in the dictionary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is
        /// true or false depending on whether the new key/value pair was added.
        /// </returns>
        Task<bool> TryAddAsync(TKey key, TValue value);

        /// <summary>
        /// Updates the value for an existing key in the dictionary, if that key has a specific value.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="comparisonValue">The old value.</param>
        /// <returns>
        /// The result is true if the value with key was equal to comparisonValue and was replaced with newValue; otherwise, false.
        /// </returns>
        [Obsolete("Please use ISharedDictionary.TryUpdate(...) instead.")]
        bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue);

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
        Task<bool> TryUpdateAsync(TKey key, TValue newValue, TValue comparisonValue);

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <param name="value">The value associated with the key, or the default value if the key does not exist.</param>
        /// <returns>The result is true if the key was found; otherwise, false.</returns>
        [Obsolete("Please use ISharedDictionary.TryGetValue(...) instead.")]
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is a tuple that
        /// contains a result that is true if the key was found, otherwise, false, and the
        /// value associated with the key, or the default value if the key does not exist.
        /// </returns>
        Task<(bool result, TValue value)> TryGetValueAsync(TKey key);

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="value">The value associated with the key if present, or the default value otherwise.</param>
        /// <returns>The result is true if the element is successfully removed; otherwise, false.</returns>
        [Obsolete("Please use ISharedDictionary.TryRemove(...) instead.")]
        bool TryRemove(TKey key, out TValue value);

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is a tuple that contains a
        /// result that is true if the key was successfully removed, otherwise, false, and the value
        /// associated with the key, or the default value if the key does not exist.
        /// </returns>
        Task<(bool result, TValue value)> TryRemoveAsync(TKey key);
    }
}
