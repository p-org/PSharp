// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Timers;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Timers;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// Runtime for systematically testing machines by controlling the scheduler.
    /// </summary>
    internal sealed class TestingRuntime : BaseRuntime
    {
        /// <summary>
        /// The bug-finding scheduler.
        /// </summary>
        internal BugFindingScheduler Scheduler;

        /// <summary>
        /// The asynchronous task scheduler.
        /// </summary>
        internal AsynchronousTaskScheduler TaskScheduler;

        /// <summary>
        /// The P# program schedule trace.
        /// </summary>
        internal ScheduleTrace ScheduleTrace;

        /// <summary>
        /// The bug trace.
        /// </summary>
        internal BugTrace BugTrace;

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        internal CoverageInfo CoverageInfo;

        /// <summary>
        /// Interface for registering runtime operations.
        /// </summary>
        internal IRegisterRuntimeOperation Reporter;

        /// <summary>
        /// The P# program state cache.
        /// </summary>
        internal StateCache StateCache;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private readonly List<Monitor> Monitors;

        /// <summary>
        /// Map from task ids to machines.
        /// </summary>
        private readonly ConcurrentDictionary<int, Machine> TaskMap;

        /// <summary>
        /// Map that stores all unique names and their corresponding machine ids.
        /// </summary>
        internal readonly ConcurrentDictionary<string, MachineId> NameValueToMachineId;

        /// <summary>
        /// Set of all machine Ids created by this runtime.
        /// </summary>
        internal HashSet<MachineId> AllCreatedMachineIds;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal readonly int? RootTaskId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingRuntime"/> class.
        /// </summary>
        internal TestingRuntime(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration)
        {
            this.Monitors = new List<Monitor>();
            this.TaskMap = new ConcurrentDictionary<int, Machine>();
            this.RootTaskId = Task.CurrentId;
            this.AllCreatedMachineIds = new HashSet<MachineId>();
            this.NameValueToMachineId = new ConcurrentDictionary<string, MachineId>();

            this.ScheduleTrace = new ScheduleTrace();
            this.BugTrace = new BugTrace();
            this.StateCache = new StateCache(this);

            this.TaskScheduler = new AsynchronousTaskScheduler(this, this.TaskMap);
            this.CoverageInfo = new CoverageInfo();
            this.Reporter = reporter;

            if (!(strategy is DPORStrategy) && !(strategy is ReplayStrategy))
            {
                var reductionStrategy = BasicReductionStrategy.ReductionStrategy.None;
                if (configuration.ReductionStrategy is ReductionStrategy.OmitSchedulingPoints)
                {
                    reductionStrategy = BasicReductionStrategy.ReductionStrategy.OmitSchedulingPoints;
                }
                else if (configuration.ReductionStrategy is ReductionStrategy.ForceSchedule)
                {
                    reductionStrategy = BasicReductionStrategy.ReductionStrategy.ForceSchedule;
                }

                strategy = new BasicReductionStrategy(strategy, reductionStrategy);
            }

            if (configuration.EnableLivenessChecking && configuration.EnableCycleDetection)
            {
                this.Scheduler = new BugFindingScheduler(this,
                    new CycleDetectionStrategy(configuration, this.StateCache, this.ScheduleTrace, this.Monitors, strategy));
            }
            else if (configuration.EnableLivenessChecking)
            {
                this.Scheduler = new BugFindingScheduler(this,
                    new TemperatureCheckingStrategy(configuration, this.Monitors, strategy));
            }
            else
            {
                this.Scheduler = new BugFindingScheduler(this, strategy);
            }
        }

        /// <summary>
        /// Creates a machine id that is uniquely tied to the specified unique name. The
        /// returned machine id can either be a fresh id (not yet bound to any machine),
        /// or it can be bound to a previously created machine. In the second case, this
        /// machine id can be directly used to communicate with the corresponding machine.
        /// </summary>
        public override MachineId CreateMachineIdFromName(Type type, string machineName)
        {
            // It is important that all machine ids use the monotonically incrementing
            // value as the id during testing, and not the unique name.
            var mid = new MachineId(type, machineName, this);
            return this.NameValueToMachineId.GetOrAdd(machineName, mid);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(Type type, Event e = null, Guid? operationGroupId = null) =>
            this.CreateMachine(null, type, null, e, operationGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(Type type, string machineName, Event e = null, Guid? operationGroupId = null) =>
            this.CreateMachine(null, type, machineName, e, operationGroupId);

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            this.Assert(mid != null, "Cannot create a machine using a null machine id.");
            return this.CreateMachine(mid, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid? operationGroupId = null) =>
            this.CreateMachineAndExecuteAsync(null, type, null, e, operationGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, string machineName, Event e = null, Guid? operationGroupId = null) =>
            this.CreateMachineAndExecuteAsync(null, type, machineName, e, operationGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            this.Assert(mid != null, "Cannot create a machine using a null machine id.");
            return this.CreateMachineAndExecuteAsync(mid, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null) =>
            this.CreateMachineAndExecuteAsync(null, type, null, e, operationGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecute(Type type, string machineName, Event e = null, Guid? operationGroupId = null) =>
            this.CreateMachineAndExecuteAsync(null, type, machineName, e, operationGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            this.Assert(mid != null, "Cannot create a machine using a null machine id.");
            return this.CreateMachineAndExecuteAsync(mid, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        public override void SendEvent(MachineId target, Event e, SendOptions options = null)
        {
            this.SendEvent(target, e, this.GetCurrentMachine(), options);
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target
        /// machine was already running. Otherwise blocks until the machine handles the
        /// event and reaches quiescense again.
        /// </summary>
        public override Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(target, e, this.GetCurrentMachine(), options);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense again.
        /// </summary>
        public override Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(target, e, options);

        /// <summary>
        /// Returns the operation group id of the specified machine. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="MachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        public override Guid GetCurrentOperationGroupId(MachineId currentMachine)
        {
            this.Assert(currentMachine == this.GetCurrentMachineId(),
                "Trying to access the operation group id of '{0}', which is not the currently executing machine.",
                currentMachine);

            if (!this.MachineMap.TryGetValue(currentMachine, out Machine machine))
            {
                return Guid.Empty;
            }

            return machine.Info.OperationGroupId;
        }

        /// <summary>
        /// Runs the specified test method inside a test harness machine.
        /// </summary>
        internal void RunTestHarness(MethodInfo testMethod, Action<IMachineRuntime> testAction)
        {
            this.Assert(Task.CurrentId != null, "The test harness machine must execute inside a task.");
            this.Assert(testMethod != null || testAction != null, "The test harness machine cannot execute a null test method or action.");

            MachineId mid = new MachineId(typeof(TestHarnessMachine), null, this);
            TestHarnessMachine harness = new TestHarnessMachine(testMethod, testAction);

            harness.Initialize(this, mid, new SchedulableInfo(mid));

            Task task = new Task(() =>
            {
                try
                {
                    BugFindingScheduler.NotifyEventHandlerStarted(harness.Info as SchedulableInfo);

                    harness.Run();

                    IO.Debug.WriteLine($"<ScheduleDebug> Completed event handler of the test harness machine.");
                    (harness.Info as SchedulableInfo).NotifyEventHandlerCompleted();
                    this.Scheduler.Schedule(OperationType.Stop, OperationTargetType.Schedulable, harness.Info.Id);
                    IO.Debug.WriteLine($"<ScheduleDebug> Terminated event handler of the test harness machine.");
                }
                catch (ExecutionCanceledException)
                {
                    IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown in the test harness.");
                }
                catch (Exception ex)
                {
                    harness.ReportUnhandledException(ex);
                }
            });

            (harness.Info as SchedulableInfo).NotifyEventHandlerCreated(task.Id, 0);
            this.Scheduler.NotifyEventHandlerCreated(harness.Info as SchedulableInfo);

            task.Start();

            this.Scheduler.WaitForEventHandlerToStart(harness.Info as SchedulableInfo);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        internal MachineId CreateMachine(MachineId mid, Type type, string machineName, Event e = null, Guid? operationGroupId = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachine(mid, type, machineName, e, creator, operationGroupId);
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override MachineId CreateMachine(MachineId mid, Type type, string machineName, Event e, Machine creator, Guid? operationGroupId)
        {
            this.AssertCorrectCallerMachine(creator, "CreateMachine");
            if (creator != null)
            {
                this.AssertNoPendingTransitionStatement(creator, "CreateMachine");
            }

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

            Machine machine = this.CreateMachine(mid, type, machineName, creator);
            SetOperationGroupIdForMachine(machine, creator, operationGroupId);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e is null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, null, null);

            return machine.Id;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        internal Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, string machineName, Event e = null, Guid? operationGroupId = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachineAndExecuteAsync(mid, type, machineName, e, creator, operationGroupId);
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        internal override async Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, string machineName, Event e,
            Machine creator, Guid? operationGroupId)
        {
            this.AssertCorrectCallerMachine(creator, "CreateMachineAndExecute");
            this.Assert(creator != null,
                "Only a machine can call 'CreateMachineAndExecute': avoid calling it directly from the 'Test' method; instead call it through a 'harness' machine.");
            this.AssertNoPendingTransitionStatement(creator, "CreateMachine");

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

            Machine machine = this.CreateMachine(mid, type, machineName, creator);
            SetOperationGroupIdForMachine(machine, creator, operationGroupId);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e is null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, creator, null);

            // wait until the machine reaches quiescence
            await creator.Receive(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).MachineId == machine.Id);

            return await Task.FromResult(machine.Id);
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        private Machine CreateMachine(MachineId mid, Type type, string machineName, Machine creator)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), "Type '{0}' is not a machine.", type.FullName);

            if (mid is null)
            {
                mid = new MachineId(type, machineName, this);
            }
            else
            {
                this.Assert(mid.Runtime is null || mid.Runtime == this, "Unbound machine id '{0}' was created by another runtime.", mid.Value);
                this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                    mid.Value, mid.Type, type.FullName);
                mid.Bind(this);
            }

            var isMachineTypeCached = MachineFactory.IsCached(type);
            Machine machine = MachineFactory.Create(type);
            machine.Initialize(this, mid, new SchedulableInfo(mid), new MockEventQueue(this, machine));
            machine.InitializeStateInformation();

            if (this.Configuration.ReportActivityCoverage && !isMachineTypeCached)
            {
                this.ReportActivityCoverageOfMachine(machine);
            }

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "Machine with id '{0}' is already bound to an existing machine.", mid.Value);

            this.Assert(!this.AllCreatedMachineIds.Contains(mid),
                "MachineId '{0}' of a previously halted machine cannot be reused to create a new machine of type '{1}'",
                mid.Value, type.FullName);
            this.AllCreatedMachineIds.Add(mid);

            this.Logger.OnCreateMachine(mid, creator?.Id);

            if (this.Configuration.EnableDataRaceDetection)
            {
                this.Reporter.RegisterCreateMachine(creator?.Id, mid);
            }

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        internal override void SendEvent(MachineId target, Event e, BaseMachine sender, SendOptions options)
        {
            if (sender != null)
            {
                this.Assert(target != null, "Machine '{0}' is sending to a null machine.", sender.Id);
                this.Assert(e != null, "Machine '{0}' is sending a null event.", sender.Id);
            }
            else
            {
                this.Assert(target != null, "Cannot send to a null machine.");
                this.Assert(e != null, "Cannot send a null event.");
            }

            this.AssertCorrectCallerMachine(sender as Machine, "SendEvent");
            this.Assert(this.AllCreatedMachineIds.Contains(target),
                "Cannot send event '{0}' to machine id '{1}' that was never previously bound to a machine of type '{2}'",
                e.GetType().FullName, target.Value, target.Type);

            this.Scheduler.Schedule(OperationType.Send, OperationTargetType.Inbox, target.Value);

            if (!this.MachineMap.TryGetValue(target, out Machine targetMachine))
            {
                this.Logger.OnSend(target, sender?.Id, (sender as Machine)?.CurrentStateName ?? string.Empty,
                    e.GetType().FullName, e.OperationGroupId, isTargetHalted: true);
                this.Assert(options is null || !options.MustHandle,
                    "A must-handle event '{0}' was sent to the halted machine '{1}'.",
                    e.GetType().FullName, target);
                this.TryHandleDroppedEvent(e, target);
                return;
            }

            if (sender is Machine)
            {
                this.AssertNoPendingTransitionStatement(sender as Machine, "Send");
            }

            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetMachine, e, sender, options, out EventInfo eventInfo);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunMachineEventHandler(targetMachine, null, false, null, eventInfo);
            }
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately
        /// if the target machine was already running. Otherwise blocks until the machine handles
        /// the event and reaches quiescense again.
        /// </summary>
        internal override async Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, BaseMachine sender, SendOptions options)
        {
            this.Assert(sender is Machine,
                "Only a machine can call 'SendEventAndExecute': avoid calling it directly from the 'Test' method; instead call it through a 'harness' machine.");
            this.Assert(target != null, "Machine '{0}' is sending to a null machine.", sender.Id);
            this.Assert(e != null, "Machine '{0}' is sending a null event.", sender.Id);
            this.AssertCorrectCallerMachine(sender as Machine, "SendEventAndExecute");
            this.Assert(this.AllCreatedMachineIds.Contains(target),
                "Cannot send event '{0}' to machine id '{1}' that was never previously bound to a machine of type '{2}'",
                e.GetType().FullName, target.Value, target.Type);

            this.Scheduler.Schedule(OperationType.Send, OperationTargetType.Inbox, target.Value);

            if (!this.MachineMap.TryGetValue(target, out Machine targetMachine))
            {
                this.Logger.OnSend(target, sender?.Id, (sender as Machine)?.CurrentStateName ?? string.Empty,
                    e.GetType().FullName, e.OperationGroupId, isTargetHalted: true);
                this.Assert(options is null || !options.MustHandle,
                    "A must-handle event '{0}' was sent to the halted machine '{1}'.", e.GetType().FullName, target);
                this.TryHandleDroppedEvent(e, target);
                return true;
            }

            if (sender is Machine)
            {
                this.AssertNoPendingTransitionStatement(sender as Machine, "Send");
            }

            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetMachine, e, sender, options, out EventInfo eventInfo);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunMachineEventHandler(targetMachine, null, false, sender as Machine, eventInfo);

                // Wait until the machine reaches quiescence.
                await (sender as Machine).Receive(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).MachineId == target);
                return true;
            }

            // 'EnqueueStatus.EventHandlerNotRunning' is not returned by 'EnqueueEvent' (even when
            // the machine was previously inactive) when the event 'e' requires no action by the
            // machine (i.e., it implicitly handles the event).
            return enqueueStatus is EnqueueStatus.NextEventUnavailable;
        }

        /// <summary>
        /// Enqueues an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        private EnqueueStatus EnqueueEvent(Machine machine, Event e, BaseMachine sender, SendOptions options, out EventInfo eventInfo)
        {
            EventOriginInfo originInfo;
            if (sender is Machine)
            {
                originInfo = new EventOriginInfo(sender.Id, (sender as Machine).GetType().FullName,
                    NameResolver.GetStateNameForLogging((sender as Machine).CurrentState));
            }
            else
            {
                // Message comes from outside P#.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            eventInfo = new EventInfo(e, originInfo)
            {
                MustHandle = options?.MustHandle ?? false,
                Assert = options?.Assert ?? -1,
                Assume = options?.Assume ?? -1,
                SendStep = this.Scheduler.ScheduledSteps
            };

            this.Logger.OnSend(machine.Id, sender?.Id, (sender as Machine)?.CurrentStateName ?? string.Empty,
                e.GetType().FullName, e.OperationGroupId, isTargetHalted: false);

            if (sender != null)
            {
                var stateName = sender is Machine ? (sender as Machine).CurrentStateName : string.Empty;
                this.BugTrace.AddSendEventStep(sender.Id, stateName, eventInfo, machine.Id);
                if (this.Configuration.EnableDataRaceDetection)
                {
                    this.Reporter.RegisterEnqueue(sender.Id, machine.Id, e, (ulong)this.Scheduler.ScheduledSteps);
                }
            }

            return machine.Enqueue(e, eventInfo);
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        /// <param name="syncCaller">Caller machine that is blocked for quiscence.</param>
        /// <param name="enablingEvent">If non-null, the event info of the sent event that caused the event handler to be restarted.</param>
        private void RunMachineEventHandler(Machine machine, Event initialEvent, bool isFresh, Machine syncCaller, EventInfo enablingEvent)
        {
            Task task = new Task(async () =>
            {
                try
                {
                    BugFindingScheduler.NotifyEventHandlerStarted(machine.Info as SchedulableInfo);

                    if (isFresh)
                    {
                        await machine.GotoStartState(initialEvent);
                    }

                    await machine.RunEventHandlerAsync();

                    if (syncCaller != null)
                    {
                        this.EnqueueEvent(syncCaller, new QuiescentEvent(machine.Id, machine.Info.OperationGroupId), machine, null, out EventInfo _);
                    }

                    IO.Debug.WriteLine($"<ScheduleDebug> Completed event handler of '{machine.Id}' with task id '{(machine.Info as SchedulableInfo).TaskId}'.");
                    (machine.Info as SchedulableInfo).NotifyEventHandlerCompleted();

                    if (machine.Info.IsHalted)
                    {
                        this.Scheduler.Schedule(OperationType.Stop, OperationTargetType.Schedulable, machine.Info.Id);
                    }
                    else
                    {
                        this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
                    }

                    IO.Debug.WriteLine($"<ScheduleDebug> Terminated event handler of '{machine.Id}' with task id '{(machine.Info as SchedulableInfo).TaskId}'.");
                }
                catch (ExecutionCanceledException)
                {
                    IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from machine '{machine.Id}'.");
                }
                catch (ObjectDisposedException ex)
                {
                    IO.Debug.WriteLine($"<Exception> ObjectDisposedException was thrown from machine '{machine.Id}' with reason '{ex.Message}'.");
                }
                finally
                {
                    this.TaskMap.TryRemove(Task.CurrentId.Value, out machine);
                }
            });

            this.TaskMap.TryAdd(task.Id, machine);

            (machine.Info as SchedulableInfo).NotifyEventHandlerCreated(task.Id, enablingEvent?.SendStep ?? 0);
            this.Scheduler.NotifyEventHandlerCreated(machine.Info as SchedulableInfo);

            task.Start(this.TaskScheduler);

            this.Scheduler.WaitForEventHandlerToStart(machine.Info as SchedulableInfo);
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        internal void Wait()
        {
            this.Scheduler.Wait();
            this.IsRunning = false;
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner machine.
        /// </summary>
        internal override IMachineTimer CreateMachineTimer(TimerInfo info, Machine owner)
        {
            var mid = this.CreateMachineId(typeof(MockMachineTimer));
            this.CreateMachine(mid, typeof(MockMachineTimer), new TimerSetupEvent(info, owner, this.Configuration.TimeoutDelay));
            this.MachineMap.TryGetValue(mid, out Machine machine);
            return machine as IMachineTimer;
        }

        /// <summary>
        /// Tries to create a new monitor of the given type.
        /// </summary>
        internal override void TryCreateMonitor(Type type)
        {
            if (this.Monitors.Any(m => m.GetType() == type))
            {
                // Idempotence: only one monitor per type can exist.
                return;
            }

            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            MachineId mid = new MachineId(type, null, this);

            SchedulableInfo info = new SchedulableInfo(mid);
            this.Scheduler.NotifyMonitorRegistered(info);

            Monitor monitor = Activator.CreateInstance(type) as Monitor;
            monitor.Initialize(this, mid);
            monitor.InitializeStateInformation();

            this.Logger.OnCreateMonitor(type.FullName, monitor.Id);

            this.ReportActivityCoverageOfMonitor(monitor);
            this.BugTrace.AddCreateMonitorStep(mid);

            this.Monitors.Add(monitor);

            monitor.GotoStartState();
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        internal override void Monitor(Type type, BaseMachine sender, Event e)
        {
            this.AssertCorrectCallerMachine(sender as Machine, "Monitor");
            if (sender is Machine)
            {
                this.AssertNoPendingTransitionStatement(sender as Machine, "Monitor");
            }

            foreach (var m in this.Monitors)
            {
                if (m.GetType() == type)
                {
                    if (this.Configuration.ReportActivityCoverage)
                    {
                        this.ReportActivityCoverageOfMonitorEvent(sender, m, e);
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

                    break;
                }
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure("Detected an assertion failure.");
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString(), arg2.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop) has not
        /// already been called. Records that RGP has been called.
        /// </summary>
        internal void AssertTransitionStatement(Machine machine)
        {
            this.Assert(!machine.Info.IsInsideOnExit,
                "Machine '{0}' has called raise, goto, push or pop inside an OnExit method.",
                machine.Id.Name);
            this.Assert(!machine.Info.CurrentActionCalledTransitionStatement,
                "Machine '{0}' has called multiple raise, goto, push or pop in the same action.",
                machine.Id.Name);
            machine.Info.CurrentActionCalledTransitionStatement = true;
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop)
        /// has not already been called.
        /// </summary>
        internal void AssertNoPendingTransitionStatement(Machine machine, string calledAPI)
        {
            if (!this.Configuration.EnableNoApiCallAfterTransitionStmtAssertion)
            {
                // The check is disabled.
                return;
            }

            this.Assert(!machine.Info.CurrentActionCalledTransitionStatement,
                "Machine '{0}' cannot call '{1}' after calling raise, goto, push or pop in the same action.",
                machine.Id.Name, calledAPI);
        }

        /// <summary>
        /// Asserts that the machine calling a P# machine method is also
        /// the machine that is currently executing.
        /// </summary>
        private void AssertCorrectCallerMachine(Machine callerMachine, string calledAPI)
        {
            if (callerMachine is null)
            {
                return;
            }

            var executingMachine = this.GetCurrentMachine();
            if (executingMachine is null)
            {
                return;
            }

            this.Assert(executingMachine.Equals(callerMachine), "Machine '{0}' invoked {1} on behalf of machine '{2}'.",
                executingMachine.Id, calledAPI, callerMachine.Id);
        }

        /// <summary>
        /// Checks that no monitor is in a hot state upon program termination.
        /// If the program is still running, then this method returns without
        /// performing a check.
        /// </summary>
        internal void CheckNoMonitorInHotStateAtTermination()
        {
            if (!this.Scheduler.HasFullyExploredSchedule)
            {
                return;
            }

            foreach (var monitor in this.Monitors)
            {
                if (monitor.IsInHotState(out string stateName))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Monitor '{0}' detected liveness bug in hot state '{1}' at the end of program execution.",
                        monitor.GetType().FullName, stateName);
                    this.Scheduler.NotifyAssertionFailure(message, false);
                }
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetNondeterministicBooleanChoice(BaseMachine caller, int maxValue)
        {
            if (caller is null)
            {
                caller = this.GetCurrentMachine();
            }

            this.AssertCorrectCallerMachine(caller as Machine, "Random");
            if (caller is Machine)
            {
                this.AssertNoPendingTransitionStatement(caller as Machine, "Random");
                (caller as Machine).Info.ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            this.Logger.OnRandom(caller?.Id, choice);

            var stateName = caller is Machine ? (caller as Machine).CurrentStateName : string.Empty;
            this.BugTrace.AddRandomChoiceStep(caller?.Id, stateName, choice);

            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetFairNondeterministicBooleanChoice(BaseMachine caller, string uniqueId)
        {
            if (caller is null)
            {
                caller = this.GetCurrentMachine();
            }

            this.AssertCorrectCallerMachine(caller as Machine, "FairRandom");
            if (caller is Machine)
            {
                this.AssertNoPendingTransitionStatement(caller as Machine, "FairRandom");
                (caller as Machine).Info.ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(2, uniqueId);
            this.Logger.OnRandom(caller?.Id, choice);

            var stateName = caller is Machine ? (caller as Machine).CurrentStateName : string.Empty;
            this.BugTrace.AddRandomChoiceStep(caller?.Id, stateName, choice);

            return choice;
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override int GetNondeterministicIntegerChoice(BaseMachine caller, int maxValue)
        {
            if (caller is null)
            {
                caller = this.GetCurrentMachine();
            }

            this.AssertCorrectCallerMachine(caller as Machine, "RandomInteger");
            if (caller is Machine)
            {
                this.AssertNoPendingTransitionStatement(caller as Machine, "RandomInteger");
            }

            var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            this.Logger.OnRandom(caller?.Id, choice);

            var stateName = caller is Machine ? (caller as Machine).CurrentStateName : string.Empty;
            this.BugTrace.AddRandomChoiceStep(caller?.Id, stateName, choice);

            return choice;
        }

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        internal override void NotifyEnteredState(Machine machine)
        {
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddGotoStateStep(machine.Id, machineState);

            this.Logger.OnMachineState(machine.Id, machineState, isEntry: true);
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.BugTrace.AddGotoStateStep(monitor.Id, monitorState);

            this.Logger.OnMonitorState(monitor.GetType().FullName, monitor.Id, monitorState, true, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        internal override void NotifyExitedState(Machine machine)
        {
            this.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: false);
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        internal override void NotifyExitedState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.Logger.OnMonitorState(monitor.GetType().FullName, monitor.Id, monitorState, false, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(machine.Id, machineState, action);

            this.Logger.OnMachineAction(machine.Id, machineState, action.Name);
            if (this.Configuration.EnableDataRaceDetection)
            {
                this.Reporter.InAction[machine.Id.Value] = true;
            }
        }

        /// <summary>
        /// Notifies that a machine completed an action.
        /// </summary>
        internal override void NotifyCompletedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.EnableDataRaceDetection)
            {
                this.Reporter.InAction[machine.Id.Value] = false;
            }
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            string monitorState = monitor.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(monitor.Id, monitorState, action);

            this.Logger.OnMonitorAction(monitor.GetType().FullName, monitor.Id, action.Name, monitorState);
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            this.AssertTransitionStatement(machine);

            string machineState = machine.CurrentStateName;
            this.BugTrace.AddRaiseEventStep(machine.Id, machineState, eventInfo);

            this.Logger.OnMachineEvent(machine.Id, machineState, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Monitor monitor, Event e, EventInfo eventInfo)
        {
            string monitorState = monitor.CurrentStateName;
            this.BugTrace.AddRaiseEventStep(monitor.Id, monitorState, eventInfo);

            this.Logger.OnMonitorEvent(monitor.GetType().FullName, monitor.Id, monitor.CurrentStateName,
                eventInfo.EventName, isProcessing: false);
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyDequeuedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
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
                this.Reporter.RegisterDequeue(eventInfo.OriginInfo?.SenderMachineId, machine.Id, e, (ulong)eventInfo.SendStep);
            }

            this.BugTrace.AddDequeueEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfReceivedEvent(machine, eventInfo);
                this.ReportActivityCoverageOfStateTransition(machine, e);
            }
        }

        /// <summary>
        /// Notifies that a machine invoked pop.
        /// </summary>
        internal override void NotifyPop(Machine machine)
        {
            this.AssertCorrectCallerMachine(machine, "Pop");
            this.AssertTransitionStatement(machine);

            this.Logger.OnPop(machine.Id, string.Empty, machine.CurrentStateName);

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfPopTransition(machine, machine.CurrentState, machine.GetStateTypeAtStackIndex(1));
            }
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        internal override void NotifyReceiveCalled(Machine machine)
        {
            this.AssertCorrectCallerMachine(machine, "Receive");
            this.AssertNoPendingTransitionStatement(machine, "Receive");
        }

        /// <summary>
        /// Notifies that a machine is handling a raised event.
        /// </summary>
        internal override void NotifyHandleRaisedEvent(Machine machine, Event e)
        {
            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfStateTransition(machine, e);
            }
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event of one of the specified types.
        /// </summary>
        internal override void NotifyWaitEvent(Machine machine, IEnumerable<Type> eventTypes)
        {
            (machine.Info as SchedulableInfo).IsEnabled = false;

            string eventNames;
            var eventWaitTypesArray = eventTypes.ToArray();
            if (eventWaitTypesArray.Length == 1)
            {
                this.Logger.OnWait(machine.Id, machine.CurrentStateName, eventWaitTypesArray[0]);
                eventNames = eventWaitTypesArray[0].FullName;
            }
            else
            {
                this.Logger.OnWait(machine.Id, machine.CurrentStateName, eventWaitTypesArray);
                if (eventWaitTypesArray.Length > 0)
                {
                    string[] eventNameArray = new string[eventWaitTypesArray.Length - 1];
                    for (int i = 0; i < eventWaitTypesArray.Length - 2; i++)
                    {
                        eventNameArray[i] = eventWaitTypesArray[i].FullName;
                    }

                    eventNames = string.Join(", ", eventNameArray) + " or " + eventWaitTypesArray[eventWaitTypesArray.Length - 1].FullName;
                }
                else
                {
                    eventNames = string.Empty;
                }
            }

            this.BugTrace.AddWaitToReceiveStep(machine.Id, machine.CurrentStateName, eventNames);
            this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        internal override void NotifyReceivedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            this.Logger.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: true);
            this.BugTrace.AddReceivedEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            // A subsequent enqueue unblocked the receive action of machine.
            if (this.Configuration.EnableDataRaceDetection)
            {
                this.Reporter.RegisterDequeue(eventInfo.OriginInfo?.SenderMachineId, machine.Id, e, (ulong)eventInfo.SendStep);
            }

            (machine.Info as SchedulableInfo).IsEnabled = true;
            (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong)eventInfo.SendStep;

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfReceivedEvent(machine, eventInfo);
            }
        }

        /// <summary>
        /// Notifies that a machine received an event without waiting because the event
        /// was already in the inbox when the machine invoked the receive statement.
        /// </summary>
        internal override void NotifyReceivedEventWithoutWaiting(Machine machine, Event e, EventInfo eventInfo)
        {
            this.Logger.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: false);

            (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong)eventInfo.SendStep;

            if (this.Configuration.EnableDataRaceDetection)
            {
                this.Reporter.RegisterDequeue(eventInfo.OriginInfo?.SenderMachineId, machine.Id, e, (ulong)eventInfo.SendStep);
            }

            this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        internal override void NotifyHalted(Machine machine)
        {
            this.BugTrace.AddHaltStep(machine.Id, null);
            this.MachineMap.TryRemove(machine.Id, out Machine _);
        }

        /// <summary>
        /// Notifies that the inbox of the specified machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        internal override void NotifyDefaultEventHandlerCheck(Machine machine)
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
        internal override void NotifyDefaultHandlerFired(Machine machine)
        {
            // NextOperationMatchingSendIndex is set in NotifyDefaultEventHandlerCheck.
            this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
        }

        /// <summary>
        /// Reports coverage for the specified received event.
        /// </summary>
        private void ReportActivityCoverageOfReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            string originMachine = eventInfo.OriginInfo.SenderMachineName;
            string originState = eventInfo.OriginInfo.SenderStateName;
            string edgeLabel = eventInfo.EventName;
            string destMachine = machine.GetType().FullName;
            string destState = NameResolver.GetStateNameForLogging(machine.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified monitor event.
        /// </summary>
        private void ReportActivityCoverageOfMonitorEvent(BaseMachine sender, Monitor monitor, Event e)
        {
            string originMachine = sender is null ? "Env" : sender.GetType().FullName;
            string originState = sender is null ? "Env" :
                (sender is Machine) ? NameResolver.GetStateNameForLogging((sender as Machine).CurrentState) : "Env";

            string edgeLabel = e.GetType().FullName;
            string destMachine = monitor.GetType().FullName;
            string destState = NameResolver.GetStateNameForLogging(monitor.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified machine.
        /// </summary>
        private void ReportActivityCoverageOfMachine(Machine machine)
        {
            var machineName = machine.GetType().FullName;

            // Fetch states.
            var states = machine.GetAllStates();

            foreach (var state in states)
            {
                this.CoverageInfo.DeclareMachineState(machineName, state);
            }

            // Fetch registered events.
            var pairs = machine.GetAllStateEventPairs();

            foreach (var tup in pairs)
            {
                this.CoverageInfo.DeclareStateEvent(machineName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Reports coverage for the specified monitor.
        /// </summary>
        private void ReportActivityCoverageOfMonitor(Monitor monitor)
        {
            var monitorName = monitor.GetType().FullName;

            // Fetch states.
            var states = monitor.GetAllStates();

            foreach (var state in states)
            {
                this.CoverageInfo.DeclareMachineState(monitorName, state);
            }

            // Fetch registered events.
            var pairs = monitor.GetAllStateEventPairs();

            foreach (var tup in pairs)
            {
                this.CoverageInfo.DeclareStateEvent(monitorName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Reports coverage for the specified state transition.
        /// </summary>
        private void ReportActivityCoverageOfStateTransition(Machine machine, Event e)
        {
            string originMachine = machine.GetType().FullName;
            string originState = NameResolver.GetStateNameForLogging(machine.CurrentState);
            string destMachine = machine.GetType().FullName;

            string edgeLabel;
            string destState;
            if (e is GotoStateEvent gotoStateEvent)
            {
                edgeLabel = "goto";
                destState = NameResolver.GetStateNameForLogging(gotoStateEvent.State);
            }
            else if (e is PushStateEvent pushStateEvent)
            {
                edgeLabel = "push";
                destState = NameResolver.GetStateNameForLogging(pushStateEvent.State);
            }
            else if (machine.GotoTransitions.ContainsKey(e.GetType()))
            {
                edgeLabel = e.GetType().FullName;
                destState = NameResolver.GetStateNameForLogging(
                    machine.GotoTransitions[e.GetType()].TargetState);
            }
            else if (machine.PushTransitions.ContainsKey(e.GetType()))
            {
                edgeLabel = e.GetType().FullName;
                destState = NameResolver.GetStateNameForLogging(
                    machine.PushTransitions[e.GetType()].TargetState);
            }
            else
            {
                return;
            }

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for a pop transition.
        /// </summary>
        private void ReportActivityCoverageOfPopTransition(Machine machine, Type fromState, Type toState)
        {
            string originMachine = machine.GetType().FullName;
            string originState = NameResolver.GetStateNameForLogging(fromState);
            string destMachine = machine.GetType().FullName;
            string edgeLabel = "pop";
            string destState = NameResolver.GetStateNameForLogging(toState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified state transition.
        /// </summary>
        private void ReportActivityCoverageOfMonitorTransition(Monitor monitor, Event e)
        {
            string originMachine = monitor.GetType().FullName;
            string originState = NameResolver.GetStateNameForLogging(monitor.CurrentState);
            string destMachine = originMachine;

            string edgeLabel;
            string destState;
            if (e is GotoStateEvent)
            {
                edgeLabel = "goto";
                destState = NameResolver.GetStateNameForLogging((e as GotoStateEvent).State);
            }
            else if (monitor.GotoTransitions.ContainsKey(e.GetType()))
            {
                edgeLabel = e.GetType().FullName;
                destState = NameResolver.GetStateNameForLogging(
                    monitor.GotoTransitions[e.GetType()].TargetState);
            }
            else
            {
                return;
            }

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Gets the currently executing <see cref="Machine"/>, if one exists.
        /// </summary>
        internal Machine GetCurrentMachine()
        {
            // The current task does not correspond to a machine.
            if (Task.CurrentId is null)
            {
                return null;
            }

            // The current task does not correspond to a machine.
            if (!this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                return null;
            }

            return this.TaskMap[(int)Task.CurrentId];
        }

        /// <summary>
        /// Gets the id of the currently executing <see cref="Machine"/>.
        /// <returns>MachineId or null, if not present</returns>
        /// </summary>
        internal MachineId GetCurrentMachineId()
        {
            return this.GetCurrentMachine()?.Id;
        }

        /// <summary>
        /// Returns the fingerprint of the current program state.
        /// </summary>
        internal Fingerprint GetProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                int hash = 19;

                foreach (var machine in this.MachineMap.Values.OrderBy(mi => mi.Id.Value))
                {
                    hash = (hash * 31) + machine.GetCachedState();
                    hash = (hash * 31) + (int)(machine.Info as SchedulableInfo).NextOperationType;
                }

                foreach (var monitor in this.Monitors)
                {
                    hash = (hash * 31) + monitor.GetCachedState();
                }

                fingerprint = new Fingerprint(hash);
            }

            return fingerprint;
        }

        /// <summary>
        /// Logs the specified text.
        /// </summary>
        protected internal override void Log(string format, params object[] args)
        {
            this.Logger.WriteLine(format, args);
        }

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        internal override void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, s, args);
            this.Scheduler.NotifyAssertionFailure(message);
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Monitors.Clear();
                this.MachineMap.Clear();
                this.TaskMap.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
