//-----------------------------------------------------------------------
// <copyright file="MachineLogger.cs">
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
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// An implementation of <see cref="ILogger"/> that by default passes all logging commands to the variants
    /// of the <see cref="Write(string)"/> method if the <see cref="Configuration.Verbose"/> option is set to
    /// >= <see cref="LoggingVerbosity"/>. This class may be subclassed and its methods overridden.
    /// </summary>
    public abstract class MachineLogger : ILogger
    {
        /// <summary>
        /// The configuration that sets the logging verbosity.
        /// </summary>
        public Configuration Configuration { get; set; }

        /// <summary>
        /// The minimum logging verbosity level. If <see cref="Configuration.Verbose"/> is >=
        /// <see cref="LoggingVerbosity"/>, then messages are logged (0 logs all messages).
        /// </summary>
        public int LoggingVerbosity { get; set; } = 2;

        /// <summary>
        /// Convenience method to indicate whether <see cref="Configuration.Verbose"/> is at or
        /// above <see cref="LoggingVerbosity"/>.
        /// </summary>
        public bool IsVerbose => this.Configuration.Verbose >= LoggingVerbosity;

        /// <summary>
        /// Constructs the logger. The logger will be assigned the runtime
        /// <see cref="PSharp.Configuration"/> object when it is passed to
        /// <see cref="IPSharpRuntime.SetLogger(ILogger)"/>.
        /// </summary>
        /// <param name="loggingVerbosity">The initial logging verbosity level.</param>
        public MachineLogger(int loggingVerbosity = 2)
        {
            this.LoggingVerbosity = loggingVerbosity;
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public abstract void Write(string value);

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public abstract void Write(string format, params object[] args);

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public abstract void WriteLine(string value);

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public abstract void WriteLine(string format, params object[] args);

        /// <summary>
        /// Called when an event is about to be enqueued to a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnEnqueue(MachineId machineId, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnEnqueueString(machineId, eventName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnEnqueue"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual string FormatOnEnqueueString(MachineId machineId, string eventName)
        {
            return $"<EnqueueLog> Machine '{machineId}' enqueued event '{eventName}'.";
        }

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnDequeue(MachineId machineId, string currStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnDequeueString(machineId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnDequeue"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual string FormatOnDequeueString(MachineId machineId, string currStateName, string eventName)
        {
            return $"<DequeueLog> Machine '{machineId}' in state '{currStateName}' dequeued event '{eventName}'.";
        }

        /// <summary>
        /// Called when the default event handler for a state is about to be executed.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        public virtual void OnDefault(MachineId machineId, string currStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnDefaultString(machineId, currStateName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnDefault"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        public virtual string FormatOnDefaultString(MachineId machineId, string currStateName)
        {
            return $"<DefaultLog> Machine '{machineId}' in state '{currStateName}' is executing the default handler.";
        }

        /// <summary>
        /// Called when a machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="machineId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public void OnGoto(MachineId machineId, string currStateName, string newStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnGotoString(machineId, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnGoto"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public virtual string FormatOnGotoString(MachineId machineId, string currStateName, string newStateName)
        {
            return $"<GotoLog> Machine '{machineId}' is transitioning from state '{currStateName}' to state '{newStateName}'.";
        }
        
        /// <summary>
        /// Called when a machine is being pushed to a state.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public virtual void OnPush(MachineId machineId, string currStateName, string newStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnPushString(machineId, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPush"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public virtual string FormatOnPushString(MachineId machineId, string currStateName, string newStateName)
        {
            return $"<PushLog> Machine '{machineId}' pushed from state '{currStateName}' to state '{newStateName}'.";
        }

        /// <summary>
        /// Called when a machine has been popped from a state.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public virtual void OnPop(MachineId machineId, string currStateName, string restoredStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnPopString(machineId, currStateName, restoredStateName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPop"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public virtual string FormatOnPopString(MachineId machineId, string currStateName, string restoredStateName)
        {
            var curStateName = string.IsNullOrEmpty(currStateName) ? "[not recorded]" : currStateName;
            var reenteredStateName = restoredStateName ?? String.Empty;
            return $"<PopLog> Machine '{machineId}' popped state '{currStateName}' and reentered state '{reenteredStateName}'.";
        }

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is 
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine
        ///     (which is being re-entered), if any</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual void OnPopUnhandledEvent(MachineId machineId, string currStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnPopUnhandledEventString(machineId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPopUnhandledEvent"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual string FormatOnPopUnhandledEventString(MachineId machineId, string currStateName, string eventName)
        {
            var reenteredStateName = string.IsNullOrEmpty(currStateName)
                ? String.Empty
                : $" and reentered state '{currStateName}";
            return $"<PopLog> Machine '{machineId}' popped with unhandled event '{eventName}'{reenteredStateName}.";
        }

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        public virtual void OnReceive(MachineId machineId, string currStateName, string eventName, bool wasBlocked)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnReceiveString(machineId, currStateName, eventName, wasBlocked));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnReceive"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        public virtual string FormatOnReceiveString(MachineId machineId, string currStateName, string eventName, bool wasBlocked)
        {
            var unblocked = wasBlocked ? " and unblocked" : String.Empty;
            return $"<ReceiveLog> Machine '{machineId}' in state '{currStateName}' dequeued event '{eventName}'{unblocked}.";
        }

        /// <summary>
        /// Called when a machine enters a wait state.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventNames">The names of the specific events being waited for, if any.</param>
        public virtual void OnWait(MachineId machineId, string currStateName, string eventNames)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnWaitString(machineId, currStateName, ref eventNames));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnWait"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventNames">The names of the specific events being waited for, if any.</param>
        public virtual string FormatOnWaitString(MachineId machineId, string currStateName, ref string eventNames)
        {
            if (string.IsNullOrEmpty(eventNames))
            {
                eventNames = "[any]";
            }
            return $"<ReceiveLog> Machine '{machineId}' in state '{currStateName}' dequeued event '{eventNames}'.";
        }

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="operationGroupId">The operation group id, if any.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public virtual void OnSend(MachineId targetMachineId, IMachineId senderId, string senderStateName,
            string eventName, Guid? operationGroupId, bool isTargetHalted)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnSendString(targetMachineId, senderId, senderStateName, eventName, operationGroupId, isTargetHalted));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnSend"/> event and its parameters.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="operationGroupId">The operation group id, if any.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public virtual string FormatOnSendString(MachineId targetMachineId, IMachineId senderId, string senderStateName, string eventName,
            Guid? operationGroupId, bool isTargetHalted)
        {
            var guid = (operationGroupId.HasValue && operationGroupId.Value != Guid.Empty) ?
                operationGroupId.Value.ToString() : "<none>";
            var target = isTargetHalted
                        ? $"a halted machine '{targetMachineId}'"
                        : $"machine '{targetMachineId}'";
            var sender = senderId != null
                ? $"Machine '{senderId}' in state '{senderStateName}'"
                : $"The Runtime";
            return $"<SendLog> Operation Group {guid}: {sender} sent event '{eventName}' to {target}.";
        }

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the host machine, null otherwise.</param>
        public virtual void OnCreateMachine(MachineId machineId, IMachineId creator)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnCreateMachineString(machineId, creator));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnCreateMachine"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the host machine, null otherwise.</param>
        public virtual string FormatOnCreateMachineString(MachineId machineId, IMachineId creator)
        {
            var source = creator == null ? "the Runtime" : $"machine '{creator.Name}'";
            return $"<CreateLog> Machine '{machineId}' was created by {source}.";
        }

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        public virtual void OnCreateMonitor(string monitorTypeName, MachineId monitorId)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnCreateMonitorString(monitorTypeName, monitorId));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnCreateMonitor"/> event and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        public virtual string FormatOnCreateMonitorString(string monitorTypeName, MachineId monitorId)
        {
            return $"<CreateLog> Monitor '{monitorTypeName}' with id '{monitorId}' was created.";
        }

        /// <summary>
        /// Called when a machine has been halted.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        public virtual void OnHalt(MachineId machineId, int inboxSize)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnHaltString(machineId, inboxSize));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnHalt"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        public virtual string FormatOnHaltString(MachineId machineId, int inboxSize)
        {
            return $"<HaltLog> Machine '{machineId}' halted with '{inboxSize}' events in its inbox.";
        }

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="machineId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual void OnRandom(MachineId machineId, object result)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnRandomString(machineId, result));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnRandom"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual string FormatOnRandomString(MachineId machineId, object result)
        {
            var source = machineId != null ? $"Machine '{machineId}'" : "Runtime";
            return $"<RandomLog> {source} nondeterministically chose '{result}'.";
        }

        /// <summary>
        /// Called when a machine enters or exits a state.
        /// </summary>
        /// <param name="machineId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual void OnMachineState(MachineId machineId, string stateName, bool isEntry)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnMachineStateString(machineId, stateName, isEntry));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineState"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual string FormatOnMachineStateString(MachineId machineId, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            return $"<StateLog> Machine '{machineId}' {direction} state '{stateName}'.";
        }

        /// <summary>
        /// Called when a machine raises an event.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual void OnMachineEvent(MachineId machineId, string currStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnMachineEventString(machineId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineEvent"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual string FormatOnMachineEventString(MachineId machineId, string currStateName, string eventName)
        {
            return $"<RaiseLog> Machine '{machineId}' in state '{currStateName}' raised event '{eventName}'.";
        }

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMachineAction(MachineId machineId, string currStateName, string actionName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnMachineActionString(machineId, currStateName, actionName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineAction"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual string FormatOnMachineActionString(MachineId machineId, string currStateName, string actionName)
        {
            return $"<ActionLog> Machine '{machineId}' in state '{currStateName}' invoked action '{actionName}'.";
        }

        /// <summary>
        /// Called when a machine throws an exception
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnMachineExceptionThrown(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnMachineExceptionThrownString(machineId, currStateName, actionName, ex));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineExceptionThrown"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="ex">The exception.</param>
        public virtual string FormatOnMachineExceptionThrownString(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
            return $"<ExceptionLog> Machine '{machineId}' in state '{currStateName}' running action '{actionName}' threw an exception '{ex.GetType().Name}'.";
        }

        /// <summary>
        /// Called when a machine's OnException method is used to handle a thrown exception
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnMachineExceptionHandled(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnMachineExceptionHandledString(machineId, currStateName, actionName, ex));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineExceptionHandled"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="ex">The exception.</param>
        public virtual string FormatOnMachineExceptionHandledString(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
            return $"<ExceptionLog> Machine '{machineId}' in state '{currStateName}' running action '{actionName}' chose to handle the exception '{ex.GetType().Name}'.";
        }

        /// <summary>
        /// Called when a monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="monitorId">The ID of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        public virtual void OnMonitorState(string monitorTypeName, MachineId monitorId, string stateName,
            bool isEntry, bool? isInHotState)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnMonitorStateString(monitorTypeName, monitorId, stateName, isEntry, isInHotState));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorState"/> event and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="monitorId">The ID of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        public virtual string FormatOnMonitorStateString(string monitorTypeName, MachineId monitorId, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot' " : "'cold' ") : String.Empty;
            var direction = isEntry ? "enters" : "exits";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' {direction} {liveness}state '{stateName}'.";
        }

        /// <summary>
        /// Called when a monitor is about to process or has raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public virtual void OnMonitorEvent(string monitorTypeName, MachineId monitorId, string currStateName,
            string eventName, bool isProcessing)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnMonitorEventString(monitorTypeName, monitorId, currStateName, eventName, isProcessing));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorEvent"/> event and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public virtual string FormatOnMonitorEventString(string monitorTypeName, MachineId monitorId, string currStateName, string eventName, bool isProcessing)
        {
            var activity = isProcessing ? "is processing" : "raised";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' in state '{currStateName}' {activity} event '{eventName}'.";
        }

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMonitorAction(string monitorTypeName, MachineId monitorId, string currStateName, string actionName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnMonitorActionString(monitorTypeName, monitorId, currStateName, actionName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorAction"/> event and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual string FormatOnMonitorActionString(string monitorTypeName, MachineId monitorId, string currStateName, string actionName)
        {
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' in state '{currStateName}' executed action '{actionName}'.";
        }

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual void OnError(string text)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnErrorString(text));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnError"/> event and its parameters.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual string FormatOnErrorString(string text)
        {
            // We do nothing here, but a subclass may override.
            return text;
        }

        /// <summary>
        /// Called for errors detected by a specific scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public virtual void OnStrategyError(SchedulingStrategy strategy, string strategyDescription)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(FormatOnStrategyErrorString(strategy, strategyDescription));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnStrategyError"/> event and its parameters.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public virtual string FormatOnStrategyErrorString(SchedulingStrategy strategy, string strategyDescription)
        {
            var desc = string.IsNullOrEmpty(strategyDescription) ? $" Description: {strategyDescription}" : String.Empty;
            return $"<StrategyLog> Found bug using '{strategy}' strategy.{desc}";
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public abstract void Dispose();
    }
}
