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
    /// Defines an action binding.
    /// </summary>
    internal sealed class ActionBinding : EventActionHandler
    {
        /// <summary>
        /// Name of the action
        /// </summary>
        public string Name;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ActionBinding(string ActionName)
        {
            Name = ActionName;
        }

    }

    /// <summary>
    /// Defines a skip action binding (for ignore)
    /// </summary>
    internal sealed class IgnoreAction : EventActionHandler
    {
    }

    /// <summary>
    /// Defines a collection of action bindings.
    /// </summary>
    internal sealed class ActionBindings : IEnumerable<KeyValuePair<Type, string>>
    {
        /// <summary>
        /// A dictionary of action bindings. A key represents
        /// the type of an event, and the value is the action
        /// that is triggered by the event.
        /// </summary>
        private Dictionary<Type, string> Dictionary;

        /// <summary>
        /// Constructor of the ActionBindings class.
        /// </summary>
        public ActionBindings()
        {
            this.Dictionary = new Dictionary<Type, string>();
        }

        /// <summary>
        /// Adds the specified pair of event and action to the collection.
        /// </summary>
        /// <param name="e">Type of the event</param>
        /// <param name="a">Action name</param>
        public void Add(Type e, string a)
        {
            this.Dictionary.Add(e, a);
        }

        /// <summary>
        /// Returns the action triggered by the specified type of event.
        /// </summary>
        /// <param name="key">Type of the event</param>
        /// <returns>Action name</returns>
        public string this[Type key]
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
        public IEnumerator<KeyValuePair<Type, string>> GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>IEnumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }
    }
}
