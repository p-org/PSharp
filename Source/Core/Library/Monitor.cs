//-----------------------------------------------------------------------
// <copyright file="Monitor.cs">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a P# monitor.
    /// </summary>
    public abstract class Monitor
    {
        #region static fields

        /// <summary>
        /// Map from monitor types to a set of all
        /// possible states types.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap;

        /// <summary>
        /// Map from monitor types to a set of all
        /// available states.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<MonitorState>> StateMap;

        /// <summary>
        /// Map from monitor types to a set of all
        /// available actions.
        /// </summary>
        private static ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MonitorActionMap;

        #endregion

        #region fields

        /// <summary>
        /// The runtime that executes this monitor.
        /// </summary>
        private PSharpRuntime Runtime;

        /// <summary>
        /// The monitor state.
        /// </summary>
        private MonitorState State;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current action bindings.
        /// </summary>
        internal Dictionary<Type, ActionBinding> ActionBindings;

        /// <summary>
        /// Map from action names to actions.
        /// </summary>
        private Dictionary<string, MethodInfo> ActionMap;

        /// <summary>
        /// Set of currently ignored event types.
        /// </summary>
        private HashSet<Type> IgnoredEvents;

        /// <summary>
        /// A counter that increases in each step of the execution,
        /// as long as the monitor remains in a hot state. If the
        /// temperature reaches the specified limit, then a potential
        /// liveness bug has been found.
        /// </summary>
        private int LivenessTemperature;

        /// <summary>
        /// Checks if the monitor is executing an OnExit method.
        /// </summary>
        internal bool IsInsideOnExit;

        /// <summary>
        /// Checks if the current action called a transition statement.
        /// </summary>
        internal bool CurrentActionCalledTransitionStatement;

        #endregion

        #region properties

        /// <summary>
        /// The unique monitor id.
        /// </summary>
        internal MachineId Id { get; private set; }

        /// <summary>
        /// Gets the name of this monitor.
        /// </summary>
        protected internal string Name => this.Id.Name;

        /// <summary>
        /// The logger installed to the P# runtime.
        /// </summary>
        protected ILogger Logger => this.Runtime.Logger;

        /// <summary>
        /// Gets the current state.
        /// </summary>
        protected internal Type CurrentState
        {
            get
            {
                if (this.State == null)
                {
                    return null;
                }

                return this.State.GetType();
            }
        }

        /// <summary>
        /// Gets the current state name.
        /// </summary>
        internal string CurrentStateName
        {
            get
            {
                return $"{this.CurrentState.DeclaringType}." +
                    $"{this.CurrentState.Name}";
            }
        }

        /// <summary>
        /// Gets the current state name with temperature.
        /// </summary>
        internal string CurrentStateNameWithTemperature
        {
            get
            {
                return CurrentStateName + 
                    (IsInHotState() ? "[hot]" :
                    IsInColdState() ? "[cold]" :
                    "");
            }
        }

        /// <summary>
        /// Gets the latest received event, or null if no event
        /// has been received.
        /// </summary>
        protected internal Event ReceivedEvent { get; private set; }

        #endregion

        #region initialization

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Monitor()
        {
            StateTypeMap = new ConcurrentDictionary<Type, HashSet<Type>>();
            StateMap = new ConcurrentDictionary<Type, HashSet<MonitorState>>();
            MonitorActionMap = new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Monitor()
            : base()
        {
            this.ActionMap = new Dictionary<string, MethodInfo>();
            this.LivenessTemperature = 0;
        }

        /// <summary>
        /// Initializes this monitor.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal void Initialize(MachineId mid)
        {
            this.Id = mid;
            this.Runtime = mid.Runtime;
            this.IsInsideOnExit = false;
            this.CurrentActionCalledTransitionStatement = false;
        }

        #endregion

        #region P# user API

        /// <summary>
        /// Returns from the execution context, and transitions
        /// the monitor to the given <see cref="MonitorState"/>.
        /// </summary>
        /// <param name="s">Type of the state</param>
        protected void Goto(Type s)
        {
            // If the state is not a state of the monitor, then report an error and exit.
            this.Assert(StateTypeMap[this.GetType()].Any(val
                => val.DeclaringType.Equals(s.DeclaringType) &&
                val.Name.Equals(s.Name)), $"Monitor '{this.Id}' " +
                $"is trying to transition to non-existing state '{s.Name}'.");
            this.Raise(new GotoStateEvent(s));
        }

        /// <summary>
        /// Raises an <see cref="Event"/> internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        protected void Raise(Event e)
        {
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Monitor '{this.GetType().Name}' is raising a null event.");
            EventInfo raisedEvent = new EventInfo(e, new EventOriginInfo(
                this.Id, this.GetType().Name, StateGroup.GetQualifiedStateName(this.CurrentState)));
            this.Runtime.NotifyRaisedEvent(this, raisedEvent);
            this.HandleEvent(e);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws
        /// an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        protected void Assert(bool predicate)
        {
            this.Runtime.Assert(predicate);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws
        /// an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        protected void Assert(bool predicate, string s, params object[] args)
        {
            this.Runtime.Assert(predicate, s, args);
        }

        #endregion

        #region monitoring

        /// <summary>
        /// Notifies the monitor to handle the received event.
        /// </summary>
        /// <param name="e">Event</param>
        internal void MonitorEvent(Event e)
        {
            this.Runtime.Log($"<MonitorLog> Monitor '{this.GetType().Name}' " +
                $"is processing event '{e.GetType().FullName}'.");
            this.HandleEvent(e);
        }

        #endregion

        #region event handling

        /// <summary>
        /// Handles the given event.
        /// </summary>
        /// <param name="e">Event to handle</param>
        private void HandleEvent(Event e)
        {
            this.CurrentActionCalledTransitionStatement = false;

            // Do not process an ignored event.
            if (this.IgnoredEvents.Contains(e.GetType()))
            {
                return;
            }

            // Assigns the receieved event.
            this.ReceivedEvent = e;

            while (true)
            {
                if (this.State == null)
                {
                    // If the event cannot be handled, then report an error and exit.
                    this.Assert(false, $"Monitor '{this.GetType().Name}' received event " +
                        $"'{e.GetType().FullName}' that cannot be handled.");
                }

                // If current state cannot handle the event then null the state.
                if (!this.CanHandleEvent(e.GetType()))
                {
                    this.Runtime.NotifyExitedState(this);
                    this.State = null;
                    continue;
                }

                // Checks if the event is a goto state event.
                if (e.GetType() == typeof(GotoStateEvent))
                {
                    Type targetState = (e as GotoStateEvent).State;
                    this.GotoState(targetState, null);
                }
                // Checks if the event can trigger a goto state transition.
                else if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    var transition = this.GotoTransitions[e.GetType()];
                    this.GotoState(transition.TargetState, transition.Lambda);
                }
                // Checks if the event can trigger an action.
                else if (this.ActionBindings.ContainsKey(e.GetType()))
                {
                    var handler = this.ActionBindings[e.GetType()];
                    this.Do(handler.Name);
                }

                break;
            }
        }

        /// <summary>
        /// Invokes an action.
        /// </summary>
        /// <param name="actionName">Action name</param>
        [DebuggerStepThrough]
        private void Do(string actionName)
        {
            MethodInfo action = this.ActionMap[actionName];
            this.Runtime.NotifyInvokedAction(this, action, ReceivedEvent);
            this.ExecuteAction(action);
        }

        /// <summary>
        /// Executes the on entry function of the current state.
        /// </summary>
        [DebuggerStepThrough]
        private void ExecuteCurrentStateOnEntry()
        {
            this.Runtime.NotifyEnteredState(this);

            MethodInfo entryAction = null;
            if (this.State.EntryAction != null)
            {
                entryAction = this.ActionMap[this.State.EntryAction];
            }

            // Invokes the entry action of the new state,
            // if there is one available.
            if (entryAction != null)
            {
                this.ExecuteAction(entryAction);
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        /// <param name="eventHandlerExitActionName">Action name</param>
        [DebuggerStepThrough]
        private void ExecuteCurrentStateOnExit(string eventHandlerExitActionName)
        {
            this.Runtime.NotifyExitedState(this);

            MethodInfo exitAction = null;
            if (this.State.ExitAction != null)
            {
                exitAction = this.ActionMap[this.State.ExitAction];
            }

            this.IsInsideOnExit = true;

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                this.ExecuteAction(exitAction);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null)
            {
                MethodInfo eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                this.ExecuteAction(eventHandlerExitAction);
            }

            this.IsInsideOnExit = false;
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <param name="action">MethodInfo</param>
        [DebuggerStepThrough]
        private void ExecuteAction(MethodInfo action)
        {
            try
            {
                action.Invoke(this, null);
            }
            catch (ExecutionCanceledException ex)
            {
                throw ex;
            }
            catch (TaskSchedulerException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                // Reports the unhandled exception.
                this.ReportUnhandledException(ex, action.Name);
            }
        }

        /// <summary>
        /// Performs a goto transition to the given state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="onExitActionName">Action name</param>
        private void GotoState(Type s, string onExitActionName)
        {
            // The monitor performs the on exit statements of the current state.
            this.ExecuteCurrentStateOnExit(onExitActionName);

            var nextState = StateMap[this.GetType()].First(val => val.GetType().Equals(s));
            this.ConfigureStateTransitions(nextState);

            // The monitor transitions to the new state.
            this.State = nextState;

            if (nextState.IsCold)
            {
                this.LivenessTemperature = 0;
            }

            // The monitor performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Checks if the state can handle the given event type. An event
        /// can be handled if it is deferred, or leads to a transition or
        /// action binding.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean</returns>
        private bool CanHandleEvent(Type e)
        {
            if (this.GotoTransitions.ContainsKey(e) ||
                this.ActionBindings.ContainsKey(e) ||
                e == typeof(GotoStateEvent))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the state has a default handler.
        /// </summary>
        /// <returns></returns>
        private bool HasDefaultHandler()
        {
            if (this.GotoTransitions.ContainsKey(typeof(Default)) ||
                this.ActionBindings.ContainsKey(typeof(Default)))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region liveness checking

        /// <summary>
        /// Checks the liveness temperature of the monitor and report
        /// a potential liveness bug if the temperature passes the
        /// specified threshold. Only works in a liveness monitor.
        /// </summary>
        internal void CheckLivenessTemperature()
        {
            if (this.State.IsHot &&
                this.Runtime.Configuration.LivenessTemperatureThreshold > 0)
            {
                this.LivenessTemperature++;
                this.Runtime.Assert(this.LivenessTemperature <= this.Runtime.
                    Configuration.LivenessTemperatureThreshold,
                    $"Monitor '{this.GetType().Name}' detected potential liveness " +
                    $"bug in hot state '{this.CurrentStateName}'.");
            }
        }

        /// <summary>
        /// Returns true if the monitor is in a hot state.
        /// </summary>
        /// <returns>Boolean</returns>
        internal bool IsInHotState()
        {
            return this.State.IsHot;
        }

        /// <summary>
        /// Returns true if the monitor is in a hot state. Also outputs
        /// the name of the current state.
        /// </summary>
        /// <param name="stateName">State name</param>
        /// <returns>Boolean</returns>
        internal bool IsInHotState(out string stateName)
        {
            stateName = this.CurrentStateName;
            return this.State.IsHot;
        }

        /// <summary>
        /// Returns true if the monitor is in a cold state.
        /// </summary>
        /// <returns>Boolean</returns>
        internal bool IsInColdState()
        {
            return this.State.IsCold;
        }

        /// <summary>
        /// Returns true if the monitor is in a cold state. Also outputs
        /// the name of the current state.
        /// </summary>
        /// <param name="stateName">State name</param>
        /// <returns>Boolean</returns>
        internal bool IsInColdState(out string stateName)
        {
            stateName = this.CurrentStateName;
            return this.State.IsCold;
        }

        #endregion

        #region generic public and override methods

        /// <summary>
        /// Returns the hashed state of this monitor.
        /// </summary>
        /// <returns></returns>
        protected virtual int GetHashedState()
        {
            return 0;
        }

        /// <summary>
        /// Returns the cached state of this monitor.
        /// </summary>
        /// <returns>Hash value</returns>
        internal int GetCachedState()
        {
            unchecked
            {
                var hash = 19;

                hash = hash * 31 + this.GetType().GetHashCode();
                hash = hash * 31 + this.CurrentState.GetHashCode();

                // Adds the user-defined hashed state.
                if (this.Runtime.Configuration.UserHash)
                {
                    hash = hash * 31 + this.GetHashedState();
                }
                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current monitor.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.GetType().Name;
        }

        #endregion

        #region initialization

        /// <summary>
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        internal void GotoStartState()
        {
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Initializes information about the states of the monitor.
        /// </summary>
        internal void InitializeStateInformation()
        {
            Type monitorType = this.GetType();

            // Caches the available state types for this monitor type.
            if (StateTypeMap.TryAdd(monitorType, new HashSet<Type>()))
            {
                Type baseType = monitorType;
                while (baseType != typeof(Monitor))
                {
                    foreach (var s in baseType.GetNestedTypes(BindingFlags.Instance |
                        BindingFlags.NonPublic | BindingFlags.Public |
                        BindingFlags.DeclaredOnly))
                    {
                        this.ExtractStateTypes(s);
                    }

                    baseType = baseType.BaseType;
                }
            }

            // Caches the available state instances for this monitor type.
            if (StateMap.TryAdd(monitorType, new HashSet<MonitorState>()))
            {
                foreach (var type in StateTypeMap[monitorType])
                {
                    Type stateType = type;
                    if (type.IsGenericType)
                    {
                        // If the state type is generic (only possible if inherited by a
                        // generic monitor declaration), then iterate through the base
                        // monitor classes to identify the runtime generic type, and use
                        // it to instantiate the runtime state type. This type can be
                        // then used to create the state constructor.
                        Type declaringType = this.GetType();
                        while (!declaringType.IsGenericType ||
                            !type.DeclaringType.FullName.Equals(declaringType.FullName.Substring(
                            0, declaringType.FullName.IndexOf('['))))
                        {
                            declaringType = declaringType.BaseType;
                        }

                        if (declaringType.IsGenericType)
                        {
                            stateType = type.MakeGenericType(declaringType.GetGenericArguments());
                        }
                    }

                    ConstructorInfo constructor = stateType.GetConstructor(Type.EmptyTypes);
                    var lambda = Expression.Lambda<Func<MonitorState>>(
                        Expression.New(constructor)).Compile();
                    MonitorState state = lambda();

                    state.InitializeState();

                    this.Assert((state.IsCold && !state.IsHot) ||
                        (!state.IsCold && state.IsHot) ||
                        (!state.IsCold && !state.IsHot),
                        $"State '{type.FullName}' of monitor '{this.Id}' " +
                        "cannot be both cold and hot.");

                    StateMap[monitorType].Add(state);
                }
            }

            // Caches the actions declarations for this monitor type.
            if (MonitorActionMap.TryAdd(monitorType, new Dictionary<string, MethodInfo>()))
            {
                foreach (var state in StateMap[monitorType])
                {
                    if (state.EntryAction != null &&
                        !MonitorActionMap[monitorType].ContainsKey(state.EntryAction))
                    {
                        MonitorActionMap[monitorType].Add(state.EntryAction,
                            this.GetActionWithName(state.EntryAction));
                    }

                    if (state.ExitAction != null &&
                        !MonitorActionMap[monitorType].ContainsKey(state.ExitAction))
                    {
                        MonitorActionMap[monitorType].Add(state.ExitAction,
                            this.GetActionWithName(state.ExitAction));
                    }

                    foreach (var transition in state.GotoTransitions)
                    {
                        if (transition.Value.Lambda != null &&
                            !MonitorActionMap[monitorType].ContainsKey(transition.Value.Lambda))
                        {
                            MonitorActionMap[monitorType].Add(transition.Value.Lambda,
                                this.GetActionWithName(transition.Value.Lambda));
                        }
                    }

                    foreach (var action in state.ActionBindings)
                    {
                        if (!MonitorActionMap[monitorType].ContainsKey(action.Value.Name))
                        {
                            MonitorActionMap[monitorType].Add(action.Value.Name,
                                this.GetActionWithName(action.Value.Name));
                        }
                    }
                }
            }

            // Populates the map of actions for this monitor instance.
            foreach (var kvp in MonitorActionMap[monitorType])
            {
                this.ActionMap.Add(kvp.Key, kvp.Value);
            }

            var initialStates = StateMap[monitorType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, $"Monitor '{this.Id}' must declare a start state.");
            this.Assert(initialStates.Count == 1, $"Monitor '{this.Id}' " +
                "can not declare more than one start states.");

            this.ConfigureStateTransitions(initialStates.Single());
            this.State = initialStates.Single();

            this.AssertStateValidity();
        }

        /// <summary>
        /// Processes a type, looking for monitor states.
        /// </summary>
        /// <param name="type">Type</param>
        private void ExtractStateTypes(Type type)
        {
            Stack<Type> stack = new Stack<Type>();
            stack.Push(type);

            while (stack.Count > 0)
            {
                Type nextType = stack.Pop();

                if (nextType.IsClass && nextType.IsSubclassOf(typeof(MonitorState)))
                {
                    StateTypeMap[this.GetType()].Add(nextType);
                }
                else if (nextType.IsClass && nextType.IsSubclassOf(typeof(StateGroup)))
                {
                    // Adds the contents of the group of states to the stack.
                    foreach (var t in nextType.GetNestedTypes(BindingFlags.Instance |
                        BindingFlags.NonPublic | BindingFlags.Public |
                        BindingFlags.DeclaredOnly))
                    {
                        this.Assert(t.IsSubclassOf(typeof(StateGroup)) ||
                            t.IsSubclassOf(typeof(MonitorState)), $"'{t.Name}' " +
                            $"is neither a group of states nor a state.");
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Configures the state transitions of the monitor.
        /// </summary>
        /// <param name="state">State</param>
        private void ConfigureStateTransitions(MonitorState state)
        {
            this.GotoTransitions = state.GotoTransitions;
            this.ActionBindings = state.ActionBindings;
            this.IgnoredEvents = state.IgnoredEvents;
        }

        #endregion

        #region utilities

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <returns>Action</returns>
        private MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo method = null;
            Type monitorType = this.GetType();

            do
            {
                method = monitorType.GetMethod(actionName,
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    Type.DefaultBinder, new Type[0], null);
                monitorType = monitorType.BaseType;
            }
            while (method == null && monitorType != typeof(Monitor));

            this.Assert(method != null, "Cannot detect action declaration '{0}' " +
                "in monitor '{1}'.", actionName, this.GetType().Name);
            this.Assert(method.GetParameters().Length == 0, "Action '{0}' in monitor " +
                "'{1}' must have 0 formal parameters.", method.Name, this.GetType().Name);
            this.Assert(method.ReturnType == typeof(void), "Action '{0}' in monitor " +
                "'{1}' must have 'void' return type.", method.Name, this.GetType().Name);

            return method;
        }

        #endregion

        #region error checking

        /// <summary>
        /// Check monitor for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(StateTypeMap[this.GetType()].Count > 0, $"Monitor '{this.GetType().Name}' " +
                "must have one or more states.\n");
            this.Assert(this.State != null, $"Monitor '{this.GetType().Name}' " +
                "must not have a null current state.\n");
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="actionName">Action name</param>
        private void ReportUnhandledException(Exception ex, string actionName)
        {
            string state = "<unknown>";
            if (this.CurrentState != null)
            {
                state = this.CurrentStateName;
            }

            this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                $"in monitor '{this.Id}', state '{state}', action '{actionName}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        #endregion

        #region code coverage methods

        /// <summary>
        /// Returns the set of all states in the monitor
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the monitor</returns>
        internal HashSet<string> GetAllStates()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()),
                $"Monitor '{this.Id}' hasn't populated its states yet.");

            var allStates = new HashSet<string>();
            foreach (var state in StateMap[this.GetType()])
            {
                allStates.Add(StateGroup.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the monitor
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all (states, registered event) pairs in the monitor</returns>
        internal HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()),
                $"Monitor '{this.Id}' hasn't populated its states yet.");

            var pairs = new HashSet<Tuple<string, string>>();
            foreach (var state in StateMap[this.GetType()])
            {
                foreach (var binding in state.ActionBindings)
                {
                    pairs.Add(Tuple.Create(StateGroup.GetQualifiedStateName(state.GetType()), binding.Key.Name));
                }

                foreach (var transition in state.GotoTransitions)
                {
                    pairs.Add(Tuple.Create(StateGroup.GetQualifiedStateName(state.GetType()), transition.Key.Name));
                }
            }

            return pairs;
        }

        #endregion

        #region cleanup methods

        /// <summary>
        /// Resets the static caches.
        /// </summary>
        internal static void ResetCaches()
        {
            StateTypeMap.Clear();
            StateMap.Clear();
            MonitorActionMap.Clear();
        }

        #endregion
    }
}
