//-----------------------------------------------------------------------
// <copyright file="BugFindingRuntime.cs">
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

using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Class implementing the P# bug-finding runtime.
    /// </summary>
    internal sealed class BugFindingRuntime : PSharpRuntime
    {
        #region fields

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
        private List<Monitor> Monitors;

        /// <summary>
        /// Map from task ids to machines.
        /// </summary>
        private ConcurrentDictionary<int, Machine> TaskMap;

        /// <summary>
        /// Set of all machine Ids created by this runtime.
        /// </summary>
        internal HashSet<MachineId> AllCreatedMachineIds;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal int? RootTaskId;

        #endregion

        #region initialization

        /// <summary>
        /// Constructor.
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// <param name="reporter">Reporter to register runtime operations.</param>
        /// </summary>
        internal BugFindingRuntime(Configuration configuration, ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
            : base(configuration)
        {
            this.Initialize();

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

        /// <summary>
        /// Initializes various components of the runtime.
        /// </summary>
        private void Initialize()
        {
            this.Monitors = new List<Monitor>();
            this.TaskMap = new ConcurrentDictionary<int, Machine>();
            this.RootTaskId = Task.CurrentId;
            this.AllCreatedMachineIds = new HashSet<MachineId>();
        }

        #endregion

        #region runtime interface

        /// <summary>
        /// Creates a new machine of the specified type and with
        /// the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachine(null, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        public override void CreateMachine(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            this.Assert(mid != null, "Cannot pass a null MachineId.");
            this.CreateMachine(mid, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and
        /// with the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachine(null, type, friendlyName, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        internal MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachine(mid, type, friendlyName, e, creator, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecute(null, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <returns>MachineId</returns>
        public override async Task CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid? operationGroupId = null)
        {
            this.Assert(mid != null, "Cannot pass a null MachineId.");
            await this.CreateMachineAndExecute(mid, type, null, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            return this.CreateMachineAndExecute(null, type, friendlyName, e, operationGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        internal Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName, Event e = null, Guid? operationGroupId = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachineAndExecute(mid, type, friendlyName, e, creator, operationGroupId);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and with
        /// the specified optional event. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateRemoteMachine(type, null, endpoint, e, creator, operationGroupId);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and name, and
        /// with the specified optional event. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="operationGroupId">Optional operation group id</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e = null, Guid? operationGroupId = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateRemoteMachine(type, friendlyName, endpoint, e, creator, operationGroupId);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        public override void SendEvent(MachineId target, Event e, SendOptions options = null)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.SendEvent(target, e, this.GetCurrentMachine(), options);
        }

        /// <summary>
        /// Synchronously delivers an <see cref="Event"/> to a machine
        /// and executes it if the machine is available.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        public override async Task SendEventAndExecute(MachineId target, Event e, SendOptions options = null)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            await this.SendEventAndExecute(target, e, this.GetCurrentMachine(), options);
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine, which
        /// is modeled as a local machine during testing.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        public override void RemoteSendEvent(MachineId target, Event e, SendOptions options = null)
        {
            this.SendEvent(target, e, options);
        }

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        public override void RegisterMonitor(Type type)
        {
            this.TryCreateMonitor(type);
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
            base.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor(type, null, e);
        }

        /// <summary>
        /// Returns the operation group id of the specified machine. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="MachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        /// <param name="currentMachine">MachineId of the currently executing machine.</param>
        /// <returns>Guid</returns>
        public override Guid GetCurrentOperationGroupId(MachineId currentMachine)
        {
            this.Assert(currentMachine == GetCurrentMachineId(), "Trying to access the operation group id of " +
                $"'{currentMachine}', which is not the currently executing machine.");

            if (!this.MachineMap.TryGetValue(currentMachine, out Machine machine))
            {
                return Guid.Empty;
            }

            return machine.Info.OperationGroupId;
        }

        /// <summary>
        /// Notifies each active machine to halt execution to allow the runtime
        /// to reach quiescence. This is an experimental feature, which should
        /// be used only for testing purposes.
        /// </summary>
        public override void Stop()
        {
            base.IsRunning = false;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Runs the specified test method inside a test harness machine.
        /// </summary>
        /// <param name="testAction">Action</param>
        /// <param name="testMethod">MethodInfo</param>
        internal void RunTestHarness(MethodInfo testMethod, Action<PSharpRuntime> testAction)
        {
            this.Assert(Task.CurrentId != null, "The test harness machine must execute inside a task.");
            this.Assert(testMethod != null || testAction != null, "The test harness machine " +
                "cannot execute a null test method or action.");

            MachineId mid = new MachineId(typeof(TestHarnessMachine), null, this);
            TestHarnessMachine harness = new TestHarnessMachine(testMethod, testAction);

            harness.Initialize(this, mid, new SchedulableInfo(mid));

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
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId CreateMachine(MachineId mid, Type type, string friendlyName, Event e, Machine creator, Guid? operationGroupId)
        {
            this.AssertCorrectCallerMachine(creator, "CreateMachine");
            if (creator != null)
            {
                this.AssertNoPendingTransitionStatement(creator, "CreateMachine");
            }

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

            Machine machine = this.CreateMachine(mid, type, friendlyName, creator);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e == null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, false, null);

            return machine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override async Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, string friendlyName, Event e,
            Machine creator, Guid? operationGroupId)
        {
            this.AssertCorrectCallerMachine(creator, "CreateMachineAndExecute");
            if (creator != null)
            {
                this.AssertNoPendingTransitionStatement(creator, "CreateMachine");
            }

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.Schedule(OperationType.Create, OperationTargetType.Schedulable, ulong.MaxValue);

            Machine machine = this.CreateMachine(mid, type, friendlyName, creator);
            this.SetOperationGroupIdForMachine(machine, creator, operationGroupId);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e == null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, true, null);

            return await Task.FromResult(machine.Id);
        }

        /// <summary>
        /// Creates a new remote <see cref="Machine"/> of the specified
        /// <see cref="System.Type"/>, which is modeled as a local
        /// machine during testing.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator, Guid? operationGroupId)
        {
            return this.CreateMachine(null, type, friendlyName, e, creator, operationGroupId);
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="mid">Unbound machine id</param>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="creator">The id of the machine that created the returned machine.</param>
        /// <returns>Machine</returns>
        private Machine CreateMachine(MachineId mid, Type type, string friendlyName, Machine creator)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), "Type '{0}' is not a machine.", type.Name);

            if (mid == null)
            {
                mid = new MachineId(type, friendlyName, this);
            }
            else
            {
                this.Assert(mid.Runtime == null || mid.Runtime == this, "Unbound machine id '{0}' was created by another runtime.", mid.Value);
                this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                    mid.Value, mid.Type, type.FullName);
                mid.Bind(this);
            }

            var isMachineTypeCached = MachineFactory.IsCached(type);
            Machine machine = MachineFactory.Create(type);

            machine.Initialize(this, mid, new SchedulableInfo(mid));
            machine.InitializeStateInformation();

            if (base.Configuration.ReportActivityCoverage && !isMachineTypeCached)
            {
                this.ReportActivityCoverageOfMachine(machine);
            }

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "Machine with id '{0}' is already bound to an existing machine.",
                mid.Value);

            this.Assert(!AllCreatedMachineIds.Contains(mid), "MachineId '{0}' of a previously halted machine cannot be reused " +
                "to create a new machine of type {1}", mid.Value, type.FullName);
            AllCreatedMachineIds.Add(mid);

            this.Logger.OnCreateMachine(mid, creator?.Id);

            if (base.Configuration.EnableDataRaceDetection)
            {
                Reporter.RegisterCreateMachine(creator?.Id, mid);
            }

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            this.AssertCorrectCallerMachine(sender as Machine, "SendEvent");
            this.Assert(AllCreatedMachineIds.Contains(mid), "Cannot Send event {0} to a MachineId '{1}' that was never " +
                "previously bound to a machine of type {2}", e.GetType().FullName, mid.Value, mid);

            this.Scheduler.Schedule(OperationType.Send, OperationTargetType.Inbox, mid.Value);
            var operationGroupId = base.GetNewOperationGroupId(sender, options?.OperationGroupId);

            if (!base.GetTargetMachine(mid, e, sender, operationGroupId, out Machine machine))
            {
                this.Assert(options == null || !options.MustHandle,
                    $"A must-handle event '{e.GetType().Name}' was sent to the halted machine '{mid}'.\n");
                return;
            }

            bool runNewHandler = false;
            EventInfo eventInfo = this.EnqueueEvent(machine, e, sender, operationGroupId, options?.MustHandle ?? false, ref runNewHandler);
            if (runNewHandler)
            {
                this.RunMachineEventHandler(machine, null, false, false, eventInfo);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine and
        /// executes the event handler if the machine is available.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        internal override async Task SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            this.AssertCorrectCallerMachine(sender as Machine, "SendEventAndExecute");
            this.Assert(AllCreatedMachineIds.Contains(mid), "Cannot Send event {0} to a MachineId ({0},{1}) that was never " +
                "previously bound to a machine of type {2}", e.GetType().FullName, mid.Value, mid.Generation, mid);

            this.Scheduler.Schedule(OperationType.Send, OperationTargetType.Inbox, mid.Value);
            var operationGroupId = base.GetNewOperationGroupId(sender, options?.OperationGroupId);

            if (!base.GetTargetMachine(mid, e, sender, operationGroupId, out Machine machine))
            {
                this.Assert(options == null || !options.MustHandle,
                    $"A must-handle event '{e.GetType().FullName}' was sent to the halted machine '{mid}'.\n");
                return;
            }

            bool runNewHandler = false;
            EventInfo eventInfo = this.EnqueueEvent(machine, e, sender, operationGroupId, options?.MustHandle ?? false, ref runNewHandler);
            if (runNewHandler)
            {
                this.RunMachineEventHandler(machine, null, false, true, eventInfo);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine, which
        /// is modeled as a local machine during testing.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="options">Optional parameters of a send operation.</param>
        internal override void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, SendOptions options)
        {
            this.SendEvent(mid, e, sender, options);
        }

        /// <summary>
        /// Enqueues an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="operationGroupId">Operation group id</param>
        /// <param name="mustHandle">MustHandle event</param>
        /// <param name="runNewHandler">Run a new handler</param>
        /// <returns>EventInfo</returns>
        private EventInfo EnqueueEvent(Machine machine, Event e, AbstractMachine sender, Guid operationGroupId, bool mustHandle, ref bool runNewHandler)
        {
            if (sender != null && sender is Machine)
            {
                this.AssertNoPendingTransitionStatement(sender as Machine, "Send");
            }

            EventOriginInfo originInfo = null;
            if (sender != null && sender is Machine)
            {
                originInfo = new EventOriginInfo(sender.Id, (sender as Machine).GetType().Name,
                    (sender as Machine).CurrentState == null ? "None" : 
                    StateGroup.GetQualifiedStateName((sender as Machine).CurrentState));
            }
            else
            {
                // Message comes from outside P#.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            EventInfo eventInfo = new EventInfo(e, originInfo, Scheduler.ScheduledSteps);
            eventInfo.SetOperationGroupId(operationGroupId);
            eventInfo.SetMustHandle(mustHandle);

            var senderState = (sender as Machine)?.CurrentStateName ?? string.Empty;
            this.Logger.OnSend(machine.Id, sender?.Id, senderState,
                e.GetType().FullName, operationGroupId, isTargetHalted:false);

            if (sender != null)
            {
                var stateName = sender is Machine ? (sender as Machine).CurrentStateName : "";
                this.BugTrace.AddSendEventStep(sender.Id, stateName, eventInfo, machine.Id);
                if (base.Configuration.EnableDataRaceDetection)
                {
                    this.Reporter.RegisterEnqueue(sender.Id, machine.Id, e, (ulong)Scheduler.ScheduledSteps);
                }
            }

            machine.Enqueue(eventInfo, ref runNewHandler, sender);

            return eventInfo;
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        /// <param name="executeSynchronously">If true, this operation executes synchronously.</param>
        /// <param name="enablingEvent">If non-null, the event info of the sent event that caused the event handler to be restarted.</param>
        private void RunMachineEventHandler(Machine machine, Event initialEvent, bool isFresh,
            bool executeSynchronously, EventInfo enablingEvent)
        {
            Task task = new Task(async () =>
            {
                try
                {
                    this.Scheduler.NotifyEventHandlerStarted(machine.Info as SchedulableInfo);

                    machine.IsInsideSynchronousCall = executeSynchronously;

                    if (isFresh)
                    {
                        await machine.GotoStartState(initialEvent);
                    }

                    await machine.RunEventHandler(executeSynchronously);
                    machine.IsInsideSynchronousCall = false;

                    if (executeSynchronously)
                    {
                        await machine.RunEventHandler();
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
                catch (ExecutionCanceledException)
                {
                    IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from machine '{machine.Id}'.");
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
        /// Checks that a machine can start its event handler. Returns false if the event
        /// handler should not be started. The bug finding runtime may return false because
        /// it knows that there are currently no events in the inbox that can be handled.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <returns>Boolean</returns>
        internal override bool CheckStartEventHandler(Machine machine)
        {
            return machine.TryDequeueEvent(true) != null;
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        internal void Wait()
        {
            this.Scheduler.Wait();
            base.IsRunning = false;
        }

        #endregion

        #region specifications and error checking

        /// <summary>
        /// Tries to create a new monitor of the given type.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal override void TryCreateMonitor(Type type)
        {
            this.Assert(type.IsSubclassOf(typeof(Monitor)), $"Type '{type.Name}' " +
                "is not a subclass of Monitor.\n");

            MachineId mid = new MachineId(type, null, this);

            SchedulableInfo info = new SchedulableInfo(mid);
            Scheduler.NotifyMonitorRegistered(info);

            Monitor monitor = Activator.CreateInstance(type) as Monitor;
            monitor.Initialize(mid);
            monitor.InitializeStateInformation();

            this.Logger.OnCreateMonitor(type.Name, monitor.Id);

            this.ReportActivityCoverageOfMonitor(monitor);
            this.BugTrace.AddCreateMonitorStep(mid);

            this.Monitors.Add(monitor);

            monitor.GotoStartState();
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="type">Type of the monitor</param>
        /// <param name="e">Event</param>
        internal override void Monitor(Type type, AbstractMachine sender, Event e)
        {
            this.AssertCorrectCallerMachine(sender as Machine, "Monitor");
            if (sender != null && sender is Machine)
            {
                this.AssertNoPendingTransitionStatement(sender as Machine, "Monitor");
            }

            foreach (var m in this.Monitors)
            {
                if (m.GetType() == type)
                {
                    if (base.Configuration.ReportActivityCoverage)
                    {
                        this.ReportActivityCoverageOfMonitorEvent(sender, m, e);
                        this.ReportActivityCoverageOfMonitorTransition(m, e);
                    }
                    if (base.Configuration.EnableDataRaceDetection)
                    {
                        this.Reporter.InMonitor = (long)m.Id.Value;
                    }
                    m.MonitorEvent(e);
                    if (base.Configuration.EnableDataRaceDetection)
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
        /// Asserts that a transition statement (raise, goto or pop) has not
        /// already been called. Records that RGP has been called.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void AssertTransitionStatement(Machine machine)
        {
            this.Assert(!machine.Info.IsInsideOnExit, "Machine '{0}' has called raise, goto, push or pop " +
                "inside an OnExit method.", machine.Id.Name);
            this.Assert(!machine.Info.CurrentActionCalledTransitionStatement, "Machine '{0}' has called multiple " +
                "raise, goto, push or pop in the same action.", machine.Id.Name);
            machine.Info.CurrentActionCalledTransitionStatement = true;
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop)
        /// has not already been called.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="calledAPI">Called API</param>
        internal void AssertNoPendingTransitionStatement(Machine machine, string calledAPI)
        {
            this.Assert(!machine.Info.CurrentActionCalledTransitionStatement, "Machine '{0}' cannot call '{1}' " +
                "after calling raise, goto, push or pop in the same action.", machine.Id.Name, calledAPI);
        }

        /// <summary>
        /// Asserts that the machine calling a P# machine method is also
        /// the machine that is currently executing.
        /// </summary>
        /// <param name="callerMachine">Caller machine</param>
        /// <param name="calledAPI">Called API name</param>
        private void AssertCorrectCallerMachine(Machine callerMachine, string calledAPI)
        {
            if (callerMachine == null)
            {
                return;
            }

            var executingMachine = this.GetCurrentMachine();
            if (executingMachine == null)
            {
                return;
            }

            this.Assert(executingMachine.Equals(callerMachine), $"Machine '{executingMachine.Id}' " +
                $"invoked {calledAPI} on behalf of machine '{callerMachine.Id}'.");
        }

        /// <summary>
        /// Checks that no monitor is in a hot state upon program termination.
        /// If the program is still running, then this method returns without
        /// performing a check.
        /// </summary>
        internal void AssertNoMonitorInHotStateAtTermination()
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

        #endregion

        #region nondeterministic choices

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="caller">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        internal override bool GetNondeterministicBooleanChoice(AbstractMachine caller, int maxValue)
        {
            this.AssertCorrectCallerMachine(caller as Machine, "Random");
            if (caller != null && caller is Machine)
            {
                this.AssertNoPendingTransitionStatement(caller as Machine, "Random");
                (caller as Machine).Info.ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            base.Logger.OnRandom(caller?.Id, choice);

            var stateName = caller is Machine ? (caller as Machine).CurrentStateName : "";
            this.BugTrace.AddRandomChoiceStep(caller == null ? null : caller.Id, stateName, choice);

            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="caller">Machine</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal override bool GetFairNondeterministicBooleanChoice(AbstractMachine caller, string uniqueId)
        {
            this.AssertCorrectCallerMachine(caller as Machine, "FairRandom");
            if (caller != null && caller is Machine)
            {
                this.AssertNoPendingTransitionStatement(caller as Machine, "FairRandom");
                (caller as Machine).Info.ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(2, uniqueId);
            base.Logger.OnRandom(caller?.Id, choice);

            var stateName = caller is Machine ? (caller as Machine).CurrentStateName : "";
            this.BugTrace.AddRandomChoiceStep(caller == null ? null : caller.Id, stateName, choice);

            return choice;
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="caller">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
        internal override int GetNondeterministicIntegerChoice(AbstractMachine caller, int maxValue)
        {
            this.AssertCorrectCallerMachine(caller as Machine, "RandomInteger");
            if (caller != null && caller is Machine)
            {
                this.AssertNoPendingTransitionStatement(caller as Machine, "RandomInteger");
            }

            var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            base.Logger.OnRandom(caller?.Id, choice);

            var stateName = caller is Machine ? (caller as Machine).CurrentStateName : "";
            this.BugTrace.AddRandomChoiceStep(caller == null ? null : caller.Id, stateName, choice);

            return choice;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyEnteredState(Machine machine)
        {
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddGotoStateStep(machine.Id, machineState);

            base.Logger.OnMachineState(machine.Id, machineState, isEntry: true);
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.BugTrace.AddGotoStateStep(monitor.Id, monitorState);

            base.Logger.OnMonitorState(monitor.GetType().Name, monitor.Id, monitorState, true, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyExitedState(Machine machine)
        {
            base.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: false);
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        internal override void NotifyExitedState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            base.Logger.OnMonitorState(monitor.GetType().Name, monitor.Id, monitorState, false, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        internal override void NotifyInvokedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(machine.Id, machineState, action);

            this.Logger.OnMachineAction(machine.Id, machineState, action.Name);
            if (base.Configuration.EnableDataRaceDetection)
            {
                Reporter.InAction[machine.Id.Value] = true;
            }
        }

        /// <summary>
        /// Notifies that a machine completed an action.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        internal override void NotifyCompletedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            if (base.Configuration.EnableDataRaceDetection)
            {
                Reporter.InAction[machine.Id.Value] = false;
            }
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            string monitorState = monitor.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(monitor.Id, monitorState, action);

            base.Logger.OnMonitorAction(monitor.GetType().Name, monitor.Id, action.Name, monitorState);
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            this.AssertTransitionStatement(machine);
            eventInfo.SetOperationGroupId(base.GetNewOperationGroupId(machine, null));

            string machineState = machine.CurrentStateName;
            this.BugTrace.AddRaiseEventStep(machine.Id, machineState, eventInfo);

            this.Logger.OnMachineEvent(machine.Id, machineState, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyRaisedEvent(Monitor monitor, EventInfo eventInfo)
        {
            string monitorState = monitor.CurrentStateName;
            this.BugTrace.AddRaiseEventStep(monitor.Id, monitorState, eventInfo);

            this.Logger.OnMonitorEvent(monitor.GetType().Name, monitor.Id, monitor.CurrentStateName,
                eventInfo.EventName, isProcessing:false);
        }

        /// <summary>
        /// Notifies that a machine dequeued an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
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
                (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong) eventInfo.SendStep;
                this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
            }

            this.Logger.OnDequeue(machine.Id, machine.CurrentStateName, eventInfo.EventName);

            if (base.Configuration.EnableDataRaceDetection)
            {
                Reporter.RegisterDequeue(eventInfo.OriginInfo?.SenderMachineId, machine.Id, eventInfo.Event,
                    (ulong)eventInfo.SendStep);
            }

            this.BugTrace.AddDequeueEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            if (base.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfReceivedEvent(machine, eventInfo);
                this.ReportActivityCoverageOfStateTransition(machine, eventInfo);
            }
        }

        /// <summary>
        /// Notifies that a machine invoked pop.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyPop(Machine machine)
        {
            this.AssertCorrectCallerMachine(machine, "Pop");
            this.AssertTransitionStatement(machine);

            this.Logger.OnPop(machine.Id, string.Empty, machine.CurrentStateName);

            if (base.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfPopTransition(machine, machine.CurrentState, machine.GetStateTypeAtStackIndex(1));
            }
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyReceiveCalled(Machine machine)
        {
            this.Assert(!machine.IsInsideSynchronousCall, $"Machine '{machine.Id}' called " +
                "receive while executing synchronously.");
            this.AssertCorrectCallerMachine(machine, "Receive");
            this.AssertNoPendingTransitionStatement(machine, "Receive");
        }

        /// <summary>
        /// Notifies that a machine is handling a raised event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyHandleRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            if (base.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfStateTransition(machine, eventInfo);
            }
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one or more events.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfoInInbox">The event info if it is in the inbox, else null</param>
        internal override void NotifyWaitEvents(Machine machine, EventInfo eventInfoInInbox)
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
                (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong) eventInfoInInbox.SendStep;

                // The event was already in the inbox when we executed a receive action.
                // We've dequeued it by this point.
                if (base.Configuration.EnableDataRaceDetection)
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
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            this.BugTrace.AddReceivedEventStep(machine.Id, machine.CurrentStateName, eventInfo);
            this.Logger.OnReceive(machine.Id, machine.CurrentStateName, eventInfo.EventName, wasBlocked:true);

            // A subsequent enqueue from m' unblocked the receive action of machine.
            if (base.Configuration.EnableDataRaceDetection)
            {
                Reporter.RegisterDequeue(eventInfo.OriginInfo?.SenderMachineId, machine.Id, eventInfo.Event, (ulong) eventInfo.SendStep);
            }

            machine.Info.IsWaitingToReceive = false;
            (machine.Info as SchedulableInfo).IsEnabled = true;
            (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong) eventInfo.SendStep;
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="inbox">Machine inbox.</param>
        internal override void NotifyHalted(Machine machine, LinkedList<EventInfo> inbox)
        {
            var mustHandleEvent = inbox.FirstOrDefault(ev => ev.MustHandle);
            this.Assert(mustHandleEvent == null,
                $"Machine '{machine.Id}' halted before dequeueing must-handle event '{mustHandleEvent?.EventName ?? string.Empty}'.\n");

            this.BugTrace.AddHaltStep(machine.Id, null);
            this.Logger.OnHalt(machine.Id, inbox.Count);
            this.MachineMap.TryRemove(machine.Id, out machine);
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
            (machine.Info as SchedulableInfo).NextOperationMatchingSendIndex = (ulong) this.Scheduler.ScheduledSteps;
        }

        /// <summary>
        /// Notifies that the default handler of the specified machine has been fired.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyDefaultHandlerFired(Machine machine)
        {
            // NextOperationMatchingSendIndex is set in NotifyDefaultEventHandlerCheck.
            this.Scheduler.Schedule(OperationType.Receive, OperationTargetType.Inbox, machine.Info.Id);
        }

        #endregion

        #region code coverage

        /// <summary>
        /// Reports coverage for the specified received event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        private void ReportActivityCoverageOfReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            string originMachine = eventInfo.OriginInfo.SenderMachineName;
            string originState = eventInfo.OriginInfo.SenderStateName;
            string edgeLabel = eventInfo.EventType.Name;
            string destMachine = machine.GetType().Name;
            string destState = StateGroup.GetQualifiedStateName(machine.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified monitor event.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="monitor">Monitor</param>
        /// <param name="e">Event</param>
        private void ReportActivityCoverageOfMonitorEvent(AbstractMachine sender, Monitor monitor, Event e)
        {
            string originMachine = (sender == null) ? "Env" : sender.GetType().Name;
            string originState = (sender == null) ? "Env" :
                (sender is Machine) ? StateGroup.GetQualifiedStateName((sender as Machine).CurrentState) :
                "Env";

            string edgeLabel = e.GetType().Name;
            string destMachine = monitor.GetType().Name;
            string destState = StateGroup.GetQualifiedStateName(monitor.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void ReportActivityCoverageOfMachine(Machine machine)
        {
            var machineName = machine.GetType().Name;

            // fetch states
            var states = machine.GetAllStates();

            foreach (var state in states)
            {
                this.CoverageInfo.DeclareMachineState(machineName, state);
            }

            // fetch registered events
            var pairs = machine.GetAllStateEventPairs();

            foreach (var tup in pairs)
            {
                this.CoverageInfo.DeclareStateEvent(machineName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Reports coverage for the specified monitor.
        /// </summary>
        /// <param name="monitor">Monitor</param>
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
        /// Reports coverage for the specified state transition.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        private void ReportActivityCoverageOfStateTransition(Machine machine, EventInfo eventInfo)
        {
            string originMachine = machine.GetType().Name;
            string originState = StateGroup.GetQualifiedStateName(machine.CurrentState);
            string destMachine = machine.GetType().Name;

            string edgeLabel = "";
            string destState = "";
            if (eventInfo.Event is GotoStateEvent)
            {
                edgeLabel = "goto";
                destState = StateGroup.GetQualifiedStateName((eventInfo.Event as GotoStateEvent).State);
            }
            if (eventInfo.Event is PushStateEvent)
            {
                edgeLabel = "push";
                destState = StateGroup.GetQualifiedStateName((eventInfo.Event as PushStateEvent).State);
            }
            else if (machine.GotoTransitions.ContainsKey(eventInfo.EventType))
            {
                edgeLabel = eventInfo.EventType.Name;
                destState = StateGroup.GetQualifiedStateName(
                    machine.GotoTransitions[eventInfo.EventType].TargetState);
            }
            else if (machine.PushTransitions.ContainsKey(eventInfo.EventType))
            {
                edgeLabel = eventInfo.EventType.Name;
                destState = StateGroup.GetQualifiedStateName(
                    machine.PushTransitions[eventInfo.EventType].TargetState);
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
        /// <param name="machine">Machine</param>
        /// <param name="fromState">Top of the stack state</param>
        /// <param name="toState">Next to top state of the stack</param>
        private void ReportActivityCoverageOfPopTransition(Machine machine, Type fromState, Type toState)
        {
            string originMachine = machine.GetType().Name;
            string originState = StateGroup.GetQualifiedStateName(fromState);
            string destMachine = machine.GetType().Name;
            string edgeLabel = "pop";
            string destState = StateGroup.GetQualifiedStateName(toState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified state transition.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <param name="e">Event</param>
        private void ReportActivityCoverageOfMonitorTransition(Monitor monitor, Event e)
        {
            string originMachine = monitor.GetType().Name;
            string originState = StateGroup.GetQualifiedStateName(monitor.CurrentState);
            string destMachine = originMachine;

            string edgeLabel = "";
            string destState = "";
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
        /// Gets the currently executing <see cref="Machine"/>.
        /// </summary>
        /// <returns>Machine or null, if not present</returns>
        internal Machine GetCurrentMachine()
        {
            //  The current task does not correspond to a machine.
            if (Task.CurrentId == null)
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

        #region logging

        /// <summary>
        /// Logs the specified text.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        protected internal override void Log(string format, params object[] args)
        {
            base.Logger.WriteLine(format, args);
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
        internal override void WrapAndThrowException(Exception exception, string s, params object[] args)
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
            this.MachineMap.Clear();
            this.TaskMap.Clear();
            base.Dispose();
        }

        #endregion
    }
}
