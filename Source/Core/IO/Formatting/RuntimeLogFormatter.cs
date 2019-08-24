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
    /// Default implementation of the runtime log formatter.
    /// </summary>
    public class RuntimeLogFormatter : IRuntimeLogFormatter
    {
        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnEnqueue"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual string FormatOnEnqueueLogMessage(MachineId machineId, string eventName) =>
            $"<EnqueueLog> Machine '{machineId}' enqueued event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnDequeue"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual string FormatOnDequeueLogMessage(MachineId machineId, string currStateName, string eventName) =>
            $"<DequeueLog> Machine '{machineId}' in state '{currStateName}' dequeued event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnDefault"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        public virtual string FormatOnDefaultLogMessage(MachineId machineId, string currStateName) =>
            $"<DefaultLog> Machine '{machineId}' in state '{currStateName}' is executing the default handler.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnGoto"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public virtual string FormatOnGotoLogMessage(MachineId machineId, string currStateName, string newStateName) =>
            $"<GotoLog> Machine '{machineId}' is transitioning from state '{currStateName}' to state '{newStateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPush"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public virtual string FormatOnPushLogMessage(MachineId machineId, string currStateName, string newStateName) =>
            $"<PushLog> Machine '{machineId}' pushed from state '{currStateName}' to state '{newStateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPop"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public virtual string FormatOnPopLogMessage(MachineId machineId, string currStateName, string restoredStateName)
        {
            currStateName = string.IsNullOrEmpty(currStateName) ? "[not recorded]" : currStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            return $"<PopLog> Machine '{machineId}' popped state '{currStateName}' and reentered state '{reenteredStateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPopUnhandledEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual string FormatOnPopUnhandledEventLogMessage(MachineId machineId, string currStateName, string eventName)
        {
            var reenteredStateName = string.IsNullOrEmpty(currStateName)
                ? string.Empty
                : $" and reentered state '{currStateName}";
            return $"<PopLog> Machine '{machineId}' popped with unhandled event '{eventName}'{reenteredStateName}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnReceive"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        public virtual string FormatOnReceiveLogMessage(MachineId machineId, string currStateName, string eventName, bool wasBlocked)
        {
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            return $"<ReceiveLog> Machine '{machineId}' in state '{currStateName}' dequeued event '{eventName}'{unblocked}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnWait(MachineId, string, Type)"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public virtual string FormatOnWaitLogMessage(MachineId machineId, string currStateName, Type eventType) =>
            $"<ReceiveLog> Machine '{machineId}' in state '{currStateName}' is waiting to dequeue an event of type '{eventType.FullName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnWait(MachineId, string, Type[])"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public virtual string FormatOnWaitLogMessage(MachineId machineId, string currStateName, params Type[] eventTypes)
        {
            string eventNames;
            if (eventTypes.Length == 0)
            {
                eventNames = "'<missing>'";
            }
            else if (eventTypes.Length == 1)
            {
                eventNames = "'" + eventTypes[0].FullName + "'";
            }
            else if (eventTypes.Length == 2)
            {
                eventNames = "'" + eventTypes[0].FullName + "' or '" + eventTypes[1].FullName + "'";
            }
            else if (eventTypes.Length == 3)
            {
                eventNames = "'" + eventTypes[0].FullName + "', '" + eventTypes[1].FullName + "' or '" + eventTypes[2].FullName + "'";
            }
            else
            {
                string[] eventNameArray = new string[eventTypes.Length - 1];
                for (int i = 0; i < eventTypes.Length - 2; i++)
                {
                    eventNameArray[i] = eventTypes[i].FullName;
                }

                eventNames = "'" + string.Join("', '", eventNameArray) + "' or '" + eventTypes[eventTypes.Length - 1].FullName + "'";
            }

            return $"<ReceiveLog> Machine '{machineId}' in state '{currStateName}' is waiting to dequeue an event of type {eventNames}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnSend"/> log message and its parameters.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public virtual string FormatOnSendLogMessage(MachineId targetMachineId, MachineId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var target = isTargetHalted ? $"halted machine '{targetMachineId}'" : $"machine '{targetMachineId}'";
            var sender = senderId != null ? $"Machine '{senderId}' in state '{senderStateName}'" : $"The runtime";
            return $"<SendLog> {sender} sent event '{eventName}' to {target}{opGroupIdMsg}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateMachine"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the creator machine, null otherwise.</param>
        public virtual string FormatOnCreateMachineLogMessage(MachineId machineId, MachineId creator)
        {
            var source = creator is null ? "the runtime" : $"machine '{creator.Name}'";
            return $"<CreateLog> Machine '{machineId}' was created by {source}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateMonitor"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        public virtual string FormatOnCreateMonitorLogMessage(string monitorTypeName, MachineId monitorId) =>
            $"<CreateLog> Monitor '{monitorTypeName}' with id '{monitorId}' was created.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual string FormatOnCreateTimerLogMessage(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"machine '{info.OwnerId.Name}'";
            if (info.Period.TotalMilliseconds >= 0)
            {
                return $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms; " +
                    $"period :{info.Period.TotalMilliseconds}ms) was created by {source}.";
            }
            else
            {
                return $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms) was created by {source}.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnStopTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual string FormatOnStopTimerLogMessage(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"machine '{info.OwnerId.Name}'";
            return $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnHalt"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        public virtual string FormatOnHaltLogMessage(MachineId machineId, int inboxSize) =>
            $"<HaltLog> Machine '{machineId}' halted with '{inboxSize}' events in its inbox.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnRandom"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual string FormatOnRandomLogMessage(MachineId machineId, object result)
        {
            var source = machineId != null ? $"Machine '{machineId}'" : "Runtime";
            return $"<RandomLog> {source} nondeterministically chose '{result}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineState"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual string FormatOnMachineStateLogMessage(MachineId machineId, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            return $"<StateLog> Machine '{machineId}' {direction} state '{stateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual string FormatOnMachineEventLogMessage(MachineId machineId, string currStateName, string eventName) =>
            $"<RaiseLog> Machine '{machineId}' in state '{currStateName}' raised event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineAction"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual string FormatOnMachineActionLogMessage(MachineId machineId, string currStateName, string actionName) =>
            $"<ActionLog> Machine '{machineId}' in state '{currStateName}' invoked action '{actionName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineExceptionThrown"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual string FormatOnMachineExceptionThrownLogMessage(MachineId machineId, string currStateName, string actionName, Exception ex) =>
            $"<ExceptionLog> Machine '{machineId}' in state '{currStateName}' running action '{actionName}' threw an exception '{ex.GetType().Name}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineExceptionHandled"/> log message and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual string FormatOnMachineExceptionHandledLogMessage(MachineId machineId, string currStateName, string actionName, Exception ex) =>
            $"<ExceptionLog> Machine '{machineId}' in state '{currStateName}' running action '{actionName}' chose to handle the exception '{ex.GetType().Name}'.";

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
        public virtual string FormatOnMonitorStateLogMessage(string monitorTypeName, MachineId monitorId, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot' " : "'cold' ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' {direction} {liveness}state '{stateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMonitorEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public virtual string FormatOnMonitorEventLogMessage(string monitorTypeName, MachineId monitorId, string currStateName, string eventName, bool isProcessing)
        {
            var activity = isProcessing ? "is processing" : "raised";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' in state '{currStateName}' {activity} event '{eventName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMonitorAction"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual string FormatOnMonitorActionLogMessage(string monitorTypeName, MachineId monitorId, string currStateName, string actionName) =>
            $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' in state '{currStateName}' executed action '{actionName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnError"/> log message and its parameters.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual string FormatOnErrorLogMessage(string text) => text;

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnStrategyError"/> log message and its parameters.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public virtual string FormatOnStrategyErrorLogMessage(SchedulingStrategy strategy, string strategyDescription)
        {
            var desc = string.IsNullOrEmpty(strategyDescription) ? $" Description: {strategyDescription}" : string.Empty;
            return $"<StrategyLog> Found bug using '{strategy}' strategy.{desc}";
        }
    }
}
