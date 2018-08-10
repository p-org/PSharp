//-----------------------------------------------------------------------
// <copyright file="BaseMachine.cs">
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
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Base abstract class of a P# machine.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class BaseMachine
    {
        #region static fields

        /// <summary>
        /// Is the machine state cached yet?
        /// </summary>
        private static ConcurrentDictionary<Type, bool> MachineStateCached;

        /// <summary>
        /// Map from machine types to a set of all
        /// possible states types.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap;

        /// <summary>
        /// Map from machine types to a set of all
        /// available states.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<MachineState>> StateMap;

        /// <summary>
        /// Map from machine types to a set of all
        /// available actions.
        /// </summary>
        private static ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MachineActionMap;

        #endregion

        /// <summary>
        /// The runtime that executes this machine.
        /// </summary>
        private protected BaseRuntime Runtime { get; private set; }

        /// <summary>
        /// A stack of machine states. The state on the top of
        /// the stack represents the current state.
        /// </summary>
        private protected Stack<MachineState> StateStack;

        /// <summary>
        /// A stack of maps that determine event handling action for
        /// each event type. These maps do not keep transition handlers.
        /// This stack has always the same height as StateStack.
        /// </summary>
        private readonly Stack<Dictionary<Type, EventActionHandler>> ActionHandlerStack;

        /// <summary>
        /// Map from action names to actions.
        /// </summary>
        private readonly Dictionary<string, CachedAction> ActionMap;

        /// <summary>
        /// Gets the raised event. If no event has been raised
        /// this will return null.
        /// </summary>
        private EventInfo RaisedEvent;

        /// <summary>
        /// Is the machine running.
        /// </summary>
        internal bool IsRunning;

        /// <summary>
        /// Is pop invoked in the current action.
        /// </summary>
        private bool IsPopInvoked;

        /// <summary>
        /// User OnException asked for the machine to be gracefully halted
        /// (suppressing the exception).
        /// </summary>
        private bool OnExceptionRequestedGracefulHalt;

        /// <summary>
        /// The unique machine id.
        /// </summary>
        protected internal MachineId Id { get; private set; }

        /// <summary>
        /// Stores machine-related information, which can used
        /// for scheduling and testing.
        /// </summary>
        internal MachineInfo Info { get; private set; }

        /// <summary>
        /// The logger installed to the P# runtime.
        /// </summary>
        protected ILogger Logger => this.Runtime.Logger;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions { get; private set; }

        /// <summary>
        /// Dictionary containing all the current push state transitions.
        /// </summary>
        internal Dictionary<Type, PushStateTransition> PushTransitions { get; private set; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the current state.
        /// </summary>
        protected internal Type CurrentState
        {
            get
            {
                if (this.StateStack.Count == 0)
                {
                    return null;
                }

                return this.StateStack.Peek().GetType();
            }
        }

        /// <summary>
        /// Gets the current action handler map.
        /// </summary>
        private protected Dictionary<Type, EventActionHandler> CurrentActionHandlerMap
        {
            get
            {
                if (this.ActionHandlerStack.Count == 0)
                {
                    return null;
                }

                return this.ActionHandlerStack.Peek();
            }
        }

        /// <summary>
        /// Gets the name of the current state.
        /// </summary>
        internal string CurrentStateName
        {
            get
            {
                return this.CurrentState == null
                    ? String.Empty
                    : $"{this.CurrentState.DeclaringType}.{StateGroup.GetQualifiedStateName(this.CurrentState)}";
            }
        }

        /// <summary>
        /// Gets the latest received <see cref="Event"/>, or null if
        /// no <see cref="Event"/> has been received.
        /// </summary>
        protected internal Event ReceivedEvent { get; set; }

        /// <summary>
        /// User-defined hashed state of the machine. Override to improve the
        /// accuracy of liveness checking when state-caching is enabled.
        /// </summary>
        protected virtual int HashedState => 0;

        /// <summary>
        /// Unique id of the group of operations that is
        /// associated with the next operation.
        /// </summary>
        protected Guid OperationGroupId
        {
            get
            {
                return Info.OperationGroupId;
            }
            set
            {
                Info.OperationGroupId = value;
            }
        }

        #region initialization

        /// <summary>
        /// Static constructor.
        /// </summary>
        static BaseMachine()
        {
            MachineStateCached = new ConcurrentDictionary<Type, bool>();
            StateTypeMap = new ConcurrentDictionary<Type, HashSet<Type>>();
            StateMap = new ConcurrentDictionary<Type, HashSet<MachineState>>();
            MachineActionMap = new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected BaseMachine() : base()
        {
            this.StateStack = new Stack<MachineState>();
            this.ActionHandlerStack = new Stack<Dictionary<Type, EventActionHandler>>();
            this.ActionMap = new Dictionary<string, CachedAction>();

            this.IsRunning = true;
            this.IsPopInvoked = false;
            this.OnExceptionRequestedGracefulHalt = false;
        }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        /// <param name="runtime">The P# runtime instance.</param>
        /// <param name="mid">The id of this machine.</param>
        /// <param name="info">The metadata of this machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal Task InitializeAsync(BaseRuntime runtime, MachineId mid, MachineInfo info)
        {
            this.Runtime = runtime;
            this.Id = mid;
            this.Info = info;
            return this.InitializeStateInformationAsync();
        }

        /// <summary>
        /// Initializes information about the states of the machine.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private async Task InitializeStateInformationAsync()
        {
            Type machineType = this.GetType();

            if (MachineStateCached.TryAdd(machineType, false))
            {
                // Caches the available state types for this machine type.
                if (StateTypeMap.TryAdd(machineType, new HashSet<Type>()))
                {
                    Type baseType = machineType;
                    while (baseType != this.GetMachineType())
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

                // Caches the available state instances for this machine type.
                if (StateMap.TryAdd(machineType, new HashSet<MachineState>()))
                {
                    foreach (var type in StateTypeMap[machineType])
                    {
                        Type stateType = type;
                        if (type.IsAbstract)
                        {
                            continue;
                        }

                        if (type.IsGenericType)
                        {
                            // If the state type is generic (only possible if inherited by a
                            // generic machine declaration), then iterate through the base
                            // machine classes to identify the runtime generic type, and use
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
                        var lambda = Expression.Lambda<Func<MachineState>>(
                            Expression.New(constructor)).Compile();
                        MachineState state = lambda();

                        try
                        {
                            state.InitializeState();
                        }
                        catch (InvalidOperationException ex)
                        {
                            this.Assert(false, $"Machine '{this.Id}' {ex.Message} in state '{state}'.");
                        }

                        StateMap[machineType].Add(state);
                    }
                }

                // Caches the actions declarations for this machine type.
                if (MachineActionMap.TryAdd(machineType, new Dictionary<string, MethodInfo>()))
                {
                    foreach (var state in StateMap[machineType])
                    {
                        if (state.EntryAction != null &&
                            !MachineActionMap[machineType].ContainsKey(state.EntryAction))
                        {
                            MachineActionMap[machineType].Add(state.EntryAction,
                                this.GetActionWithName(state.EntryAction));
                        }

                        if (state.ExitAction != null &&
                            !MachineActionMap[machineType].ContainsKey(state.ExitAction))
                        {
                            MachineActionMap[machineType].Add(state.ExitAction,
                                this.GetActionWithName(state.ExitAction));
                        }

                        foreach (var transition in state.GotoTransitions)
                        {
                            if (transition.Value.Lambda != null &&
                                !MachineActionMap[machineType].ContainsKey(transition.Value.Lambda))
                            {
                                MachineActionMap[machineType].Add(transition.Value.Lambda,
                                    this.GetActionWithName(transition.Value.Lambda));
                            }
                        }

                        foreach (var action in state.ActionBindings)
                        {
                            if (!MachineActionMap[machineType].ContainsKey(action.Value.Name))
                            {
                                MachineActionMap[machineType].Add(action.Value.Name,
                                    this.GetActionWithName(action.Value.Name));
                            }
                        }
                    }
                }

                // Cache completed.
                lock (MachineStateCached)
                {
                    MachineStateCached[machineType] = true;
                    System.Threading.Monitor.PulseAll(MachineStateCached);
                }
            }
            else if (!MachineStateCached[machineType])
            {
                lock (MachineStateCached)
                {
                    while (!MachineStateCached[machineType])
                    {
                        System.Threading.Monitor.Wait(MachineStateCached);
                    }
                }
            }

            // Populates the map of actions for this machine instance.
            foreach (var kvp in MachineActionMap[machineType])
            {
                this.ActionMap.Add(kvp.Key, new CachedAction(kvp.Value, this));
            }

            var initialStates = StateMap[machineType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, $"Machine '{this.Id}' must declare a start state.");
            this.Assert(initialStates.Count == 1, $"Machine '{this.Id}' " +
                "can not declare more than one start states.");

            await this.DoStatePushAsync(initialStates.Single());

            this.AssertStateValidity();
        }

        #endregion

        #region user interface

        /// <summary>
        /// Raises an <see cref="Event"/> internally at the end of the current action.
        /// </summary>
        /// <param name="e">Event</param>
        protected void Raise(Event e)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Raise while halted.");
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{this.Id}' is raising a null event.");
            this.RaisedEvent = new EventInfo(e, new EventOriginInfo(
                this.Id, this.GetType().Name, StateGroup.GetQualifiedStateName(this.CurrentState)));
            this.Runtime.NotifyRaisedEvent(this, this.RaisedEvent);
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action.
        /// </summary>
        /// <typeparam name="S">Type of the state</typeparam>
        protected void Goto<S>() where S : MachineState
        {
#pragma warning disable 618
            Goto(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action. Deprecated in favor of Goto&lt;T&gt;().
        /// </summary>
        /// <param name="s">Type of the state</param>
        [Obsolete("Goto(typeof(T)) is deprecated; use Goto<T>() instead.")]
        protected void Goto(Type s)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Goto while halted.");
            // If the state is not a state of the machine, then report an error and exit.
            this.Assert(StateTypeMap[this.GetType()].Any(val
                => val.DeclaringType.Equals(s.DeclaringType) &&
                val.Name.Equals(s.Name)), $"Machine '{this.Id}' " +
                $"is trying to transition to non-existing state '{s.Name}'.");
            this.Raise(new GotoStateEvent(s));
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action, pushing current state on the stack.
        /// </summary>
        /// <typeparam name="S">Type of the state</typeparam>
        protected void Push<S>() where S : MachineState
        {
#pragma warning disable 618
            Push(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action, pushing current state on the stack.
        /// Deprecated in favor of Push&lt;T&gt;().
        /// </summary>
        /// <param name="s">Type of the state</param>
        [Obsolete("Push(typeof(T)) is deprecated; use Push<T>() instead.")]
        protected void Push(Type s)
        {
            this.Assert(!this.Info.IsHalted, $"Machine '{this.Id}' invoked Push while halted.");
            // If the state is not a state of the machine, then report an error and exit.
            this.Assert(StateTypeMap[this.GetType()].Any(val
                => val.DeclaringType.Equals(s.DeclaringType) &&
                val.Name.Equals(s.Name)), $"Machine '{this.Id}' " +
                $"is trying to transition to non-existing state '{s.Name}'.");
            this.Raise(new PushStateEvent(s));
        }

        /// <summary>
        /// Pops the current <see cref="MachineState"/> from the state stack
        /// at the end of the current action.
        /// </summary>
        protected void Pop()
        {
            this.Runtime.NotifyPop(this);
            this.IsPopInvoked = true;
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        protected void Monitor<T>(Event e)
        {
            this.Monitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="e">Event</param>
        protected void Monitor(Type type, Event e)
        {
            // If the event is null, then report an error and exit.
            this.Assert(e != null, $"Machine '{this.Id}' is sending a null event.");
            this.Runtime.Monitor(type, this, e);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        protected bool Random()
        {
            return this.Runtime.GetNondeterministicBooleanChoice(this, 2);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate a number in the range [0..maxValue), where 0
        /// triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>Boolean</returns>
        protected bool Random(int maxValue)
        {
            return this.Runtime.GetNondeterministicBooleanChoice(this, maxValue);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerMemberName">CallerMemberName</param>
        /// <param name="callerFilePath">CallerFilePath</param>
        /// <param name="callerLineNumber">CallerLineNumber</param>
        /// <returns>Boolean</returns>
        protected bool FairRandom(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var havocId = string.Format("{0}_{1}_{2}_{3}_{4}", this.Id.Name, this.CurrentStateName,
                callerMemberName, callerFilePath, callerLineNumber);
            return this.Runtime.GetFairNondeterministicBooleanChoice(this, havocId);
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate an integer in the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>Integer</returns>
        protected int RandomInteger(int maxValue)
        {
            return this.Runtime.GetNondeterministicIntegerChoice(this, maxValue);
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

        #region inbox accessing

        /// <summary>
        /// Enqueues the specified <see cref="EventInfo"/>.
        /// </summary>
        /// <param name="eventInfo">The event metadata.</param>
        /// <param name="runNewHandler">Run a new handler</param>
        internal abstract void Enqueue(EventInfo eventInfo, ref bool runNewHandler);

        /// <summary>
        /// Dequeues the next available <see cref="EventInfo"/> from the
        /// inbox if there is one available, else returns null.
        /// </summary>
        /// <param name="checkOnly">Only check if event can get dequeued, do not modify inbox</param>
        /// <returns>EventInfo</returns>
        internal abstract EventInfo TryDequeueEvent(bool checkOnly = false);

        /// <summary>
        /// Returns the raised <see cref="EventInfo"/> if
        /// there is one available, else returns null.
        /// </summary>
        /// <returns>EventInfo</returns>
        private protected EventInfo TryGetRaisedEvent()
        {
            EventInfo raisedEventInfo = null;
            if (this.RaisedEvent != null)
            {
                raisedEventInfo = this.RaisedEvent;
                this.RaisedEvent = null;

                // Checks if the raised event is ignored.
                if (this.IsIgnored(raisedEventInfo.EventType))
                {
                    raisedEventInfo = null;
                }
            }

            return raisedEventInfo;
        }

        /// <summary>
        /// Returns the default <see cref="EventInfo"/>.
        /// </summary>
        /// <returns>EventInfo</returns>
        private protected EventInfo GetDefaultEvent()
        {
            this.Runtime.Logger.OnDefault(this.Id, this.CurrentStateName);
            return new EventInfo(new Default(), new EventOriginInfo(
                this.Id, this.GetType().Name, StateGroup.GetQualifiedStateName(this.CurrentState)));
        }

        #endregion

        #region event and action handling

        /// <summary>
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal virtual Task GotoStartStateAsync(Event e)
        {
            this.ReceivedEvent = e;
            return this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal abstract Task<bool> RunEventHandlerAsync();

        /// <summary>
        /// Handles the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="e">Event to handle</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private protected async Task HandleEvent(Event e)
        {
            this.Info.CurrentActionCalledTransitionStatement = false;
            var currentState = this.CurrentStateName;

            while (true)
            {
                if (this.CurrentState == null)
                {
                    // If the stack of states is empty and the event
                    // is halt, then terminate the machine.
                    if (e.GetType().Equals(typeof(Halt)))
                    {
                        await this.HaltMachineAsync();
                        return;
                    }

                    var unhandledEx = new UnhandledEventException(this.Id, currentState, e, "Unhandled Event");
                    if (OnUnhandledEventExceptionHandler("HandleEvent", unhandledEx))
                    {
                        await this.HaltMachineAsync();
                        return;
                    }
                    else
                    {
                        // If the event cannot be handled then report an error and exit.
                        this.Assert(false, $"Machine '{this.Id}' received event " +
                            $"'{e.GetType().FullName}' that cannot be handled.");
                    }
                }

                // Checks if the event is a goto state event.
                if (e.GetType() == typeof(GotoStateEvent))
                {
                    Type targetState = (e as GotoStateEvent).State;
                    await this.GotoState(targetState, null);
                }
                // Checks if the event is a push state event.
                else if (e.GetType() == typeof(PushStateEvent))
                {
                    Type targetState = (e as PushStateEvent).State;
                    await this.PushState(targetState);
                }
                // Checks if the event can trigger a goto state transition.
                else if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    var transition = this.GotoTransitions[e.GetType()];
                    await this.GotoState(transition.TargetState, transition.Lambda);
                }
                else if (this.GotoTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    var transition = this.GotoTransitions[typeof(WildCardEvent)];
                    await this.GotoState(transition.TargetState, transition.Lambda);
                }
                // Checks if the event can trigger a push state transition.
                else if (this.PushTransitions.ContainsKey(e.GetType()))
                {
                    Type targetState = this.PushTransitions[e.GetType()].TargetState;
                    await this.PushState(targetState);
                }
                else if (this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    Type targetState = this.PushTransitions[typeof(WildCardEvent)].TargetState;
                    await this.PushState(targetState);
                }
                // Checks if the event can trigger an action.
                else if (this.CurrentActionHandlerMap.ContainsKey(e.GetType()) &&
                    this.CurrentActionHandlerMap[e.GetType()] is ActionBinding)
                {
                    var handler = this.CurrentActionHandlerMap[e.GetType()] as ActionBinding;
                    await this.Do(handler.Name);
                }
                else if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent))
                    && this.CurrentActionHandlerMap[typeof(WildCardEvent)] is ActionBinding)
                {
                    var handler = this.CurrentActionHandlerMap[typeof(WildCardEvent)] as ActionBinding;
                    await this.Do(handler.Name);
                }
                // If the current state cannot handle the event.
                else
                {
                    // The machine performs the on exit action of the current state.
                    await this.ExecuteCurrentStateOnExit(null);
                    if (this.Info.IsHalted)
                    {
                        return;
                    }

                    await this.DoStatePopAsync();
                    this.Runtime.Logger.OnPopUnhandledEvent(this.Id, this.CurrentStateName, e.GetType().FullName);
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Invokes an action.
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private async Task Do(string actionName)
        {
            var cachedAction = this.ActionMap[actionName];
            this.Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, this.ReceivedEvent);
            await this.ExecuteAction(cachedAction);
            this.Runtime.NotifyCompletedAction(this, cachedAction.MethodInfo, this.ReceivedEvent);

            if (this.IsPopInvoked)
            {
                // Performs the state transition, if pop was invoked during the action.
                await this.PopState();
            }
        }

        /// <summary>
        /// Executes the on entry function of the current state.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private async Task ExecuteCurrentStateOnEntry()
        {
            this.Runtime.NotifyEnteredState(this);

            CachedAction entryAction = null;
            if (this.StateStack.Peek().EntryAction != null)
            {
                entryAction = this.ActionMap[this.StateStack.Peek().EntryAction];
            }

            // Invokes the entry action of the new state,
            // if there is one available.
            if (entryAction != null)
            {
                this.Runtime.NotifyInvokedAction(this, entryAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(entryAction);
                this.Runtime.NotifyCompletedAction(this, entryAction.MethodInfo, this.ReceivedEvent);
            }

            if (this.IsPopInvoked)
            {
                // Performs the state transition, if pop was invoked during the action.
                await this.PopState();
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        /// <param name="eventHandlerExitActionName">Action name</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private async Task ExecuteCurrentStateOnExit(string eventHandlerExitActionName)
        {
            this.Runtime.NotifyExitedState(this);

            CachedAction exitAction = null;
            if (this.StateStack.Peek().ExitAction != null)
            {
                exitAction = this.ActionMap[this.StateStack.Peek().ExitAction];
            }

            this.Info.IsInsideOnExit = true;

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                this.Runtime.NotifyInvokedAction(this, exitAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(exitAction);
                this.Runtime.NotifyCompletedAction(this, exitAction.MethodInfo, this.ReceivedEvent);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null)
            {
                CachedAction eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                this.Runtime.NotifyInvokedAction(this, eventHandlerExitAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(eventHandlerExitAction);
                this.Runtime.NotifyCompletedAction(this, eventHandlerExitAction.MethodInfo, this.ReceivedEvent);
            }

            this.Info.IsInsideOnExit = false;
        }

        /// <summary>
        /// An exception filter that calls OnFailure, which can choose to fast-fail the app
        /// to get a full dump.
        /// </summary>
        /// <param name="ex">The exception being tested</param>
        /// <param name="action">The machine action being executed when the failure occurred</param>
        /// <returns></returns>
        private bool InvokeOnFailureExceptionFilter(CachedAction action, Exception ex)
        {
            // This is called within the exception filter so the stack has not yet been unwound.
            // If OnFailure does not fail-fast, return false to process the exception normally.
            this.Runtime.RaiseOnFailureEvent(new MachineActionExceptionFilterException(action.MethodInfo.Name, ex));
            return false;
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <param name="cachedAction">The cached methodInfo and corresponding delegate</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private async Task ExecuteAction(CachedAction cachedAction)
        {
            try
            {
                if (cachedAction.IsAsync)
                {
                    try
                    {
                        // We have no reliable stack for awaited operations.
                        await cachedAction.ExecuteAsync();
                    }
                    catch (Exception ex) when (OnExceptionHandler(cachedAction.MethodInfo.Name, ex))
                    {
                        // user handled the exception, return normally
                    }
                }
                else
                {
                    // Use an exception filter to call OnFailure before the stack has been unwound.
                    try
                    {
                        cachedAction.Execute();
                    }
                    catch (Exception ex) when (OnExceptionHandler(cachedAction.MethodInfo.Name, ex))
                    {
                        // user handled the exception, return normally
                    }
                    catch (Exception ex) when (!OnExceptionRequestedGracefulHalt && InvokeOnFailureExceptionFilter(cachedAction, ex))
                    {
                        // If InvokeOnFailureExceptionFilter does not fail-fast, it returns
                        // false to process the exception normally.
                    }
                }
            }
            catch (Exception ex)
            {
                Exception innerException = ex;
                while (innerException is TargetInvocationException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is AggregateException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is ExecutionCanceledException)
                {
                    this.Info.IsHalted = true;
                    IO.Debug.WriteLine("<Exception> ExecutionCanceledException was " +
                        $"thrown from Machine '{this.Id}'.");
                }
                else if (innerException is TaskSchedulerException)
                {
                    this.Info.IsHalted = true;
                    IO.Debug.WriteLine("<Exception> TaskSchedulerException was " +
                        $"thrown from Machine '{this.Id}'.");
                }
                else if (OnExceptionRequestedGracefulHalt)
                {
                    // Gracefully halt.
                    await this.HaltMachineAsync();
                }
                else
                {
                    // Reports the unhandled exception.
                    this.ReportUnhandledException(innerException, cachedAction.MethodInfo.Name);
                }
            }
        }

        /// <summary>
        /// Performs a goto transition to the specified state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <param name="onExitActionName">Action name</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private async Task GotoState(Type s, string onExitActionName)
        {
            this.Logger.OnGoto(this.Id, this.CurrentStateName,
                $"{s.DeclaringType}.{StateGroup.GetQualifiedStateName(s)}");

            // The machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExit(onExitActionName);
            if (this.Info.IsHalted)
            {
                return;
            }

            await this.DoStatePopAsync();

            var nextState = StateMap[this.GetType()].First(val
                => val.GetType().Equals(s));

            // The machine transitions to the new state.
            await this.DoStatePushAsync(nextState);

            // The machine performs the on entry action of the new state.
            await this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a push transition to the specified state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private async Task PushState(Type s)
        {
            this.Runtime.Logger.OnPush(this.Id, this.CurrentStateName, s.FullName);

            var nextState = StateMap[this.GetType()].First(val => val.GetType().Equals(s));
            await this.DoStatePushAsync(nextState);

            // The machine performs the on entry statements of the new state.
            await this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a pop transition from the current state.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private async Task PopState()
        {
            this.IsPopInvoked = false;
            var prevStateName = this.CurrentStateName;

            // The machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExit(null);
            if (this.Info.IsHalted)
            {
                return;
            }

            await this.DoStatePopAsync();
            this.Runtime.Logger.OnPop(this.Id, prevStateName, this.CurrentStateName);

            // Watch out for an extra pop.
            this.Assert(this.CurrentState != null, $"Machine '{this.Id}' popped with no matching push.");
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is pushed on to the stack.
        /// </summary>
        /// <param name="state">State that is to be pushed on to the top of the stack</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private protected virtual Task DoStatePushAsync(MachineState state)
        {
            this.GotoTransitions = state.GotoTransitions;
            this.PushTransitions = state.PushTransitions;

            // Gets existing map for actions.
            var eventHandlerMap = this.CurrentActionHandlerMap == null ?
                new Dictionary<Type, EventActionHandler>() :
                new Dictionary<Type, EventActionHandler>(this.CurrentActionHandlerMap);

            // Updates the map with defer annotations.
            foreach (var deferredEvent in state.DeferredEvents)
            {
                if (deferredEvent.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[deferredEvent] = new DeferAction();
                    break;
                }

                eventHandlerMap[deferredEvent] = new DeferAction();
            }

            // Updates the map with actions.
            foreach (var actionBinding in state.ActionBindings)
            {
                if (actionBinding.Key.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[actionBinding.Key] = actionBinding.Value;
                    break;
                }

                eventHandlerMap[actionBinding.Key] = actionBinding.Value;
            }

            // Updates the map with ignores.
            foreach (var ignoreEvent in state.IgnoredEvents)
            {
                if (ignoreEvent.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[ignoreEvent] = new IgnoreAction();
                    break;
                }

                eventHandlerMap[ignoreEvent] = new IgnoreAction();
            }

            // Removes the ones on which transitions are defined.
            foreach (var eventType in this.GotoTransitions.Keys.Union(this.PushTransitions.Keys))
            {
                if (eventType.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    break;
                }

                eventHandlerMap.Remove(eventType);
            }

            this.StateStack.Push(state);
            this.ActionHandlerStack.Push(eventHandlerMap);
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is popped.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        private protected virtual Task DoStatePopAsync()
        {
            this.StateStack.Pop();
            this.ActionHandlerStack.Pop();
            if (this.StateStack.Count > 0)
            {
                this.GotoTransitions = this.StateStack.Peek().GotoTransitions;
                this.PushTransitions = this.StateStack.Peek().PushTransitions;
            }
            else
            {
                this.GotoTransitions = null;
                this.PushTransitions = null;
            }
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Checks if the machine ignores the specified event.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean</returns>
        private protected bool IsIgnored(Type e)
        {
            // If a transition is defined, then the event is not ignored.
            if (this.GotoTransitions.ContainsKey(e) || this.PushTransitions.ContainsKey(e) ||
                this.GotoTransitions.ContainsKey(typeof(WildCardEvent)) ||
                this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
            {
                return false;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(e))
            {
                return this.CurrentActionHandlerMap[e] is IgnoreAction;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentActionHandlerMap[typeof(WildCardEvent)] is IgnoreAction)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the machine defers the specified event.
        /// </summary>
        /// <param name="e">Event type</param>
        /// <returns>Boolean</returns>
        private protected bool IsDeferred(Type e)
        {
            // if transition is defined, then no
            if (this.GotoTransitions.ContainsKey(e) || this.PushTransitions.ContainsKey(e) ||
                this.GotoTransitions.ContainsKey(typeof(WildCardEvent)) ||
                this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
            {
                return false;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(e))
            {
                return this.CurrentActionHandlerMap[e] is DeferAction;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentActionHandlerMap[typeof(WildCardEvent)] is DeferAction)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the machine has a default handler.
        /// </summary>
        /// <returns></returns>
        private protected bool HasDefaultHandler()
        {
            return this.CurrentActionHandlerMap.ContainsKey(typeof(Default)) ||
                this.GotoTransitions.ContainsKey(typeof(Default)) ||
                this.PushTransitions.ContainsKey(typeof(Default));
        }

        #endregion

        #region state caching

        /// <summary>
        /// Returns the cached state of this machine.
        /// </summary>
        /// <returns>Hash value</returns>
        internal virtual int GetCachedState()
        {
            unchecked
            {
                var hash = 19;

                hash = hash * 31 + this.GetType().GetHashCode();
                hash = hash * 31 + this.Id.Value.GetHashCode();
                hash = hash * 31 + this.IsRunning.GetHashCode();

                hash = hash * 31 + this.Info.IsHalted.GetHashCode();
                hash = hash * 31 + this.Info.ProgramCounter;

                if (this.Runtime.Configuration.EnableUserDefinedStateHashing)
                {
                    // Adds the user-defined hashed machine state.
                    hash = hash * 31 + HashedState;
                }

                return hash;
            }
        }

        #endregion

        #region utilities

        /// <summary>
        /// Returns the type of the state at the specified state
        /// stack index, if there is one.
        /// </summary>
        /// <param name="index">State stack index</param>
        /// <returns>Type</returns>
        internal Type GetStateTypeAtStackIndex(int index)
        {
            return this.StateStack.ElementAtOrDefault(index)?.GetType();
        }

        /// <summary>
        /// Processes a type, looking for machine states.
        /// </summary>
        /// <param name="type">Type</param>
        private void ExtractStateTypes(Type type)
        {
            Stack<Type> stack = new Stack<Type>();
            stack.Push(type);

            while (stack.Count > 0)
            {
                Type nextType = stack.Pop();

                if (nextType.IsClass && nextType.IsSubclassOf(typeof(MachineState)))
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
                            t.IsSubclassOf(typeof(MachineState)), $"'{t.Name}' " +
                            $"is neither a group of states nor a state.");
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <returns>MethodInfo</returns>
        private MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo method = null;
            Type machineType = this.GetType();

            do
            {
                method = machineType.GetMethod(actionName,
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    Type.DefaultBinder, new Type[0], null);
                machineType = machineType.BaseType;
            }
            while (method == null && machineType != this.GetMachineType());

            this.Assert(method != null, "Cannot detect action declaration '{0}' " +
                "in machine '{1}'.", actionName, this.GetType().Name);
            this.Assert(method.GetParameters().Length == 0, "Action '{0}' in machine " +
                "'{1}' must have 0 formal parameters.", method.Name, this.GetType().Name);

            if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            {
                this.Assert(method.ReturnType == typeof(Task), "Async action '{0}' in machine " +
                    "'{1}' must have 'Task' return type.", method.Name, this.GetType().Name);
            }
            else
            {
                this.Assert(method.ReturnType == typeof(void), "Action '{0}' in machine " +
                    "'{1}' must have 'void' return type.", method.Name, this.GetType().Name);
            }

            return method;
        }

        /// <summary>
        /// Returns the base machine type of the user machine.
        /// </summary>
        /// <returns>The machine type.</returns>
        private protected abstract Type GetMachineType();

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            BaseMachine m = obj as BaseMachine;
            if (m == null ||
                this.GetType() != m.GetType())
            {
                return false;
            }

            return this.Id.Equals(m.Id);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.Id.Name;
        }

        #endregion

        #region code coverage methods

        /// <summary>
        /// Returns the set of all states in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the machine</returns>
        internal HashSet<string> GetAllStates()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()),
                $"Machine '{this.Id}' hasn't populated its states yet.");

            var allStates = new HashSet<string>();
            foreach (var state in StateMap[this.GetType()])
            {
                allStates.Add(StateGroup.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all (states, registered event) pairs in the machine</returns>
        internal HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()),
                $"Machine '{this.Id}' hasn't populated its states yet.");

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

                foreach (var pushtransition in state.PushTransitions)
                {
                    pairs.Add(Tuple.Create(StateGroup.GetQualifiedStateName(state.GetType()), pushtransition.Key.Name));
                }
            }

            return pairs;
        }

        #endregion

        #region error checking

        /// <summary>
        /// Check machine for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(StateTypeMap[this.GetType()].Count > 0, $"Machine '{this.Id}' " +
                "must have one or more states.");
            this.Assert(this.StateStack.Peek() != null, $"Machine '{this.Id}' " +
                "must not have a null current state.");
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
                $"in machine '{this.Id}', state '{state}', action '{actionName}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        /// <summary>
        /// Invokes user callback when a machine receives an event it cannot handle
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if the machine should gracefully halt</returns>
        private bool OnUnhandledEventExceptionHandler(string methodName, UnhandledEventException ex)
        {
            this.Logger.OnMachineExceptionThrown(this.Id, ex.CurrentStateName, methodName, ex);

            var ret = OnException(methodName, ex);
            OnExceptionRequestedGracefulHalt = false;
            switch (ret)
            {
                case OnExceptionOutcome.HaltMachine:
                case OnExceptionOutcome.HandledException:
                    this.Logger.OnMachineExceptionHandled(this.Id, ex.CurrentStateName, methodName, ex);
                    OnExceptionRequestedGracefulHalt = true;
                    return true;
                case OnExceptionOutcome.ThrowException:
                    return false;
            }
            return false;
        }

        /// <summary>
        /// Invokes user callback when a machine throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if it was handled in this method</returns>
        private bool OnExceptionHandler(string methodName, Exception ex)
        {
            if (ex is ExecutionCanceledException)
            {
                // Internal exception, used by the testing infrastructure.
                return false;
            }

            this.Logger.OnMachineExceptionThrown(this.Id, CurrentStateName, methodName, ex);

            var ret = OnException(methodName, ex);
            OnExceptionRequestedGracefulHalt = false;

            switch (ret)
            {
                case OnExceptionOutcome.ThrowException:
                    return false;
                case OnExceptionOutcome.HandledException:
                    this.Logger.OnMachineExceptionHandled(this.Id, CurrentStateName, methodName, ex);
                    return true;
                case OnExceptionOutcome.HaltMachine:
                    OnExceptionRequestedGracefulHalt = true;
                    return false;
            }

            return false;
        }

        /// <summary>
        /// User callback when a machine throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>The action that the runtime should take</returns>
        protected virtual OnExceptionOutcome OnException(string methodName, Exception ex)
        {
            return OnExceptionOutcome.ThrowException;
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
            MachineActionMap.Clear();
        }

        /// <summary>
        /// Halts the machine.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns> 
        private protected virtual Task HaltMachineAsync()
        {
            // Invokes user callback outside the lock.
            this.OnHalt();
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// User callback when a machine halts.
        /// </summary>
        protected virtual void OnHalt() { }

        #endregion
    }
}
