using Microsoft.PSharp;

namespace Core.Utilities.Profiling
{
    /// <summary>
    /// Interface to the P# critical path profiler
    /// </summary>
    public interface ICriticalPathProfiler
    {
        /// <summary>
        /// The configuration that says whether profiling is enabled
        /// </summary>
        Configuration Configuration { get; set; }

        /// <summary>
        /// Start our profiler. To be invoked immediately after runtime creation and
        /// profiler installation on the runtime
        /// </summary>
        void StartCriticalPathProfiling();

        /// <summary>
        /// Stop the profiler
        /// </summary>
        void StopCriticalPathProfiling();

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="machine">The machine that the event is being dequeued by.</param>
        /// <param name="eventSequenceNumber">the global sequence number associated with the 
        /// send corresponding to this dequeue.</param>
        void OnDequeue(Machine machine, long eventSequenceNumber);

        ///// <summary>
        ///// Called when the default event handler for a state is about to be executed.
        ///// </summary>
        ///// <param name="machineId">Id of the machine that the state will execute in.</param>
        ///// <param name="currentStateName">Name of the current state of the machine.</param>
        //void OnDefault(MachineId machineId, string currentStateName);

        ///// <summary>
        ///// Called when a machine is being pushed to a state.
        ///// </summary>
        ///// <param name="machineId">Id of the machine being pushed to the state.</param>
        ///// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        ///// <param name="newStateName">The state the machine is pushed to.</param>
        //void OnPush(MachineId machineId, string currentStateName, string newStateName);

        ///// <summary>
        ///// Called when a machine has been popped from a state.
        ///// </summary>
        ///// <param name="machineId">Id of the machine that the pop executed in.</param>
        ///// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        ///// <param name="restoredStateName">The name of the state being restored, if any.</param>
        //void OnPop(MachineId machineId, string currentStateName, string restoredStateName);

        ///// <summary>
        ///// When an event cannot be handled in the current state, its exit handler is executed and then the state is
        ///// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        ///// </summary>
        ///// <param name="machineId">Id of the machine that the pop executed in.</param>
        ///// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>
        /////     (which is being re-entered), if any.</param>
        ///// <param name="eventName">The name of the event that cannot be handled.</param>
        //void OnPopUnhandledEvent(MachineId machineId, string currentStateName, string eventName);

        /// <summary>
        /// Called when a machine begins waiting to receive an event.
        /// </summary>
        /// <param name="machine">Id of the machine that is entering the wait state.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machine"/>, if any.</param>
        /// <param name="eventNames">The names of the specific events being waited for, if any.</param>
        void OnReceiveBegin(Machine machine, string currentStateName, string eventNames);

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="machine">The machine that received the event.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machine"/>, if any.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        /// <param name="eventSequenceNumber">the sequence number of the send corresponding to this event.</param>
        void OnReceiveEnd(Machine machine, string currentStateName, string eventName, bool wasBlocked, long eventSequenceNumber);

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="source">A reference to the source machine.</param>
        /// <param name="eventSequenceNumber">The global sequence number of this send.</param>
        void OnSend(Machine source, long eventSequenceNumber);

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="parent">The creator machine.</param>
        /// <param name="child">The machine that has been created.</param>
        void OnCreateMachine(Machine parent, Machine child);

        ///// <summary>
        ///// Called when a monitor has been created.
        ///// </summary>
        ///// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        ///// <param name="monitorId">The id of the monitor that has been created.</param>
        //void OnCreateMonitor(string monitorTypeName, MachineId monitorId);

        /// <summary>
        /// Called when a machine has been halted.
        /// </summary>
        /// <param name="machine">The machine that has been halted.</param>
        void OnHalt(Machine machine);

        ///// <summary>
        ///// Called when a machine enters or exits a state.
        ///// </summary>
        ///// <param name="machineId">The id of the machine entering or exiting the state.</param>
        ///// <param name="stateName">The name of the state being entered or exited.</param>
        ///// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        //void OnMachineState(MachineId machineId, string stateName, bool isEntry);

        ///// <summary>
        ///// Called when a machine raises an event.
        ///// </summary>
        ///// <param name="machineId">The id of the machine raising the event.</param>
        ///// <param name="currentStateName">The name of the current state of the machine raising the event.</param>
        ///// <param name="eventName">The name of the event being raised.</param>
        //void OnMachineEvent(MachineId machineId, string currentStateName, string eventName);

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="machine">The machine executing the action.</param>
        /// <param name="currentStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnActionEnter(Machine machine, string currentStateName, string actionName);

        /// <summary>
        /// Called when a machine finishes an action.
        /// </summary>
        /// <param name="machine">The machine executing the action.</param>
        /// <param name="currentStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnActionExit(Machine machine, string currentStateName, string actionName);

        ///// <summary>
        ///// Called when a monitor enters or exits a state.
        ///// </summary>
        ///// <param name="monitorTypeName">The name of the monitor entering or exiting the state.</param>
        ///// <param name="monitorId">The id of the monitor entering or exiting the state.</param>
        ///// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /////     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        ///// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        ///// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /////     else no liveness state is available.</param>
        //void OnMonitorState(string monitorTypeName, MachineId monitorId, string stateName, bool isEntry, bool? isInHotState);

        ///// <summary>
        ///// Called when a monitor is about to process or has raised an event.
        ///// </summary>
        ///// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        ///// <param name="monitorId">Id of the monitor that will process or has raised the event.</param>
        ///// <param name="currentStateName">The name of the state in which the event is being raised.</param>
        ///// <param name="eventName">The name of the event.</param>
        ///// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        //void OnMonitorEvent(string monitorTypeName, MachineId monitorId, string currentStateName, string eventName, bool isProcessing);

        ///// <summary>
        ///// Called when a monitor executes an action.
        ///// </summary>
        ///// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        ///// <param name="monitorId">Name of type of the monitor that is executing the action.</param>
        ///// <param name="currentStateName">The name of the state in which the action is being executed.</param>
        ///// <param name="actionName">The name of the action being executed.</param>
        //void OnMonitorAction(string monitorTypeName, MachineId monitorId, string currentStateName, string actionName);

        ///// <summary>
        ///// Called for general error reporting via pre-constructed text.
        ///// </summary>
        ///// <param name="text">The text of the error report.</param>
        //void OnError(string text);

        ///// <summary>
        ///// Called for errors detected by a specific scheduling strategy.
        ///// </summary>
        ///// <param name="strategy">The scheduling strategy that was used.</param>
        ///// <param name="strategyDescription">More information about the scheduling strategy.</param>
        //void OnStrategyError(SchedulingStrategy strategy, string strategyDescription);
    }
}