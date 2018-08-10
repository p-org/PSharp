//-----------------------------------------------------------------------
// <copyright file="TestingRuntime.cs">
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
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// Runtime for executing machines in bug-finding mode.
    /// </summary>
    internal class TestingRuntime : BaseTestingRuntime, IStateMachineRuntime
    {
        /// <summary>
        /// The base machine types that can execute on this runtime.
        /// </summary>
        private readonly HashSet<Type> SupportedBaseMachineTypes;

        /// <summary>
        /// Records if a machine was triggered by an enqueue.
        /// </summary>
        private bool StartEventHandlerCalled;

        /// <summary>
        /// Creates a P# runtime that executes in bug-finding mode.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <param name="reporter">Reporter to register runtime operations.</param>
        /// <returns>TestingRuntime</returns>
        [TestRuntimeCreate]
        internal static TestingRuntime Create(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
        {
            return new TestingRuntime(configuration, strategy, reporter);
        }

        /// <summary>
        /// Returns the type of the bug-finding runtime.
        /// </summary>
        /// <returns></returns>
        [TestRuntimeGetType]
        internal static Type GetRuntimeType() => typeof(IStateMachineRuntime);

        /// <summary>
        /// Constructor.
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <param name="reporter">Reporter to register runtime operations.</param>
        /// </summary>
        protected TestingRuntime(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration, strategy, reporter)
        {
            this.SupportedBaseMachineTypes = new HashSet<Type> { typeof(Machine), typeof(TestHarnessMachine) };
        }

        #region runtime interface

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        public Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecute(null, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecute(null, type, friendlyName, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            this.Assert(mid != null, "Cannot pass a null MachineId.");
            return this.CreateMachineAndExecute(mid, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        internal Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName, Event e = null,
            Guid? operationGroupId = null)
        {
            BaseMachine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachineAndExecute(mid, type, friendlyName, e, creator, operationGroupId);
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense again.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        public Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            return this.SendEventAndExecute(target, e, this.GetCurrentMachine(), options);
        }

        #endregion

        #region machine creation and execution

        /// <summary>
        /// Creates a new P# machine using the specified unbound <see cref="MachineId"/> and type.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine.</returns>
        protected override async Task<BaseMachine> CreateMachineAsync(MachineId mid, Type type)
        {
            Machine machine = MachineFactory.Create(type);
            await machine.InitializeAsync(this, mid, new SchedulableInfo(mid));
            return machine;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="e">Event passed during machine construction.</param>
        /// <param name="creator">The creator machine.</param>
        /// <returns>The result is the <see cref="MachineId"/>.</returns>
        private async Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName,
            Event e, BaseMachine creator, Guid? operationGroupId)
        {
            this.CheckMachineMethodInvocation(creator, MachineApiNames.CreateMachineAndExecuteApiName);

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

            BaseMachine machine = await this.CreateMachineAsync(mid, type, friendlyName, creator);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e == null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, creator.Id, null);

            // wait until the machine reaches quiescence
            await (creator as Machine).Receive(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).mid == machine.Id);

            return await Task.FromResult(machine.Id);
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately
        /// if the target machine was already running. Otherwise blocks until the machine handles
        /// the event and reaches quiescense again.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">The sender machine.</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>True if event was handled, false if the event was only enqueued</returns>
        private async Task<bool> SendEventAndExecute(MachineId mid, Event e, BaseMachine sender, SendOptions options)
        {
            this.CheckMachineMethodInvocation(sender, MachineApiNames.SendEventAndExecuteApiName);
            this.Assert(this.CreatedMachineIds.Contains(mid), "Cannot Send event {0} to a MachineId ({0},{1}) that was never " +
                "previously bound to a machine of type {2}", e.GetType().FullName, mid.Value, mid, mid);

            this.Scheduler.Schedule(OperationType.Send, OperationTargetType.Inbox, mid.Value);
            var operationGroupId = this.GetNewOperationGroupId(sender, options?.OperationGroupId);

            if (!this.GetTargetMachine(mid, e, sender, operationGroupId, out BaseMachine machine))
            {
                this.Assert(options == null || !options.MustHandle,
                    $"A must-handle event '{e.GetType().FullName}' was sent to the halted machine '{mid}'.\n");
                return true;
            }

            bool runNewHandler = false;

            // This is set true by CheckStartEventHandler, called by EnqueueEvent. runNewHandler is not
            // set to true by EnqueueEvent (even when the machine was previously Idle) when the event
            // e requires no action by the machine (i.e., it implicitly handles the event). In such a case, 
            // CheckStartEventHandler must have been called.
            this.StartEventHandlerCalled = false; 

            EventInfo eventInfo = this.EnqueueEvent(machine, e, sender, operationGroupId, options?.MustHandle ?? false, ref runNewHandler);
            if (runNewHandler)
            {
                this.RunMachineEventHandler(machine, null, false, sender.Id, eventInfo);

                // wait until the machine reaches quiescence
                await (sender as Machine).Receive(typeof(QuiescentEvent),
                    rev => (rev as QuiescentEvent).mid == mid);
                return true;
            }
            return this.StartEventHandlerCalled;
        }

        /// <summary>
        /// Checks that a machine can start its event handler. Returns false if the event
        /// handler should not be started. The bug finding runtime may return false because
        /// it knows that there are currently no events in the inbox that can be handled.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <returns>Boolean</returns>
        protected internal override bool CheckStartEventHandler(BaseMachine machine)
        {
            this.StartEventHandlerCalled = true;
            return machine.TryDequeueEvent(true) != null;
        }

        /// <summary>
        /// Checks if the specified type is a machine that can execute on this runtime.
        /// </summary>
        /// <returns>True if the type is supported, else false.</returns>
        protected override bool IsSupportedMachineType(Type type) =>
            this.SupportedBaseMachineTypes.Any(machineType => type.IsSubclassOf(machineType));

        /// <summary>
        /// Checks if the constructor of the machine constructor for the
        /// specified machine type exists in the cache.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Boolean</returns>
        protected override bool IsMachineConstructorCached(Type type) => MachineFactory.IsCached(type);

        #endregion

        #region specifications and error checking

        /// <summary>
        /// Checks that the specified machine method was invoked properly.
        /// </summary>
        /// <param name="caller">The caller machine.</param>
        /// <param name="method">The invoked machine method.</param>
        protected override void CheckMachineMethodInvocation(BaseMachine caller, string method)
        {
            if (caller == null)
            {
                return;
            }

            var executingMachine = this.GetCurrentMachine();
            if (executingMachine == null)
            {
                return;
            }

            // Check that the caller is a supported machine type (if it is not the environment).
            this.Assert(this.IsSupportedMachineType(caller.GetType()), "Object '{0}' invoked method '{1}' without being a machine.",
                caller.Id, method);

            // Asserts that the machine calling a P# machine method is also
            // the machine that is currently executing.
            this.Assert(executingMachine.Equals(caller), "Machine '{0}' invoked method '{1}' on behalf of machine '{2}'.",
                executingMachine.Id, method, caller.Id);

            switch (method)
            {
                case MachineApiNames.CreateMachineApiName:
                case MachineApiNames.SendEventApiName:
                    this.AssertNoPendingTransitionStatement(caller, method);
                    break;

                case MachineApiNames.CreateMachineAndExecuteApiName:
                    this.Assert(caller != null, "Only a machine can execute 'CreateMachineAndExecute'. Avoid calling " +
                        "directly from the PSharp Test method. Instead call through a 'harness' machine.");
                    this.Assert(caller is Machine, "Only a machine of type '{0}' can execute 'CreateMachineAndExecute'.",
                        typeof(Machine).FullName);
                    this.AssertNoPendingTransitionStatement(caller, method);
                    break;

                case MachineApiNames.SendEventAndExecuteApiName:
                    this.Assert(caller != null, "Only a machine can execute 'SendEventAndExecute'. Avoid calling " +
                        "directly from the PSharp Test method. Instead call through a 'harness' machine.");
                    this.Assert(caller is Machine, "Only a machine of type '{0}' can execute 'SendEventAndExecute'.",
                        typeof(Machine).FullName);
                    this.AssertNoPendingTransitionStatement(caller, method);
                    break;

                case MachineApiNames.RaiseEventApiName:
                case MachineApiNames.PopStateApiName:
                    this.AssertTransitionStatement(caller);
                    break;

                case MachineApiNames.MonitorEventApiName:
                case MachineApiNames.RandomApiName:
                case MachineApiNames.RandomIntegerApiName:
                case MachineApiNames.FairRandomApiName:
                case MachineApiNames.ReceiveEventApiName:
                    this.AssertNoPendingTransitionStatement(caller, method);
                    break;

                default:
                    this.Assert(false, "Machine '{0}' invoked unexpected method '{1}'.", executingMachine.Id, method);
                    break;
            }
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop) has not
        /// already been called. Records that RGP has been called.
        /// </summary>
        /// <param name="caller">The caller machine.</param>
        private void AssertTransitionStatement(BaseMachine caller)
        {
            this.Assert(!caller.Info.IsInsideOnExit, "Machine '{0}' has called raise, goto, push or pop " +
                "inside an OnExit method.", caller.Id.Name);
            this.Assert(!caller.Info.CurrentActionCalledTransitionStatement, "Machine '{0}' has called multiple " +
                "raise, goto, push or pop in the same action.", caller.Id.Name);
            caller.Info.CurrentActionCalledTransitionStatement = true;
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop)
        /// has not already been called.
        /// </summary>
        /// <param name="caller">The caller machine.</param>
        /// <param name="method">The invoked machine method.</param>
        private void AssertNoPendingTransitionStatement(BaseMachine caller, string method)
        {
            this.Assert(!caller.Info.CurrentActionCalledTransitionStatement, "Machine '{0}' cannot call '{1}' " +
                "after calling raise, goto, push or pop in the same action.", caller.Id.Name, method);
        }

        #endregion

        #region timers

        /// <summary>
        /// Return the timer machine type
        /// </summary>
        /// <returns></returns>
        protected internal override Type GetTimerMachineType()
        {
            var timerType = base.GetTimerMachineType();
            if (timerType == null)
            {
                return typeof(Timers.ModelTimerMachine);
            }

            return timerType;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">The machine.</param>
        protected internal override void NotifyReceiveCalled(Machine machine)
        {
            this.CheckMachineMethodInvocation(machine, MachineApiNames.ReceiveEventApiName);
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        protected internal override void NotifyWaitEvents(Machine machine, EventInfo eventInfoInInbox)
        {
            if (eventInfoInInbox == null)
            {
                string events = machine.GetEventWaitHandlerNames();
                this.BugTrace.AddWaitToReceiveStep(machine.Id, machine.CurrentStateName, events);
                this.Logger.OnWait(machine.Id, machine.CurrentStateName, events);
                machine.Info.IsWaitingToReceive = true;
                (machine.Info as SchedulableInfo).IsEnabled = false;
            }
            else
            {
                (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong)eventInfoInInbox.SendStep;

                // The event was already in the inbox when we executed a receive action.
                // We've dequeued it by this point.
                if (this.Configuration.EnableDataRaceDetection)
                {
                    Reporter.RegisterDequeue(eventInfoInInbox.OriginInfo?.SenderMachineId, machine.Id,
                        eventInfoInInbox.Event, (ulong)eventInfoInInbox.SendStep);
                }
            }

            this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        protected internal override void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            this.BugTrace.AddReceivedEventStep(machine.Id, machine.CurrentStateName, eventInfo);
            this.Logger.OnReceive(machine.Id, machine.CurrentStateName, eventInfo.EventName, wasBlocked: true);

            // A subsequent enqueue from m' unblocked the receive action of machine.
            if (this.Configuration.EnableDataRaceDetection)
            {
                Reporter.RegisterDequeue(eventInfo.OriginInfo?.SenderMachineId, machine.Id, eventInfo.Event, (ulong)eventInfo.SendStep);
            }

            machine.Info.IsWaitingToReceive = false;
            (machine.Info as SchedulableInfo).IsEnabled = true;
            (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong)eventInfo.SendStep;

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfReceivedEvent(machine, eventInfo);
            }
        }

        #endregion
    }
}
