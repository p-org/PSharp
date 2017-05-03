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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Implements a shared dictionary
    /// </summary>
    internal sealed class MockSharedDictionary<TKey, TValue> : ISharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// The dictionary
        /// </summary>
        MachineId dictionaryMachine;

        BugFindingRuntime Runtime;

        /// <summary>
        /// Initializes the dictionary
        /// </summary>
        /// <param name="comparer">Comparre for keys</param>
        /// <param name="Runtime">Runtime</param>
        public MockSharedDictionary(IEqualityComparer<TKey> comparer, BugFindingRuntime Runtime)
        {
            this.Runtime = Runtime;
            if (comparer != null)
            {
                dictionaryMachine = Runtime.CreateMachine(typeof(SharedDictionaryMachine<TKey, TValue>),
                    SharedDictionaryEvent.InitEvent(comparer));
            }
            else
            {
                dictionaryMachine = Runtime.CreateMachine(typeof(SharedDictionaryMachine<TKey, TValue>));
            }
        }

        /// <summary>
        /// Add a new key to the dictionary, if it doesn’t already exist in the dictionary
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>true or false depending on whether the new key/value pair was added.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            var currentMachine = Runtime.GetCurrentMachine();
            Runtime.SendEvent(dictionaryMachine, SharedDictionaryEvent.TryAddEvent(key, value, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
            return e.value;
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
            var currentMachine = Runtime.GetCurrentMachine();
            Runtime.SendEvent(dictionaryMachine, SharedDictionaryEvent.TryUpdateEvent(key, newValue, comparisonValue, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<bool>)).Result as SharedDictionaryResponseEvent<bool>;
            return e.value;
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
                var currentMachine = Runtime.GetCurrentMachine();
                Runtime.SendEvent(dictionaryMachine, SharedDictionaryEvent.GetEvent(key, currentMachine.Id));
                var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<TValue>)).Result as SharedDictionaryResponseEvent<TValue>;
                return e.value;
            }
            set
            {
                Runtime.SendEvent(dictionaryMachine, SharedDictionaryEvent.SetEvent(key, value));
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
            var currentMachine = Runtime.GetCurrentMachine();
            Runtime.SendEvent(dictionaryMachine, SharedDictionaryEvent.TryRemoveEvent(key, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<Tuple<bool, TValue>>)).Result as SharedDictionaryResponseEvent<Tuple<bool, TValue>>;
            value = e.value.Item2;
            return e.value.Item1;
        }

        /// <summary>
        /// Gets the number of elements in the dictionary
        /// </summary>
        /// <returns>Size</returns>
        public int Count
        {
            get
            {
                var currentMachine = Runtime.GetCurrentMachine();
                Runtime.SendEvent(dictionaryMachine, SharedDictionaryEvent.CountEvent(currentMachine.Id));
                var e = currentMachine.Receive(typeof(SharedDictionaryResponseEvent<int>)).Result as SharedDictionaryResponseEvent<int>;
                return e.value;
            }
        }
    }
}
