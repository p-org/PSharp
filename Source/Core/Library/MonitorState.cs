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
        /// Handle to the monitor that owns this state instance.
        /// </summary>
        private Monitor Monitor;

        /// <summary>
        /// The entry action, if the OnEntry is not overriden.
        /// </summary>
        private Action EntryAction;

        /// <summary>
        /// The exit action, if the OnExit is not overriden.
        /// </summary>
        private Action ExitAction;

        /// <summary>
        /// Dictionary containing all the goto state transitions.
        /// </summary>
        internal GotoStateTransitions GotoTransitions;

        /// <summary>
        /// Dictionary containing all the action bindings.
        /// </summary>
        internal ActionBindings ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

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
        /// <param name="monitor">Monitor</param>
        /// <param name="isHot">Is hot</param>
        /// <param name="isCold">Is cold</param>
        internal void InitializeState(Monitor monitor, bool isHot, bool isCold)
        {
            this.Monitor = monitor;

            this.IsHot = isHot;
            this.IsCold = isCold;

            this.GotoTransitions = new GotoStateTransitions();
            this.ActionBindings = new ActionBindings();

            this.IgnoredEvents = new HashSet<Type>();

            var entryAttribute = this.GetType().GetCustomAttribute(typeof(OnEntry), false) as OnEntry;
            var exitAttribute = this.GetType().GetCustomAttribute(typeof(OnExit), false) as OnExit;

            if (entryAttribute != null)
            {
                var method = this.Monitor.GetType().GetMethod(entryAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Monitor, method);
                this.EntryAction = action;
            }

            if (exitAttribute != null)
            {
                var method = this.Monitor.GetType().GetMethod(exitAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Monitor, method);
                this.ExitAction = action;
            }

            var gotoAttributes = this.GetType().GetCustomAttributes(typeof(OnEventGotoState), false)
                as OnEventGotoState[];
            var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoAction), false)
                as OnEventDoAction[];

            foreach (var attr in gotoAttributes)
            {
                if (attr.Action == null)
                {
                    this.GotoTransitions.Add(attr.Event, attr.State);
                }
                else
                {
                    var method = this.Monitor.GetType().GetMethod(attr.Action,
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Monitor, method);
                    this.GotoTransitions.Add(attr.Event, attr.State, action);
                }
            }

            foreach (var attr in doAttributes)
            {
                var method = this.Monitor.GetType().GetMethod(attr.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                this.Monitor.Runtime.Assert(method.GetParameters().Length == 0, "Action '{0}' in monitor " +
                    "'{1}' must have 0 formal parameters.", method.Name, this.Monitor.GetType().Name);
                this.Monitor.Runtime.Assert(method.ReturnType == typeof(void), "Action '{0}' in monitor " +
                    "'{1}' must have 'void' return type.", method.Name, this.Monitor.GetType().Name);

                var action = (Action)Delegate.CreateDelegate(typeof(Action), this.Monitor, method);
                this.ActionBindings.Add(attr.Event, action);
            }

            var ignoreEventsAttribute = this.GetType().GetCustomAttribute(typeof(IgnoreEvents), false) as IgnoreEvents;

            if (ignoreEventsAttribute != null)
            {
                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
            }
        }

        /// <summary>
        /// Executes the on entry function.
        /// </summary>
        internal void ExecuteEntryFunction()
        {
            if (this.EntryAction != null)
            {
                this.EntryAction();
            }
        }

        /// <summary>
        /// Executes the on exit function.
        /// </summary>
        internal void ExecuteExitFunction()
        {
            if (this.ExitAction != null)
            {
                this.ExitAction();
            }
        }

        #endregion
    }
}
