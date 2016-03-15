//-----------------------------------------------------------------------
// <copyright file="OnEventDoAction.cs">
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
    /// Attribute for declaring what action a machine should perform
    /// when it receives an event in a given state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OnEventDoAction : Attribute
    {
        /// <summary>
        /// Event type.
        /// </summary>
        internal Type Event;

        /// <summary>
        /// Action name.
        /// </summary>
        internal string Action;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="actionName">Action name</param>
        public OnEventDoAction(Type eventType, string actionName)
        {
            this.Event = eventType;
            this.Action = actionName;
        }
    }
}
