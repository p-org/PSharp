//-----------------------------------------------------------------------
// <copyright file="ILogger.cs">
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

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// Interface of the P# runtime logger.
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// The configuration that sets the logging verbosity.
        /// </summary>
        Configuration Configuration { get; set; }

        /// <summary>
        /// The minimum logging verbosity level (should be >= 0, use 0 to log all messages).
        /// </summary>
        int LoggingVerbosity { get; set; }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        void Write(string value);

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        void Write(string format, params object[] args);

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        void WriteLine(string value);

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        void WriteLine(string format, params object[] args);

        /// <summary>
        /// Called when an event is about to be enqueued to a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        void OnEnqueue(MachineId machineId, string eventName);

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        void OnDequeue(MachineId machineId, string currentStateName, string eventName);

        /// <summary>
        /// Called when the default event handler for a state is about to be executed.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currentStateName">Name of the current state of the machine.</param>
        void OnDefault(MachineId machineId, string currentStateName);

        /// <summary>
        /// Called when a machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="machineId">Id of the machine.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        void OnGoto(MachineId machineId, string currentStateName, string newStateName);

        /// <summary>
        /// Called when a machine is being pushed to a state.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        void OnPush(MachineId machineId, string currentStateName, string newStateName);

        /// <summary>
        /// Called when a machine has been popped from a state.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="restoredStateName">The name of the state being restored, if any.</param>
        void OnPop(MachineId machineId, string currentStateName, string restoredStateName);

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is 
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>
        ///     (which is being re-entered), if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        void OnPopUnhandledEvent(MachineId machineId, string currentStateName, string eventName);

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        void OnReceive(MachineId machineId, string currentStateName, string eventName, bool wasBlocked);

        /// <summary>
        /// Called when a machine enters a wait state.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currentStateName">The name of the current state of <paramref name="machineId"/>, if any.</param>
        /// <param name="eventNames">The names of the specific events being waited for, if any.</param>
        void OnWait(MachineId machineId, string currentStateName, string eventNames);

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="operationGroupId">The operation group id, if any.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        void OnSend(MachineId targetMachineId, MachineId senderId, string senderStateName,
            string eventName, Guid? operationGroupId, bool isTargetHalted);

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the host machine, null otherwise.</param>
        void OnCreateMachine(MachineId machineId, MachineId creator);

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        void OnCreateMonitor(string monitorTypeName, MachineId monitorId);

        /// <summary>
        /// Called when a machine has been halted.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        void OnHalt(MachineId machineId, int inboxSize);

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="machineId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        void OnRandom(MachineId machineId, object result);

        /// <summary>
        /// Called when a machine enters or exits a state.
        /// </summary>
        /// <param name="machineId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        void OnMachineState(MachineId machineId, string stateName, bool isEntry);

        /// <summary>
        /// Called when a machine raises an event.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currentStateName">The name of the current state of the machine raising the event.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        void OnMachineEvent(MachineId machineId, string currentStateName, string eventName);

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currentStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnMachineAction(MachineId machineId, string currentStateName, string actionName);

        /// <summary>
        /// Called when a machine throws an exception
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="currentStateName">The name of the current machine state.</param>
        /// <param name="ex">The exception.</param>
        void OnMachineExceptionThrown(MachineId machineId, string currentStateName, string actionName, Exception ex);

        /// <summary>
        /// Called when a machine's OnException method is used to handle a thrown exception
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="currentStateName">The name of the current machine state.</param>
        /// <param name="ex">The exception.</param>
        void OnMachineExceptionHandled(MachineId machineId, string currentStateName, string actionName, Exception ex);

        /// <summary>
        /// Called when a monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the monitor entering or exiting the state.</param>
        /// <param name="monitorId">The id of the monitor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        void OnMonitorState(string monitorTypeName, MachineId monitorId, string stateName, bool isEntry, bool? isInHotState);

        /// <summary>
        /// Called when a monitor is about to process or has raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">Id of the monitor that will process or has raised the event.</param>
        /// <param name="currentStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        void OnMonitorEvent(string monitorTypeName, MachineId monitorId, string currentStateName, string eventName, bool isProcessing);

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">Name of type of the monitor that is executing the action.</param>
        /// <param name="currentStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnMonitorAction(string monitorTypeName, MachineId monitorId, string currentStateName, string actionName);

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        void OnError(string text);

        /// <summary>
        /// Called for errors detected by a specific scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        void OnStrategyError(SchedulingStrategy strategy, string strategyDescription);
    }
}
