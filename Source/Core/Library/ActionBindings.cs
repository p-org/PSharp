//-----------------------------------------------------------------------
// <copyright file="ActionBindings.cs">
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
    /// Class representing a collection of action bindings.
    /// </summary>
    public sealed class ActionBindings : IEnumerable<KeyValuePair<Type, Action>>
    {
        /// <summary>
        /// A dictionary of action bindings. A key represents
        /// the type of an event, and the value is the action
        /// that is triggered by the event.
        /// </summary>
        private Dictionary<Type, Action> Dictionary;

        /// <summary>
        /// Default constructor of the ActionBindings class.
        /// </summary>
        public ActionBindings()
        {
            this.Dictionary = new Dictionary<Type, Action>();
        }

        /// <summary>
        /// Adds the specified pair of event and action to the collection.
        /// </summary>
        /// <param name="e">Type of the event</param>
        /// <param name="a">Action</param>
        public void Add(Type e, Action a)
        {
            this.Dictionary.Add(e, a);
        }

        /// <summary>
        /// Returns the action triggered by the specified type of event.
        /// </summary>
        /// <param name="key">Type of the event</param>
        /// <returns>Action</returns>
        public Action this[Type key]
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
        /// <returns></returns>
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
        public IEnumerator<KeyValuePair<Type, Action>> GetEnumerator()
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
