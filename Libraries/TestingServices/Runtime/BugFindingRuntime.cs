﻿//-----------------------------------------------------------------------
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
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Coverage;
using Microsoft.PSharp.TestingServices.Liveness;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Tracing.Error;
using Microsoft.PSharp.TestingServices.Tracing.Machines;
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
        /// The P# liveness checker.
        /// </summary>
        internal LivenessChecker LivenessChecker;

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
        /// The P# program state cache.
        /// </summary>
        internal StateCache StateCache;
        
        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private List<Monitor> Monitors;

        /// <summary>
        /// Map from unique machine ids to machines.
        /// </summary>
        private ConcurrentDictionary<ulong, Machine> MachineMap;

        /// <summary>
        /// Map from task ids to machines.
        /// </summary>
        private ConcurrentDictionary<int, Machine> TaskMap;

        /// <summary>
        /// A map from unique machine ids to action traces.
        /// Only used for dynamic data race detection.
        /// </summary>
        internal IDictionary<MachineId, MachineActionTrace> MachineActionTraceMap;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal int? RootTaskId;
        
        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        private int OperationIdCounter;

        #endregion

        #region initialization

        /// <summary>
        /// Constructor.
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        /// </summary>
        internal BugFindingRuntime(Configuration configuration, ISchedulingStrategy strategy)
            : base(configuration)
        {
            this.Initialize();

            this.ScheduleTrace = new ScheduleTrace();
            this.BugTrace = new BugTrace();
            
            this.Scheduler = new BugFindingScheduler(this, strategy);
            this.LivenessChecker = new LivenessChecker(this, strategy);
            this.TaskScheduler = new AsynchronousTaskScheduler(this, this.TaskMap);
            this.StateCache = new StateCache(this);
            this.CoverageInfo = new CoverageInfo();
        }

        /// <summary>
        /// Initializes various components of the runtime.
        /// </summary>
        private void Initialize()
        {
            this.Monitors = new List<Monitor>();
            this.MachineMap = new ConcurrentDictionary<ulong, Machine>();
            this.TaskMap = new ConcurrentDictionary<int, Machine>();
            this.MachineActionTraceMap = new ConcurrentDictionary<MachineId, MachineActionTrace>();

            this.RootTaskId = Task.CurrentId;
            this.OperationIdCounter = 0;
        }

        #endregion

        #region runtime interface

        /// <summary>
        /// Creates a new machine of the specified type and with
        /// the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachine(type, null, e, creator);
        }

        /// <summary>
        /// Creates a new machine of the specified type and name, and
        /// with the specified optional event. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId CreateMachine(Type type, string friendlyName, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachine(type, friendlyName, e, creator);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachineAndExecute(type, null, e, creator);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateMachineAndExecute(type, friendlyName, e, creator);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and with
        /// the specified optional event. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string endpoint, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateRemoteMachine(type, null, endpoint, e, creator);
        }

        /// <summary>
        /// Creates a new remote machine of the specified type and name, and
        /// with the specified optional event. This event can only be used
        /// to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns>
        public override MachineId RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e = null)
        {
            Machine creator = null;
            if (this.TaskMap.ContainsKey((int)Task.CurrentId))
            {
                creator = this.TaskMap[(int)Task.CurrentId];
            }

            return this.CreateRemoteMachine(type, friendlyName, endpoint, e, creator);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void SendEvent(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.SendEvent(target, e, this.GetCurrentMachine(), false);
        }

        /// <summary>
        /// Synchronously delivers an <see cref="Event"/> to a machine
        /// and executes it if the machine is available.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override async Task SendEventAndExecute(MachineId target, Event e)
        {
            // If the target machine is null then report an error and exit.
            this.Assert(target != null, "Cannot send to a null machine.");
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            await this.SendEventAndExecute(target, e, this.GetCurrentMachine(), false);
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine, which
        /// is modeled as a local machine during testing.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void RemoteSendEvent(MachineId target, Event e)
        {
            this.SendEvent(target, e);
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
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        public override void InvokeMonitor<T>(Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor<T>(null, e);
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

            harness.SetMachineId(mid);

            Task task = new Task(() =>
            {
                try
                {
                    this.Scheduler.NotifyTaskStarted();

                    harness.Run();

                    this.Scheduler.NotifyTaskCompleted();
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

            this.Scheduler.NotifyNewTaskCreated(task.Id, harness);

            task.Start();

            this.Scheduler.WaitForTaskToStart(task.Id);
            this.Scheduler.Schedule();
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        internal void Wait() => this.Scheduler.Wait();

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId CreateMachine(Type type, string friendlyName, Event e, Machine creator)
        {
            creator?.AssertNoPendingRGP("CreateMachine");

            Machine machine = this.CreateMachine(type, friendlyName);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e == null ? null : new EventInfo(e));
            if (base.Configuration.EnableDataRaceDetection)
            {
                // Traces machine actions, if data-race detection is enabled.
                this.MachineActionTraceMap.Add(machine.Id, new MachineActionTrace(machine.Id));
                if (creator != null && MachineActionTraceMap.Keys.Contains(creator.Id))
                {
                    this.MachineActionTraceMap[creator.Id].AddCreateMachineInfo(machine.Id);
                }
            }

            this.RunMachineEventHandler(machine, e, true, false);
            this.Scheduler.Schedule();

            return machine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override async Task<MachineId> CreateMachineAndExecute(Type type, string friendlyName, Event e, Machine creator)
        {
            creator?.AssertNoPendingRGP("CreateMachine");

            Machine machine = this.CreateMachine(type, friendlyName);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e == null ? null : new EventInfo(e));
            if (base.Configuration.EnableDataRaceDetection)
            {
                // Traces machine actions, if data-race detection is enabled.
                this.MachineActionTraceMap.Add(machine.Id, new MachineActionTrace(machine.Id));
                if (creator != null && MachineActionTraceMap.Keys.Contains(creator.Id))
                {
                    this.MachineActionTraceMap[creator.Id].AddCreateMachineInfo(machine.Id);
                }
            }

            this.RunMachineEventHandler(machine, e, true, true);
            this.Scheduler.Schedule();

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
        /// <param name="e">Event passed during machine construction</param>
        /// <param name="creator">Creator machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId CreateRemoteMachine(Type type, string friendlyName, string endpoint,
            Event e, Machine creator)
        {
            return this.CreateMachine(type, friendlyName, e, creator);
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <returns>Machine</returns>
        private Machine CreateMachine(Type type, string friendlyName)
        {
            this.Assert(type.IsSubclassOf(typeof(Machine)), $"Type '{type.Name}' is not a machine.");

            MachineId mid = new MachineId(type, friendlyName, this);
            var isMachineTypeCached = MachineFactory.IsCached(type);
            Machine machine = MachineFactory.Create(type);

            machine.SetMachineId(mid);
            machine.InitializeStateInformation();

            if (base.Configuration.ReportCodeCoverage && !isMachineTypeCached)
            {
                this.ReportCodeCoverageOfMachine(machine);
            }

            bool result = this.MachineMap.TryAdd(mid.Value, machine);
            this.Assert(result, $"Machine '{mid}' was already created.");

            this.Log($"<CreateLog> Machine '{mid}' is created.");

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void SendEvent(MachineId mid, Event e, AbstractMachine sender, bool isStarter)
        {
            Machine machine = null;
            if (!this.MachineMap.TryGetValue(mid.Value, out machine))
            {
                if (sender != null)
                {
                    this.Log($"<SendLog> Machine '{sender.Id}' sent event '{e.GetType().FullName}' to a halted machine '{mid}'.");
                }
                else
                {
                    this.Log($"<SendLog> The event '{e.GetType().FullName}' was sent to a halted machine '{mid}'.");
                }

                return;
            }

            bool runNewHandler = false;
            this.EnqueueEvent(machine, e, sender, isStarter, ref runNewHandler);
            if (runNewHandler)
            {
                this.RunMachineEventHandler(machine, null, false, false);
            }

            this.Scheduler.Schedule();
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine and
        /// executes the event handler if the machine is available.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override async Task SendEventAndExecute(MachineId mid, Event e, AbstractMachine sender, bool isStarter)
        {
            Machine machine = null;
            if (!this.MachineMap.TryGetValue(mid.Value, out machine))
            {
                if (sender != null)
                {
                    this.Log($"<SendLog> Machine '{sender.Id}' sent event '{e.GetType().FullName}' to a halted machine '{mid}'.");
                }
                else
                {
                    this.Log($"<SendLog> The event '{e.GetType().FullName}' was sent to a halted machine '{mid}'.");
                }

                return;
            }

            bool runNewHandler = false;
            this.EnqueueEvent(machine, e, sender, isStarter, ref runNewHandler);
            if (runNewHandler)
            {
                this.RunMachineEventHandler(machine, null, false, true);
            }

            this.Scheduler.Schedule();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a remote machine, which
        /// is modeled as a local machine during testing.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">If true, the send is starting a new operation</param>
        internal override void SendEventRemotely(MachineId mid, Event e, AbstractMachine sender, bool isStarter)
        {
            this.SendEvent(mid, e, sender, isStarter);
        }

        /// <summary>
        /// Enqueues an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">Is starting a new operation</param>
        /// <param name="runNewHandler">Run a new handler</param>
        private void EnqueueEvent(Machine machine, Event e, AbstractMachine sender, bool isStarter, ref bool runNewHandler)
        {
            if (sender != null)
            {
                sender.AssertNoPendingRGP("Send");
            }

            EventOriginInfo originInfo = null;
            if (sender != null && sender is Machine)
            {
                originInfo = new EventOriginInfo(sender.Id, (sender as Machine).GetType().Name,
                    StateGroup.GetQualifiedStateName((sender as Machine).CurrentState));
            }
            else
            {
                // Message comes from outside P#.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            EventInfo eventInfo = new EventInfo(e, originInfo);
            this.SetOperationIdForEvent(eventInfo, sender, isStarter);

            if (base.Configuration.BoundOperations && sender != null)
            {
                this.Log($"<SendLog> Machine '{sender.Id}' sent event " +
                    $"'{eventInfo.EventName}({eventInfo.OperationId})' to '{machine.Id}'.");
            }
            else if (sender != null)
            {
                this.Log($"<SendLog> Machine '{sender.Id}' sent event " +
                    $"'{eventInfo.EventName}' to '{machine.Id}'.");
            }
            else
            {
                this.Log($"<SendLog> Event '{eventInfo.EventName}' was sent to '{machine.Id}'.");
            }

            if (sender != null)
            {
                this.BugTrace.AddSendEventStep(sender.Id, this.GetStateNameOfMachine(sender), eventInfo, machine.Id);
                if (base.Configuration.EnableDataRaceDetection)
                {
                    // Traces machine actions, if data-race detection is enabled.
                    this.MachineActionTraceMap[sender.Id].AddSendActionInfo(machine.Id, e);
                }
            }

            machine.Enqueue(eventInfo, ref runNewHandler);
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        /// <param name="isFresh">Is a new machine</param>
        /// <param name="executeSynchronously">If true, this operation executes synchronously</param> 
        private void RunMachineEventHandler(Machine machine, Event e, bool isFresh, bool executeSynchronously)
        {
            Task task = new Task(async () =>
            {
                try
                {
                    this.Scheduler.NotifyTaskStarted();

                    machine.IsInsideSynchronousCall = executeSynchronously;

                    if (isFresh)
                    {
                        await machine.GotoStartState(e);
                    }

                    await machine.RunEventHandler(executeSynchronously);
                    machine.IsInsideSynchronousCall = false;

                    if (executeSynchronously)
                    {
                        await machine.RunEventHandler();
                    }

                    this.Scheduler.NotifyTaskCompleted();
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

            this.Scheduler.NotifyNewTaskCreated(task.Id, machine);

            task.Start(this.TaskScheduler);

            this.Scheduler.WaitForTaskToStart(task.Id);
        }

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
            Object monitor = Activator.CreateInstance(type);
            (monitor as Monitor).SetMachineId(mid);
            (monitor as Monitor).InitializeStateInformation();

            this.Log($"<CreateLog> Monitor '{type.Name}' is created.");

            this.ReportCodeCoverageOfMachine(monitor as Monitor);
            this.BugTrace.AddCreateMonitorStep(mid);

            this.Monitors.Add(monitor as Monitor);
            this.LivenessChecker.RegisterMonitor(monitor as Monitor);

            (monitor as Monitor).GotoStartState();
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal override void Monitor<T>(AbstractMachine sender, Event e)
        {
            if (sender != null)
            {
                sender.AssertNoPendingRGP("Monitor");
            }

            foreach (var m in this.Monitors)
            {
                if (m.GetType() == typeof(T))
                {
                    if (base.Configuration.ReportCodeCoverage)
                    {
                        this.ReportCodeCoverageOfMonitorEvent(sender, m, e);
                        this.ReportCodeCoverageOfMonitorTransition(m, e);
                    }

                    m.MonitorEvent(e);
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

        #endregion

        #region nondeterministic choices

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        internal override bool GetNondeterministicBooleanChoice(AbstractMachine machine, int maxValue)
        {
            if (machine != null)
            {
                machine.AssertNoPendingRGP("Random");
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            if (machine != null)
            {
                this.Log($"<RandomLog> Machine '{machine.Id}' nondeterministically chose '{choice}'.");
            }
            else
            {
                this.Log($"<RandomLog> Runtime nondeterministically chose '{choice}'.");
            }
            
            this.BugTrace.AddRandomChoiceStep(machine == null ? null : machine.Id, this.GetStateNameOfMachine(machine), choice);

            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal override bool GetFairNondeterministicBooleanChoice(AbstractMachine machine, string uniqueId)
        {
            if (machine != null)
            {
                machine.AssertNoPendingRGP("FairRandom");
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(2, uniqueId);
            if (machine != null)
            {
                this.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{choice}'.");
            }
            else
            {
                this.Log($"<RandomLog> Runtime nondeterministically chose '{choice}'.");
            }
            
            this.BugTrace.AddRandomChoiceStep(machine == null ? null : machine.Id, this.GetStateNameOfMachine(machine), choice);

            return choice;
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
        internal override int GetNondeterministicIntegerChoice(AbstractMachine machine, int maxValue)
        {
            if (machine != null)
            {
                machine.AssertNoPendingRGP("RandomInteger");
            }

            var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            if (machine != null)
            {
                this.Log($"<RandomLog> Machine '{machine.Id}' " +
                    $"nondeterministically chose '{choice}'.");
            }
            else
            {
                this.Log($"<RandomLog> Runtime nondeterministically chose '{choice}'.");
            }

            this.BugTrace.AddRandomChoiceStep(machine == null ? null : machine.Id, this.GetStateNameOfMachine(machine), choice);

            return choice;
        }

        #endregion

        #region notifications

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal override void NotifyEnteredState(AbstractMachine machine)
        {
            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;
                this.BugTrace.AddGotoStateStep(machine.Id, machineState);

                this.Log($"<StateLog> Machine '{machine.Id}' enters " +
                    $"state '{machineState}'.");

            }
            else if (machine is Monitor)
            {
                string monitorState = (machine as Monitor).CurrentStateNameWithTemperature;
                this.BugTrace.AddGotoStateStep(machine.Id, monitorState);

                string liveness = "";

                if ((machine as Monitor).IsInHotState())
                {
                    liveness = "'hot' ";
                }
                else if ((machine as Monitor).IsInColdState())
                {
                    liveness = "'cold' ";
                }

                this.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' " +
                    $"enters {liveness}state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        internal override void NotifyExitedState(AbstractMachine machine)
        {
            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;

                this.Log($"<StateLog> Machine '{machine.Id}' exits " +
                    $"state '{machineState}'.");
            }
            else if (machine is Monitor)
            {
                string liveness = "";
                string monitorState = (machine as Monitor).CurrentStateName;

                if ((machine as Monitor).IsInHotState())
                {
                    liveness = "'hot' ";
                    monitorState += "[hot]";
                }
                else if ((machine as Monitor).IsInColdState())
                {
                    liveness = "'cold' ";
                    monitorState += "[cold]";
                }

                this.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' " +
                    $"exits {liveness}state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <param name="action">Action</param>
        /// <param name="receivedEvent">Event</param>
        internal override void NotifyInvokedAction(AbstractMachine machine, MethodInfo action, Event receivedEvent)
        {
            if (machine is Machine)
            {
                string machineState = (machine as Machine).CurrentStateName;
                this.BugTrace.AddInvokeActionStep(machine.Id, machineState, action);

                this.Log($"<ActionLog> Machine '{machine.Id}' invoked action " +
                    $"'{action.Name}' in state '{machineState}'.");

                if (base.Configuration.EnableDataRaceDetection)
                {
                    // Traces machine actions, if data-race detection is enabled.
                    this.MachineActionTraceMap[machine.Id].AddInvocationActionInfo(action.Name, receivedEvent);
                }
            }
            else if (machine is Monitor)
            {
                string monitorState = (machine as Monitor).CurrentStateName;
                this.BugTrace.AddInvokeActionStep(machine.Id, monitorState, action);

                this.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' executed " +
                    $"action '{action.Name}' in state '{monitorState}'.");
            }
        }

        /// <summary>
        /// Notifies that a machine dequeued an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyDequeuedEvent(Machine machine, EventInfo eventInfo)
        {
            if (base.Configuration.BoundOperations)
            {
                this.Log($"<DequeueLog> Machine '{machine.Id}' dequeued " +
                    $"event '{eventInfo.EventName}({eventInfo.OperationId})'.");
            }
            else
            {
                this.Log($"<DequeueLog> Machine '{machine.Id}' dequeued " +
                    $"event '{eventInfo.EventName}'.");
            }

            this.BugTrace.AddDequeueEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            var prevMachineOpId = machine.OperationId;
            machine.SetOperationId(eventInfo.OperationId);
            
            if (base.Configuration.ReportCodeCoverage)
            {
                this.ReportCodeCoverageOfReceivedEvent(machine, eventInfo);
                this.ReportCodeCoverageOfStateTransition(machine, eventInfo);
            }

            //if (base.Configuration.BoundOperations && prevMachineOpId != machine.OperationId)
            //{
            //    this.Scheduler.Schedule();
            //}
        }

        /// <summary>
        /// Notifies that a machine invoked pop.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyPop(Machine machine)
        {
            machine.AssertCorrectRGPInvocation();

            this.Log($"<PopLog> Machine '{machine.Id}' invoked pop in state '{machine.CurrentStateName}'.");

            if (base.Configuration.ReportCodeCoverage)
            {
                this.ReportCodeCoverageOfPopTransition(machine, machine.CurrentState, machine.GetStateTypeAtStackIndex(1));
            }
        }

        /// <summary>
        /// Notifies that a machine raised an event.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void NotifyRaisedEvent(AbstractMachine machine, EventInfo eventInfo,
            bool isStarter)
        {
            machine.AssertCorrectRGPInvocation();

            if (machine is Machine)
            {
                this.SetOperationIdForEvent(eventInfo, machine, isStarter);

                string machineState = (machine as Machine).CurrentStateName;
                this.BugTrace.AddRaiseEventStep(machine.Id, machineState, eventInfo);

                if (base.Configuration.BoundOperations)
                {
                    this.Log($"<RaiseLog> Machine '{machine.Id}' raised " +
                        $"event '{eventInfo.EventName}({eventInfo.OperationId})'.");
                }
                else
                {
                    this.Log($"<RaiseLog> Machine '{machine.Id}' raised " +
                        $"event '{eventInfo.EventName}'.");
                }
            }
            else if (machine is Monitor)
            {
                string monitorState = (machine as Monitor).CurrentStateName;
                this.BugTrace.AddRaiseEventStep(machine.Id, monitorState, eventInfo);

                this.Log($"<MonitorLog> Monitor '{machine.GetType().Name}' raised " +
                    $"event '{eventInfo.EventName}'.");

                if (base.Configuration.ReportCodeCoverage)
                {
                    this.ReportCodeCoverageOfMonitorTransition(machine as Monitor, eventInfo.Event);
                }
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
            machine.AssertNoPendingRGP("Receive");
        }

        /// <summary>
        /// Notifies that a machine is handling a raised event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyHandleRaisedEvent(Machine machine, EventInfo eventInfo)
        {
            var prevMachineOpId = machine.OperationId;
            machine.SetOperationId(eventInfo.OperationId);
            
            if (base.Configuration.ReportCodeCoverage)
            {
                this.ReportCodeCoverageOfStateTransition(machine, eventInfo);
            }

            //if (base.Configuration.BoundOperations && prevMachineOpId != machine.OperationId)
            //{
            //    this.Scheduler.Schedule();
            //}
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive one
        /// or more events.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyWaitEvents(Machine machine)
        {
            string events = machine.GetEventWaitHandlerNames();

            this.BugTrace.AddWaitToReceiveStep(machine.Id, machine.CurrentStateName, events);

            this.Log($"<ReceiveLog> Machine '{machine.Id}' is waiting on events:{events}.");

            this.Scheduler.NotifyTaskBlockedOnEvent(Task.CurrentId.Value);
            this.Scheduler.Schedule();
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        internal override void NotifyReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            this.BugTrace.AddReceivedEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            if (base.Configuration.BoundOperations)
            {
                this.Log($"<ReceiveLog> Machine '{machine.Id}' received " +
                    $"event '{eventInfo.EventName}({eventInfo.OperationId})' and unblocked.");
            }
            else
            {
                this.Log($"<ReceiveLog> Machine '{machine.Id}' received " +
                    $"event '{eventInfo.EventName}' and unblocked.");
            }

            this.Scheduler.NotifyTaskReceivedEvent(machine);
            machine.IsWaitingToReceive = false;
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyHalted(Machine machine)
        {
            this.BugTrace.AddHaltStep(machine.Id, null);
            this.Log($"<HaltLog> Machine '{machine.Id}' halted.");
            this.MachineMap.TryRemove(machine.Id.Value, out machine);
        }

        /// <summary>
        /// Notifies that a default handler has been used.
        /// </summary>
        internal override void NotifyDefaultHandlerFired()
        {
            this.Scheduler.Schedule();
        }

        #endregion

        #region code coverage

        /// <summary>
        /// Reports code coverage for the specified received event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        private void ReportCodeCoverageOfReceivedEvent(Machine machine, EventInfo eventInfo)
        {
            string originMachine = eventInfo.OriginInfo.SenderMachineName;
            string originState = eventInfo.OriginInfo.SenderStateName;
            string edgeLabel = eventInfo.EventType.Name;
            string destMachine = machine.GetType().Name;
            string destState = StateGroup.GetQualifiedStateName(machine.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports code coverage for the specified monitor event.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="monitor">Monitor</param>
        /// <param name="e">Event</param>
        private void ReportCodeCoverageOfMonitorEvent(AbstractMachine sender, Monitor monitor, Event e)
        {
            string originMachine = (sender == null) ? "Env" : sender.GetType().Name;
            string originState = (sender == null) ? "Env" :
                (sender is Machine) ? StateGroup.GetQualifiedStateName((sender as Machine).CurrentState) :
                (sender is Monitor) ? StateGroup.GetQualifiedStateName((sender as Monitor).CurrentState) :
                "Env";

            string edgeLabel = e.GetType().Name;
            string destMachine = monitor.GetType().Name;
            string destState = StateGroup.GetQualifiedStateName(monitor.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports code coverage for the specified machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void ReportCodeCoverageOfMachine(AbstractMachine machine)
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
        /// Reports code coverage for the specified state transition.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="eventInfo">EventInfo</param>
        private void ReportCodeCoverageOfStateTransition(Machine machine, EventInfo eventInfo)
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
            else if (machine.GotoTransitions.ContainsKey(eventInfo.EventType))
            {
                edgeLabel = eventInfo.EventType.Name;
                destState = StateGroup.GetQualifiedStateName(
                    machine.GotoTransitions[eventInfo.EventType].TargetState);
            }
            else if(machine.PushTransitions.ContainsKey(eventInfo.EventType))
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
        /// Reports code coverage for a pop transition.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="fromState">Top of the stack state</param>
        /// <param name="toState">Next to top state of the stack</param>
        private void ReportCodeCoverageOfPopTransition(Machine machine, Type fromState, Type toState)
        {
            string originMachine = machine.GetType().Name;
            string originState = StateGroup.GetQualifiedStateName(fromState);
            string destMachine = machine.GetType().Name;            
            string edgeLabel = "pop";
            string destState = StateGroup.GetQualifiedStateName(toState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }
        
        /// <summary>
        /// Reports code coverage for the specified state transition.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        /// <param name="e">Event</param>
        private void ReportCodeCoverageOfMonitorTransition(Monitor monitor, Event e)
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
        /// Returns the fingerprint of the current program state.
        /// </summary>
        /// <returns>Fingerprint</returns>
        internal Fingerprint GetProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                int hash = 19;

                foreach (var machine in this.MachineMap.Values)
                {
                    hash = hash + 31 * machine.GetCachedState();
                }

                foreach (var monitor in this.Monitors)
                {
                    hash = hash + 31 * monitor.GetCachedState();
                }

                fingerprint = new Fingerprint(hash);
            }

            return fingerprint;
        }

        /// <summary>
        /// Returns the state name of the specified machine,
        /// if the machine is in such a state.
        /// </summary>
        /// <param name="machine">AbstractMachine</param>
        /// <returns>StateName</returns>
        private string GetStateNameOfMachine(AbstractMachine machine)
        {
            string machineState = null;
            if (machine is Machine)
            {
                machineState = (machine as Machine).CurrentStateName;
            }
            else if (machine is Monitor)
            {
                machineState = (machine as Monitor).CurrentStateName;
            }

            return machineState;
        }

        /// <summary>
        /// Sets the operation id for the given event.
        /// </summary>
        /// <param name="eventInfo">EventInfo</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">Is starting a new operation</param>
        private void SetOperationIdForEvent(EventInfo eventInfo, AbstractMachine sender, bool isStarter)
        {
            if (isStarter)
            {
                this.OperationIdCounter++;
                eventInfo.SetOperationId(this.OperationIdCounter);
            }
            else if (sender != null)
            {
                eventInfo.SetOperationId(sender.OperationId);
            }
            else
            {
                eventInfo.SetOperationId(0);
            }
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
            this.MachineActionTraceMap.Clear();

            this.LivenessChecker = null;
            this.StateCache = null;
            this.ScheduleTrace = null;
            this.BugTrace = null;

            base.Dispose();
        }

        #endregion
    }
}
