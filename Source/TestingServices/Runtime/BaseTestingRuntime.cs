//-----------------------------------------------------------------------
// <copyright file="BaseTestingRuntime.cs">
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// The base P# testing runtime.
    /// </summary>
    internal abstract class BaseTestingRuntime : BaseRuntime, ITestingRuntime
    {
        /// <summary>
        /// The asynchronous task scheduler.
        /// </summary>
        internal readonly AsynchronousTaskScheduler TaskScheduler;

        /// <summary>
        /// Interface for registering runtime operations.
        /// </summary>
        internal readonly IRegisterRuntimeOperation Reporter;

        /// <summary>
        /// The P# program state cache.
        /// </summary>
        internal readonly StateCache StateCache;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private readonly List<Monitor> Monitors;

        /// <summary>
        /// Map from task ids to machine data.
        /// </summary>
        protected readonly ConcurrentDictionary<int, (IMachineId mid, SchedulableInfo info)> TaskMap;

        /// <summary>
        /// Set of all machine Ids created by this runtime.
        /// </summary>
        internal readonly HashSet<IMachineId> CreatedMachineIds;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal readonly int? RootTaskId;

        /// <summary>
        /// The scheduler used to serialize the execution of
        /// the program, and explore schedules to find bugs.
        /// </summary>
        public BugFindingScheduler Scheduler { get; }

        /// <summary>
        /// The P# program schedule trace.
        /// </summary>
        public ScheduleTrace ScheduleTrace { get; }

        /// <summary>
        /// The bug trace.
        /// </summary>
        public BugTrace BugTrace { get; }

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        public CoverageInfo CoverageInfo { get; }

        /// <summary>
        /// True if testing mode is enabled, else false.
        /// </summary>
        public override bool IsTestingModeEnabled => true;

        /// <summary>
        /// Constructor.
        /// <param name="strategy">The scheduling strategy to use during exploration.</param>
        /// <param name="reporter">Reporter to register runtime operations.</param>
        /// <param name="logger">The logger to install.</param>
        /// <param name="configuration">The configuration to use during runtime.</param>
        /// </summary>
        protected BaseTestingRuntime(ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter, IO.ILogger logger, Configuration configuration)
            : base(logger, configuration)
        {
            this.Monitors = new List<Monitor>();
            this.TaskMap = new ConcurrentDictionary<int, (IMachineId, SchedulableInfo)>();
            this.RootTaskId = Task.CurrentId;
            this.CreatedMachineIds = new HashSet<IMachineId>();

            this.ScheduleTrace = new ScheduleTrace();
            this.BugTrace = new BugTrace();
            this.StateCache = new StateCache(this);

            this.TaskScheduler = new AsynchronousTaskScheduler(this, this.TaskMap);
            this.CoverageInfo = new CoverageInfo();
            this.Reporter = reporter;

            if (!(strategy is DPORStrategy) && !(strategy is ReplayStrategy))
            {
                var reductionStrategy = BasicReductionStrategy.ReductionStrategy.None;
                if (configuration.ReductionStrategy == Utilities.ReductionStrategy.OmitSchedulingPoints)
                {
                    reductionStrategy = BasicReductionStrategy.ReductionStrategy.OmitSchedulingPoints;
                }
                else if (configuration.ReductionStrategy == Utilities.ReductionStrategy.ForceSchedule)
                {
                    reductionStrategy = BasicReductionStrategy.ReductionStrategy.ForceSchedule;
                }

                strategy = new BasicReductionStrategy(strategy, reductionStrategy);
            }

            if (configuration.EnableLivenessChecking && configuration.EnableCycleDetection)
            {
                this.Scheduler = new BugFindingScheduler(this, new CycleDetectionStrategy(
                    configuration, this.StateCache, this.ScheduleTrace, this.Monitors, strategy));
            }
            else if (configuration.EnableLivenessChecking)
            {
                this.Scheduler = new BugFindingScheduler(this, new TemperatureCheckingStrategy(
                    configuration, this.Monitors, strategy));
            }
            else
            {
                this.Scheduler = new BugFindingScheduler(this, strategy);
            }
        }

        #region testing runtime interface

        /// <summary>
        /// Runs a test harness that executes the specified test method.
        /// </summary>
        /// <param name="testMethod">The test method.</param>
        public virtual void RunTestHarness(MethodInfo testMethod)
        {
            this.Assert(Task.CurrentId != null, "The test harness machine must execute inside a task.");
            this.Assert(testMethod != null, "The test harness machine cannot execute a null test method.");
            MachineId mid = this.CreateMachineId(typeof(TestHarnessMachine));
            TestHarnessMachine harness = new TestHarnessMachine(testMethod);
            harness.Initialize(this, this, mid, new SchedulableInfo(mid, typeof(TestHarnessMachine)));
            this.RunTestHarness(harness);
        }

        /// <summary>
        /// Runs a test harness that executes the specified test action.
        /// </summary>
        /// <param name="testAction">The test action.</param>
        public virtual void RunTestHarness(Action<IMachineRuntime> testAction)
        {
            this.Assert(Task.CurrentId != null, "The test harness machine must execute inside a task.");
            this.Assert(testAction != null, "The test harness machine cannot execute a null test action.");
            MachineId mid = this.CreateMachineId(typeof(TestHarnessMachine));
            TestHarnessMachine harness = new TestHarnessMachine(testAction);
            harness.Initialize(this, this, mid, new SchedulableInfo(mid, typeof(TestHarnessMachine)));
            this.RunTestHarness(harness);
        }

        /// <summary>
        /// Checks that no monitor is in a hot state upon program termination.
        /// If the program is still running, then this method returns without
        /// performing a check.
        /// </summary>
        void ITestingRuntime.CheckNoMonitorInHotStateAtTermination()
        {
            if (!this.Scheduler.HasFullyExploredSchedule)
            {
                return;
            }

            foreach (var monitor in this.Monitors)
            {
                if (monitor.IsInHotState(out string stateName))
                {
                    string message = IO.Utilities.Format("Monitor '{0}' detected liveness bug " +
                        "in hot state '{1}' at the end of program execution.",
                        monitor.GetType().Name, stateName);
                    this.Scheduler.NotifyAssertionFailure(message, false);
                }
            }
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        void ITestingRuntime.Wait()
        {
            this.Scheduler.Wait();
            this.IsRunning = false;
        }

        #endregion

        #region machine creation and execution

        /// <summary>
        /// Runs the specified test harness.
        /// </summary>
        /// <param name="harness">The test harness machine.</param>
        protected void RunTestHarness(TestHarnessMachine harness)
        {
            Task task = new Task(() =>
            {
                try
                {
                    this.Scheduler.NotifyEventHandlerStarted(harness.Info as SchedulableInfo);

                    harness.Run();

                    IO.Debug.WriteLine($"<ScheduleDebug> Completed event handler of the test harness machine.");
                    (harness.Info as SchedulableInfo).NotifyEventHandlerCompleted();
                    this.Scheduler.Schedule(OperationType.Stop, OperationTargetType.Schedulable, harness.Info.Id);
                    IO.Debug.WriteLine($"<ScheduleDebug> Exit event handler of the test harness machine.");
                }
                catch (Exception ex)
                {
                    if (ex is ExecutionCanceledException || ex.InnerException is ExecutionCanceledException)
                    {
                        IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown in the test harness.");
                    }
                    else
                    {
                        harness.ReportUnhandledException(ex);
                    }
                }
            });

            (harness.Info as SchedulableInfo).NotifyEventHandlerCreated(task.Id, 0);
            this.Scheduler.NotifyEventHandlerCreated(harness.Info as SchedulableInfo);

            task.Start();

            this.Scheduler.WaitForEventHandlerToStart(harness.Info as SchedulableInfo);
        }

        /// <summary>
        /// Creates a new P# machine using the specified unbound <see cref="MachineId"/> and type.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine.</returns>
        protected async Task<IMachine> CreateMachineAsync(MachineId mid, Type type)
        {
            Machine machine = MachineFactory.Create(type);
            await machine.InitializeAsync(this, mid, new SchedulableInfo(mid, type));
            return machine;
        }

        /// <summary>
        /// Creates a new machine of the specified type and with
        /// the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAsync(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAsync(type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and
        /// with the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAsync(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            IMachine creator = this.GetCurrentMachine();
            return this.CreateMachineAsync(null, type, friendlyName, e, operationGroupId, creator?.Id, creator?.Info, creator?.CurrentStateName ?? String.Empty);
        }

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAsync(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            this.Assert(mid != null, "Cannot pass a null MachineId.");
            IMachine creator = this.GetCurrentMachine();
            return this.CreateMachineAsync(mid, type, mid.FriendlyName, e, operationGroupId, creator?.Id, creator?.Info, creator?.CurrentStateName ?? String.Empty);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="operationGroupId">Optional operation group id.</param>
        /// <param name="e">Event</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid? operationGroupId = null)
        {
            IMachine creator = this.GetCurrentMachine();
            return this.CreateMachineAndExecuteAsync(null, type, null, e, creator, operationGroupId);
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
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            IMachine creator = this.GetCurrentMachine();
            return this.CreateMachineAndExecuteAsync(null, type, friendlyName, e, creator, operationGroupId);
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
        public override Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            this.Assert(mid != null, "Cannot pass a null MachineId.");
            IMachine creator = this.GetCurrentMachine();
            return this.CreateMachineAndExecuteAsync(mid, type, null, e, creator, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="e">Event passed during machine construction.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="creatorId">The id of the creator machine, if any.</param>
        /// <param name="creatorInfo">The metadata of the creator machine.</param>
        /// <param name="creatorStateName">The state name of the creator machine.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        public override async Task<MachineId> CreateMachineAsync(MachineId mid, Type type, string friendlyName, Event e,
            Guid? operationGroupId, IMachineId creatorId, MachineInfo creatorInfo, string creatorStateName)
        {
            this.Assert(this.IsSupportedMachineType(type), "Type '{0}' is not a machine.", type.Name);
            this.CheckMachineMethodInvocation(creatorId, creatorInfo, MachineApiNames.CreateMachineApiName);

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

            IMachine machine = await this.CreateMachineAsync(mid, type, friendlyName);
            this.Logger.OnCreateMachine(mid, creatorId);

            if (this.Configuration.EnableDataRaceDetection)
            {
                Reporter.RegisterCreateMachine(creatorId, mid);
            }

            this.SetOperationGroupIdForMachine(creatorInfo, machine.Info, operationGroupId);

            this.BugTrace.AddCreateMachineStep(creatorId, creatorStateName, machine.Id, e == null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, null, null);

            return machine.Id;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <param name="e">Event passed during machine construction.</param>
        /// <param name="creator">The creator machine.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="MachineId"/>.</returns>
        private async Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, string friendlyName,
            Event e, IMachine creator, Guid? operationGroupId)
        {
            this.Assert(this.IsSupportedMachineType(type), "Type '{0}' is not a machine.", type.Name);
            this.Assert(creator != null, "Only a machine can execute 'CreateMachineAndExecute'. Avoid calling " +
                "directly from the PSharp Test method. Instead call through a 'harness' machine.");
            this.CheckMachineMethodInvocation(creator.Id, creator.Info, MachineApiNames.CreateMachineAndExecuteApiName);

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

            IMachine machine = await this.CreateMachineAsync(mid, type, friendlyName);
            this.Logger.OnCreateMachine(mid, creator.Id);

            if (this.Configuration.EnableDataRaceDetection)
            {
                Reporter.RegisterCreateMachine(creator.Id, mid);
            }

            this.SetOperationGroupIdForMachine(creator.Info, machine.Info, operationGroupId);

            this.BugTrace.AddCreateMachineStep(creator.Id, creator.CurrentStateName, machine.Id, e == null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, creator.Id, null);

            // wait until the machine reaches quiescence
            await (creator as Machine).Receive(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).mid == machine.Id);

            return await Task.FromResult(machine.Id);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Friendly machine name used for logging.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine.</returns>
        protected async Task<IMachine> CreateMachineAsync(MachineId mid, Type type, string friendlyName)
        {
            if (mid == null)
            {
                mid = this.CreateMachineId(type, friendlyName);
            }
            else
            {
                this.Assert(mid.RuntimeProxy == null || mid.RuntimeProxy == this, "Unbound machine id '{0}' was created by another runtime.",
                    mid.Value);
                this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                    mid.Value, mid.Type, type.FullName);
                mid.Bind(this);
            }

            var isMachineTypeCached = this.IsMachineConstructorCached(type);
            IMachine machine = await this.CreateMachineAsync(mid, type);

            if (this.Configuration.ReportActivityCoverage && !isMachineTypeCached)
            {
                this.ReportActivityCoverageOfMachine(machine.GetType(), machine.GetAllStates(), machine.GetAllStateEventPairs());
            }

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "Machine id '{0}' is already bound to an existing machine.", mid.Value);

            this.Assert(!this.CreatedMachineIds.Contains(mid), "Machine id '{0}' of a previously halted machine cannot be reused " +
                "to create a new machine of type '{1}'.", mid.Value, type.FullName);
            this.CreatedMachineIds.Add(mid);

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public override Task SendEventAsync(MachineId target, Event e, SendOptions options = null)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            var sender = this.GetCurrentMachine();
            return this.SendEventAsync(target, e, options, sender?.Id, sender?.Info, sender?.CurrentState, sender?.CurrentStateName ?? String.Empty);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <param name="senderId">The id of the sender machine.</param>
        /// <param name="senderInfo">The metadata of the sender machine.</param>
        /// <param name="senderState">The state of the sender machine.</param>
        /// <param name="senderStateName">The state name of the sender machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public override async Task SendEventAsync(MachineId mid, Event e, SendOptions options, IMachineId senderId, MachineInfo senderInfo,
            Type senderState, string senderStateName)
        {
            this.CheckMachineMethodInvocation(senderId, senderInfo, MachineApiNames.SendEventApiName);
            this.Assert(this.CreatedMachineIds.Contains(mid), "Cannot send event '{0}' to unbound machine id '{1}'.", e.GetType().FullName, mid);

            this.Scheduler.Schedule(OperationType.Send, OperationTargetType.Inbox, mid.Value);
            var operationGroupId = this.GetNewOperationGroupId(senderInfo, options?.OperationGroupId);

            if (this.GetMachineFromId(mid, out IMachine machine))
            {
                MachineStatus machineStatus = await this.EnqueueEventAsync(machine, e, senderId, senderInfo, senderState, senderStateName,
                    operationGroupId, options?.MustHandle ?? false, out EventInfo eventInfo);
                if (machineStatus == MachineStatus.EventHandlerNotRunning)
                {
                    this.RunMachineEventHandler(machine, null, false, null, eventInfo);
                }
            }
            else
            {
                this.Logger.OnSend(mid, senderId, senderStateName, e.GetType().FullName, operationGroupId, isTargetHalted: true);
                this.Assert(options == null || !options.MustHandle,
                    $"A must-handle event '{e.GetType().Name}' was sent to the halted machine '{mid}'.");
            }
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
        public override Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, SendOptions options = null)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            return this.SendEventAndExecuteAsync(target, e, options, this.GetCurrentMachine());
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately
        /// if the target machine was already running. Otherwise blocks until the machine handles
        /// the event and reaches quiescense again.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        /// <param name="sender">The sender machine.</param>
        /// <returns>True if the event was handled, false if the event was only enqueued</returns>
        private async Task<bool> SendEventAndExecuteAsync(MachineId mid, Event e, SendOptions options, IMachine sender)
        {
            this.Assert(sender != null, "Only a machine can execute 'SendEventAndExecuteAsync'. Avoid calling " +
                "directly from the PSharp Test method. Instead call through a 'harness' machine.");
            this.CheckMachineMethodInvocation(sender.Id, sender.Info, MachineApiNames.SendEventAndExecuteApiName);
            this.Assert(this.CreatedMachineIds.Contains(mid), "Cannot send event '{0}' to unbound machine id '{1}'.", e.GetType().FullName, mid);

            this.Scheduler.Schedule(OperationType.Send, OperationTargetType.Inbox, mid.Value);
            var operationGroupId = this.GetNewOperationGroupId(sender.Info, options?.OperationGroupId);

            if (!this.GetMachineFromId(mid, out IMachine machine))
            {
                this.Logger.OnSend(mid, sender?.Id, sender?.CurrentStateName ?? String.Empty,
                    e.GetType().FullName, operationGroupId, isTargetHalted: true);
                this.Assert(options == null || !options.MustHandle,
                    $"A must-handle event '{e.GetType().FullName}' was sent to the halted machine '{mid}'.");
                return true;
            }

            MachineStatus machineStatus = await this.EnqueueEventAsync(machine, e, sender.Id, sender.Info, sender.CurrentState,
                sender.CurrentStateName, operationGroupId, options?.MustHandle ?? false, out EventInfo eventInfo);
            if (machineStatus == MachineStatus.EventHandlerNotRunning)
            {
                this.RunMachineEventHandler(machine, null, false, sender.Id, eventInfo);

                // wait until the machine reaches quiescence
                await (sender as Machine).Receive(typeof(QuiescentEvent),
                    rev => (rev as QuiescentEvent).mid == mid);
                return true;
            }

            // The 'MachineStatus.EventHandlerNotRunning' is not set to true by 'EnqueueEvent'
            // (even when the machine was previously inactive) when the event 'e' requires no
            // action by the machine (i.e., it implicitly handles the event).
            return machineStatus == MachineStatus.NextEventUnavailable;
        }

        /// <summary>
        /// Enqueues an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="e">Event</param>
        /// <param name="senderId">The id of the sender machine.</param>
        /// <param name="senderInfo">The metadata of the sender machine.</param>
        /// <param name="senderState">The state of the sender machine.</param>
        /// <param name="senderStateName">The state name of the sender machine.</param>
        /// <param name="operationGroupId">The operation group id.</param>
        /// <param name="mustHandle">MustHandle event</param>
        /// <param name="eventInfo">The enqueued event metadata.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the machine status after the enqueue.</returns>
        protected Task<MachineStatus> EnqueueEventAsync(IMachine machine, Event e, IMachineId senderId, MachineInfo senderInfo, Type senderState,
            string senderStateName, Guid operationGroupId, bool mustHandle, out EventInfo eventInfo)
        {
            EventOriginInfo originInfo = null;
            if (senderId != null)
            {
                originInfo = new EventOriginInfo(senderId, senderInfo.MachineType.Name,
                    senderState == null ? "None" : StateGroup.GetQualifiedStateName(senderState));
            }
            else
            {
                // Message comes from the environment.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            eventInfo = new EventInfo(e, originInfo, Scheduler.ScheduledSteps);
            eventInfo.SetOperationGroupId(operationGroupId);
            eventInfo.SetMustHandle(mustHandle);

            this.Logger.OnSend(machine.Id, senderId, senderStateName, e.GetType().FullName, operationGroupId, isTargetHalted: false);

            if (senderId != null)
            {
                this.BugTrace.AddSendEventStep(senderId, senderStateName, eventInfo, machine.Id);
                if (this.Configuration.EnableDataRaceDetection)
                {
                    this.Reporter.RegisterEnqueue(senderId, machine.Id, e, (ulong)Scheduler.ScheduledSteps);
                }
            }

            return machine.EnqueueAsync(eventInfo);
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">The machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        /// <param name="syncCaller">Caller machine that is blocked for quiescence.</param>
        /// <param name="enablingEvent">If non-null, the event info of the sent event that caused the event handler to be restarted.</param>
        protected void RunMachineEventHandler(IMachine machine, Event initialEvent, bool isFresh, MachineId syncCaller, EventInfo enablingEvent)
        {
            Task task = new Task(async () =>
            {
                try
                {
                    this.Scheduler.NotifyEventHandlerStarted(machine.Info as SchedulableInfo);

                    if (isFresh)
                    {
                        await machine.GotoStartStateAsync(initialEvent);
                    }

                    await machine.RunEventHandlerAsync();

                    if (syncCaller != null)
                    {
                        await this.SendEventAsync(syncCaller, new QuiescentEvent(machine.Id));
                    }

                    IO.Debug.WriteLine($"<ScheduleDebug> Completed event handler of '{machine.Id}'.");
                    (machine.Info as SchedulableInfo).NotifyEventHandlerCompleted();

                    if (machine.Info.IsHalted)
                    {
                        this.Scheduler.Schedule(OperationType.Stop, OperationTargetType.Schedulable, machine.Info.Id);
                    }
                    else
                    {
                        this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
                    }

                    IO.Debug.WriteLine($"<ScheduleDebug> Exit event handler of '{machine.Id}'.");
                }
                catch (Exception ex)
                {
                    if (ex is ExecutionCanceledException || ex.InnerException is ExecutionCanceledException)
                    {
                        IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from machine '{machine.Id}'.");
                    }
                }
                finally
                {
                    this.TaskMap.TryRemove(Task.CurrentId.Value, out (IMachineId, SchedulableInfo) data);
                }
            });

            this.TaskMap.TryAdd(task.Id, (machine.Id, machine.Info as SchedulableInfo));

            (machine.Info as SchedulableInfo).NotifyEventHandlerCreated(task.Id, enablingEvent?.SendStep ?? 0);
            this.Scheduler.NotifyEventHandlerCreated(machine.Info as SchedulableInfo);

            task.Start(this.TaskScheduler);

            this.Scheduler.WaitForEventHandlerToStart(machine.Info as SchedulableInfo);
        }

        /// <summary>
        /// Gets the id of the currently executing machine.
        /// </summary>
        /// <returns>The machine id, or null, if not present.</returns>
        public IMachineId GetCurrentMachineId()
        {
            return this.GetCurrentMachine()?.Id;
        }

        /// <summary>
        /// Gets the currently executing machine.
        /// </summary>
        /// <returns>The machine, or null if not present.</returns>
        private IMachine GetCurrentMachine()
        {
            //  The current task does not correspond to a machine.
            if (Task.CurrentId != null && this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                (IMachineId id, SchedulableInfo info) = this.TaskMap[(int)Task.CurrentId];
                if (id is MachineId mid && this.GetMachineFromId(mid, out IMachine machine))
                {
                    return machine;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the currently executing machine.
        /// </summary>
        /// <returns>The machine, or null if not present.</returns>
        Machine ITestingRuntime.GetCurrentMachine() => this.GetCurrentMachine() as Machine;

        #endregion

        #region specifications and error checking

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        public override void RegisterMonitor(Type type)
        {
            this.TryCreateMonitor(type);
        }

        /// <summary>
        /// Tries to create a new monitor of the given type.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        private void TryCreateMonitor(Type type)
        {
            this.Assert(type.IsSubclassOf(typeof(Monitor)), $"Type '{type.Name}' is not a subclass of Monitor.");

            MachineId mid = this.CreateMachineId(type);

            SchedulableInfo info = new SchedulableInfo(mid, type);
            Scheduler.NotifyMonitorRegistered(info);

            Monitor monitor = Activator.CreateInstance(type) as Monitor;
            monitor.Initialize(this, mid);
            monitor.InitializeStateInformation();

            this.Logger.OnCreateMonitor(type.Name, monitor.Id);

            this.ReportActivityCoverageOfMonitor(monitor);
            this.BugTrace.AddCreateMonitorStep(mid);

            this.Monitors.Add(monitor);

            monitor.GotoStartState();
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        public override void InvokeMonitor<T>(Event e)
        {
            this.InvokeMonitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="e">Event</param>
        public override void InvokeMonitor(Type type, Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor(type, null, null, null, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="callerState">The state of the caller machine.</param>
        /// <param name="e">Event sent to the monitor.</param>
        public override void Monitor(Type type, IMachineId callerId, MachineInfo callerInfo, Type callerState, Event e)
        {
            this.CheckMachineMethodInvocation(callerId, callerInfo, MachineApiNames.MonitorEventApiName);

            foreach (var m in this.Monitors)
            {
                if (m.GetType() == type)
                {
                    if (this.Configuration.ReportActivityCoverage)
                    {
                        this.ReportActivityCoverageOfMonitorEvent(callerInfo, callerState, m, e);
                        this.ReportActivityCoverageOfMonitorTransition(m, e);
                    }
                    if (this.Configuration.EnableDataRaceDetection)
                    {
                        this.Reporter.InMonitor = (long)m.Id.Value;
                    }

                    m.MonitorEvent(e);
                    if (this.Configuration.EnableDataRaceDetection)
                    {
                        this.Reporter.InMonitor = -1;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an
        /// <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public override void Assert(bool predicate)
        {
            if (!predicate)
            {
                string message = "Detected an assertion failure.";
                this.Scheduler.NotifyAssertionFailure(message);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an
        /// <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public override void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                string message = IO.Utilities.Format(s, args);
                this.Scheduler.NotifyAssertionFailure(message);
            }
        }

        /// <summary>
        /// Checks that the specified machine method was invoked properly.
        /// </summary>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="method">The called method.</param>
        protected void CheckMachineMethodInvocation(IMachineId callerId, MachineInfo callerInfo, string method)
        {
            if (callerId == null)
            {
                return;
            }

            var executingMachine = this.GetCurrentMachine();
            if (executingMachine == null)
            {
                return;
            }

            // Check that the caller is a supported machine type (if it is not the environment).
            this.Assert(this.IsSupportedMachineType(callerInfo.MachineType), "Object '{0}' invoked method '{1}' without being a machine.",
                callerId, method);

            // Asserts that the machine calling a P# machine method is also
            // the machine that is currently executing.
            this.Assert(executingMachine.Id.Equals(callerId), "Machine '{0}' invoked method '{1}' on behalf of machine '{2}'.",
                executingMachine.Id, method, callerId);

            switch (method)
            {
                case MachineApiNames.CreateMachineApiName:
                case MachineApiNames.SendEventApiName:
                    this.AssertNoPendingTransitionStatement(callerId, callerInfo, method);
                    break;

                case MachineApiNames.CreateMachineAndExecuteApiName:
                    this.Assert(callerInfo.MachineType.IsSubclassOf(typeof(Machine)), "Only a machine of type '{0}' can execute 'CreateMachineAndExecute'.",
                        typeof(Machine).FullName);
                    this.AssertNoPendingTransitionStatement(callerId, callerInfo, method);
                    break;

                case MachineApiNames.SendEventAndExecuteApiName:
                    this.Assert(callerInfo.MachineType.IsSubclassOf(typeof(Machine)), "Only a machine of type '{0}' can execute 'SendEventAndExecute'.",
                        typeof(Machine).FullName);
                    this.AssertNoPendingTransitionStatement(callerId, callerInfo, method);
                    break;

                case MachineApiNames.RaiseEventApiName:
                case MachineApiNames.PopStateApiName:
                    this.AssertTransitionStatement(callerId, callerInfo);
                    break;

                case MachineApiNames.MonitorEventApiName:
                case MachineApiNames.RandomApiName:
                case MachineApiNames.RandomIntegerApiName:
                case MachineApiNames.FairRandomApiName:
                case MachineApiNames.ReceiveEventApiName:
                    this.AssertNoPendingTransitionStatement(callerId, callerInfo, method);
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
        /// <param name="mid">The id of the machine.</param>
        /// <param name="info">The metadata of the machine.</param>
        private void AssertTransitionStatement(IMachineId mid, MachineInfo info)
        {
            this.Assert(!info.IsInsideOnExit, "Machine '{0}' has called raise, goto, push or pop " +
                "inside an OnExit method.", mid.Name);
            this.Assert(!info.CurrentActionCalledTransitionStatement, "Machine '{0}' has called multiple " +
                "raise, goto, push or pop in the same action.", mid.Name);
            info.CurrentActionCalledTransitionStatement = true;
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop)
        /// has not already been called.
        /// </summary>
        /// <param name="mid">The id of the machine.</param>
        /// <param name="info">The metadata of the machine.</param>
        /// <param name="method">The invoked machine method.</param>
        private void AssertNoPendingTransitionStatement(IMachineId mid, MachineInfo info, string method)
        {
            this.Assert(!info.CurrentActionCalledTransitionStatement, "Machine '{0}' cannot call '{1}' " +
                "after calling raise, goto, push or pop in the same action.", mid.Name, method);
        }

        #endregion

        #region nondeterministic choices

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="callerStateName">The state name of the caller machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        public override bool GetNondeterministicBooleanChoice(IMachineId callerId, MachineInfo callerInfo, string callerStateName, int maxValue)
        {
            this.CheckMachineMethodInvocation(callerId, callerInfo, MachineApiNames.RandomApiName);

            if (callerInfo != null)
            {
                callerInfo.ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            this.BugTrace.AddRandomChoiceStep(callerId, callerStateName, choice);
            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="callerStateName">The state name of the caller machine.</param>
        /// <param name="uniqueId">Unique id.</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        public override bool GetFairNondeterministicBooleanChoice(IMachineId callerId, MachineInfo callerInfo, string callerStateName, string uniqueId)
        {
            this.CheckMachineMethodInvocation(callerId, callerInfo, MachineApiNames.FairRandomApiName);

            if (callerInfo != null)
            {
                callerInfo.ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(2, uniqueId);
            this.BugTrace.AddRandomChoiceStep(callerId, callerStateName, choice);
            return choice;
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="callerId">The id of the caller machine.</param>
        /// <param name="callerInfo">The metadata of the caller machine.</param>
        /// <param name="callerStateName">The state name of the caller machine.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic integer choice.</returns>
        public override int GetNondeterministicIntegerChoice(IMachineId callerId, MachineInfo callerInfo, string callerStateName, int maxValue)
        {
            this.CheckMachineMethodInvocation(callerId, callerInfo, MachineApiNames.RandomIntegerApiName);
            var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            this.BugTrace.AddRandomChoiceStep(callerId, callerStateName, choice);
            return choice;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        public override void NotifyEnteredState(IMachine machine)
        {
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddGotoStateStep(machine.Id, machineState);
            this.Logger.OnMachineState(machine.Id, machineState, isEntry: true);
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        public override void NotifyEnteredState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.BugTrace.AddGotoStateStep(monitor.Id, monitorState);
            this.Logger.OnMonitorState(monitor.GetType().Name, monitor.Id, monitorState, true, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        public override void NotifyExitedState(IMachine machine)
        {
            this.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: false);
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        public override void NotifyExitedState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.Logger.OnMonitorState(monitor.GetType().Name, monitor.Id, monitorState, false, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine is performing a 'goto' transition to the specified state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="newStateName">The target state.</param>
        public override void NotifyGotoState(IMachine machine, string currStateName, string newStateName)
        {
            this.Logger.OnGoto(machine.Id, currStateName, newStateName);
        }

        /// <summary>
        /// Notifies that a machine is performing a 'goto' transition to the specified state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="newStateName">The target state.</param>
        public override void NotifyPushState(IMachine machine, string currStateName, string newStateName)
        {
            this.Logger.OnPush(machine.Id, currStateName, newStateName);
        }

        /// <summary>
        /// Notifies that a machine is performing a 'pop' transition from the current state.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="restoredStateName">The name of the state being restored, if any.</param>
        public override void NotifyPopState(IMachine machine, string currStateName, string restoredStateName)
        {
            this.Logger.OnPop(machine.Id, currStateName, restoredStateName);
        }

        /// <summary>
        /// Notifies that a machine popped its state because it cannot handle the current event.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current state, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public override void NotifyPopUnhandledEvent(IMachine machine, string currStateName, string eventName)
        {
            this.Logger.OnPopUnhandledEvent(machine.Id, currStateName, eventName);
        }

        /// <summary>
        /// Notifies that a machine invoked the 'pop' state action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="fromState">Top of the stack state.</param>
        /// <param name="toState">Next to top state of the stack.</param>
        public override void NotifyPopAction(IMachine machine, Type fromState, Type toState)
        {
            this.CheckMachineMethodInvocation(machine.Id, machine.Info, "Pop");
            this.Logger.OnPop(machine.Id, machine.CurrentStateName, String.Empty);
            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfPopTransition(machine.GetType(), fromState, toState);
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        public override void NotifyInvokedAction(IMachine machine, MethodInfo action, Event receivedEvent)
        {
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(machine.Id, machineState, action);
            this.Logger.OnMachineAction(machine.Id, machineState, action.Name);
            if (this.Configuration.EnableDataRaceDetection)
            {
                Reporter.InAction[machine.Id.Value] = true;
            }
        }

        /// <summary>
        /// Notifies that a machine completed an action.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        public override void NotifyCompletedAction(IMachine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.EnableDataRaceDetection)
            {
                Reporter.InAction[machine.Id.Value] = false;
            }
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        public override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            string monitorState = monitor.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(monitor.Id, monitorState, action);
            this.Logger.OnMonitorAction(monitor.GetType().Name, monitor.Id, action.Name, monitorState);
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        public override void NotifyRaisedEvent(IMachine machine, EventInfo eventInfo)
        {
            this.CheckMachineMethodInvocation(machine.Id, machine.Info, MachineApiNames.RaiseEventApiName);
            eventInfo.SetOperationGroupId(this.GetNewOperationGroupId(machine.Info, null));
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddRaiseEventStep(machine.Id, machineState, eventInfo);
            this.Logger.OnMachineEvent(machine.Id, machineState, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="eventInfo">The event metadata.</param>
        public override void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo)
        {
            string monitorState = monitor.CurrentStateName;
            this.BugTrace.AddRaiseEventStep(monitor.Id, monitorState, eventInfo);
            this.Logger.OnMonitorEvent(monitor.GetType().Name, monitor.Id, monitor.CurrentStateName,
                eventInfo.EventName, isProcessing: false);
        }

        /// <summary>
        /// Notifies that a machine is handling a raised event.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        public override void NotifyHandleRaisedEvent(IMachine machine, EventInfo eventInfo)
        {
            if (this.Configuration.ReportActivityCoverage)
            {
                var transition = machine.GetCurrentStateTransition(eventInfo);
                if (transition.machine.Length > 0)
                {
                    this.CoverageInfo.AddTransition(transition.machine, transition.originState, transition.edgeLabel,
                        transition.machine, transition.destState);
                }
            }
        }

        /// <summary>
        /// Notifies that a machine dequeued an event.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfo">The event metadata.</param>
        public override void NotifyDequeuedEvent(IMachine machine, EventInfo eventInfo)
        {
            // The machine inherits the operation group id of the dequeued event.
            machine.Info.OperationGroupId = eventInfo.OperationGroupId;

            // Skip `Receive` if the last operation exited the previous event handler,
            // to avoid scheduling duplicate `Receive` operations.
            if ((machine.Info as SchedulableInfo).SkipNextReceiveSchedulingPoint)
            {
                (machine.Info as SchedulableInfo).SkipNextReceiveSchedulingPoint = false;
            }
            else
            {
                (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong)eventInfo.SendStep;
                this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
            }

            this.Logger.OnDequeue(machine.Id, machine.CurrentStateName, eventInfo.EventName);

            if (this.Configuration.EnableDataRaceDetection)
            {
                Reporter.RegisterDequeue(eventInfo.OriginInfo?.SenderMachineId, machine.Id, eventInfo.Event,
                    (ulong)eventInfo.SendStep);
            }

            this.BugTrace.AddDequeueEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfReceivedEvent(machine.GetType(), machine.CurrentState, eventInfo);

                var transition = machine.GetCurrentStateTransition(eventInfo);
                if (transition.machine.Length > 0)
                {
                    this.CoverageInfo.AddTransition(transition.machine, transition.originState, transition.edgeLabel,
                        transition.machine, transition.destState);
                }
            }
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">The machine.</param>
        public override void NotifyReceiveCalled(IMachine machine)
        {
            this.CheckMachineMethodInvocation(machine.Id, machine.Info, MachineApiNames.ReceiveEventApiName);
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        /// <param name="eventNames">The names of the events that the machine is waiting for.</param>
        public override void NotifyWaitEvents(IMachine machine, EventInfo eventInfoInInbox, string eventNames)
        {
            if (eventInfoInInbox == null)
            {
                this.BugTrace.AddWaitToReceiveStep(machine.Id, machine.CurrentStateName, eventNames);
                this.Logger.OnWait(machine.Id, machine.CurrentStateName, eventNames);
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
        public override void NotifyReceivedEvent(IMachine machine, EventInfo eventInfo)
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
                this.ReportActivityCoverageOfReceivedEvent(machine.GetType(), machine.CurrentState, eventInfo);
            }
        }

        /// <summary>
        /// Notifies that the inbox of the specified machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        public override void NotifyDefaultEventHandlerCheck(IMachine machine)
        {
            this.Scheduler.Schedule(OperationType.Send, OperationTargetType.Inbox, machine.Info.Id);
            // If the default event handler fires, the next receive in NotifyDefaultHandlerFired
            // will use this as its NextOperationMatchingSendIndex.
            // If it does not fire, NextOperationMatchingSendIndex will be overwritten.
            (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong)this.Scheduler.ScheduledSteps;
        }

        /// <summary>
        /// Notifies that the default handler of the specified machine has been fired.
        /// </summary>
        /// <param name="machine">The machine.</param>
        public override void NotifyDefaultHandlerFired(IMachine machine)
        {
            // NextOperationMatchingSendIndex is set in NotifyDefaultEventHandlerCheck.
            this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public override Task NotifyHaltedAsync(IMachine machine)
        {
            this.BugTrace.AddHaltStep(machine.Id, null);
            this.MachineMap.TryRemove(machine.Id, out machine);
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Notifies that a machine is throwing an exception.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public override void NotifyMachineExceptionThrown(IMachine machine, string currStateName, string actionName, Exception ex)
        {
            this.Logger.OnMachineExceptionThrown(machine.Id, currStateName, actionName, ex);
        }

        /// <summary>
        /// Notifies that a machine is using 'OnException' to handle a thrown exception.
        /// </summary>
        /// <param name="machine">The machine.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public override void NotifyMachineExceptionHandled(IMachine machine, string currStateName, string actionName, Exception ex)
        {
            this.Logger.OnMachineExceptionHandled(machine.Id, currStateName, actionName, ex);
        }

        #endregion

        #region code coverage

        /// <summary>
        /// Reports coverage for the specified received event.
        /// </summary>
        /// <param name="machineType">The machine type.</param>
        /// <param name="currState">The current machine state.</param>
        /// <param name="eventInfo">The event metadata.</param>
        protected void ReportActivityCoverageOfReceivedEvent(Type machineType, Type currState, EventInfo eventInfo)
        {
            string originMachine = eventInfo.OriginInfo.SenderMachineName;
            string originState = eventInfo.OriginInfo.SenderStateName;
            string edgeLabel = eventInfo.EventType.Name;
            string destMachine = machineType.Name;
            string destState = StateGroup.GetQualifiedStateName(currState);
            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified monitor event.
        /// </summary>
        /// <param name="senderInfo">The metadata of the sender machine.</param>
        /// <param name="senderState">The state of the sender machine.</param>
        /// <param name="monitor">The monitor.</param>
        /// <param name="e">Event</param>
        private void ReportActivityCoverageOfMonitorEvent(MachineInfo senderInfo, Type senderState, Monitor monitor, Event e)
        {
            string originMachine = (senderInfo == null) ? "Env" : senderInfo.MachineType.Name;
            string originState = (senderInfo == null) ? "Env" : ((senderState == null) ? "None" :
                StateGroup.GetQualifiedStateName(senderState));
            string edgeLabel = e.GetType().Name;
            string destMachine = monitor.GetType().Name;
            string destState = StateGroup.GetQualifiedStateName(monitor.CurrentState);
            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified machine.
        /// </summary>
        /// <param name="machineType">The machine type.</param>
        /// <param name="allStates">All states of the machine.</param>
        /// <param name="allStateEventPairs">All state event pairs of the machine.</param>
        protected void ReportActivityCoverageOfMachine(Type machineType, HashSet<string> allStates, HashSet<(string state, string e)> allStateEventPairs)
        {
            var machineName = machineType.Name;
            foreach (var state in allStates)
            {
                this.CoverageInfo.DeclareMachineState(machineName, state);
            }

            foreach (var (state, e) in allStateEventPairs)
            {
                this.CoverageInfo.DeclareStateEvent(machineName, state, e);
            }
        }

        /// <summary>
        /// Reports coverage for the specified monitor.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        private void ReportActivityCoverageOfMonitor(Monitor monitor)
        {
            var monitorName = monitor.GetType().Name;

            // fetch states
            var states = monitor.GetAllStates();

            foreach (var state in states)
            {
                this.CoverageInfo.DeclareMachineState(monitorName, state);
            }

            // fetch registered events
            var pairs = monitor.GetAllStateEventPairs();

            foreach (var tup in pairs)
            {
                this.CoverageInfo.DeclareStateEvent(monitorName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Reports coverage for a pop transition.
        /// </summary>
        /// <param name="machineType">The type of the machine.</param>
        /// <param name="fromState">Top of the stack state.</param>
        /// <param name="toState">Next to top state of the stack.</param>
        protected void ReportActivityCoverageOfPopTransition(Type machineType, Type fromState, Type toState)
        {
            string originMachine = machineType.Name;
            string originState = StateGroup.GetQualifiedStateName(fromState);
            string destMachine = machineType.Name;
            string edgeLabel = "pop";
            string destState = StateGroup.GetQualifiedStateName(toState);
            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified state transition.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="e">Event</param>
        private void ReportActivityCoverageOfMonitorTransition(Monitor monitor, Event e)
        {
            string originMachine = monitor.GetType().Name;
            string originState = StateGroup.GetQualifiedStateName(monitor.CurrentState);
            string destMachine = originMachine;

            string edgeLabel = String.Empty;
            string destState = String.Empty;
            if (e is GotoStateEvent)
            {
                edgeLabel = "goto";
                destState = StateGroup.GetQualifiedStateName((e as GotoStateEvent).State);
            }
            else if (monitor.GotoTransitions.ContainsKey(e.GetType()))
            {
                edgeLabel = e.GetType().Name;
                destState = StateGroup.GetQualifiedStateName(
                    monitor.GotoTransitions[e.GetType()].TargetState);
            }
            else
            {
                return;
            }

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        #endregion

        #region utilities

        /// <summary>
        /// Returns the fingerprint of the current program state.
        /// </summary>
        /// <returns>Fingerprint</returns>
        internal Fingerprint GetProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                int hash = 19;

                foreach (var machine in this.MachineMap.Values.OrderBy(mi => mi.Id.Value))
                {
                    hash = hash * 31 + machine.GetCachedState();
                    hash = hash * 31 + (int)(machine.Info as SchedulableInfo).NextOperationType;
                }

                foreach (var monitor in this.Monitors)
                {
                    hash = hash * 31 + monitor.GetCachedState();
                }

                fingerprint = new Fingerprint(hash);
            }

            return fingerprint;
        }

        #endregion

        #region operation group id

        /// <summary>
        /// Returns the operation group id of the specified machine. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="IMachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        /// <param name="currentMachineId">The id of the currently executing machine.</param>
        /// <returns>Guid</returns>
        public override Guid GetCurrentOperationGroupId(IMachineId currentMachineId)
        {
            this.Assert(currentMachineId == this.GetCurrentMachineId(), "Trying to access the operation group id of " +
                $"'{currentMachineId}', which is not the currently executing machine.");

            if (!this.MachineMap.TryGetValue(currentMachineId, out IMachine machine))
            {
                return Guid.Empty;
            }

            return machine.Info.OperationGroupId;
        }

        #endregion

        #region exceptions

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public override void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            string message = IO.Utilities.Format(s, args);
            this.Scheduler.NotifyAssertionFailure(message);
        }

        #endregion

        #region cleanup

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public override void Dispose()
        {
            this.Monitors.Clear();
            this.TaskMap.Clear();
            base.Dispose();
        }

        #endregion
    }
}
