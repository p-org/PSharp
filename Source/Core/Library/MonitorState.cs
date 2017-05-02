//-----------------------------------------------------------------------
// <copyright file="MonitorState.cs">
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
using System.Reflection;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state of a P# monitor.
    /// </summary>
    public abstract class MonitorState
    {
        #region fields

        /// <summary>
        /// The entry action of the state.
        /// </summary>
        internal string EntryAction { get; private set; }

        /// <summary>
        /// The exit action of the state.
        /// </summary>
        internal string ExitAction { get; private set; }

        /// <summary>
        /// Dictionary containing all the goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the action bindings.
        /// </summary>
        internal Dictionary<Type, ActionBinding> ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        /// <summary>
        /// True if this is the start state.
        /// </summary>
        internal bool IsStart { get; private set; }

        /// <summary>
        /// Returns true if this is a hot state.
        /// </summary>
        internal bool IsHot { get; private set; }

        /// <summary>
        /// Returns true if this is a cold state.
        /// </summary>
        internal bool IsCold { get; private set; }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        protected MonitorState() { }

        /// <summary>
        /// Initializes the state.
        /// </summary>
        internal void InitializeState()
        {
            this.IsStart = false;
            this.IsHot = false;
            this.IsCold = false;

            this.GotoTransitions = new Dictionary<Type, GotoStateTransition>();
            this.ActionBindings = new Dictionary<Type, ActionBinding>();

            this.IgnoredEvents = new HashSet<Type>();

            var entryAttribute = this.GetType().GetCustomAttribute(typeof(OnEntry), false) as OnEntry;
            var exitAttribute = this.GetType().GetCustomAttribute(typeof(OnExit), false) as OnExit;

            if (entryAttribute != null)
            {
                this.EntryAction = entryAttribute.Action;
            }

            if (exitAttribute != null)
            {
                this.ExitAction = exitAttribute.Action;
            }

            var gotoAttributes = this.GetType().GetCustomAttributes(typeof(OnEventGotoState), false)
                as OnEventGotoState[];
            var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoAction), false)
                as OnEventDoAction[];

            foreach (var attr in gotoAttributes)
            {
                if (attr.Action == null)
                {
                    this.GotoTransitions.Add(attr.Event, new GotoStateTransition(attr.State));
                }
                else
                {
                    this.GotoTransitions.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                }
            }

            foreach (var attr in doAttributes)
            {
                this.ActionBindings.Add(attr.Event, new ActionBinding(attr.Action));
            }

            var ignoreEventsAttribute = this.GetType().GetCustomAttribute(typeof(IgnoreEvents), false) as IgnoreEvents;

            if (ignoreEventsAttribute != null)
            {
                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
            }

            if (this.GetType().IsDefined(typeof(Start), false))
            {
                this.IsStart = true;
            }

            if (this.GetType().IsDefined(typeof(Hot), false))
            {
                this.IsHot = true;
            }

            if (this.GetType().IsDefined(typeof(Cold), false))
            {
                this.IsCold = true;
            }
        }

        #endregion
    }
}
