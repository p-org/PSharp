//-----------------------------------------------------------------------
// <copyright file="GotoStateTransitions.cs">
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
    /// Class representing a collection of goto state transitions.
    /// </summary>
    internal sealed class GotoStateTransitions : IEnumerable<KeyValuePair<Type, Tuple<Type, Action>>>
    {
        /// <summary>
        /// A dictionary of goto state transitions. A key represents
        /// the type of an event, and the value is the target state
        /// of the goto transition and an optional lambda function,
        /// which can execute after the default OnExit function of
        /// the exiting state.
        /// </summary>
        private Dictionary<Type, Tuple<Type, Action>> Dictionary;

        /// <summary>
        /// Constructor of the GotoStateTransitions class.
        /// </summary>
        public GotoStateTransitions()
        {
            this.Dictionary = new Dictionary<Type, Tuple<Type, Action>>();
        }

        /// <summary>
        /// Adds the specified pair of event, state to transition to, and an
        /// optional lambda function, which can execute after the default
        /// OnExit function of the exiting state, to the collection.
        /// </summary>
        /// <param name="e">Type of the event</param>
        /// <param name="s">Type of the state</param>
        /// <param name="a">Optional OnExit lambda</param>
        public void Add(Type e, Type s, Action a = null)
        {
            this.Dictionary.Add(e, new Tuple<Type, Action>(s, a));
        }

        /// <summary>
        /// Returns the state to transition to when receiving the
        /// specified type of event.
        /// </summary>
        /// <param name="key">Type of the event</param>
        /// <returns>Type of the state</returns>
        public Tuple<Type, Action> this[Type key]
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
        public IEnumerator<KeyValuePair<Type, Tuple<Type, Action>>> GetEnumerator()
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
