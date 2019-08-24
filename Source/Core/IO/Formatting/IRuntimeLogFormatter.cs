// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.Timers;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Interface of the runtime log formatter.
    /// </summary>
    public interface IRuntimeLogFormatter
    {
        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnEnqueue"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        string FormatOnEnqueueLogMessage(MachineId machineId, string eventName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnDequeue"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        string FormatOnDequeueLogMessage(MachineId machineId, string currStateName, string eventName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnDefault"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        string FormatOnDefaultLogMessage(MachineId machineId, string currStateName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnGoto"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        string FormatOnGotoLogMessage(MachineId machineId, string currStateName, string newStateName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPush"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        string FormatOnPushLogMessage(MachineId machineId, string currStateName, string newStateName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPop"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        string FormatOnPopLogMessage(MachineId machineId, string currStateName, string restoredStateName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPopUnhandledEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        string FormatOnPopUnhandledEventLogMessage(MachineId machineId, string currStateName, string eventName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnReceive"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        string FormatOnReceiveLogMessage(MachineId machineId, string currStateName, string eventName, bool wasBlocked);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnWait(MachineId, string, Type)"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        string FormatOnWaitLogMessage(MachineId machineId, string currStateName, Type eventType);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnWait(MachineId, string, Type[])"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        string FormatOnWaitLogMessage(MachineId machineId, string currStateName, params Type[] eventTypes);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnSend"/> log message and its parameters.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        string FormatOnSendLogMessage(MachineId targetMachineId, MachineId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateMachine"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the creator machine, null otherwise.</param>
        string FormatOnCreateMachineLogMessage(MachineId machineId, MachineId creator);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateMonitor"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        string FormatOnCreateMonitorLogMessage(string monitorTypeName, MachineId monitorId);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        string FormatOnCreateTimerLogMessage(TimerInfo info);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnStopTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        string FormatOnStopTimerLogMessage(TimerInfo info);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnHalt"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        string FormatOnHaltLogMessage(MachineId machineId, int inboxSize);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnRandom"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        string FormatOnRandomLogMessage(MachineId machineId, object result);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineState"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        string FormatOnMachineStateLogMessage(MachineId machineId, string stateName, bool isEntry);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        string FormatOnMachineEventLogMessage(MachineId machineId, string currStateName, string eventName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineAction"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        string FormatOnMachineActionLogMessage(MachineId machineId, string currStateName, string actionName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineExceptionThrown"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        string FormatOnMachineExceptionThrownLogMessage(MachineId machineId, string currStateName,
            string actionName, Exception ex);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineExceptionHandled"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        string FormatOnMachineExceptionHandledLogMessage(MachineId machineId, string currStateName,
            string actionName, Exception ex);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMonitorState"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="monitorId">The ID of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        string FormatOnMonitorStateLogMessage(string monitorTypeName, MachineId monitorId, string stateName,
            bool isEntry, bool? isInHotState);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMonitorEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        string FormatOnMonitorEventLogMessage(string monitorTypeName, MachineId monitorId, string currStateName,
            string eventName, bool isProcessing);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMonitorAction"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        string FormatOnMonitorActionLogMessage(string monitorTypeName, MachineId monitorId, string currStateName,
            string actionName);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnError"/> log message and its parameters.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        string FormatOnErrorLogMessage(string text);

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnStrategyError"/> log message and its parameters.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        string FormatOnStrategyErrorLogMessage(SchedulingStrategy strategy, string strategyDescription);
    }
}
