//-----------------------------------------------------------------------
// <copyright file="PushStateTransitions.cs">
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
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class representing a collection of push state transitions.
    /// </summary>
    internal sealed class PushStateTransitions : IEnumerable<KeyValuePair<Type, Type>>
    {
        /// <summary>
        /// A dictionary of push state transitions. A key represents
        /// the type of an event, and the value is the target state
        /// of the push transition.
        /// </summary>
        private Dictionary<Type, Type> Dictionary;

        /// <summary>
        /// Constructor of the PushStateTransitions class.
        /// </summary>
        public PushStateTransitions()
        {
            this.Dictionary = new Dictionary<Type, Type>();
        }

        /// <summary>
        /// Adds the specified pair of event and state for transition.
        /// </summary>
        /// <param name="e">Type of the event</param>
        /// <param name="s">Type of the state</param>
        public void Add(Type e, Type s)
        {
            this.Dictionary.Add(e, s);
        }

        /// <summary>
        /// Returns the state to transition to when receiving the
        /// specified type of event.
        /// </summary>
        /// <param name="key">Type of the event</param>
        /// <returns>Type of the state</returns>
        public Type this[Type key]
        {
            internal get
            {
                return this.Dictionary[key];
            }
            set
            {
                this.Dictionary[key] = value;
            }
        }

        /// <summary>
        /// Gets a collection containing the keys.
        /// </summary>
        /// <returns>Types</returns>
        public IEnumerable<Type> Keys()
        {
            return this.Dictionary.Keys;
        }

        /// <summary>
        /// Determines whether the collection contains the specified key.
        /// </summary>
        /// <param name="key">Type of the event</param>
        /// <returns>Boolean</returns>
        public bool ContainsKey(Type key)
        {
            return this.Dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>IEnumerator</returns>
        public IEnumerator<KeyValuePair<Type, Type>> GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.Dictionary.GetEnumerator();
        }
    }
}
