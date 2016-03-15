//-----------------------------------------------------------------------
// <copyright file="OnEventGotoState.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring which state a machine should transition to
    /// when it receives an event in a given state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OnEventGotoState : Attribute
    {
        /// <summary>
        /// Event type.
        /// </summary>
        internal Type Event;

        /// <summary>
        /// State type.
        /// </summary>
        internal Type State;

        /// <summary>
        /// Action name.
        /// </summary>
        internal string Action;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="stateType">State type</param>
        public OnEventGotoState(Type eventType, Type stateType)
        {
            this.Event = eventType;
            this.State = stateType;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="stateType">State type</param>
        /// <param name="actionName">Name of action to perform on exit</param>
        public OnEventGotoState(Type eventType, Type stateType, string actionName)
        {
            this.Event = eventType;
            this.State = stateType;
            this.Action = actionName;
        }
    }
}
