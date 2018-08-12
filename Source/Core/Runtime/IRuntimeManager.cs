//-----------------------------------------------------------------------
// <copyright file="IRuntimeManager.cs">
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
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Net;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// The base interface of the P# runtime manager. It provides APIs for
    /// accessing internal functionality of the runtime.
    /// </summary>
    internal interface IRuntimeManager
    {
        /// <summary>
        /// The P# runtime.
        /// </summary>
        IPSharpRuntime Runtime { get; }

        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        Configuration Configuration { get; }

        /// <summary>
        /// Network provider used for remote communication.
        /// </summary>
        INetworkProvider NetworkProvider { get; }

        /// <summary>
        /// The installed logger.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Records if the runtime is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Event that is fired when the P# program throws an exception.
        /// </summary>
        event OnFailureHandler OnFailure;

        #region machine creation and execution

        /// <summary>
        /// Creates a fresh machine id that has not yet been bound to any machine.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>

        MachineId CreateMachineId(Type type, string friendlyName = null);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="e">Event passed during machine construction.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="creator">The creator machine.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        Task<MachineId> CreateMachineAsync(MachineId mid, Type type, string friendlyName, Event e, BaseMachine creator, Guid? operationGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">The sender machine.</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        Task SendEventAsync(MachineId mid, Event e, BaseMachine sender, SendOptions options);

        #endregion

        #region specifications and error checking

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="invoker">The machine invoking the monitor.</param>
        /// <param name="e">Event sent to the monitor.</param>
        void Monitor(Type type, BaseMachine invoker, Event e);

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an
        /// <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        void Assert(bool predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an
        /// <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        void Assert(bool predicate, string s, params object[] args);

        #endregion

        #region nondeterministic choices

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>Boolean</returns>
        bool GetNondeterministicBooleanChoice(BaseMachine machine, int maxValue);

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        bool GetFairNondeterministicBooleanChoice(BaseMachine machine, string uniqueId);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>Integer</returns>
        int GetNondeterministicIntegerChoice(BaseMachine machine, int maxValue);

        #endregion

        #region timers

        /// <summary>
        /// Overrides the default machine type for instantiating timers.
        /// </summary>
        /// <param name="type">Type of the timer.</param>
        void SetTimerMachineType(Type type);

        /// <summary>
        /// Returns the timer machine type.
        /// </summary>
        /// <returns>The timer machine type.</returns>
        Type GetTimerMachineType();

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        void NotifyEnteredState(BaseMachine machine);

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        void NotifyEnteredState(Monitor monitor);

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        void NotifyExitedState(BaseMachine machine);

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        void NotifyExitedState(Monitor monitor);

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        void NotifyInvokedAction(BaseMachine machine, MethodInfo action, Event receivedEvent);

        /// <summary>
        /// Notifies that a machine completed invoking an action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        void NotifyCompletedAction(BaseMachine machine, MethodInfo action, Event receivedEvent);

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent);

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        void NotifyRaisedEvent(BaseMachine machine, EventInfo eventInfo);

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="eventInfo">The event metadata.</param>
        void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo);

        /// <summary>
        /// Notifies that a machine is handling a raised <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        void NotifyHandleRaisedEvent(BaseMachine machine, EventInfo eventInfo);

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        void NotifyDequeuedEvent(BaseMachine machine, EventInfo eventInfo);

        /// <summary>
        /// Notifies that a machine invoked pop.
        /// </summary>
        /// <param name="machine">The machine.</param>
        void NotifyPop(BaseMachine machine);

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="inbox">The machine inbox.</param>
        void NotifyHalted(BaseMachine machine, LinkedList<EventInfo> inbox);

        /// <summary>
        /// Notifies that the inbox of the specified machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        void NotifyDefaultEventHandlerCheck(BaseMachine machine);

        /// <summary>
        /// Notifies that the default handler of the specified machine has been fired.
        /// </summary>
        /// <param name="machine">The machine.</param>
        void NotifyDefaultHandlerFired(BaseMachine machine);

        #endregion

        #region logging

        /// <summary>
        /// Logs the specified text.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        void Log(string format, params object[] args);

        #endregion

        #region exceptions

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        /// <param name="exception">Exception</param>
        void RaiseOnFailureEvent(Exception exception);

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        void WrapAndThrowException(Exception exception, string s, params object[] args);

        #endregion
    }
}
