// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
    public abstract class BaseMachine// : IMachine
    {
        #region static fields

        /// <summary>
        /// Is the machine state cached yet?
        /// </summary>
        private static ConcurrentDictionary<Type, bool> MachineStateCached;

        /// <summary>
        /// Map from machine types to a set of all possible states types.
        /// </summary>
        private protected static ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap;

        /// <summary>
        /// Map from machine types to a set of all possible states.
        /// </summary>
        private static ConcurrentDictionary<Type, HashSet<MachineState>> StateMap;

        /// <summary>
        /// Map from machine types to a set of all possible actions.
        /// </summary>
        private static ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MachineActionMap;

        #endregion

        /// <summary>
        /// Stores machine-related information, which can used
        /// for scheduling and testing.
        /// </summary>
        internal MachineInfo Info { get; private set; }

        /// <summary>
        /// The unique name of this machine.
        /// </summary>
        private protected string Name { get; private set; }

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
        private protected EventInfo RaisedEvent;

        /// <summary>
        /// True if the machine is active, else false. The machine is active if it
        /// is able to perform a next action (e.g. an event was enqueued). If the
        /// event handler cannot dequeue an event, then it assigns this to false.
        /// </summary>
        private protected bool IsActive;

        /// <summary>
        /// Is pop invoked in the current action.
        /// </summary>
        private protected bool IsPopInvoked;

        /// <summary>
        /// User OnException asked for the machine to be gracefully halted
        /// (suppressing the exception).
        /// </summary>
        private bool OnExceptionRequestedGracefulHalt;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        private protected Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current push state transitions.
        /// </summary>
        private protected Dictionary<Type, PushStateTransition> PushTransitions;

        /// <summary>
        /// All possible machine states of this machine.
        /// </summary>
        private protected IEnumerable<MachineState> MachineStates { get; private set; }

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
        private protected string CurrentStateName => this.CurrentState == null ? string.Empty
            : $"{this.CurrentState.DeclaringType}.{StateGroup.GetQualifiedStateName(this.CurrentState)}";

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
                return this.Info.OperationGroupId;
            }
            set
            {
                this.Info.OperationGroupId = value;
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

            this.IsActive = true;
            this.IsPopInvoked = false;
            this.OnExceptionRequestedGracefulHalt = false;
        }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        /// <param name="info">The metadata of this machine.</param>
        /// <param name="name">The unique name of this machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal Task InitializeAsync(MachineInfo info, string name)
        {
            this.Info = info;
            this.Name = name;
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
                            this.CheckProperty(false, $"{this.Name} {ex.Message} in state '{state}'.");
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

            this.MachineStates = StateMap[machineType];

            var initialStates = this.MachineStates.Where(state => state.IsStart).ToList();
            this.CheckProperty(initialStates.Count != 0, $"{this.Name} must declare a start state.");
            this.CheckProperty(initialStates.Count == 1, $"{this.Name} can not declare more than one start states.");

            await this.DoStatePushAsync(initialStates.Single());

            this.CheckStateValidity();
        }

        #endregion

        #region inbox accessing

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

                    var unhandledEx = new UnhandledEventException(currentState, e, "Unhandled Event");
                    if (this.OnUnhandledEventExceptionHandler("HandleEvent", unhandledEx))
                    {
                        await this.HaltMachineAsync();
                        return;
                    }
                    else
                    {
                        // If the event cannot be handled then report an error and exit.
                        this.CheckProperty(false, $"{this.Name} received event '{e.GetType().FullName}' that cannot be handled.");
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
                    this.NotifyPopUnhandledEvent(this.CurrentStateName, e.GetType().FullName);
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
            this.NotifyInvokedAction(cachedAction.MethodInfo, this.ReceivedEvent);
            await this.ExecuteAction(cachedAction);
            this.NotifyCompletedAction(cachedAction.MethodInfo, this.ReceivedEvent);

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
            this.NotifyEnteredState();

            CachedAction entryAction = null;
            if (this.StateStack.Peek().EntryAction != null)
            {
                entryAction = this.ActionMap[this.StateStack.Peek().EntryAction];
            }

            // Invokes the entry action of the new state,
            // if there is one available.
            if (entryAction != null)
            {
                this.NotifyInvokedAction(entryAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(entryAction);
                this.NotifyCompletedAction(entryAction.MethodInfo, this.ReceivedEvent);
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
            this.NotifyExitedState();

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
                this.NotifyInvokedAction(exitAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(exitAction);
                this.NotifyCompletedAction(exitAction.MethodInfo, this.ReceivedEvent);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null)
            {
                CachedAction eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                this.NotifyInvokedAction(eventHandlerExitAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(eventHandlerExitAction);
                this.NotifyCompletedAction(eventHandlerExitAction.MethodInfo, this.ReceivedEvent);
            }

            this.Info.IsInsideOnExit = false;
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
                    catch (Exception ex) when (this.OnExceptionHandler(cachedAction.MethodInfo.Name, ex))
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
                    catch (Exception ex) when (this.OnExceptionHandler(cachedAction.MethodInfo.Name, ex))
                    {
                        // user handled the exception, return normally
                    }
                    catch (Exception ex) when (!this.OnExceptionRequestedGracefulHalt && this.InvokeOnFailureExceptionFilter(cachedAction, ex))
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
                    Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from {this.Name}.");
                }
                else if (innerException is TaskSchedulerException)
                {
                    this.Info.IsHalted = true;
                    Debug.WriteLine($"<Exception> TaskSchedulerException was thrown from {this.Name}.");
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
            this.NotifyGotoState(this.CurrentStateName, $"{s.DeclaringType}.{StateGroup.GetQualifiedStateName(s)}");

            // The machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExit(onExitActionName);
            if (this.Info.IsHalted)
            {
                return;
            }

            await this.DoStatePopAsync();

            var nextState = this.MachineStates.First(val => val.GetType().Equals(s));

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
            this.NotifyPushState(this.CurrentStateName, s.FullName);

            var nextState = this.MachineStates.First(val => val.GetType().Equals(s));
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
            this.NotifyPopState(prevStateName, this.CurrentStateName);

            // Watch out for an extra pop.
            this.CheckProperty(this.CurrentState != null, $"{this.Name} popped with no matching push.");
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
        internal abstract int GetCachedState();

        #endregion

        #region code coverage methods

        /// <summary>
        /// Returns the set of all states in the machine (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the machine</returns>
        private protected HashSet<string> GetAllStates()
        {
            this.CheckProperty(this.MachineStates != null, "{0} hasn't populated its states yet.", this.Name);

            var allStates = new HashSet<string>();
            foreach (var state in this.MachineStates)
            {
                allStates.Add(StateGroup.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (state, registered event) pairs in the machine (for code coverage).
        /// </summary>
        /// <returns>Set of all (state, registered event) pairs in the machine</returns>
        private protected HashSet<(string state, string e)> GetAllStateEventPairs()
        {
            this.CheckProperty(this.MachineStates != null, "{0} hasn't populated its states yet.", this.Name);

            var pairs = new HashSet<(string state, string e)>();
            foreach (var state in this.MachineStates)
            {
                foreach (var binding in state.ActionBindings)
                {
                    pairs.Add((state: StateGroup.GetQualifiedStateName(state.GetType()), e: binding.Key.Name));
                }

                foreach (var transition in state.GotoTransitions)
                {
                    pairs.Add((state: StateGroup.GetQualifiedStateName(state.GetType()), e: transition.Key.Name));
                }

                foreach (var pushtransition in state.PushTransitions)
                {
                    pairs.Add((state: StateGroup.GetQualifiedStateName(state.GetType()), e: pushtransition.Key.Name));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Returns the current state transition. Used for code coverage.
        /// </summary>
        /// <param name="eventInfo">The metadata of the event that caused the current transition.</param>
        private protected (string machine, string originState, string destState, string edgeLabel) GetCurrentStateTransition(EventInfo eventInfo)
        {
            string originState = StateGroup.GetQualifiedStateName(this.CurrentState);
            string edgeLabel = string.Empty;
            string destState = string.Empty;
            if (eventInfo.Event is GotoStateEvent)
            {
                edgeLabel = "goto";
                destState = StateGroup.GetQualifiedStateName((eventInfo.Event as GotoStateEvent).State);
            }
            else if (eventInfo.Event is PushStateEvent)
            {
                edgeLabel = "push";
                destState = StateGroup.GetQualifiedStateName((eventInfo.Event as PushStateEvent).State);
            }
            else if (this.GotoTransitions.ContainsKey(eventInfo.EventType))
            {
                edgeLabel = eventInfo.EventType.Name;
                destState = StateGroup.GetQualifiedStateName(
                    this.GotoTransitions[eventInfo.EventType].TargetState);
            }
            else if (this.PushTransitions.ContainsKey(eventInfo.EventType))
            {
                edgeLabel = eventInfo.EventType.Name;
                destState = StateGroup.GetQualifiedStateName(
                    this.PushTransitions[eventInfo.EventType].TargetState);
            }
            else
            {
                return (string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return (this.GetType().Name, originState, edgeLabel, destState);
        }

        #endregion

        #region error checking and exceptions

        /// <summary>
        /// Checks if the specified property holds.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        private protected abstract void CheckProperty(bool predicate);

        /// <summary>
        /// Checks if the specified property holds.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        private protected abstract void CheckProperty(bool predicate, string s, params object[] args);

        /// <summary>
        /// Check machine for state related errors.
        /// </summary>
        private void CheckStateValidity()
        {
            this.CheckProperty(StateTypeMap[this.GetType()].Count > 0, $"{this.Name} must have one or more states.");
            this.CheckProperty(this.StateStack.Peek() != null, $"{this.Name} must not have a null current state.");
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="actionName">Action name</param>
        private protected abstract void ReportUnhandledException(Exception ex, string actionName);

        /// <summary>
        /// An exception filter that calls OnFailure, which can choose to fast-fail the app
        /// to get a full dump.
        /// </summary>
        /// <param name="ex">The exception being tested</param>
        /// <param name="action">The machine action being executed when the failure occurred</param>
        /// <returns></returns>
        private protected abstract bool InvokeOnFailureExceptionFilter(CachedAction action, Exception ex);

        /// <summary>
        /// Invokes user callback when the machine receives an event it cannot handle.
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if the machine should gracefully halt</returns>
        private bool OnUnhandledEventExceptionHandler(string methodName, UnhandledEventException ex)
        {
            this.NotifyMachineExceptionThrown(ex.CurrentStateName, methodName, ex);

            var ret = OnException(methodName, ex);
            this.OnExceptionRequestedGracefulHalt = false;
            switch (ret)
            {
                case OnExceptionOutcome.HaltMachine:
                case OnExceptionOutcome.HandledException:
                    this.NotifyMachineExceptionHandled(ex.CurrentStateName, methodName, ex);
                    this.OnExceptionRequestedGracefulHalt = true;
                    return true;
                case OnExceptionOutcome.ThrowException:
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Invokes user callback when the machine throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if it was handled in this method</returns>
        private bool OnExceptionHandler(string methodName, Exception ex)
        {
            if (ex is ExecutionCanceledException || ex.InnerException is ExecutionCanceledException)
            {
                // Internal exception, used by the testing infrastructure.
                return false;
            }

            this.NotifyMachineExceptionThrown(this.CurrentStateName, methodName, ex);

            var ret = OnException(methodName, ex);
            this.OnExceptionRequestedGracefulHalt = false;
            switch (ret)
            {
                case OnExceptionOutcome.ThrowException:
                    return false;
                case OnExceptionOutcome.HandledException:
                    this.NotifyMachineExceptionHandled(this.CurrentStateName, methodName, ex);
                    return true;
                case OnExceptionOutcome.HaltMachine:
                    this.OnExceptionRequestedGracefulHalt = true;
                    return false;
            }

            return false;
        }

        /// <summary>
        /// User callback when the machine throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>The action that the runtime should take</returns>
        protected virtual OnExceptionOutcome OnException(string methodName, Exception ex)
        {
            return OnExceptionOutcome.ThrowException;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that the machine entered a state.
        /// </summary>
        private protected abstract void NotifyEnteredState();

        /// <summary>
        /// Notifies that the machine exited a state.
        /// </summary>
        private protected abstract void NotifyExitedState();

        /// <summary>
        /// Notifies that the machine is performing a 'goto' transition to the specified state.
        /// </summary>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="newStateName">The target state.</param>
        private protected abstract void NotifyGotoState(string currStateName, string newStateName);

        /// <summary>
        /// Notifies that the machine is performing a 'push' transition to the specified state.
        /// </summary>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="newStateName">The target state.</param>
        private protected abstract void NotifyPushState(string currStateName, string newStateName);

        /// <summary>
        /// Notifies that the machine is performing a 'pop' transition from the current state.
        /// </summary>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="restoredStateName">The name of the state being restored, if any.</param>
        private protected abstract void NotifyPopState(string currStateName, string restoredStateName);

        /// <summary>
        /// Notifies that the machine popped its state because it cannot handle the current event.
        /// </summary>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        private protected abstract void NotifyPopUnhandledEvent(string currStateName, string eventName);

        /// <summary>
        /// Notifies that the machine invoked an action.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        private protected abstract void NotifyInvokedAction(MethodInfo action, Event receivedEvent);

        /// <summary>
        /// Notifies that the machine completed an action.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        private protected abstract void NotifyCompletedAction(MethodInfo action, Event receivedEvent);

        /// <summary>
        /// Notifies that the machine is throwing an exception.
        /// </summary>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="ex">The exception.</param>
        private protected abstract void NotifyMachineExceptionThrown(string currStateName, string actionName, Exception ex);

        /// <summary>
        /// Notifies that the machine is using 'OnException' to handle a thrown exception.
        /// </summary>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        private protected abstract void NotifyMachineExceptionHandled(string currStateName, string actionName, Exception ex);

        #endregion

        #region utilities

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
                        this.CheckProperty(t.IsSubclassOf(typeof(StateGroup)) ||
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

            this.CheckProperty(method != null, "Cannot detect action declaration '{0}' " +
                "in machine '{1}'.", actionName, this.GetType().Name);
            this.CheckProperty(method.GetParameters().Length == 0, "Action '{0}' in machine " +
                "'{1}' must have 0 formal parameters.", method.Name, this.GetType().Name);

            if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            {
                this.CheckProperty(method.ReturnType == typeof(Task), "Async action '{0}' in machine " +
                    "'{1}' must have 'Task' return type.", method.Name, this.GetType().Name);
            }
            else
            {
                this.CheckProperty(method.ReturnType == typeof(void), "Action '{0}' in machine " +
                    "'{1}' must have 'void' return type.", method.Name, this.GetType().Name);
            }

            return method;
        }

        /// <summary>
        /// Returns the base machine type of the user machine.
        /// </summary>
        /// <returns>The machine type.</returns>
        private protected abstract Type GetMachineType();

        #endregion

        #region cleanup

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
        private protected abstract Task HaltMachineAsync();

        /// <summary>
        /// User callback when the machine halts.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        protected virtual Task OnHaltAsync()
        {
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        #endregion
    }
}
