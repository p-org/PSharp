//-----------------------------------------------------------------------
// <copyright file="StateMachineLogger.cs">
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

using Microsoft.PSharp.Utilities;
using System;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// An implementation of <see cref="ILogger"/> that by default passes all logging commands to the variants
    /// of the <see cref="Write(string)"/> method if the <see cref="Configuration.Verbose"/> option is set to
    /// >= <see cref="LoggingVerbosity"/>. This class may be subclassed and its methods overridden.
    /// </summary>
    public abstract class StateMachineLogger : ILogger
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
        /// Constructs the logger. The logger will be assigned the Runtime's Configuration
        /// object when it is passed to <see cref="PSharpRuntime.SetLogger(ILogger)"/>.
        /// </summary>
        /// <param name="loggingVerbosity">The initial logging verbosity level.</param>
        public StateMachineLogger(int loggingVerbosity = 2)
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
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnEnqueue(MachineId machineId, string currentStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine($"<EnqueueLog> Machine '{machineId}' in state '{currentStateName}' enqueued event '{eventName}'.");
            }
        }

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnDequeue(MachineId machineId, string currentStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine($"<DequeueLog> Machine '{machineId}' in state '{currentStateName}' dequeued event '{eventName}'.");
            }
        }

        /// <summary>
        /// Called when the default event handler for a state is about to be executed.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currentStateName">Name of the current state of the machine.</param>
        public virtual void OnDefault(MachineId machineId, string currentStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine($"<DefaultLog> Machine '{machineId}' in state '{currentStateName}' is executing the default handler.");
            }
        }

        /// <summary>
        /// Called when a machine is being pushed to a state.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public virtual void OnPush(MachineId machineId, string currentStateName, string newStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine($"<PushLog> Machine '{0}' pushed from state '{1}' to state '{2}'.",
                    machineId, currentStateName, newStateName);
            }
        }

        /// <summary>
        /// Called when a machine has been popped from a state.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public virtual void OnPop(MachineId machineId, string currentStateName, string restoredStateName)
        {
            if (this.IsVerbose)
            {
                var curStateName = string.IsNullOrEmpty(currentStateName) ? "[not recorded]" : currentStateName;
                var reenteredStateName = restoredStateName ?? string.Empty;
                this.WriteLine("<PopLog> Machine '{0}' popped state '{1}' and reentered state '{2}'.",
                    machineId, currentStateName, reenteredStateName);
            }
        }

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is 
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>
        ///     (which is being re-entered), if any</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual void OnPopUnhandledEvent(MachineId machineId, string currentStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                var reenteredStateName = string.IsNullOrEmpty(currentStateName) 
                    ? string.Empty
                    : $" and reentered state '{currentStateName}";
                this.WriteLine($"<PopLog> Machine '{machineId}' popped with unhandled event '{eventName}'{reenteredStateName}.");
            }
        }

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        public virtual void OnReceive(MachineId machineId, string currentStateName, string eventName, bool wasBlocked)
        {
            if (this.IsVerbose)
            {
                var unblocked = wasBlocked ? " and unblocked" : string.Empty;
                this.WriteLine("<ReceiveLog> Machine '{0}' in state '{1}' dequeued event '{2}'{3}.",
                    machineId, currentStateName, eventName, unblocked);
            }
        }

        /// <summary>
        /// Called when a machine enters a wait state.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="eventNames">The names of the specific events being waited for, if any.</param>
        public virtual void OnWait(MachineId machineId, string currentStateName, string eventNames)
        {
            if (this.IsVerbose)
            {
                if (string.IsNullOrEmpty(eventNames))
                {
                    eventNames = "[any]";
                }

                this.WriteLine($"<ReceiveLog> Machine '{machineId}' in state '{currentStateName}' dequeued event '{eventNames}'.");
            }
        }

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="targetStateName">The name of the current state of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if applicable
        ///     (if it is a non-Machine specialization of an AbstractMachine, it is not applicable).</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="operationGroupId">The operation group id, if any.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public virtual void OnSend(MachineId targetMachineId, string targetStateName, MachineId senderId, string senderStateName,
            string eventName, Guid? operationGroupId, bool isTargetHalted)
        {
            if (this.IsVerbose)
            {
                var guid = operationGroupId.HasValue ? operationGroupId.Value.ToString() : "<none>";
                var target = isTargetHalted
                            ? $"a halted machine '{targetMachineId}'"
                            : $"machine '{targetMachineId}' in state '{targetStateName}'";
                var message = senderId != null
                    ? $"<SendLog> Operation Group {guid}: Machine '{senderId}' in state '{senderStateName}'" +
                            $" sent event '{eventName}' to {target}."
                    : $"<SendLog> Operation Group {guid}: The event '{eventName}' was sent to {target}.";
                this.WriteLine(message);
            }
        }

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        public virtual void OnCreateMachine(MachineId machineId)
        {
            if (this.IsVerbose)
            {
                this.WriteLine($"<CreateLog> Machine '{machineId}' is created.");
            }
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
                this.WriteLine($"<CreateLog> Monitor '{monitorTypeName}' with id '{monitorId}' is created.");
            }
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
                this.WriteLine($"<HaltLog> Machine '{machineId}' halted with '{inboxSize}' events in its inbox.");
            }
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
                var source = machineId != null ? $"Machine '{machineId}'" : "Runtime";
                this.WriteLine($"<RandomLog> {source} nondeterministically chose '{result}'.");
            }
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
                var direction = isEntry ? "enters" : "exits";
                this.WriteLine($"<StateLog> Machine '{machineId}' {direction} state '{stateName}'.");
            }
        }

        /// <summary>
        /// Called when a machine raises an event.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currentStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual void OnMachineEvent(MachineId machineId, string currentStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine($"<RaiseLog> Machine '{machineId}' in state '{currentStateName}' raised event '{eventName}'.");
            }
        }

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currentStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMachineAction(MachineId machineId, string currentStateName, string actionName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine($"<ActionLog> Machine '{machineId}' in state '{currentStateName}' invoked action '{actionName}'.");
            }
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
                var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot'" : "'cold'") : string.Empty;
                this.WriteLine($"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' enters {liveness}state '{stateName}'.");
            }
        }

        /// <summary>
        /// Called when a monitor is about to process or has raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currentStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public virtual void OnMonitorEvent(string monitorTypeName, MachineId monitorId, string currentStateName,
            string eventName, bool isProcessing)
        {
            if (this.IsVerbose)
            {
                var activity = isProcessing ? "is processing" : "raised";
                this.WriteLine("<MonitorLog> Monitor '{0}' with id '{1}' in state '{2}' {3} event '{4}'.",
                    monitorTypeName, monitorId, currentStateName, activity, eventName);
            }
        }

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currentStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMonitorAction(string monitorTypeName, MachineId monitorId, string currentStateName, string actionName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine("<MonitorLog> Monitor '{0}' with id '{1}' in state '{2}' executed action '{3}'.",
                    monitorTypeName, monitorId, currentStateName, actionName);
            }
        }

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual void OnError(string text)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(text);
            }
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
                var desc = string.IsNullOrEmpty(strategyDescription) ? $" Description: {strategyDescription}" : string.Empty;
                this.WriteLine($"<StrategyLog> Found bug using '{strategy}' strategy.{desc}");
            }
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public abstract void Dispose();
    }
}
